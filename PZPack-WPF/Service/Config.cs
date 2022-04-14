using System.IO;
using IniParser;
using IniParser.Model;

namespace PZPack.View.Service
{
    internal class Config
    {
        private const string NS_UserSetting = "UserSetting";
        private const string NS_Application = "Application";

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
            get { return _data[NS_UserSetting][nameof(ExternalPlayer)]; }
            set
            {
                _data[NS_UserSetting][nameof(ExternalPlayer)] = value;
                Save();
            }
        }

        public string Language
        {
            get { return _data[NS_UserSetting][nameof(Language)]; }
            set
            {
                _data[NS_UserSetting][nameof(Language)] = value;
                Save();
            }
        }

        public string LastOpenDirectory
        {
            get { return _data[NS_Application][nameof(LastOpenDirectory)]; }
            set
            {
                if (string.IsNullOrEmpty(value)) return;

                _data[NS_Application][nameof(LastOpenDirectory)] = value;
                Save();
            }
        }
        public string LastSaveDirectory
        {
            get { return _data[NS_Application][nameof(LastSaveDirectory)]; }
            set
            {
                if (string.IsNullOrEmpty(value)) return;

                _data[NS_Application][nameof(LastSaveDirectory)] = value;
                Save();
            }
        }
        public string LastPWBookDirectory
        {
            get { return _data[NS_Application][nameof(LastPWBookDirectory)]; }
            set
            {
                if (string.IsNullOrEmpty(value)) return;

                _data[NS_Application][nameof(LastPWBookDirectory)] = value;
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
