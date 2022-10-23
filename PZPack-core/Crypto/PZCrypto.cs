namespace PZPack.Core.Crypto;

public class PZCrypto
{
    public static IPZCrypto CreateCrypto(string password, int version, int blockSize)
    {
        return version switch
        {
            1 or 2 => new PZCryptoV2(password),
            4 => new PZCryptoV4(password),
            11 => new PZCryptoV11(password, blockSize),
            _ => throw new Exceptions.FileVersionNotCompatiblityException(version)
        };
    }
}
