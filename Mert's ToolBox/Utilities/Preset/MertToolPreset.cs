using System;
using System.Collections.Generic;

namespace MertsToolBox.Utilities.Preset
{
    [Serializable]
    public class MertToolPreset
    {
        public string ToolId;
        public string ToolName;
        public string PrefabName;
        public string DisplayName;
        public Dictionary<string, float> Values = new();
    }

    [Serializable]
    public class MertToolPresetFile
    {
        public List<MertToolPreset> Presets = new();
    }
}