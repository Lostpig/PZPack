using PZPack.Core.Index;
using PZPack.Core.Utility;

namespace PZPack.Core.Crypto;

internal class PZCryptoV11 : IPZCrypto
{
    private readonly PZCryptoBase _base;
    private readonly int _blockSize;
    public byte[] Hash { get { return _base.Hash; } }

    public PZCryptoV11(string password, int blockSize) : this(PZCrypto.CreateKey(password), blockSize) { }
    public PZCryptoV11(byte[] key, int blockSize)
    {
        this._base = new PZCryptoBase(key);
        this._blockSize = blockSize;
    }

    public byte[] Encrypt(byte[] bytes)
    {
        using MemoryStream targetStream = new(), sourceStream = new(bytes);
        _base.EncryptStream(sourceStream, targetStream);
        return targetStream.ToArray();
    }
    public byte[] Decrypt(byte[] bytes)
    {
        using MemoryStream targetStream = new(), sourceStream = new(bytes);
        byte[] IV = new byte[16];
        sourceStream.Seek(0, SeekOrigin.Begin);
        sourceStream.Read(IV, 0, IV.Length);

        using ProgressStream partStream = new(sourceStream, 16, sourceStream.Length - 16);
        _base.DecryptStream(partStream, targetStream, IV);
        return targetStream.ToArray();
    }

    public long EncryptStream(Stream source, Stream destination)
    {
        StreamBlockWrapper wrapper = new(source, 0, source.Length, _blockSize);
        BlockReader reader = new(wrapper);
        return _base.EncryptStreamBlock(reader, destination);
    }
    public long DecryptStream(Stream source, Stream destination, long offset, long size)
    {
        int encryptedBlockSize = 16 - (_blockSize % 16) + _blockSize + 16;
        StreamBlockWrapper wrapper = new(source, offset, size, encryptedBlockSize);
        BlockReader reader = new(wrapper);
        return _base.DecryptStreamBlock(reader, destination);
    }

    public async Task<long> EncryptStreamAsync(Stream source, Stream destination, IProgress<(long, long)>? progress = default, CancellationToken? cancelToken = null)
    {
        StreamBlockWrapper wrapper = new(source, 0, source.Length, _blockSize);
        BlockReader reader = new(wrapper, progress);
        return await _base.EncryptStreamBlockAsync(reader, destination, cancelToken??CancellationToken.None);
    }
    public async Task<long> DecryptStreamAsync(Stream source, Stream destination, long offset, long size, IProgress<(long, long)>? progress = default, CancellationToken? cancelToken = null)
    {
        int encryptedBlockSize = 16 - (_blockSize % 16) + _blockSize + 16;
        StreamBlockWrapper wrapper = new(source, offset, size, encryptedBlockSize);
        BlockReader reader = new(wrapper, progress);

        return await _base.DecryptStreamBlockAsync(reader, destination, cancelToken ?? CancellationToken.None);
    }

    public PZFileStream CreatePZFileStream(FileStream source, PZFile file)
    {
        return new PZFileStream(file, source, _base, _blockSize);
    }

    public void Dispose()
    {
        _base.Dispose();
    }
}
