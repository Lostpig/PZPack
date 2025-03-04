using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PZPack.View.Service
{
    internal class PZHistory
    {
        private static PZHistory? _instance;
        public static PZHistory Instance
        {
            get
            {
                _instance ??= new PZHistory();
                return _instance;
            }
        }

        private const int MAX_COUNT = 20;
        public readonly List<string> Items;
        private PZHistory()
        {
            Items = new();
            Load();
        }
        public void PushHistory(string path)
        {
            Items.Remove(path);
            Items.Add(path);

            if (Items.Count > MAX_COUNT)
            {
                int removeCount = Items.Count - MAX_COUNT;
                Items.RemoveRange(0, removeCount);
            }

            HistoryChanged?.Invoke(this, EventArgs.Empty);

            Save();
        }
        public void Clear ()
        {
            Items.Clear();
            HistoryChanged?.Invoke(this, EventArgs.Empty);

            Save();
        }
        public bool Remove(string item)
        {
            bool res = Items.Remove(item);
            if (res) Save();

            return res;
        }
        private void Save()
        {
            StringBuilder sb = new();
            foreach (string item in Items) { sb.AppendLine(item); }

            string rootPath = System.AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Join(rootPath, "history.pzh");

            File.WriteAllText(filePath, sb.ToString());
        }
        private void Load()
        {
            string rootPath = System.AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Join(rootPath, "history.pzh");

            if (File.Exists(filePath))
            {
                string[] lines = File.ReadAllLines(filePath);
                Items.Clear();
                foreach (string line in lines)
                {
                    Items.Add(line);
                }
            }

            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }
    
        public event EventHandler? HistoryChanged;
    }
}
