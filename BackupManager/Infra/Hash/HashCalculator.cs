using System;
using System.IO;
using System.Security.Cryptography;

namespace BackupManager.Infra.Hash
{
    public static class HashCalculator
    {
        public static string CalculateHash(string filePath)
        {
            using MD5 md5 = MD5.Create();
            using Stream stream = File.OpenRead(filePath);
            byte[] hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }
    }
}