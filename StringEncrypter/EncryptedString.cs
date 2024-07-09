using System.Security.Cryptography;

namespace StringEncrypter
{
    public class EncryptedString
    {
        public byte[]? Entropy { get; set; }
        public byte[]? Encrypted { get; set; }
        public DataProtectionScope Scope { get; set; } = DataProtectionScope.LocalMachine;
    }
}
