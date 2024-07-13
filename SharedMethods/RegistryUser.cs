using System;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Win32;
using Shared.Objects;

namespace Shared;

public class RegistryUser
{
    public string KeyLocation = @"cSharpRegistryConnector";

    public bool AddUserValue(string name, string value)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(KeyLocation))
                return false;

            EncryptedString encryptedValue = EncodeDecode.Encrypt(value, DataProtectionScope.CurrentUser);

            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\" + KeyLocation);

            key.SetValue(name, JsonSerializer.Serialize(encryptedValue));
            key.Close();
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return false;
    }

    public bool AddUnencryptedUserValue(string name, string value)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(KeyLocation))
                return false;


            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\" + KeyLocation);

            key.SetValue(name, value);
            key.Close();
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return false;
    }

    public string GetUserValue(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(KeyLocation))
            return string.Empty;
        try
        {
            RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\" + KeyLocation);
            if (key == null)
                return string.Empty;

            string? value = key.GetValue(name)?.ToString();
            key.Close();

            if (!string.IsNullOrWhiteSpace(value))
            {
                EncryptedString? encryptedValue = JsonSerializer.Deserialize<EncryptedString>(value);
                if (encryptedValue != null)
                    return EncodeDecode.Decrypt(encryptedValue);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return string.Empty;
    }

    public string GetUnencryptedUserValue(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(KeyLocation))
            return string.Empty;

        try
        {
            RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\" + KeyLocation);
            if (key == null)
                return string.Empty;

            string? value = key.GetValue(name)?.ToString();
            key.Close();
            return value ?? string.Empty;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return string.Empty;
    }

    public bool AddSystemValue(string name, string value)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(KeyLocation))
                return false;

            EncryptedString encryptedValue = EncodeDecode.Encrypt(value, DataProtectionScope.LocalMachine);

            RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\" + KeyLocation);

            key.SetValue(name, JsonSerializer.Serialize(encryptedValue));
            key.Close();
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return false;
    }

    public string GetSystemValue(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(KeyLocation))
            return string.Empty;

        try
        {
            RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\" + KeyLocation);
            if (key == null)
                return string.Empty;

            string? value = key.GetValue(name)?.ToString();
            key.Close();

            if (!string.IsNullOrWhiteSpace(value))
            {
                EncryptedString? encryptedValue = JsonSerializer.Deserialize<EncryptedString>(value);
                if (encryptedValue != null)
                    return EncodeDecode.Decrypt(encryptedValue);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return string.Empty;
    }
}