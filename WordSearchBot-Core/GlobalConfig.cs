using System;
using System.IO;

namespace WordSearchBot.Core {
    public static class GlobalConfig {

        private static IniFile file;
        private static string CONFIG_FILE_NAME = "config.ini";

        private static string[] CONFIG_PATHS = {
            "~",
            ".",
            Environment.GetFolderPath(Environment.SpecialFolder.Personal)
        };

        public static IniFile Ini {
            get {
                Load();
                return file;
            }
        }

        private static string FindConfigFile() {
            // return CONFIG_PATHS.Select(prefix => prefix + "/" + CONFIG_FILE_NAME)
            //                    .FirstOrDefault(File.Exists);

            foreach (string s in CONFIG_PATHS) {
                string full = s + "/" + CONFIG_FILE_NAME;
                Console.WriteLine($"Checking if {full} exists.");
                if (File.Exists(full)) {
                    Console.WriteLine($" - It does");
                    return full;
                }
                Console.WriteLine($" - It does not");
            }

            return null;
        }

        private static string GetConfigFilePath() {
            string path = FindConfigFile();
            if (path == null)
                throw new FileLoadException(CONFIG_FILE_NAME);

            return path;
        }

        private static void Load() {
            if (file != null)
                return;
            file = new IniFile();
            file.Load(GetConfigFilePath());
        }

        public static IniSection GetSection(string section) {
            return Ini[section];
        }

        public static IniValue GetValue(string section, string key) {
            return GetSection(section)[key];
        }

        public static IniValue GetValue<T>(ConfigKey<T> key) {
            return GetValue(key.section, key.key);
        }

    }

    public struct ConfigKey<T> {
        public string section;
        public string key;

        private Func<string, T> builder;

        public ConfigKey(string section, string key, Func<string, T> builder) {
            this.section = section;
            this.key = key;
            this.builder = builder;
        }

        public readonly T Get() {
            if (builder == null)
                throw new Exception();

            return builder(GlobalConfig.GetValue(this).GetString(false, false));
        }

        public static implicit operator T(ConfigKey<T> key) {
            return key.Get();
        }

        public override string ToString() {
            return Get().ToString();
        }
    }

    public static class ConfigKeys {
        private static readonly string SECTION_KEY = "Discord.net";
        public static readonly ConfigKey<string> TOKEN = new(SECTION_KEY, "Token", x => x);
        public static readonly ConfigKey<ulong> LOG_CHANNEL_ID = new(SECTION_KEY, "LogChannel", ulong.Parse);
        public static readonly ConfigKey<ulong> TEST_CHANNEL_ID = new(SECTION_KEY, "TestChannel", ulong.Parse);
        public static readonly ConfigKey<ulong> GUILD_ID = new(SECTION_KEY, "Guild", ulong.Parse);


        public static class Cache {
            private static readonly string SECTION_KEY = "Cache";
            public static readonly ConfigKey<string> CACHE_DIR = new(SECTION_KEY, "RootDir",  x=> x);
            public static readonly ConfigKey<string> DB_FILE = new(SECTION_KEY, "Database",  x=> x);
        }

        public static class Emoji {
            private static readonly string SECTION_KEY = "Emoji";
            public static readonly ConfigKey<int> VOTE_THRESHOLD = new(SECTION_KEY, "Threshold", int.Parse);

            public static readonly ConfigKey<ulong> SUGGESTION_CHANNEL_ID =
                new(SECTION_KEY, "SuggestionChannelId", ulong.Parse);
        }

        public static class Web {
            private static readonly string SECTION_KEY = "Web";
            public static readonly ConfigKey<int> PORT = new(SECTION_KEY, "Port", int.Parse);
        }
    }

}