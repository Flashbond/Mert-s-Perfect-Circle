using Colossal.Json;
using MertsToolBox.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MertsToolBox.Utulities.Preset
{
    public static class MertToolPresetStorage
    {
        private static string PresetRoot =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                    .Replace("AppData\\Local", "AppData\\LocalLow"),
                "Colossal Order",
                "Cities Skylines II",
                "ModsData",
                "MertsToolBox",
                "Presets"
            );

        private static string GetToolFilePath(string toolId)
        {
            Directory.CreateDirectory(PresetRoot);
            return Path.Combine(PresetRoot, $"{toolId}.json");
        }

        public static void SavePreset(MertToolPreset preset)
        {
            if (preset == null || string.IsNullOrWhiteSpace(preset.ToolId))
                return;

            string path = GetToolFilePath(preset.ToolId);
            MertToolPresetFile file = LoadFile(preset.ToolId);

            file.Presets.RemoveAll(p =>
                p.ToolId == preset.ToolId &&
                p.PrefabName == preset.PrefabName &&
                p.DisplayName == preset.DisplayName);

            file.Presets.Add(preset);

            File.WriteAllText(path, JSON.Dump(file, 0));
        }

        public static List<MertToolPreset> LoadPresets(string toolId, string prefabName)
        {
            MertToolPresetFile file = LoadFile(toolId);

            return file.Presets
                .Where(p =>
                    p.ToolId == toolId &&
                    string.Equals(p.PrefabName, prefabName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.DisplayName)
                .ToList();
        }

        private static MertToolPresetFile LoadFile(string toolId)
        {
            string path = GetToolFilePath(toolId);

            if (!File.Exists(path))
                return new MertToolPresetFile();

            try
            {
                string jsonText = File.ReadAllText(path);
                Variant json = JSON.Load(jsonText);

                return JSON.MakeInto<MertToolPresetFile>(json)
                       ?? new MertToolPresetFile();
            }
            catch (Exception e)
            {
                ModRuntime.Warn($"[Preset] Failed to read preset file: {e.Message}");
                return new MertToolPresetFile();
            }
        }
        public static bool DeletePreset(string toolId, string prefabName, string displayName)
        {
            if (string.IsNullOrWhiteSpace(toolId) ||
                string.IsNullOrWhiteSpace(prefabName) ||
                string.IsNullOrWhiteSpace(displayName))
                return false;

            string path = GetToolFilePath(toolId);
            MertToolPresetFile file = LoadFile(toolId);

            int removed = file.Presets.RemoveAll(p =>
                p.ToolId == toolId &&
                string.Equals(p.PrefabName, prefabName, StringComparison.OrdinalIgnoreCase) &&
                p.DisplayName == displayName);

            if (removed <= 0)
                return false;

            File.WriteAllText(path, JSON.Dump(file, 0));
            return true;
        }
    }
}