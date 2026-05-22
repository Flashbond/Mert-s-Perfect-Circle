using Game.UI.InGame;
using HarmonyLib;
using MertsToolBox.Management;
using System.Reflection;
using Unity.Entities;

public static class MertToolbarReflection
{
    private static readonly MethodInfo s_SelectAsset =
        AccessTools.Method(
            typeof(ToolbarUISystem),
            "SelectAsset",
            new[] { typeof(Entity), typeof(bool) });
    private static readonly MethodInfo s_SelectAssetCategory =
        AccessTools.Method(
            typeof(ToolbarUISystem),
            "SelectAssetCategory",
            new[] { typeof(Entity) });

    public static void ReplaySelectAsset(
        ToolbarUISystem toolbar,
        Entity assetEntity,
        bool updateTool)
    {
        if (toolbar == null ||
            assetEntity == Entity.Null ||
            s_SelectAsset == null)
            return;

        try
        {
            MertToolState.ControlledSelectAssetReplay = true;

            s_SelectAsset.Invoke(
                toolbar,
                new object[] { assetEntity, updateTool });
        }
        finally
        {
            MertToolState.ControlledSelectAssetReplay = false;
        }
    }
    public static void ReplaySelectAssetCategory(
    ToolbarUISystem toolbar,
    Entity category)
    {
        if (toolbar == null ||
            category == Entity.Null ||
            s_SelectAssetCategory == null)
            return;

        try
        {
            MertToolState.ControlledSelectCategoryReplay = true;

            s_SelectAssetCategory.Invoke(
                toolbar,
                new object[] { category });
        }
        finally
        {
            MertToolState.ControlledSelectCategoryReplay = false;
        }
    }
}