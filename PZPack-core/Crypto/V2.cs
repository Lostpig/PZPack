using PZPack.Core.Utility;

namespace PZPack.Core.Crypto;

/**
 * V1 及 V2 版本
 */
internal class PZCryptoV2 : IPZCrypto
{
    private readonly PZCryptoBase _base;
    private readonly byte[] IV;
    public byte[] Hash { get { return _base.Hash; } }

    public PZCryptoV2(string password) : this(PZCryptoBase.CreateKey(password)) { }
    public PZCryptoV2(byte[] key)
    {
        byte[] iv = new byte[16];
        byte[] ivHash = PZHash.Sha256(key);
        Array.Copy(ivHash, 0, iv, 0, iv.Length);
        IV = iv;

        _base = new(key);
    }

    public byte[] Encrypt(byte[] bytes)
    {
        throw new Exceptions.OldVersionEncryptException();
    }
    public byte[] Decrypt(byte[] bytes)
    {
        using MemoryStream targetStream = new(), sourceStream = new(bytes);
        DecryptStream(sourceStream, targetStream, 0, bytes.Length);
        return targetStream.ToArray();
    }

    public long EncryptStream(Stream source, Stream destination)
    {
        throw new Exceptions.OldVersionEncryptException();
    }
    public long DecryptStream(Stream source, Stream destination, long offset, long size)
    {
        ProgressStream partStream = new(source, offset, size);
        return _base.DecryptStream(partStream, destination, IV);
    }

    public Task<long> EncryptStreamAsync(Stream source, Stream destination, IProgress<(long, long)>? progress = default, CancellationToken? cancelToken = null)
    {
        throw new Exceptions.OldVersionEncryptException();
    }
    public async Task<long> DecryptStreamAsync(Stream source, Stream destination, long offset, long size, IProgress<(long, long)>? progress = default, CancellationToken? cancelToken = null)
    {
        ProgressStream progressStream = new(source, offset, size, progress);
        return await _base.DecryptStreamAsync(progressStream, destination, IV, cancelToken ?? CancellationToken.None);
    }

    public void Dispose()
    {
        _base.Dispose();
    }
}
