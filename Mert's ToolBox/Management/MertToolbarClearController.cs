using Game.Prefabs;
using Game.UI.InGame;
using HarmonyLib;
using MertsToolBox.Management;
using System;
using System.Reflection;
using Unity.Entities;
using UnityEngine.InputSystem;

public static class MertToolbarClearController
{
    private static readonly MethodInfo s_ClearAssetSelectionNoArg =
        AccessTools.Method(
            typeof(ToolbarUISystem),
            "ClearAssetSelection",
            Type.EmptyTypes);

    private static readonly MethodInfo s_ClearAssetSelectionBool =
        AccessTools.Method(
            typeof(ToolbarUISystem),
            "ClearAssetSelection",
            new[] { typeof(bool) });

    public static bool TryHandleClearSelection(
    ToolbarUISystem toolbar,
    bool? updateTool)
    {
        if (MertToolState.ControlledClearSelectionReplay)
            return true;

        if (MertToolState.ControlledSelectAssetReplay)
            return true;

        if (!MertToolbarHandoffMemory.IsAnyCustomToolOpen())
            return true;

        NetPrefab oldRoad = MertToolState.LaunchRoadPrefab;
        if (oldRoad == null)
            return true;

        if (!MertToolbarHandoffMemory.TryResolveEntity(
                oldRoad,
                out Entity oldRoadEntity))
            return true;

        bool skipClearReplay = IsEscapeClearNow();

        MertToolState.ActiveTool?.RequestDisable(
            ToolExitMode.VanillaToolbarClear);

        MertToolbarReflection.ReplaySelectAsset(
            toolbar,
            oldRoadEntity,
            true);

        if (skipClearReplay) return false;
        
        try
        {
            MertToolState.ControlledClearSelectionReplay = true;

            if (updateTool.HasValue)
            {
                s_ClearAssetSelectionBool?.Invoke(
                    toolbar,
                    new object[] { updateTool.Value });
            }
            else
            {
                s_ClearAssetSelectionNoArg?.Invoke(
                    toolbar,
                    Array.Empty<object>());
            }
        }
        finally
        {
            MertToolState.ControlledClearSelectionReplay = false;
        }

        return false;
    }
    private static bool IsEscapeClearNow()
    {
        return Keyboard.current != null &&
               Keyboard.current.escapeKey.isPressed;
    }
}