using System;
using System.IO;
using System.Security.Cryptography;

namespace BackupManager.Infra.Hash
{
    public static class HashCalculator
    {
        public static string CalculateHash(string filePath)
        {
            using SHA256 sha256HashAlgorithm = SHA256.Create();
            using Stream stream = File.OpenRead(filePath);
            byte[] hashBytes = sha256HashAlgorithm.ComputeHash(stream);  
            return BitConverter.ToString(hashBytes).Replace("-", string.Empty);  
        }
    }
}