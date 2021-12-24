using PZPack.Core.Compatible;

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
                _ => new PZCrypto(password),
            };
        }
    }
}
