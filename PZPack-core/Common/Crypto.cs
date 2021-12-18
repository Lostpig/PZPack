using System.Security.Cryptography;
using System.Text;
using System;

namespace PZPack.Core
{
    public class PZCrypto : IDisposable
    {
        private readonly Aes Cryptor;
        private readonly byte[] Key;
        private readonly byte[] Nonce;
        public PZCrypto(string password)
        {
            Cryptor = Aes.Create();
            Cryptor.Mode = CipherMode.CBC;

            byte[] key = new byte[Cryptor.KeySize / 8];
            byte[] pwHash = Hash(password);
            Array.Copy(pwHash, 0, key, 0, key.Length);
            Key = key;

            byte[] nonce = new byte[Cryptor.BlockSize / 8];
            byte[] ivHash = Hash(pwHash);
            Array.Copy(ivHash, 0, nonce, 0, nonce.Length);
            Nonce = nonce;
        }

        public byte[] GetPwCheckHash()
        {
            string hex = HashHex(Key);
            return Hash(hex);
        }
        public byte[] Encrypt(byte[] bytes)
        {
            byte[] result;

            using (ICryptoTransform encryptor = Cryptor.CreateEncryptor(Key, Nonce))
            using (MemoryStream memStream = new())
            using (CryptoStream cryptoStream = new(memStream, encryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(bytes, 0, bytes.Length);
                cryptoStream.FlushFinalBlock();
                result = memStream.ToArray();
            }

            return result;
        }
        public byte[] Decrypt(byte[] bytes)
        {
            byte[] result;

            using (ICryptoTransform decryptor = Cryptor.CreateDecryptor(Key, Nonce))
            using (MemoryStream memStream = new())
            using (CryptoStream cryptoStream = new(memStream, decryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(bytes, 0, bytes.Length);
                cryptoStream.FlushFinalBlock();
                result = memStream.ToArray();
            }

            return result;
        }

        public async Task<long> EncryptStream(Stream source, Stream target, IProgress<(long, long)>? progress = default, CancellationToken? cancelToken = null)
        {
            long startPos = target.Position;
            byte[] buffer = new byte[8192];

            using (ICryptoTransform encryptor = Cryptor.CreateEncryptor(Key, Nonce))
            using (CryptoStream encryptStream = new(source, encryptor, CryptoStreamMode.Read))
            {
                int bytesRead;
                if (cancelToken.HasValue)
                {
                    do
                    {
                        bytesRead = await encryptStream.ReadAsync(buffer, cancelToken.Value).ConfigureAwait(false);
                        await target.WriteAsync(buffer, cancelToken.Value).ConfigureAwait(false);
                        progress?.Report((source.Position, source.Length));
                    } while (bytesRead > 0);
                }
                else
                {
                    do
                    {
                        bytesRead = await encryptStream.ReadAsync(buffer).ConfigureAwait(false);
                        await target.WriteAsync(buffer).ConfigureAwait(false);
                        progress?.Report((source.Position, source.Length));
                    } while (bytesRead > 0);
                }
            }

            return target.Position - startPos;
        }
        public async Task<long> DecryptStream(Stream source, Stream target, long offset, long size, IProgress<(long, long)>? progress = default, CancellationToken? cancelToken = null)
        {
            byte[] buffer = new byte[8192];
            long resultLen = 0;

            using (ICryptoTransform decryptor = Cryptor.CreateDecryptor(Key, Nonce))
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

        public static byte[] Hash(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            return Hash(bytes);
        }
        public static byte[] Hash(byte[] source)
        {
            SHA256 sHA256 = SHA256.Create();
            return sHA256.ComputeHash(source);
        }
        public static string HashHex(string text)
        {
            byte[] hashBytes = Hash(text);
            return Convert.ToHexString(hashBytes);
        }
        public static string HashHex(byte[] source)
        {
            byte[] hashBytes = Hash(source);
            return Convert.ToHexString(hashBytes);
        }

        public void Dispose()
        {
            Cryptor.Clear();
            Cryptor.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
