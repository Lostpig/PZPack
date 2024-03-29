﻿using System;
using System.Reflection;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace PZPack.View.Service
{
    internal static class Language
    {
        const string DefaultLanguage = "zh-CN";
        public static string Current { get; private set; } = DefaultLanguage;
        private static readonly List<string> _languages = new();

        public static void Init()
        {
            InitLanguageList();
            string userSetLang = ReadLanguageSet();
            LoadLanguage(userSetLang);
            Current = userSetLang;
        }

        public static void ChangeLanguage(string lang)
        {
            if (Current == lang)
            {
                return;
            }

            LoadLanguage(lang);
            SaveLanguageSet(lang);
            Current = lang;
        }
        public static string[] GetLanguages ()
        {
            return _languages.ToArray();
        }
        private static void InitLanguageList ()
        {
            string rootPath = System.AppDomain.CurrentDomain.BaseDirectory;
            string dirPath = Path.Join(rootPath, "Localization");

            string[] files = Directory.GetFiles(dirPath);
            string[] langFiles = files
                .Where(f => Path.GetExtension(f).ToLower() == ".json")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .ToArray();

            _languages.Clear();
            _languages.AddRange(langFiles);
        }
        private static string ReadLanguageSet ()
        {
            string userSetCurrent = Config.Instance.Language;
            if (String.IsNullOrEmpty(userSetCurrent))
            {
                userSetCurrent = DefaultLanguage;
                Config.Instance.Language = userSetCurrent;
            }
            return userSetCurrent;
        }
        private static void SaveLanguageSet (string lang)
        {
            Config.Instance.Language = lang;
        }
        private static void LoadLanguage(string lang)
        {
            string rootPath = System.AppDomain.CurrentDomain.BaseDirectory;
            string langPath = Path.Join(rootPath, "Localization", $"{lang}.json");
            Dictionary<string, string>? map;
            if (File.Exists(langPath))
            {
                string langJson = File.ReadAllText(langPath, Encoding.UTF8);
                map = JsonConvert.DeserializeObject<Dictionary<string, string>>(langJson);
            }
            else
            {
                map = null;
                Debug.Assert(false, $"language file {lang}.json not found");
            }

            UpdateLanguage(map);
        }
        private static void UpdateLanguage(Dictionary<string, string>? languageMap)
        {
            Type t = typeof(Translate);
            foreach (PropertyInfo pi in t.GetProperties())
            {
                Attribute? attr = pi.GetCustomAttribute(typeof(TranslateBindAttribute));
                if (attr is TranslateBindAttribute trans)
                {
                    pi.SetValue(null, getText(trans.Key));
                    Translate.NotifyPropertyChanged(pi.Name);
                }
            }

            string getText (string key)
            {
                if (languageMap != null && languageMap.ContainsKey(key))
                {
                    return languageMap[key];
                }
                return key;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    internal class TranslateBindAttribute : Attribute
    {
        public string Key { get; private set; }
        public TranslateBindAttribute(string key)
        {
            Key = key;
        }
    }
}
