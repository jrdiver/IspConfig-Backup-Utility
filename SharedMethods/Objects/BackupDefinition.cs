using System.Security.Cryptography;

namespace Shared.Objects;

public class BackupDefinition
{
    public string Host { get; set; } = string.Empty;

    public int FtpPort { get; set; }

    public string FtpUsername { get; set; } = string.Empty;

    public string FtpPassword = string.Empty;

    public EncryptedString FtpPasswordEncoded
    {
        get => EncodeDecode.Encrypt(FtpPassword, DataProtectionScope.CurrentUser);
        set => FtpPassword = EncodeDecode.Decrypt(value);
    }

    public string ServerFolder { get; set; } = string.Empty;

    public string LocalFolder { get; set; } = string.Empty;

    public int SqlPort { get; set; }

    public string SqlUsername { get; set; } = string.Empty;

    public string LoginMode { get; set; } = "Password";

    public string SqlPassword = string.Empty;

    public EncryptedString SqlPasswordEncoded
    {
        get => EncodeDecode.Encrypt(SqlPassword, DataProtectionScope.CurrentUser);
        set => SqlPassword = EncodeDecode.Decrypt(value);
    }
}
