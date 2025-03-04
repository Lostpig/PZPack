using PZPack.Core.Utility;
using System.Security.Cryptography;

namespace PZPack.Core.Crypto;

public class PZCrypto
{
    public static Aes CreateAes()
    {
        Aes cryptor = Aes.Create();
        cryptor.Mode = CipherMode.CBC;
        cryptor.Padding = PaddingMode.PKCS7;
        cryptor.KeySize = 256;
        cryptor.GenerateIV();

        return cryptor;
    }
    public static byte[] CreateKey(string password)
    {
        return PZHash.Sha256(password);
    }
    public static byte[] CreateKeyHash(byte[] key)
    {
        string hex = PZHash.Sha256Hex(key);
        return PZHash.Sha256(hex);
    }

    public static IPZCrypto CreateCrypto(byte[] key, int version, int blockSize)
    {
        return version switch
        {
            1 or 2 => new PZCryptoV2(key),
            4 => new PZCryptoV4(key),
            11 or 12 => new PZCryptoV11(key, blockSize),
            _ => throw new Exceptions.FileVersionNotCompatiblityException(version)
        };
    }
    public static IPZCrypto CreateCrypto(string password, int version, int blockSize)
    {
        return version switch
        {
            1 or 2 => new PZCryptoV2(password),
            4 => new PZCryptoV4(password),
            11 or 12 => new PZCryptoV11(password, blockSize),
            _ => throw new Exceptions.FileVersionNotCompatiblityException(version)
        };
    }
}
