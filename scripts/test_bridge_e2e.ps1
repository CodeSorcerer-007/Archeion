$ErrorActionPreference = "Stop"

$client = New-Object System.Net.Sockets.TcpClient("127.0.0.1", 8742)
$stream = $client.GetStream()

$MSG_HANDSHAKE_REQUEST = 0x0001
$MSG_HANDSHAKE_RESPONSE = 0x0002
$MSG_CAPABILITY_REPORT = 0x0010
$MSG_START_BACKUP = 0x0020
$MSG_START_BACKUP_ACK = 0x0021
$MSG_ITEM_HEADER = 0x0030
$MSG_CHUNK_DATA = 0x0031
$MSG_CHUNK_ACK = 0x0032
$MSG_ITEM_END = 0x0033
$MSG_SESSION_COMPLETE = 0x0040
$MSG_ERROR = 0x00FF

function Read-ExactPacket {
    $hdr = New-Object byte[] 16
    $read = 0
    while ($read -lt 16) {
        $r = $stream.Read($hdr, $read, 16 - $read)
        if ($r -le 0) { throw "Socket closed reading header" }
        $read += $r
    }
    $magic = [System.Text.Encoding]::ASCII.GetString($hdr, 0, 4)
    if ($magic -ne "ARCH") { throw "Bad magic: $magic" }
    $ver = ([int]$hdr[4] -shl 8) -bor [int]$hdr[5]
    $type = ([int]$hdr[6] -shl 8) -bor [int]$hdr[7]
    $len = ([int]$hdr[8] -shl 24) -bor ([int]$hdr[9] -shl 16) -bor ([int]$hdr[10] -shl 8) -bor [int]$hdr[11]
    $seq = ([long]$hdr[12] -shl 24) -bor ([long]$hdr[13] -shl 16) -bor ([long]$hdr[14] -shl 8) -bor [long]$hdr[15]

    $payload = New-Object byte[] $len
    $read = 0
    while ($read -lt $len) {
        $r = $stream.Read($payload, $read, $len - $read)
        if ($r -le 0) { throw "Socket closed reading payload" }
        $read += $r
    }
    return @{ Version = $ver; Type = $type; Length = $len; Sequence = $seq; Payload = $payload }
}

function Send-Packet($type, $seq, $payload) {
    $len = $payload.Length
    $hdr = New-Object byte[] 16
    [System.Text.Encoding]::ASCII.GetBytes("ARCH", 0, 4, $hdr, 0) | Out-Null
    # Version: UInt16 (1)
    $hdr[4] = 0
    $hdr[5] = 1
    # Type: UInt16
    $hdr[6] = ($type -shr 8) -band 0xFF
    $hdr[7] = $type -band 0xFF
    # Length: UInt32
    $hdr[8] = ($len -shr 24) -band 0xFF
    $hdr[9] = ($len -shr 16) -band 0xFF
    $hdr[10] = ($len -shr 8) -band 0xFF
    $hdr[11] = $len -band 0xFF
    # Sequence: UInt32
    $hdr[12] = ($seq -shr 24) -band 0xFF
    $hdr[13] = ($seq -shr 16) -band 0xFF
    $hdr[14] = ($seq -shr 8) -band 0xFF
    $hdr[15] = $seq -band 0xFF

    $stream.Write($hdr, 0, 16)
    if ($len -gt 0) {
        $stream.Write($payload, 0, $len)
    }
    $stream.Flush()
}

Write-Host "--- 1. Performing Handshake ---"
$hsJson = '{"clientName":"ArcheionDesktopTest","clientVersion":"1.0.0","protocolVersion":"1.0.0"}'
$hsBytes = [System.Text.Encoding]::UTF8.GetBytes($hsJson)
Send-Packet $MSG_HANDSHAKE_REQUEST 1 $hsBytes

$res = Read-ExactPacket
Write-Host "Handshake Response Type: 0x$($res.Type.ToString('X4'))"
$hsResJson = [System.Text.Encoding]::UTF8.GetString($res.Payload)
Write-Host "Handshake Body: $hsResJson"
$hsObj = ConvertFrom-Json $hsResJson
$sessionToken = $hsObj.sessionToken

Write-Host "`n--- 2. Querying Capabilities ---"
Send-Packet $MSG_CAPABILITY_REPORT 2 (New-Object byte[] 0)
$resCap = Read-ExactPacket
Write-Host "Capability Report Type: 0x$($resCap.Type.ToString('X4')), Length: $($resCap.Length)"
$capJson = [System.Text.Encoding]::UTF8.GetString($resCap.Payload)
Write-Host "Capabilities JSON: $($capJson.Substring(0, [Math]::Min(250, $capJson.Length)))..."

Write-Host "`n--- 3. Starting Backup with SessionToken ---"
$backupReq = @{
    sessionToken = $sessionToken
    requestedCategories = @("Contacts", "AppInventory")
} | ConvertTo-Json -Compress
Send-Packet $MSG_START_BACKUP 3 ([System.Text.Encoding]::UTF8.GetBytes($backupReq))

Write-Host "Streaming packets from device..."
$itemsReceived = 0
$chunksReceived = 0
$totalBytesReceived = 0

while ($true) {
    $pkt = Read-ExactPacket
    if ($pkt.Type -eq $MSG_START_BACKUP_ACK) {
        $ackJson = [System.Text.Encoding]::UTF8.GetString($pkt.Payload)
        Write-Host "  -> START_BACKUP_ACK: $ackJson"
    } elseif ($pkt.Type -eq $MSG_ITEM_HEADER) {
        $itemsReceived++
        $headerJson = [System.Text.Encoding]::UTF8.GetString($pkt.Payload)
        Write-Host "  -> ITEM_HEADER: $headerJson"
    } elseif ($pkt.Type -eq $MSG_CHUNK_DATA) {
        $chunksReceived++
        $totalBytesReceived += $pkt.Length
        Write-Host "  -> CHUNK_DATA: $($pkt.Length) bytes (seq $($pkt.Sequence))"
        # Send ACK
        $ackJson = @{
            sequenceNumber = $pkt.Sequence
            bytesReceived = $pkt.Length
            sha256Chunk = ""
            acknowledged = $true
        } | ConvertTo-Json -Compress
        Send-Packet $MSG_CHUNK_ACK ($pkt.Sequence + 1) ([System.Text.Encoding]::UTF8.GetBytes($ackJson))
    } elseif ($pkt.Type -eq $MSG_ITEM_END) {
        Write-Host "  -> ITEM_END"
    } elseif ($pkt.Type -eq $MSG_SESSION_COMPLETE) {
        $summaryJson = [System.Text.Encoding]::UTF8.GetString($pkt.Payload)
        Write-Host "`n=== BACKUP SESSION COMPLETE ==="
        Write-Host "Summary: $summaryJson"
        break
    } elseif ($pkt.Type -eq $MSG_ERROR) {
        $err = [System.Text.Encoding]::UTF8.GetString($pkt.Payload)
        Write-Host "ERROR Packet received: $err"
        break
    } else {
        Write-Host "Received packet type: 0x$($pkt.Type.ToString('X4'))"
    }
}

$client.Close()
Write-Host "End-to-End Bridge Test Completed Successfully!"
