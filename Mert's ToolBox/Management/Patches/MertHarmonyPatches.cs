using Game.Prefabs;
using Game.UI.InGame;
using HarmonyLib;
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace MertsToolBox.Management.Patches
{
    [HarmonyPatch(typeof(ToolbarUISystem), "SelectAsset", new Type[] { typeof(Entity), typeof(bool) })]
    public static class ToolbarUISystem_SelectAsset_HandoffPatch
    {
        public static void Prefix(ref Entity assetEntity, ref bool updateTool)
        {
            if (assetEntity == Entity.Null)
                return;

            if (MertToolState.SuppressUiMemoryCapture)
                return;

            if (MertToolState.SuppressLiveUiCapture)
                return;

            bool toolOpen = MertToolbarHandoffMemory.IsAnyCustomToolOpen();

            bool mertContextActive =
                toolOpen ||
                MertToolState.PendingRestore ||
                MertToolState.ToolbarNavigationInProgress;

            if (!mertContextActive)
                return;

            if (!MertToolbarHandoffMemory.IsSupportedNetPrefab(assetEntity, out var netPrefab))
                return;

            MertToolState.LiveUiRoadPrefab = netPrefab;
            MertToolState.LastResolvedRoadPrefab = netPrefab;
            MertToolState.BlockRoadPrefabFallbackUntilNextRealSelection = false;

            if (MertToolbarHandoffMemory.TryResolveCategoryFromAsset(assetEntity, out var category))
            {
                MertToolState.LiveUiCategory = category;
                MertToolState.LastResolvedCategory = category;

                if (MertToolbarHandoffMemory.TryResolveMenuFromCategory(category, out var menu))
                {
                    MertToolState.LiveUiMenu = menu;
                    MertToolState.RememberSelectionForMenu(menu, category, netPrefab);
                }
            }

            if (!toolOpen)
                return;

            if (MertToolState.TabHandoffActive)
                MertToolState.ClearTabHandoff();

            MertToolState.ClearToolbarNavigation();

            MertToolState.OnToolAbortedByUI?.Invoke(ToolExitMode.UserSelectionClose);
        }
    }
    [HarmonyPatch(typeof(ToolbarUISystem), "SelectAssetCategory")]
    public static class ToolbarUISystem_SelectAssetCategory_HandoffPatch
    {
        public static void Prefix(ToolbarUISystem __instance, Entity assetCategory)
        {
            if (MertToolState.SuppressToolbarCaptureDuringColdstart)
                return;

            if (assetCategory == Entity.Null)
                return;

            if (MertToolState.SuppressCategoryCapture)
                return;

            if (MertToolState.SuppressUiAbortDuringRestore)
                return;

            if (!MertToolbarHandoffMemory.IsAnyCustomToolOpen())
                return;

            if (!MertToolbarHandoffMemory.IsSupportedNetCategory(assetCategory))
                return;

            MertToolState.LiveUiCategory = assetCategory;

            if (MertToolbarHandoffMemory.TryResolveMenuFromCategory(assetCategory, out var menu))
                MertToolState.LiveUiMenu = menu;

            MertToolState.BlockRoadPrefabFallbackUntilNextRealSelection = true;

            MertToolState.ClearTabHandoff();
            MertToolState.BeginToolbarNavigation(ToolExitMode.SilentCategoryClose);

            MertToolState.OnToolAbortedByUI?.Invoke(ToolExitMode.SilentCategoryClose);
        }
    }
    [HarmonyPatch(typeof(ToolbarUISystem), "SelectAssetMenu", new Type[] { typeof(Entity) })]
    public static class ToolbarUISystem_SelectAssetMenu_HandoffPatch
    {
        public static void Prefix(Entity assetMenu)
        {
            if (assetMenu == Entity.Null)
                return;

            if (MertToolState.SuppressToolbarCaptureDuringColdstart)
                return;

            if (MertToolState.SuppressUiAbortDuringRestore)
                return;

            bool toolOpen = MertToolbarHandoffMemory.IsAnyCustomToolOpen();
            bool pendingRestore = MertToolState.PendingRestore;
            bool hasLaunchContext =
                MertToolState.LaunchRoadPrefab != null ||
                MertToolState.LaunchCategory != Entity.Null;

            if (!toolOpen && !pendingRestore && !hasLaunchContext)
                return;

            MertToolState.LiveUiMenu = assetMenu;
            MertToolState.BlockRoadPrefabFallbackUntilNextRealSelection = true;

            MertToolState.ClearPendingRestore();
            MertToolState.ClearTabHandoff();
            MertToolState.BeginToolbarNavigation(ToolExitMode.SilentMenuClose);
            MertToolState.OnToolAbortedByUI?.Invoke(ToolExitMode.SilentMenuClose);
        }
    }
    [HarmonyPatch(typeof(ToolbarUISystem), "Apply",
        new Type[]
        {
        typeof(List<Entity>),
        typeof(List<Entity>),
        typeof(Entity),
        typeof(Entity),
        typeof(Entity),
        typeof(bool)
        })]
    public static class ToolbarUISystem_Apply_HandoffPatch
    {
        public static void Prefix(
            ToolbarUISystem __instance,
            List<Entity> themes,
            List<Entity> packs,
            ref Entity assetMenuEntity,
            ref Entity assetCategoryEntity,
            ref Entity assetEntity,
            ref bool updateTool)
        {
            bool hasMenuNavigation =
                MertToolState.ToolbarNavigationInProgress &&
                MertToolState.ToolbarNavigationMode == ToolExitMode.SilentMenuClose;

            if (!hasMenuNavigation && !MertToolState.PendingRestore)
                return;

            if (hasMenuNavigation && assetMenuEntity != Entity.Null)
            {
                bool incomingIsNull = assetEntity == Entity.Null;
                bool incomingIsSupportedNet =
                    !incomingIsNull &&
                    MertToolbarHandoffMemory.IsSupportedNetPrefab(assetEntity, out _);

                bool incomingIsOurStamp =
                    !incomingIsNull &&
                    MertToolbarHandoffMemory.IsCurrentStampAsset(assetEntity);

                // Mixed menus: train stations, electric assets, depots, etc.
                // If vanilla gives us a real unsupported asset, do not hijack it.
                if (!incomingIsNull && !incomingIsSupportedNet && !incomingIsOurStamp)
                {
                    MertToolState.ClearToolbarNavigation();
                    MertToolState.ClearPendingRestore();
                    return;
                }

                if (MertToolState.TryGetSelectionForMenu(
                assetMenuEntity,
                out Entity rememberedCategory,
                out NetPrefab rememberedRoad) &&
                rememberedRoad != null &&
                MertToolbarHandoffMemory.TryResolveEntity(
                rememberedRoad,
                out Entity rememberedRoadEntity))
                {
                    if (!MertToolbarHandoffMemory.TryResolveCategoryFromAsset(
                            rememberedRoadEntity,
                            out Entity realRememberedCategory))
                    {
                        MertToolState.ClearToolbarNavigation();
                        MertToolState.ClearPendingRestore();
                        return;
                    }

                    if (realRememberedCategory != rememberedCategory)
                    {
                        MertToolState.ClearToolbarNavigation();
                        MertToolState.ClearPendingRestore();
                        return;
                    }

                    if (assetCategoryEntity != Entity.Null &&
                        assetCategoryEntity != rememberedCategory)
                    {
                        MertToolState.ClearToolbarNavigation();
                        MertToolState.ClearPendingRestore();
                        return;
                    }

                    if (assetEntity != rememberedRoadEntity)
                    {
                        assetCategoryEntity = rememberedCategory;
                        assetEntity = rememberedRoadEntity;
                        updateTool = true;
                    }

                    MertToolState.ClearPendingRestore();
                    return;
                }

                // Supported navigation state, but no memory for this menu.
                // Let vanilla continue.
                MertToolState.ClearToolbarNavigation();
                MertToolState.ClearPendingRestore();
                return;
            }

            if (!MertToolState.PendingRestore)
                return;

            if (MertToolState.ToolbarNavigationInProgress)
                return;

            if (assetCategoryEntity == Entity.Null)
                return;

            if (assetCategoryEntity != MertToolState.PendingRestoreCategory)
                return;

            if (!MertToolbarHandoffMemory.IsSupportedNetCategory(assetCategoryEntity))
                return;

            bool restoreIncomingIsNull = assetEntity == Entity.Null;
            bool restoreIncomingIsStamp = false;

            if (!restoreIncomingIsNull &&
                MertToolbarHandoffMemory.TryResolvePrefab(assetEntity, out var incomingPrefab))
            {
                restoreIncomingIsStamp =
                    MertToolbarHandoffMemory.IsCurrentStamp(incomingPrefab);
            }

            if (!restoreIncomingIsNull && !restoreIncomingIsStamp)
                return;

            if (MertToolState.PendingRestoreRoad == null)
                return;

            if (!MertToolbarHandoffMemory.TryResolveEntity(
                    MertToolState.PendingRestoreRoad,
                    out Entity realRoadEntity))
                return;

            if (!MertToolbarHandoffMemory.TryResolveCategoryFromAsset(
                    realRoadEntity,
                    out Entity realRoadCategory))
                return;

            if (realRoadCategory != assetCategoryEntity)
                return;

            assetEntity = realRoadEntity;
            updateTool = true;
        }
    }
}
