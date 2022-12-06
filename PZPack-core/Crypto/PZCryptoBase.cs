using System.Security.Cryptography;

namespace PZPack.Core.Crypto;

internal class PZCryptoBase : IDisposable
{
    internal readonly byte[] Key;
    internal readonly byte[] Hash;
    internal PZCryptoBase(byte[] key)
    {
        Key = key;
        Hash = PZCrypto.CreateKeyHash(Key);
    }

    public long EncryptStreamBlock(BlockReader source, Stream destination)
    {
        byte[] buffer = new byte[source.BlockSize];
        long byteEncrypt = 0;

        int bytesRead;
        while (!source.End)
        {
            bytesRead = source.ReadNext(buffer);
            using MemoryStream mem = new(buffer, 0, bytesRead);
            byteEncrypt += EncryptStream(mem, destination);
        }
        return byteEncrypt;
    }
    public long DecryptStreamBlock(BlockReader source, Stream destination)
    {
        byte[] buffer = new byte[source.BlockSize];
        byte[] IV = new byte[16];
        long byteDecrypt = 0;

        int bytesRead;
        while (!source.End)
        {
            bytesRead = source.ReadNext(buffer);
            using MemoryStream mem = new(buffer, 0, bytesRead);
            mem.Read(IV, 0, IV.Length);

            byteDecrypt += DecryptStream(mem, destination, IV);
        }

        return byteDecrypt;
    }

    public async Task<long> EncryptStreamBlockAsync(BlockReader source, Stream destination, CancellationToken cancelToken)
    {
        byte[] buffer = new byte[source.BlockSize];
        long byteEncrypt = 0;

        int bytesRead;
        while (!source.End)
        {
            bytesRead = source.ReadNext(buffer);
            using MemoryStream mem = new(buffer, 0, bytesRead);
            byteEncrypt += await EncryptStreamAsync(mem, destination, cancelToken);
        }

        return byteEncrypt;
    }
    public async Task<long> DecryptStreamBlockAsync(BlockReader source, Stream destination, CancellationToken cancelToken)
    {
        byte[] buffer = new byte[source.BlockSize];
        byte[] IV = new byte[16];
        long byteDecrypt = 0;

        int bytesRead;
        while (!source.End)
        {
            bytesRead = source.ReadNext(buffer);
            using MemoryStream mem = new(buffer, 0, bytesRead);
            mem.Read(IV, 0, IV.Length);

            byteDecrypt += await DecryptStreamAsync(mem, destination, IV, cancelToken);
        }

        return byteDecrypt;
    }

    internal long EncryptStream(Stream source, Stream destination)
    {
        long originPositon = destination.Position;

        using Aes crypto = PZCrypto.CreateAes();
        using ICryptoTransform encryptor = crypto.CreateEncryptor(Key, crypto.IV);
        using CryptoStream encryptStream = new(source, encryptor, CryptoStreamMode.Read);

        destination.Write(crypto.IV, 0, crypto.IV.Length);
        encryptStream.CopyTo(destination);

        return destination.Position - originPositon;
    }
    internal long DecryptStream(Stream source, Stream destination, byte[] IV)
    {
        long originPositon = destination.Position;

        using Aes crypto = PZCrypto.CreateAes();
        using ICryptoTransform decryptor = crypto.CreateDecryptor(Key, IV);
        using CryptoStream decryptStream = new(destination, decryptor, CryptoStreamMode.Write, true);

        source.CopyTo(decryptStream);

        return destination.Position - originPositon;
    }

    internal async Task<long> EncryptStreamAsync(Stream source, Stream destination, CancellationToken cancelToken)
    {
        long originPositon = destination.Position;

        using Aes crypto = PZCrypto.CreateAes();
        using ICryptoTransform encryptor = crypto.CreateEncryptor(Key, crypto.IV);
        using CryptoStream encryptStream = new(source, encryptor, CryptoStreamMode.Read);

        destination.Write(crypto.IV, 0, crypto.IV.Length);
        await encryptStream.CopyToAsync(destination, cancelToken);

        return destination.Position - originPositon;
    }
    internal async Task<long> DecryptStreamAsync(Stream source, Stream destination, byte[] IV, CancellationToken cancelToken)
    {
        long originPositon = destination.Position;

        using Aes crypto = PZCrypto.CreateAes();
        using ICryptoTransform decryptor = crypto.CreateDecryptor(Key, IV);
        using CryptoStream decryptStream = new(destination, decryptor, CryptoStreamMode.Write, true);

        await source.CopyToAsync(decryptStream, cancelToken);

        return destination.Position - originPositon;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

internal class StreamBlockWrapper
{
    private readonly Stream _innerStream;
    private readonly long _offset;
    private readonly long _length;
    private readonly int _blockSize;
    private readonly int _count;

    public int Count => _count;
    public int BlockSize => _blockSize;
    public long Length => _length;

    public StreamBlockWrapper(Stream innerStream, long offset, long length, int blockSize)
    {
        _innerStream = innerStream;
        _offset = offset;
        _length = length;
        _blockSize = blockSize;
        _count = ComputeBlockCount();
    }
    private int ComputeBlockCount()
    {
        int count = (int)(_length / _blockSize);
        if (_length % _blockSize != 0) count += 1;
        return count;
    }
    private int GetBlockSize(int blockIndex)
    {
        if (_length % _blockSize == 0 || blockIndex < _count - 1)
        {
            return _blockSize;
        }

        return (int)(_length % _blockSize);
    }
    private long GetBlockOffset(int blockIndex)
    {
        return _blockSize * blockIndex + _offset;
    }
    public int ReadBlock(int blockIndex, byte[] buffer)
    {
        if (blockIndex < 0 || blockIndex >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(blockIndex));
        }

        long currentBlockOffset = GetBlockOffset(blockIndex);
        int currentBlockSize = GetBlockSize(blockIndex);

        _innerStream.Seek(currentBlockOffset, SeekOrigin.Begin);
        int readed = _innerStream.Read(buffer, 0, currentBlockSize);
        return readed;
    }
}

internal class BlockReader
{
    private readonly StreamBlockWrapper _wrapper;
    private int _position;
    private long _bytesRead;
    private readonly IProgress<(long, long)>? _progress;

    public int Count => _wrapper.Count;
    public int BlockSize => _wrapper.BlockSize;
    public int Position => _position;
    public bool End => Position >= Count;

    public BlockReader(StreamBlockWrapper wrapper, IProgress<(long, long)>? progress = default)
    {
        _wrapper = wrapper;
        _position = 0;
        _bytesRead = 0;
        _progress = progress;
    }
    public int ReadNext(byte[] buffer)
    {
        if (End) return 0;

        var bytesRead = _wrapper.ReadBlock(_position, buffer);
        _position++;
        _bytesRead += bytesRead;
        _progress?.Report((_bytesRead, _wrapper.Length));

        return bytesRead;
    }
}
