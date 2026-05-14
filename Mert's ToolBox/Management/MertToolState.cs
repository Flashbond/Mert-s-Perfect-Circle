using Game.Prefabs;
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace MertsToolBox.Management
{
    #region Enums
    public enum ToolExitMode
    {
        None,
        SilentCategoryClose,
        SilentMenuClose,
        UserSelectionClose,
        RestoreFromEscape,
        RestoreFromPlacement
    }
    #endregion

    public static class MertToolState
    {
        #region Events & Global Context States
        public static readonly Dictionary<Entity, (Entity category, NetPrefab road)> LastSelectionByMenu = new();

        public static void RememberSelectionForMenu(Entity menu, Entity category, NetPrefab road)
        {
            if (menu == Entity.Null || category == Entity.Null || road == null)
                return;

            LastSelectionByMenu[menu] = (category, road);
        }

        public static bool TryGetSelectionForMenu(Entity menu, out Entity category, out NetPrefab road)
        {
            category = Entity.Null;
            road = null;

            if (menu == Entity.Null)
                return false;

            if (!LastSelectionByMenu.TryGetValue(menu, out var value))
                return false;

            category = value.category;
            road = value.road;

            return category != Entity.Null && road != null;
        }
        public static readonly HashSet<Entity> SupportedToolboxMenus = new();

        public static bool IsToolboxSupportedMenu(Entity menu)
        {
            return menu != Entity.Null && SupportedToolboxMenus.Contains(menu);
        }

        public static void RememberSupportedMenu(Entity menu)
        {
            if (menu != Entity.Null)
                SupportedToolboxMenus.Add(menu);
        }
        public static Action<ToolExitMode> OnToolAbortedByUI;
        public static NetPrefab LastResolvedRoadPrefab { get; set; } = null;
        public static NetPrefab LaunchRoadPrefab { get; set; } = null;
        public static Entity LaunchCategory { get; set; } = Entity.Null;
        public static NetPrefab LiveUiRoadPrefab { get; set; } = null;
        public static Entity LiveUiCategory { get; set; } = Entity.Null;
        public static Entity LastResolvedCategory { get; set; } = Entity.Null;

        public static Entity LiveUiMenu = Entity.Null;

        public static bool ToolbarNavigationInProgress = false;

        public static ToolExitMode ToolbarNavigationMode = ToolExitMode.None;

        private static readonly Dictionary<Entity, NetPrefab> s_LastRoadPerCategory = new();
        public static MertBaseToolSystem ActiveTool { get; set; } = null;
        #endregion

        #region Suppression & Control Flags
        public static bool HelixCleanupRequested { get; set; } = false;
        public static bool HelixPierElevationBypassRequested = false;
        public static bool BlockRoadPrefabFallbackUntilNextRealSelection { get; set; } = false;
        public static bool SuppressUiMemoryCapture { get; set; } = false;
        public static bool SuppressCategoryCapture { get; set; } = false;
        public static bool HasReleasedStaleObjectToolThisFrame { get; set; } = false;
        public static bool UserJustChangedAssetCategory { get; set; } = false;
        public static bool UserJustChangedAssetMenu { get; set; } = false;
        public static bool SuppressUiAbortDuringRestore { get; set; } = false;
        public static bool SuppressLiveUiCapture { get; set; } = false;
        public static bool SuppressToolChangedDuringColdstart { get; set; } = false;
        public static bool SuppressToolbarCaptureDuringColdstart { get; set; } = false;
        #endregion

        #region Handoff & Restore States
        public static bool TabHandoffActive { get; set; } = false;
        public static NetPrefab TabHandoffFromRoad { get; set; } = null;
        public static Entity TabHandoffFromCategory { get; set; } = Entity.Null;
        public static Entity TabHandoffToCategory { get; set; } = Entity.Null;
        public static bool PendingRestore { get; private set; } = false;
        public static ToolExitMode PendingRestoreMode { get; private set; } = ToolExitMode.None;
        public static NetPrefab PendingRestoreRoad { get; private set; } = null;
        public static Entity PendingRestoreCategory { get; private set; } = Entity.Null;  
        #endregion

        #region Context Management
        /// <summary>
        /// Caches the initial road and category context when a tool is launched.
        /// </summary>
        public static void CaptureLaunchContext(NetPrefab road, Entity category)
        {
            LaunchRoadPrefab = road;
            LaunchCategory = category;
        }

        /// <summary>
        /// Stores the most recently selected road prefab for a specific asset category.
        /// </summary>
        public static void RememberRoadForCategory(Entity category, NetPrefab road)
        {
            if (category == Entity.Null || road == null)
                return;

            s_LastRoadPerCategory[category] = road;
        }
        #endregion

        #region Restore Queue Management
        /// <summary>
        /// Queues a specific exit mode and context for restoration on the next update frame.
        /// </summary>
        public static void QueueRestore(ToolExitMode mode, NetPrefab road, Entity category)
        {
            PendingRestore = true;
            PendingRestoreMode = mode;
            PendingRestoreRoad = road;
            PendingRestoreCategory = category;
        }

        /// <summary>
        /// Clears any pending restore state and resets associated variables.
        /// </summary>
        public static void ClearPendingRestore()
        {
            PendingRestore = false;
            PendingRestoreMode = ToolExitMode.None;
            PendingRestoreRoad = null;
            PendingRestoreCategory = Entity.Null;
        }
        #endregion

        #region Tab Handoff Management
        /// <summary>
        /// Prepares the source context before initiating a UI tab handoff.
        /// </summary>
        public static void PrimeTabHandoffSource(NetPrefab fromRoad, Entity fromCategory)
        {
            TabHandoffFromRoad = fromRoad;
            TabHandoffFromCategory = fromCategory;
            TabHandoffToCategory = Entity.Null;
            TabHandoffActive = false;
        }

        /// <summary>
        /// Resets all variables related to the tab handoff process.
        /// </summary>
        public static void ClearTabHandoff()
        {
            TabHandoffActive = false;
            TabHandoffFromRoad = null;
            TabHandoffFromCategory = Entity.Null;
            TabHandoffToCategory = Entity.Null;
        }

        public static void BeginToolbarNavigation(ToolExitMode mode)
        {
            ToolbarNavigationInProgress = true;
            ToolbarNavigationMode = mode;

            UserJustChangedAssetMenu = mode == ToolExitMode.SilentMenuClose;
            UserJustChangedAssetCategory = mode == ToolExitMode.SilentCategoryClose;
        }

        public static void ClearToolbarNavigation()
        {
            ToolbarNavigationInProgress = false;
            ToolbarNavigationMode = ToolExitMode.None;

            UserJustChangedAssetMenu = false;
            UserJustChangedAssetCategory = false;
        }
        #endregion
    }
}