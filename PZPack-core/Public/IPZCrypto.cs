using PZPack.Core.Compatible;
using System.Security.Cryptography;

namespace PZPack.Core
{
    public interface IPZCrypto : IDisposable
    {
        public byte[] GetPwCheckHash();
        public byte[] Encrypt(byte[] bytes);
        public byte[] Decrypt(byte[] bytes);
        public long EncryptStream(Stream source, Stream target);
        public long DecryptStream(Stream source, Stream target, long offset, long size);
        public Task<long> EncryptStreamAsync(
            Stream source,
            Stream target,
            IProgress<(long, long)>? progress = default,
            CancellationToken? cancelToken = null);
        public Task<long> DecryptStreamAsync(
            Stream source,
            Stream target,
            long offset,
            long size,
            IProgress<(long, long)>? progress = default,
            CancellationToken? cancelToken = null);
    }

    public class PZCryptoCreater
    {
        static public IPZCrypto CreateCrypto(string password, int version)
        {
            return version switch
            {
                <= 2 => new PZCryptoBeforeV2(password),
                _ => new PZCryptoCurrent(password),
            };
        }
        static public IPZCrypto CreateCrypto(byte[] key, int version)
        {
            return version switch
            {
                <= 2 => new PZCryptoBeforeV2(key),
                _ => new PZCryptoCurrent(key),
            };
        }
        static public byte[] CreateKey(string password)
        {
            byte[] key = new byte[32];
            byte[] pwHash = PZHash.Hash(password);
            Array.Copy(pwHash, 0, key, 0, key.Length);
            return key;
        }
        static public byte[] CreatePwCheckKey(byte[] key)
        {
            string hex = PZHash.HashHex(key);
            return PZHash.Hash(hex);
        }
        static internal Aes NewCryptor()
        {
            Aes cryptor = Aes.Create();
            cryptor.Mode = CipherMode.CBC;
            cryptor.Padding = PaddingMode.PKCS7;
            cryptor.KeySize = 256;
            cryptor.GenerateIV();

            return cryptor;
        }
    }
}
