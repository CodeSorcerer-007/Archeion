# ARCHEION — Companion Communication Protocol

**Protocol Version:** `1.0.0`

## 1. Framing & Wire Format
- Transports: TCP over ADB Reverse/Forward (`tcp:5037` or port `42842`), or TLS over local Wi-Fi.
- Message Framing:
  `[4 bytes Big-Endian Message Length] [JSON UTF-8 Payload] [Optional Binary Chunk Payload]`

## 2. Message Envelope
```json
{
  "protocolVersion": "1.0.0",
  "sessionId": "b47c0b93-9c8e-4a62-8bf9-231a473e6f9a",
  "messageId": "9d8e7f6a-5b4c-3d2e-1f0a-9b8c7d6e5f4a",
  "correlationId": null,
  "messageType": "HELLO",
  "timestampUtc": "2026-11-23T18:30:00Z",
  "payload": {}
}
```

## 3. Message Sequence
1. Handshake: `HELLO` -> `PROTOCOL_NEGOTIATION`
2. Pairing: `PAIRING_INIT` -> `PAIRING_CONFIRM` -> `PAIRING_COMPLETE`
3. Capability Probe: `CAPABILITY_REQUEST` -> `CAPABILITY_REPORT`
4. Export: `EXPORT_REQUEST` -> `EXPORT_STARTED` -> `DATA_CHUNK` (with `CHUNK_ACK`) -> `EXPORT_COMPLETE`
5. Verification & Close: `VERIFICATION_RESULT` -> `SESSION_CLOSE`
