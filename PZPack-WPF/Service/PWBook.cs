using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using PZPack.Core;
using PZPack.Core.Exceptions;

namespace PZPack.View.Service
{
    internal record PWBookRecord(byte[] Hash, byte[] Key);
    internal class PWBook
    {
        internal const string PZPwBookSign = "PZ-PasswordBook";

        private readonly string filename;
        private readonly IPZCrypto crypto;
        private readonly Dictionary<string, PWBookRecord> map;
        private PWBook(string filename, IPZCrypto crypto, byte[]? data)
        {
            this.filename = filename;
            this.crypto = crypto;
            this.map = InitMap(data);
        }
        private static Dictionary<string, PWBookRecord> InitMap (byte[]? data)
        {
            Dictionary<string, PWBookRecord> bookMap = new();
            if (data == null) return bookMap;

            using MemoryStream mem = new(data);
            mem.Seek(0, SeekOrigin.Begin);
            while (mem.Position < mem.Length)
            {
                byte[] hashTemp = new byte[32];
                byte[] keyTemp = new byte[32];
                mem.Read(hashTemp, 0, hashTemp.Length);
                mem.Read(keyTemp, 0, hashTemp.Length);
                string hashHex = Convert.ToHexString(hashTemp);
                bookMap.Add(hashHex, new PWBookRecord(hashTemp, keyTemp));
            }
            return bookMap;
        }
        public void AddPassword(string password)
        {
            byte[] key = PZCryptoCreater.CreateKey(password);
            byte[] hash = PZCryptoCreater.CreatePwCheckKey(key);
            string hashHex = Convert.ToHexString(hash);
            if (map.ContainsKey(hashHex)) return;

            map.Add(hashHex, new PWBookRecord(hash, key));
        }
        public void RemovePassword(string hash)
        {
            map.Remove(hash);
        }
        public bool Has(string hash)
        {
            return map.ContainsKey(hash);
        }
        public PWBookRecord? GetRecord(string hash)
        {
            bool success = map.TryGetValue(hash, out PWBookRecord? record);
            return success ? record : null;
        }
        public string[] GetKeys ()
        {
            return map.Keys.ToArray();
        }
        private byte[] Encode ()
        {
            using var mem = new MemoryStream();
            using var dataMem = new MemoryStream(map.Count * 64);

            mem.Seek(0, SeekOrigin.Begin);
            byte[] signHash = PZHash.Hash(PZPwBookSign);
            byte[] checkHash = crypto.GetPwCheckHash();
            mem.Write(signHash, 0, signHash.Length);
            mem.Write(checkHash, 0, checkHash.Length);
            Debug.Assert(mem.Position == 64);

            dataMem.Seek(0, SeekOrigin.Begin);
            foreach (PWBookRecord record in map.Values)
            {
                dataMem.Write(record.Hash, 0, record.Hash.Length);
                dataMem.Write(record.Key, 0, record.Key.Length);
            }
            crypto.EncryptStream(dataMem, mem);
            
            return mem.ToArray();
        }
        private static byte[] Decode (Stream source, IPZCrypto crypto)
        {
            string signText = PZHash.HashHex(PZPwBookSign);
            byte[] srcSign = new byte[32];
            source.Read(srcSign, 0, srcSign.Length);
            string srcSignText = Convert.ToHexString(srcSign);
            if (srcSignText != signText)
            {
                throw new PZSignCheckedException();
            }

            byte[] srcPwHash = new byte[32];
            source.Read(srcPwHash, 0, srcPwHash.Length);
            byte[] checkHash = crypto.GetPwCheckHash();
            string checkText = Convert.ToHexString(checkHash);
            string srcCheckText = Convert.ToHexString(srcPwHash);
            if (srcCheckText != checkText)
            {
                throw new PZPasswordIncorrectException();
            }

            using MemoryStream dataMem = new();
            crypto.DecryptStream(source, dataMem, 64, source.Length - 64);
            return dataMem.ToArray();
        }

        private static PWBook? _current;
        public static PWBook? Current { get { return _current; } }
        public static void Create (string filename, string masterPassword)
        {
            if (File.Exists(filename))
            {
                throw new OutputFileAlreadyExistsException(filename);
            }

            var crypto = PZCryptoCreater.CreateCrypto(masterPassword, PZVersion.Current);
            var newInstance = new PWBook(filename, crypto, null);

            Close(true);
            _current = newInstance;

            PWBookChanged?.Invoke(null, new PZPwBookChangeEventArgs(true, _current));
        }
        public static void Load (string filename, string masterPassword)
        {
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException(filename);
            }

            var fstream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            var crypto = PZCryptoCreater.CreateCrypto(masterPassword, PZVersion.Current);
            byte[] data = Decode(fstream, crypto);
            var newInstance = new PWBook(filename, crypto, data);

            Close(true);
            _current = newInstance;

            PWBookChanged?.Invoke(null, new PZPwBookChangeEventArgs(true, _current));
        }
        public static void Close (bool silent = false)
        {
            if (_current != null)
            {
                Save(_current);
            }
            _current = null;

            if (!silent)
            {
                PWBookChanged?.Invoke(null, new PZPwBookChangeEventArgs(false, null));
            }
        }
        public static void Save(PWBook book)
        {
            File.WriteAllBytes(book.filename, book.Encode());
        }

        public static event EventHandler<PZPwBookChangeEventArgs>? PWBookChanged;
    }

    internal class PZPwBookChangeEventArgs : EventArgs
    {
        public readonly bool Opened;
        public readonly PWBook? Instance;

        public PZPwBookChangeEventArgs(bool opened, PWBook? instance = null)
        {
            Opened = opened;
            Instance = instance;
        }

    }
}
