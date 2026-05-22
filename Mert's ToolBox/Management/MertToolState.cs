using Game.Prefabs;
using Unity.Entities;

namespace MertsToolBox.Management
{
    public enum ToolExitMode
    {
        None,
        UserSelectionClose,
        RestoreFromPlacement,
        VanillaToolbarClear
    }

    public static class MertToolState
    {
        public static MertBaseToolSystem ActiveTool { get; set; }

        public static NetPrefab LastResolvedRoadPrefab { get; set; }
        public static Entity LastResolvedCategory { get; set; } = Entity.Null;

        public static NetPrefab LaunchRoadPrefab { get; set; }
        public static Entity LaunchCategory { get; set; } = Entity.Null;

        public static bool ControlledSelectAssetReplay { get; set; }
        public static bool ControlledSelectCategoryReplay { get; set; }
        public static bool ControlledClearSelectionReplay { get; set; }
        public static bool SuppressToolChangedDuringColdstart { get; set; }
        public static bool SuppressToolbarCaptureDuringColdstart { get; set; }
        public static bool HasReleasedStaleObjectToolThisFrame { get; set; }
        public static bool HelixCleanupRequested { get; set; } = false;

        public static bool ActiveHelixUsesPierLikePrefab;
        public static void CaptureLaunchContext(
            NetPrefab road,
            Entity category)
        {
            LaunchRoadPrefab = road;
            LaunchCategory = category;
        }

        public static void CaptureResolvedRoadContext(
            NetPrefab road,
            Entity category)
        {
            if (road != null)
                LastResolvedRoadPrefab = road;

            if (category != Entity.Null)
                LastResolvedCategory = category;
        }

        public static void ClearLaunchContext()
        {
            LaunchRoadPrefab = null;
            LaunchCategory = Entity.Null;
        }

        public static void ClearControlledReplayFlags()
        {
            ControlledSelectAssetReplay = false;
            ControlledSelectCategoryReplay = false;
            ControlledClearSelectionReplay = false;
        }
    }
}