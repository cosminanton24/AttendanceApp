using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace AttendanceApp.Application.Common.Hash;

public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;

    private const int Iterations = 3;
    private const int MemorySize = 128 * 1024;
    private const int DegreeOfParallelism = 2;

    public static async Task<string> HashPasswordAsync(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            MemorySize = MemorySize,
            Iterations = Iterations
        };

        var hash = await argon2.GetBytesAsync(HashSize);

        var result = new byte[SaltSize + HashSize];
        Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
        Buffer.BlockCopy(hash, 0, result, SaltSize, HashSize);

        return Convert.ToBase64String(result);
    }

    public static async Task<bool> VerifyPasswordAsync(string password, string storedBase64Hash)
    {
        byte[] storedBytes;
        try
        {
            storedBytes = Convert.FromBase64String(storedBase64Hash);
        }
        catch
        {
            return false;
        }

        if (storedBytes.Length != SaltSize + HashSize)
        {
            return false;
        }

        var salt = new byte[SaltSize];
        var storedHash = new byte[HashSize];
        Buffer.BlockCopy(storedBytes, 0, salt, 0, SaltSize);
        Buffer.BlockCopy(storedBytes, SaltSize, storedHash, 0, HashSize);

        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            MemorySize = MemorySize,
            Iterations = Iterations
        };

        var computedHash = await argon2.GetBytesAsync(HashSize);
        return ConstantTimeEquals(storedHash, computedHash);
    }

    private static bool ConstantTimeEquals(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        int diff = 0;
        for (int i = 0; i < a.Length; i++)
            diff |= a[i] ^ b[i];
        return diff == 0;
    }
}
