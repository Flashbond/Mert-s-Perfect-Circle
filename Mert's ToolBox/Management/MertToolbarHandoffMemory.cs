using System;
using Game.Prefabs;
using MertsToolBox.Systems;
using Unity.Entities;

namespace MertsToolBox.Management
{
    internal static class MertToolbarHandoffMemory
    {
        public static bool IsAnyCustomToolOpen()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return false;

            return IsEnabled<ShapeToolSystem>(world) ||
                   IsEnabled<HelixToolSystem>(world) ||
                   IsEnabled<SoftBlockToolSystem>(world) ||
                   IsEnabled<GridToolSystem>(world);
        }

        private static bool IsEnabled<T>(World world)
            where T : MertBaseToolSystem
        {
            var tool = world.GetExistingSystemManaged<T>();
            return tool != null && tool.ToolEnabled;
        }

        public static bool IsCurrentStamp(PrefabBase prefab)
        {
            if (prefab == null || string.IsNullOrEmpty(prefab.name))
                return false;

            return prefab.name.StartsWith("MertsToolBox_RoadStamp_", StringComparison.Ordinal) ||
                   prefab.name.StartsWith("MertsToolBox_WarmupStamp_", StringComparison.Ordinal) ||
                   prefab.name.StartsWith("MertsToolBox_SharedPrebakedStamp", StringComparison.Ordinal);
        }

        public static bool IsSupportedNetPrefab(
            Entity assetEntity,
            out NetPrefab netPrefab)
        {
            netPrefab = null;

            if (!TryResolvePrefab(assetEntity, out var prefab))
                return false;

            if (prefab is not NetPrefab net)
                return false;

            netPrefab = net;
            return true;
        }

        public static bool TryResolvePrefab(
            Entity entity,
            out PrefabBase prefab)
        {
            prefab = null;

            if (entity == Entity.Null)
                return false;

            var prefabSystem = GetPrefabSystem();
            if (prefabSystem == null)
                return false;

            return prefabSystem.TryGetPrefab(entity, out prefab);
        }

        public static bool TryResolveEntity(
            NetPrefab prefab,
            out Entity entity)
        {
            entity = Entity.Null;

            if (prefab == null)
                return false;

            var prefabSystem = GetPrefabSystem();
            if (prefabSystem == null)
                return false;

            entity = prefabSystem.GetEntity(prefab);
            return entity != Entity.Null;
        }

        private static PrefabSystem GetPrefabSystem()
        {
            return World.DefaultGameObjectInjectionWorld?
                .GetExistingSystemManaged<PrefabSystem>();
        }
    }
}