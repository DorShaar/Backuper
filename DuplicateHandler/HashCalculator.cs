using System.Security.Cryptography;

namespace DuplicatesHandler;

internal static class HashCalculator
{
	public static string CalculateHash(string filePath)
	{
		using SHA256 sha256HashAlgorithm = SHA256.Create();
		using Stream stream = File.OpenRead(filePath);
		byte[] hashBytes = sha256HashAlgorithm.ComputeHash(stream);  
		return BitConverter.ToString(hashBytes).Replace("-", string.Empty);  
	}
}