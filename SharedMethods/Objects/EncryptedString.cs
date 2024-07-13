using System.Security.Cryptography;

namespace Shared.Objects;

public class EncryptedString
{
    public byte[]? Entropy { get; set; }
    public byte[]? Encrypted { get; set; }
    public DataProtectionScope Scope { get; set; } = DataProtectionScope.LocalMachine;
}