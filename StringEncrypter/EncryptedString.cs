using System.Security.Cryptography;

namespace StringEncrypter
{
    public class EncryptedString
    {
        public byte[]? Entropy;
        public byte[]? Encrypted;
        public DataProtectionScope Scope = DataProtectionScope.LocalMachine;
    }
}
