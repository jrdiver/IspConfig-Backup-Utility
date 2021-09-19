﻿using System.Security.Cryptography;
using Microsoft.Win32;
using Newtonsoft.Json;
using StringEncrypter;

namespace RegistryConnector
{
    public class RegistryUser
    {
        public string KeyLocation = @"cSharpRegistryConnector";

        public bool AddUserValue(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(KeyLocation))
            {
                return false;
            }

            EncryptedString encryptedValue = EncodeDecode.Encrypt(value, DataProtectionScope.CurrentUser);

            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\" + KeyLocation);

            key.SetValue(name, JsonConvert.SerializeObject(encryptedValue));
            key.Close();
            return true;
        }

        public string GetUserValue(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(KeyLocation))
            {
                return string.Empty;
            }

            RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\" + KeyLocation);
            if (key == null)
            {
                return string.Empty;
            }

            string? value = key.GetValue(name)?.ToString();
            key.Close();

            if (!string.IsNullOrWhiteSpace(value))
            {
                EncryptedString encryptedValue = JsonConvert.DeserializeObject<EncryptedString>(value);
                if (encryptedValue != null) return EncodeDecode.Decrypt(encryptedValue);
            }
            return string.Empty;
        }

        public bool AddSystemValue(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(KeyLocation))
            {
                return false;
            }

            EncryptedString encryptedValue = EncodeDecode.Encrypt(value, DataProtectionScope.LocalMachine);

            RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\" + KeyLocation);

            key.SetValue(name, JsonConvert.SerializeObject(encryptedValue));
            key.Close();
            return true;
        }

        public string GetSystemValue(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(KeyLocation))
            {
                return string.Empty;
            }

            RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\" + KeyLocation);
            if (key == null)
            {
                return string.Empty;
            }

            string? value = key.GetValue(name)?.ToString();
            key.Close();

            if (!string.IsNullOrWhiteSpace(value))
            {
                EncryptedString encryptedValue = JsonConvert.DeserializeObject<EncryptedString>(value);
                if (encryptedValue != null) return EncodeDecode.Decrypt(encryptedValue);
            }
            return string.Empty;
        }
    }
}