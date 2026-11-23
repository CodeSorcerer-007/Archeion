namespace Archeion.Contracts;

public enum BackupCategory
{
    DeviceIdentity = 0,
    Photos = 1,
    Videos = 2,
    Audio = 3,
    Documents = 4,
    Downloads = 5,
    UserFolders = 6,
    Contacts = 7,
    Calendar = 8,
    AppInventory = 9,
    Apks = 10,
    AppPrivateData = 11,
    Sms = 12,
    CallLogs = 13,
    SystemSettings = 14
}

public enum CapabilityStatus
{
    Available = 0,
    AvailableWithPermission = 1,
    AvailableWithUserAction = 2,
    Limited = 3,
    Unsupported = 4,
    BlockedByOS = 5,
    BlockedByOEM = 6,
    NotInstalled = 7,
    NotConnected = 8,
    NotTested = 9,
    PermissionDenied = 10,
    PermissionRevoked = 11,
    PartialAccess = 12,
    Error = 13
}

public enum TransportType
{
    Adb = 0,
    Mtp = 1,
    CompanionUsb = 2,
    LocalNetwork = 3,
    Offline = 4
}

public enum DeviceState
{
    Discovering = 0,
    Connecting = 1,
    Identifying = 2,
    Identified = 3,
    PartialIdentity = 4,
    UnknownDevice = 5,
    IdentityConflict = 6,
    Connected = 7,
    Disconnected = 8,
    Unauthorized = 9,
    Error = 10
}

public enum SnapshotState
{
    InProgress = 0,
    Completed = 1,
    Partial = 2,
    Failed = 3
}

public enum VerificationStatus
{
    Unverified = 0,
    Verified = 1,
    Corrupted = 2,
    Missing = 3,
    HashMismatch = 4
}

public enum EncryptionMode
{
    None = 0,
    Aes256Gcm = 1
}

public enum ChangeType
{
    Unchanged = 0,
    Added = 1,
    RemovedFromSource = 2,
    Modified = 3,
    Renamed = 4,
    Unknown = 5
}

public enum RestoreConflictAction
{
    Skip = 0,
    Merge = 1,
    Rename = 2,
    Replace = 3
}
