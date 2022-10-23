using System.Security.Cryptography;
using System.Diagnostics;
using PZPack.Core.Utility;
using PZPack.Core.Index;

namespace PZPack.Core.Crypto;

internal class PZCryptoBase : IDisposable
{
    static internal Aes CreateAes()
    {
        Aes cryptor = Aes.Create();
        cryptor.Mode = CipherMode.CBC;
        cryptor.Padding = PaddingMode.PKCS7;
        cryptor.KeySize = 256;
        cryptor.GenerateIV();

        return cryptor;
    }
    static internal byte[] CreateKey(string password)
    {
        return PZHash.Sha256(password);
    }
    static internal byte[] CreateKeyHash(byte[] key)
    {
        string hex = PZHash.Sha256Hex(key);
        return PZHash.Sha256(hex);
    }

    internal readonly byte[] Key;
    internal readonly byte[] Hash;
    internal PZCryptoBase(byte[] key)
    {
        Key = key;
        Hash = CreateKeyHash(Key);
    }

    public long EncryptStreamBlock(BlockReader source, Stream destination)
    {
        byte[] buffer = new byte[source.BlockSize];
        long byteEncrypt = 0;

        int bytesRead;
        for (int i = 0; i < source.Count; i++)
        {
            bytesRead = source.ReadBlock(i, buffer);
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
        for (int i = 0; i < source.Count; i++)
        {
            bytesRead = source.ReadBlock(i, buffer);
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
        for (int i = 0; i < source.Count; i++)
        {
            bytesRead = source.ReadBlock(i, buffer);
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
        for (int i = 0; i < source.Count; i++)
        {
            bytesRead = source.ReadBlock(i, buffer);
            using MemoryStream mem = new(buffer, 0, bytesRead);
            mem.Read(IV, 0, IV.Length);

            byteDecrypt += await DecryptStreamAsync(mem, destination, IV, cancelToken);
        }

        return byteDecrypt;
    }

    internal long EncryptStream(Stream source, Stream destination)
    {
        long originPositon = destination.Position;

        using Aes crypto = CreateAes();
        using ICryptoTransform encryptor = crypto.CreateEncryptor(Key, crypto.IV);
        using CryptoStream encryptStream = new(source, encryptor, CryptoStreamMode.Read);

        destination.Write(crypto.IV, 0, crypto.IV.Length);
        encryptStream.CopyTo(destination);

        return destination.Position - originPositon;
    }
    internal long DecryptStream(Stream source, Stream destination, byte[] IV)
    {
        long originPositon = destination.Position;

        using Aes crypto = CreateAes();
        using ICryptoTransform decryptor = crypto.CreateDecryptor(Key, IV);
        using CryptoStream decryptStream = new(destination, decryptor, CryptoStreamMode.Write, true);

        source.CopyTo(decryptStream);

        return destination.Position - originPositon;
    }

    internal async Task<long> EncryptStreamAsync(Stream source, Stream destination, CancellationToken cancelToken)
    {
        long originPositon = destination.Position;

        using Aes crypto = CreateAes();
        using ICryptoTransform encryptor = crypto.CreateEncryptor(Key, crypto.IV);
        using CryptoStream encryptStream = new(source, encryptor, CryptoStreamMode.Read);

        destination.Write(crypto.IV, 0, crypto.IV.Length);
        await encryptStream.CopyToAsync(destination, cancelToken);

        return destination.Position - originPositon;
    }
    internal async Task<long> DecryptStreamAsync(Stream source, Stream destination, byte[] IV, CancellationToken cancelToken)
    {
        long originPositon = destination.Position;

        using Aes crypto = CreateAes();
        using ICryptoTransform decryptor = crypto.CreateDecryptor(Key, IV);
        using CryptoStream decryptStream = new(destination, decryptor, CryptoStreamMode.Write);

        await source.CopyToAsync(decryptStream, cancelToken);

        return destination.Position - originPositon;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

internal class BlockReader
{
    private readonly Stream _innerStream;
    private readonly long _offset;
    private readonly long _length;
    private readonly int _blockSize;
    private readonly int _count;

    private IProgress<(long, long)>? _progress;

    public int Count => _count;
    public int BlockSize => _blockSize;
    public BlockReader(Stream innerStream, long offset, long length, int blockSize)
    {
        _innerStream = innerStream;
        _offset = offset;
        _length = length;
        _blockSize = blockSize;
        _count = ComputeBlockCount();
    }
    public int ComputeBlockCount ()
    {
        int count = (int)(_length / _blockSize);
        if (_length % _blockSize != 0) count += 1;
        return count;
    }
    public void BindingProgress(IProgress<(long, long)>? progress)
    {
        _progress = progress;
    }

    public int GetBlockSize(int blockIndex)
    {
        if (_length % _blockSize == 0 || blockIndex < _count - 1)
        {
            return _blockSize;
        }

        return (int)(_length % _blockSize);
    }
    public long GetBlockOffset(int blockIndex)
    {
        return _blockSize * blockIndex + _offset;
    }
    public int ReadBlock(int blockIndex, byte[] buffer)
    {
        long currentBlockOffset = GetBlockOffset(blockIndex);
        int currentBlockSize = GetBlockSize(blockIndex);

        _innerStream.Seek(currentBlockOffset, SeekOrigin.Begin);
        int readed = _innerStream.Read(buffer, 0, currentBlockSize);
        _progress?.Report((currentBlockOffset + readed - _offset, _length));

        return readed;
    }
}