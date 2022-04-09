using System.IO;
using IniParser;
using IniParser.Model;

namespace PZPack.View.Service
{
    internal class Config
    {
        private static Config? _instance;
        public static Config Instance
        {
            get
            {
                if (_instance == null) _instance = new Config();
                return _instance;
            }
        }

        private readonly IniData _data;

        public string ExternalPlayer
        {
            get { return _data["UserSetting"]["ExternalPlayer"]; }
            set
            {
                _data["UserSetting"]["ExternalPlayer"] = value;
                Save();
            }
        }

        public string Language
        {
            get { return _data["UserSetting"]["Language"]; }
            set
            {
                _data["UserSetting"]["Language"] = value;
                Save();
            }
        }

        private Config()
        {
            _data = Inited();
        }

        private static IniData Inited()
        {
            string rootPath = System.AppDomain.CurrentDomain.BaseDirectory;
            string configFilePath = Path.Join(rootPath, "config.ini");
            if (!File.Exists(configFilePath))
            {
                File.Create(configFilePath).Close();
            }

            FileIniDataParser parser = new();
            IniData data = parser.ReadFile(configFilePath);
            return data;
        }
        private void Save()
        {
            string rootPath = System.AppDomain.CurrentDomain.BaseDirectory;
            string configFilePath = Path.Join(rootPath, "config.ini");

            FileIniDataParser parser = new();
            parser.WriteFile(configFilePath, _data);
        }
    }
}
