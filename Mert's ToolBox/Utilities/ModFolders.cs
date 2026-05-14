using Colossal.PSI.Environment;
using System.IO;

namespace MertsToolBox.Utilities
{
    internal static class ModFolders
    {
        private const string ModName = "MertsToolBox";

        internal static string ContentFolder { get; }
        internal static string SettingsFolder { get; }
        internal static string TempFolder { get; }

        internal static string PresetsFolder { get; }
        internal static string LogsFolder { get; }

        static ModFolders()
        {
            ContentFolder = Path.Combine(
                EnvPath.kUserDataPath,
                "ModsData",
                ModName
            );

            SettingsFolder = Path.Combine(
                EnvPath.kUserDataPath,
                "ModsSettings",
                ModName,
                ModName
            );

            TempFolder = Path.Combine(
                EnvPath.kTempDataPath,
                ModName
            );

            PresetsFolder = Path.Combine(
                ContentFolder,
                "Presets"
            );

            LogsFolder = Path.Combine(
                ContentFolder,
                "Logs"
            );

            // Directory.CreateDirectory(ContentFolder);
            // Directory.CreateDirectory(SettingsFolder);
            // Directory.CreateDirectory(TempFolder);
            Directory.CreateDirectory(PresetsFolder);
            Directory.CreateDirectory(LogsFolder);
        }
    }
}