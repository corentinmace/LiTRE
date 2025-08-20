using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiTRE
{

    public class LiTRESettings
    {
        public byte menuLayout { get; set; } = 2;
        public string lastColorTablePath { get; set; } = "";
        public bool textEditorPreferHex { get; set; } = false;
        public int scriptEditorFormatPreference { get; set; } = 0;
        public bool renderSpawnables { get; set; } = true;
        public bool renderOverworlds { get; set; } = true;
        public bool renderWarps { get; set; } = true;
        public bool renderTriggers { get; set; } = true;
        public string exportPath { get; set; } = "";
        public string mapImportStarterPoint { get; set; } = "";
        public string openDefaultRom { get; set; } = "";
        public bool neverAskForOpening { get; set; } = false;
        public bool databasesPulled { get; set; } = false;
        public bool automaticallyCheckForUpdates { get; set; } = true;
		public bool automaticallyUpdateDBs { get; set; } = true;
        public bool useDecompNames { get; set; } = false;
        public string vscPath { get; set; } = "code";
    }

    public static class SettingsManager
    {
        public static LiTRESettings Settings { get; private set; }

        private static readonly string SettingsFile = Path.Combine(Program.LiTREDataPath, "userSettings.json");

        public static void Load()
        {
            AppLogger.Info("Loading app settings");
            if (File.Exists(SettingsFile))
            {
                string json = File.ReadAllText(SettingsFile);
                Settings = JsonConvert.DeserializeObject<LiTRESettings>(json);
            }
            else
            {
                Settings = new LiTRESettings();
                Save();
            }
        }

        public static void Save()
        {
            string json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
            File.WriteAllText(SettingsFile, json);
        }
    }
}
