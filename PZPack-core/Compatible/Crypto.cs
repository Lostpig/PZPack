using System.Security.Cryptography;
using System.Text;
using System;
using System.Diagnostics;

namespace PZPack.Core.Compatible
{
    // AES-256-CBC PKCS7 加密
    internal class PZCrypto : IPZCrypto
    {
        const int KeySize = 256;
        private readonly byte[] Key;
        public PZCrypto(string password)
        {
            byte[] key = new byte[KeySize / 8];
            byte[] pwHash = PZHash.Hash(password);
            Array.Copy(pwHash, 0, key, 0, key.Length);
            Key = key;
        }
        private static Aes NewCryptor()
        {
            Aes cryptor = Aes.Create();
            cryptor.Mode = CipherMode.CBC;
            cryptor.Padding = PaddingMode.PKCS7;
            cryptor.KeySize = KeySize;
            cryptor.GenerateIV();

            return cryptor;
        }

        public byte[] GetPwCheckHash()
        {
            string hex = PZHash.HashHex(Key);
            return PZHash.Hash(hex);
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
            long startPos = target.Position;
            byte[] buffer = new byte[8192];

            using (Aes crypto = NewCryptor())
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
        public long DecryptStream(Stream source, Stream target, long offset, long size)
        {
            byte[] buffer = new byte[8192];
            long resultLen = 0;

            byte[] IV = new byte[16];
            source.Seek(offset, SeekOrigin.Begin);
            source.Read(IV, 0, IV.Length);

            using (Aes crypto = NewCryptor())
            using (ICryptoTransform decryptor = crypto.CreateDecryptor(Key, IV))
            using (CryptoStream decryptStream = new(target, decryptor, CryptoStreamMode.Write))
            {
                long remain = size - 16;
                int bytesRead;
                int bytesWrite;

                Debug.Assert(source.Position == offset + 16);

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

        public async Task<long> EncryptStreamAsync(Stream source, Stream target, IProgress<(long, long)>? progress = default, CancellationToken? cancelToken = null)
        {
            long startPos = target.Position;
            byte[] buffer = new byte[8192];

            using (Aes crypto = NewCryptor())
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
        public async Task<long> DecryptStreamAsync(Stream source, Stream target, long offset, long size, IProgress<(long, long)>? progress = default, CancellationToken? cancelToken = null)
        {
            byte[] buffer = new byte[8192];
            long resultLen = 0;

            byte[] IV = new byte[16];
            source.Seek(offset, SeekOrigin.Begin);
            await source.ReadAsync(IV);

            using (Aes crypto = NewCryptor())
            using (ICryptoTransform decryptor = crypto.CreateDecryptor(Key, IV))
            using (CryptoStream decryptStream = new(target, decryptor, CryptoStreamMode.Write))
            {
                long remain = size - 16;
                int bytesRead;
                int bytesWrite;

                Debug.Assert(source.Position == offset + 16);
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
}
