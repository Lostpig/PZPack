namespace PZPack.Core.Crypto;

public interface IPZCrypto : IDisposable
{
    public byte[] Hash { get; }
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
