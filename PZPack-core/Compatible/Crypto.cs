using System.Security.Cryptography;
using System.Text;
using System;
using System.Diagnostics;

namespace PZPack.Core.Compatible
{
    internal class PZCryptoBase : IDisposable
    {
        internal readonly byte[] Key;
        internal PZCryptoBase(byte[] key)
        {
            Key = key;
        }
        internal byte[] GetPwCheckHash()
        {
            return PZCryptoCreater.CreatePwCheckKey(Key);
        }

        internal long EncryptStream(Stream source, Stream target)
        {
            long startPos = target.Position;
            byte[] buffer = new byte[8192];

            using (Aes crypto = PZCryptoCreater.NewCryptor())
            using (ICryptoTransform encryptor = crypto.CreateEncryptor(Key, crypto.IV))
            using (CryptoStream encryptStream = new(source, encryptor, CryptoStreamMode.Read))
            {
                target.Write(crypto.IV, 0, crypto.IV.Length);
                Debug.Assert(target.Position == startPos + 16);

                int bytesRead;
                do
                {
                    bytesRead = encryptStream.Read(buffer);
                    target.Write(buffer, 0, bytesRead);
                } while (bytesRead > 0);
            }

            return target.Position - startPos;
        }
        public async Task<long> EncryptStreamAsync(Stream source, Stream target, IProgress<(long, long)>? progress = default, CancellationToken? cancelToken = null)
        {
            long startPos = target.Position;
            byte[] buffer = new byte[8192];

            using (Aes crypto = PZCryptoCreater.NewCryptor())
            using (ICryptoTransform encryptor = crypto.CreateEncryptor(Key, crypto.IV))
            using (CryptoStream encryptStream = new(source, encryptor, CryptoStreamMode.Read))
            {
                await target.WriteAsync(crypto.IV.AsMemory(0, crypto.IV.Length)).ConfigureAwait(false);
                Debug.Assert(target.Position == startPos + 16);

                int bytesRead;
                if (cancelToken.HasValue)
                {
                    do
                    {
                        bytesRead = await encryptStream.ReadAsync(buffer, cancelToken.Value).ConfigureAwait(false);
                        await target.WriteAsync(buffer.AsMemory(0, bytesRead), cancelToken.Value).ConfigureAwait(false);
                        progress?.Report((source.Position, source.Length));
                    } while (bytesRead > 0);
                }
                else
                {
                    do
                    {
                        bytesRead = await encryptStream.ReadAsync(buffer).ConfigureAwait(false);
                        await target.WriteAsync(buffer.AsMemory(0, bytesRead)).ConfigureAwait(false);
                        progress?.Report((source.Position, source.Length));
                    } while (bytesRead > 0);
                }
            }

            return target.Position - startPos;
        }

