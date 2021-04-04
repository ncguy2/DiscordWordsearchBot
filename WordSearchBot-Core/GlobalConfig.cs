using System.IO;
using System.Linq;

namespace WordSearchBot.Core {
    public static class GlobalConfig {

        private static IniFile file;
        private static string CONFIG_FILE_NAME = "config.ini";

        private static string[] CONFIG_PATHS = new string[] {
            "~",
            "."
        };

        public static IniFile Ini {
            get {
                Load();
                return file;
            }
        }

        private static string FindConfigFile() {
            return CONFIG_PATHS.Select(prefix => prefix + "/" + CONFIG_FILE_NAME)
                               .FirstOrDefault(File.Exists);
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

        public static IniValue GetValue(ConfigKey key) {
            return GetValue(key.section, key.key);
        }

    }

    public struct ConfigKey {
        public string section;
        public string key;

        public ConfigKey(string section, string key) {
            this.section = section;
            this.key = key;
        }
    }

    public static class ConfigKeys {
        public static readonly ConfigKey TOKEN = new ConfigKey("Discord.net", "Token");
    }

}