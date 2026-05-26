using Colossal.IO.AssetDatabase;
using Game;
using Game.Modding;
using Game.SceneFlow;
using HarmonyLib;
using MertsToolBox.Settings;
using MertsToolBox.Systems;

namespace MertsToolBox.Core
{
    public class Mod : IMod
    {
        #region 1. CORE SYSTEMS & SETTINGS
        private Harmony m_Harmony;
        public static ToolBoxSettings settings;
        #endregion

        #region 2. MOD LIFECYCLE (IMod)

        /// <summary>
        /// Invoked by the game when the mod is loaded. Initializes settings, registers UI/localization, applies Harmony patches, and injects ECS tool systems.
        /// </summary>
        public void OnLoad(UpdateSystem updateSystem)
        {
            settings = new ToolBoxSettings(this);

            settings.RegisterInOptionsUI();
            settings.RegisterKeyBindings();

            AssetDatabase.global.LoadSettings(nameof(Settings), settings, new ToolBoxSettings(this));

            var lm = GameManager.instance.localizationManager;
            lm.AddSource("en-US", new LocaleEN(settings));

            m_Harmony = new Harmony("com.mert.toolbox");
            m_Harmony.PatchAll();

            updateSystem.UpdateAt<ShapeToolSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<HelixToolSystem>(SystemUpdatePhase.ToolUpdate);
            // updateSystem.UpdateAt<HelixNetDebugSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<SoftBlockToolSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<GridToolSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<MertToolBoxUISystem>(SystemUpdatePhase.UIUpdate);

            ModRuntime.Log("ToolBox loaded.");
        }

        /// <summary>
        /// Invoked by the game when the mod is disposed. Reverts Harmony patches and safely saves user settings before exit.
        /// </summary>
        public void OnDispose()
        {
            m_Harmony?.UnpatchAll("com.mert.toolbox");
            settings?.UnregisterInOptionsUI();
            settings = null;
        }
        #endregion
    }
}