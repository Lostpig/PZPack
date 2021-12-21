using PZPack.Core;
using System;
using System.Windows;

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
        static public void Open(string source, string password)
        {
            Close();

            try
            {
                Instance = PZReader.Open(source, password);
            } 
            catch (Exception ex)
            {
                Alert.ShowException(ex);
                Instance = null;
            }

            if (Instance != null)
            {
                PZReaderChanged?.Invoke(null, new PZReaderChangeEventArgs(PZReaderChangeAction.OPEN, source));
            }
        }
        static public void Close()
        {
            if (Instance != null)
            {
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

        public PZReaderChangeEventArgs(PZReaderChangeAction action, string? source = null)
        {
            Action = action;
            Source = source;
        }

    }
}
