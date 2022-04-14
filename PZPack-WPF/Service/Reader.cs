using PZPack.Core;
using System;
using System.IO;

namespace PZPack.View.Service
{
    enum PZReaderChangeAction
    {
        OPEN, CLOSE
    }
    internal static class Reader
    {
        static public bool IsOpened
        {
            get
            {
                return Instance != null;
            }
        }
        static public PZReader? Instance { get; private set; }
        static public bool Open(string source, string password)
        {
            var key = PZCryptoCreater.CreateKey(password);
            return Open(source, key);
        }
        static public bool Open(string source, byte[] key)
        {
            Close();

            try
            {
                Instance = PZReader.Open(source, key);
                DashServer.Instance.Binding(Instance);
                DashServer.Instance.Start();
            }
            catch (Exception ex)
            {
                Alert.ShowException(ex);
                Instance = null;
            }

            if (Instance != null)
            {
                PZReaderChanged?.Invoke(null, new PZReaderChangeEventArgs(PZReaderChangeAction.OPEN, source, Instance.PZType));
                PZHistory.Instance.PushHistory(source);
            }

            return Instance != null;
        }
        static public bool TryOpen(string source)
        {
            if (PWBook.Current == null) return false;

            var fstream = new FileStream(source, FileMode.Open, FileAccess.Read);
            string pwHash = PZReader.GetFilePwCheckHash(fstream);
            var pwRecord = PWBook.Current.GetRecord(pwHash);
            if (pwRecord == null) return false;

            return Open(source, pwRecord.Key);
        }

        static public void Close()
        {
            if (Instance != null)
            {
                DashServer.Instance.Binding(null);
                Instance.Dispose();
                Instance = null;

                PZReaderChanged?.Invoke(null, new PZReaderChangeEventArgs(PZReaderChangeAction.CLOSE));
            }
        }

        static public bool IsPicture(PZFile file)
        {
            return IsPicture(file.Name);
        }
        static public bool IsPicture(string filename)
        {
            string ext = System.IO.Path.GetExtension(filename);
            return ext switch
            {
                ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".webp" => true,
                _ => false
            };
        }

        public static event EventHandler<PZReaderChangeEventArgs>? PZReaderChanged;
    }
    internal class PZReaderChangeEventArgs : EventArgs
    {
        public string? Source { get; private set; }
        public PZReaderChangeAction Action { get; private set; }
        public PZTypes? Type { get; private set; }

        public PZReaderChangeEventArgs(PZReaderChangeAction action, string? source = null, PZTypes? type = null)
        {
            Action = action;
            Source = source;
            Type = type;
        }

    }
}
