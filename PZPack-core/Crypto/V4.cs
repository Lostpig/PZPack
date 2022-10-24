using PZPack.Core.Utility;

namespace PZPack.Core.Crypto;

/**
 * V4 版本
 */
internal class PZCryptoV4 : IPZCrypto
{
    private readonly PZCryptoBase _base;
    public byte[] Hash { get { return _base.Hash; } }

    public PZCryptoV4(string password) : this(PZCrypto.CreateKey(password)) { }
    public PZCryptoV4(byte[] key)
    {
        _base = new PZCryptoBase(key);
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
        byte[] IV = new byte[16];
        source.Seek(offset, SeekOrigin.Begin);
        source.Read(IV, 0, IV.Length);

        ProgressStream partStream = new(source, offset + 16, size - 16);
        return _base.DecryptStream(partStream, destination, IV);
    }

    public Task<long> EncryptStreamAsync(Stream source, Stream destination, IProgress<(long, long)>? progress = default, CancellationToken? cancelToken = null)
    {
        throw new Exceptions.OldVersionEncryptException();
    }
    public async Task<long> DecryptStreamAsync(Stream source, Stream destination, long offset, long size, IProgress<(long, long)>? progress = default, CancellationToken? cancelToken = null)
    {
        byte[] IV = new byte[16];
        source.Seek(offset, SeekOrigin.Begin);
        await source.ReadAsync(IV);

        ProgressStream progressStream = new(source, offset + 16, size - 16, progress);
        return await _base.DecryptStreamAsync(progressStream, destination, IV, cancelToken ?? CancellationToken.None);
    }

    public void Dispose()
    {
        _base.Dispose();
    }
}


