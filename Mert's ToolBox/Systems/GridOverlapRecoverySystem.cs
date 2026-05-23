using Game;
using Game.Prefabs;
using Game.Tools;
using MertsToolBox.Management;
using Unity.Collections;
using Unity.Entities;

public partial class GridOverlapRecoverySystem : GameSystemBase
{
    private EntityQuery m_ToolErrorQuery;

    protected override void OnCreate()
    {
        m_ToolErrorQuery = GetEntityQuery(
            ComponentType.ReadOnly<ToolErrorData>()
        );
    }

    protected override void OnUpdate()
    {
        if (!MertToolState.TrimOverlappingEdges)
            return;

        using var entities =
            m_ToolErrorQuery.ToEntityArray(Allocator.Temp);

        foreach (Entity e in entities)
        {
            ToolErrorData data =
                EntityManager.GetComponentData<ToolErrorData>(e);

            if (data.m_Error != ErrorType.OverlapExisting)
                continue;

            MertToolState.PendingOverlapTrimRebuild = true;
            return;
        }
    }
}