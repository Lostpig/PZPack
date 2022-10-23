using System.Security.Cryptography;
using System.Text;

namespace PZPack.Core.Utility;

internal class PZHash
{
    public static byte[] Sha256(string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        return Sha256(bytes);
    }
    public static byte[] Sha256(byte[] source)
    {
        SHA256 sHA256 = SHA256.Create();
        return sHA256.ComputeHash(source);
    }
    public static string Sha256Hex(string text)
    {
        byte[] hashBytes = Sha256(text);
        return Convert.ToHexString(hashBytes);
    }
    public static string Sha256Hex(byte[] source)
    {
        byte[] hashBytes = Sha256(source);
        return Convert.ToHexString(hashBytes);
    }
}
