using System.Security.Cryptography;

namespace PZPack.Core.Compatible
{
    internal class PZCryptoBeforeV2: IPZCrypto
    {
        const int KeySize = 256;
        const int IVSize = 128;
        private readonly byte[] Key;
        private readonly byte[] IV;

        public PZCryptoBeforeV2(string password)
        {
            byte[] key = new byte[KeySize / 8];
            byte[] pwHash = PZHash.Hash(password);
            Array.Copy(pwHash, 0, key, 0, key.Length);
            Key = key;

            byte[] iv = new byte[IVSize / 8];
            byte[] ivHash = PZHash.Hash(pwHash);
            Array.Copy(ivHash, 0, iv, 0, iv.Length);
            IV = iv;
        }

        private static Aes NewCryptor()
        {
            Aes cryptor = Aes.Create();
            cryptor.Mode = CipherMode.CBC;
            cryptor.KeySize = KeySize;
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
            using (ICryptoTransform encryptor = crypto.CreateEncryptor(Key, IV))
            using (CryptoStream encryptStream = new(source, encryptor, CryptoStreamMode.Read))
            {
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

            source.Seek(offset, SeekOrigin.Begin);
            using (Aes crypto = NewCryptor())
            using (ICryptoTransform decryptor = crypto.CreateDecryptor(Key, IV))
            using (CryptoStream decryptStream = new(target, decryptor, CryptoStreamMode.Write))
            {
                long remain = size;
                int bytesRead;
                int bytesWrite;

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
            using (ICryptoTransform encryptor = crypto.CreateEncryptor(Key, IV))
            using (CryptoStream encryptStream = new(source, encryptor, CryptoStreamMode.Read))
            {
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

            source.Seek(offset, SeekOrigin.Begin);

            using (Aes crypto = NewCryptor())
            using (ICryptoTransform decryptor = crypto.CreateDecryptor(Key, IV))
            using (CryptoStream decryptStream = new(target, decryptor, CryptoStreamMode.Write))
            {
                long remain = size;
                int bytesRead;
                int bytesWrite;

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