        internal long DecryptStream(Stream source, Stream target, byte[] IV, long offset, long size)
        {
            byte[] buffer = new byte[8192];
            long resultLen = 0;

            using (Aes crypto = PZCryptoCreater.NewCryptor())
            using (ICryptoTransform decryptor = crypto.CreateDecryptor(Key, IV))
            using (CryptoStream decryptStream = new(target, decryptor, CryptoStreamMode.Write))
            {
                long remain = size;
                int bytesRead;
                int bytesWrite;
                source.Seek(offset, SeekOrigin.Begin);

                do
                {
                    bytesRead = source.Read(buffer);
                    if (remain < bytesRead) bytesWrite = (int)remain;
                    else bytesWrite = bytesRead;

                    decryptStream.Write(buffer, 0, bytesWrite);
                    remain -= bytesRead;
                } while (remain > 0);
                decryptStream.FlushFinalBlock();

                resultLen = target.Length;
            }

            return resultLen;
        }
        public async Task<long> DecryptStreamAsync(Stream source, Stream target, byte[] IV, long offset, long size, IProgress<(long, long)>? progress = default, CancellationToken? cancelToken = null)
        {
            byte[] buffer = new byte[8192];
            long resultLen = 0;

            using (Aes crypto = PZCryptoCreater.NewCryptor())
            using (ICryptoTransform decryptor = crypto.CreateDecryptor(Key, IV))
            using (CryptoStream decryptStream = new(target, decryptor, CryptoStreamMode.Write))
            {
                long remain = size;
                int bytesRead;
                int bytesWrite;
                source.Seek(offset, SeekOrigin.Begin);

                if (cancelToken.HasValue)
                {
                    do
                    {
                        bytesRead = await source.ReadAsync(buffer, cancelToken.Value).ConfigureAwait(false);
                        if (remain < bytesRead) bytesWrite = (int)remain;
                        else bytesWrite = bytesRead;

                        await decryptStream.WriteAsync(buffer.AsMemory(0, bytesWrite), cancelToken.Value).ConfigureAwait(false);
                        remain -= bytesRead;
                        progress?.Report((size - remain, size));
                    } while (remain > 0);
                    await decryptStream.FlushFinalBlockAsync(cancelToken.Value);
                }
                else
                {
                    do
                    {
                        bytesRead = await source.ReadAsync(buffer).ConfigureAwait(false);
                        if (remain < bytesRead) bytesWrite = (int)remain;
                        else bytesWrite = bytesRead;

                        await decryptStream.WriteAsync(buffer.AsMemory(0, bytesWrite)).ConfigureAwait(false);
                        remain -= bytesRead;
                        progress?.Report((size - remain, size));
                    } while (remain > 0);
                    await decryptStream.FlushFinalBlockAsync();
                }

                resultLen = target.Length;
            }
            return resultLen;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    // 当前版本
    internal class PZCryptoCurrent : IPZCrypto
    {
        private readonly PZCryptoBase baseInst;
        public PZCryptoCurrent(string password) : this(PZCryptoCreater.CreateKey(password)) { }
        public PZCryptoCurrent(byte[] key)
        {
            baseInst = new PZCryptoBase(key);
        }
        public byte[] GetPwCheckHash()
        {
            return baseInst.GetPwCheckHash();
        }
        public byte[] Encrypt(byte[] bytes)
        {
            byte[] result;

            using (MemoryStream targetStream = new(), sourceStream = new(bytes))
            {
                EncryptStream(sourceStream, targetStream);
                result = targetStream.ToArray();
            }

            return result;
        }
        public byte[] Decrypt(byte[] bytes)
        {
            byte[] result;

            using (MemoryStream targetStream = new(), sourceStream = new(bytes))
            {
                DecryptStream(sourceStream, targetStream, 0, bytes.Length);
                result = targetStream.ToArray();
            }

            return result;
        }

        public long EncryptStream(Stream source, Stream target)
        {
            return baseInst.EncryptStream(source, target);
        }
        public long DecryptStream(Stream source, Stream target, long offset, long size)
        {
            byte[] IV = new byte[16];
            source.Seek(offset, SeekOrigin.Begin);
            source.Read(IV, 0, IV.Length);

            return baseInst.DecryptStream(source, target, IV, offset + 16, size - 16);
        }

        public async Task<long> EncryptStreamAsync(Stream source, Stream target, IProgress<(long, long)>? progress = default, CancellationToken? cancelToken = null)
        {
            return await baseInst.EncryptStreamAsync(source, target, progress, cancelToken);
        }
        public async Task<long> DecryptStreamAsync(Stream source, Stream target, long offset, long size, IProgress<(long, long)>? progress = default, CancellationToken? cancelToken = null)
        {
            byte[] IV = new byte[16];
            source.Seek(offset, SeekOrigin.Begin);
            await source.ReadAsync(IV);

            return await baseInst.DecryptStreamAsync(source, target, IV, offset + 16, size - 16, progress, cancelToken);
        }

        public void Dispose()
        {
            baseInst.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    // V2及之前版本
    internal class PZCryptoBeforeV2 : IPZCrypto
    {
        private readonly PZCryptoBase baseInst;
        private readonly byte[] IV;

        public PZCryptoBeforeV2(string password) : this(PZCryptoCreater.CreateKey(password)) { }
        public PZCryptoBeforeV2(byte[] key)
        {
            byte[] iv = new byte[16];
            byte[] ivHash = PZHash.Hash(key);
            Array.Copy(ivHash, 0, iv, 0, iv.Length);
            IV = iv;

            baseInst = new(key);
        }

        public byte[] GetPwCheckHash()
        {
            return baseInst.GetPwCheckHash();
        }
        public byte[] Encrypt(byte[] bytes)
        {
            byte[] result;

            using (MemoryStream targetStream = new(), sourceStream = new(bytes))
            {
                EncryptStream(sourceStream, targetStream);
                result = targetStream.ToArray();
            }

            return result;
        }
        public byte[] Decrypt(byte[] bytes)
        {
            byte[] result;

            using (MemoryStream targetStream = new(), sourceStream = new(bytes))
            {
                DecryptStream(sourceStream, targetStream, 0, bytes.Length);
                result = targetStream.ToArray();
            }

            return result;
        }

        public long EncryptStream(Stream source, Stream target)
        {
            return baseInst.EncryptStream(source, target);
        }
        public long DecryptStream(Stream source, Stream target, long offset, long size)
        {
            return baseInst.DecryptStream(source, target, this.IV, offset, size);
        }

        public async Task<long> EncryptStreamAsync(Stream source, Stream target, IProgress<(long, long)>? progress = default, CancellationToken? cancelToken = null)
        {
            return await baseInst.EncryptStreamAsync(source, target, progress, cancelToken);
        }
        public async Task<long> DecryptStreamAsync(Stream source, Stream target, long offset, long size, IProgress<(long, long)>? progress = default, CancellationToken? cancelToken = null)
        {
            return await baseInst.DecryptStreamAsync(source, target, this.IV, offset, size, progress, cancelToken);
        }

        public void Dispose()
        {
            baseInst.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
