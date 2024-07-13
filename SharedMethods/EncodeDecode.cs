using System.Security.Cryptography;
using System.Text;
using Integrative.Encryption;
using Shared.Objects;

namespace Shared;

public static class EncodeDecode
{
    /// <summary> Takes a string input and Encrypts it and returns object with the entropy values </summary>
    public static EncryptedString Encrypt(string stringInput, DataProtectionScope scope = DataProtectionScope.LocalMachine)
    {
        if (string.IsNullOrWhiteSpace(stringInput))
            return new EncryptedString();
        EncryptedString input = new() { Scope = scope, Entropy = CreateRandomEntropy() };

        input.Encrypted = CrossProtect.Protect(Encoding.ASCII.GetBytes(stringInput), input.Entropy, input.Scope);

        return input;
    }

    /// <summary> Takes the Encrypted object and returns a string </summary>
    public static string Decrypt(EncryptedString input)
    {
        if (input.Encrypted != null && input.Entropy != null && input.Encrypted.Length > 0 && input.Entropy.Length > 0)
            return Encoding.ASCII.GetString(CrossProtect.Unprotect(input.Encrypted, input.Entropy, input.Scope));
        return string.Empty;
    }

    internal static byte[] CreateRandomEntropy()
    {
        byte[] entropy = new byte[16];
        RandomNumberGenerator.Create().GetBytes(entropy);
        return entropy;
    }
}