using Game;
using Game.Prefabs;
using Game.Tools;
using MertsToolBox.Management;
using Unity.Collections;
using Unity.Entities;

namespace MertsToolBox.Systems
{
    public partial class HelixToolErrorFlagSystem : GameSystemBase
    {
        private EntityQuery m_ToolErrorPrefabQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_ToolErrorPrefabQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<ToolErrorData>(),
                    ComponentType.ReadOnly<NotificationIconData>()
                }
            });

            RequireForUpdate(m_ToolErrorPrefabQuery);
        }

        protected override void OnUpdate()
        {
            if (!MertToolState.HelixCleanupRequested)
                return;

            bool isPier = MertToolState.ActiveHelixUsesPierLikePrefab;

            using var prefabs = m_ToolErrorPrefabQuery.ToEntityArray(Allocator.Temp);

            foreach (Entity entity in prefabs)
            {
                ToolErrorData data = EntityManager.GetComponentData<ToolErrorData>(entity);

                if (!ShouldSuppress(data.m_Error, isPier))
                    continue;

                ToolErrorFlags desired =
                    data.m_Flags |
                    ToolErrorFlags.DisableInGame |
                    ToolErrorFlags.DisableInEditor;

                if (desired == data.m_Flags)
                    continue;

                data.m_Flags = desired;
                EntityManager.SetComponentData(entity, data);
            }
        }

        private static bool ShouldSuppress(ErrorType error, bool isPier)
        {
            return error == ErrorType.OverlapExisting ||
                   error == ErrorType.LowElevation ||
                   error == ErrorType.NotEnoughClearance ||
                   (isPier && error == ErrorType.SteepSlope);
        }
    }
}