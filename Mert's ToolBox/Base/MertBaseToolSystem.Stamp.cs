using Colossal.Entities;
using Colossal.Mathematics;
using Game.Prefabs;
using Game.Tools;
using MertsToolBox.Core;
using MertsToolBox.Management;
using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MertsToolBox
{
    public abstract partial class MertBaseToolSystem
    {

        #region Fields & Constants
        private static bool s_RoadProfileDiscoveryCompleted;
        private static NetPrefab s_CachedSmallRoad;
        #endregion


        #region Initialization & Prebaking
        private void TryDiscoverRoadProfiles()
        {
            if (s_RoadProfileDiscoveryCompleted)
                return;

            EntityQuery query = EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<PrefabData>(),
                ComponentType.ReadOnly<NetData>());

            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            int discovered = 0;

            foreach (Entity entity in entities)
            {
                if (!m_PrefabSystem.TryGetPrefab<PrefabBase>(
                        entity,
                        out var prefab))
                {
                    continue;
                }

                if (prefab is not NetPrefab netPrefab)
                    continue;

                IsStandardRoadPrefab(netPrefab);
                discovered++;
            }

            if (discovered > 0)
                s_RoadProfileDiscoveryCompleted = true;
        }

        /// <summary>
        /// Creates a detached warmup stamp prefab that is never stored in the per-road registry.
        /// This isolates ObjectTool foundation warmup from real gameplay stamps.
        /// </summary>
        private AssetStampPrefab CreateSharedRuntimeStampPrefab()
        {
            var stamp = ScriptableObject.CreateInstance<AssetStampPrefab>();

            stamp.name = "MertsToolBox_RuntimeStamp";

            if (!stamp.Has<ObjectSubNets>())
                stamp.AddComponent<ObjectSubNets>();

            if (!stamp.Has<PlaceableObject>())
                stamp.AddComponent<PlaceableObject>();

            if (!stamp.Has<PlaceableNet>())
                stamp.AddComponent<PlaceableNet>();

            m_PrefabSystem.AddPrefab(stamp);

            return stamp;
        }

        /// <summary>
        /// Attempts to perform a late prebake using a real road prefab once the game data is loaded.
        /// Use vanilla "Small Road" as a stable warmup anchor.
        /// It is not treated as proof that all roads are fully loaded yet;
        /// only as a reliable signal that the vanilla road prefab set has started to appear.
        /// </summary>
        private void TryLatePrebakeWithRealRoad()
        {
            if (s_ObjectToolFoundationWarmed)
                return;

            if (!s_RoadProfileDiscoveryCompleted)
                TryDiscoverRoadProfiles();

            NetPrefab smallRoad = GetCachedSmallRoad();

            if (smallRoad == null)
                return;

            if (!EnsureSharedRuntimeStamp())
                return;

            PrepareSharedStampForRoad(smallRoad);
            TryWarmObjectToolPreviewFoundationOnce();
        }

        private void PrepareSharedStampForRoad(NetPrefab roadPrefab)
        {
            if (!s_SharedRuntimeStamp.TryGet<ObjectSubNets>(
                out var subNets) ||
                subNets == null)
            {
                subNets =
                    s_SharedRuntimeStamp.AddComponent<ObjectSubNets>();
            }

            subNets.m_SubNets = new[]
            {
                new ObjectSubNetInfo
                {
                    m_NetPrefab = roadPrefab,
                    m_BezierCurve = new Bezier4x3(
                        new float3(0f,0f,0f),
                        new float3(2f,0f,0f),
                        new float3(4f,0f,0f),
                        new float3(6f,0f,0f)
                    ),
                    m_NodeIndex = new int2(0,1),
                    m_ParentMesh = new int2(-1,-1)
                }
            };

            s_SharedRuntimeStamp.asset?.MarkDirty();

            Entity entity =
                m_PrefabSystem.GetEntity(
                    s_SharedRuntimeStamp);

            if (entity != Entity.Null &&
                EntityManager.Exists(entity))
            {
                m_PrefabSystem.UpdatePrefab(
                    s_SharedRuntimeStamp,
                    entity);
            }
        }

        private void TryWarmObjectToolPreviewFoundationOnce()
        {
            if (s_ObjectToolFoundationWarmed)
                return;

            if (m_ObjectToolSystem == null ||
                m_ToolSystem == null)
                return;

            if (s_SharedRuntimeStamp == null)
                return;

            try
            {
                MertToolState.SuppressToolChangedDuringColdstart = true;
                MertToolState.SuppressToolbarCaptureDuringColdstart = true;

                if (m_ToolSystem.activeTool != m_ObjectToolSystem)
                {
                    m_ToolSystem.selected = Entity.Null;
                    m_ToolSystem.activeTool = m_ObjectToolSystem;
                }

                if (!m_ObjectToolSystem.TrySetPrefab(s_SharedRuntimeStamp))
                    return;

                m_ObjectToolSystem.InitializeRaycast();

                s_ObjectToolFoundationWarmed = true;
            }
            finally
            {
                MertToolState.SuppressToolbarCaptureDuringColdstart = false;
                MertToolState.SuppressToolChangedDuringColdstart = false;
            }
        }

        private NetPrefab GetCachedSmallRoad()
        {
            if (s_CachedSmallRoad != null)
                return s_CachedSmallRoad;

            EntityQuery query = EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<NetData>(),
                ComponentType.ReadOnly<PrefabData>());

            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var entity in entities)
            {
                if (!m_PrefabSystem.TryGetPrefab<PrefabBase>(entity, out var prefab))
                {
                    continue;
                }

                if (prefab is NetPrefab net && string.Equals(net.name, "Small Road", StringComparison.OrdinalIgnoreCase))
                {
                    s_CachedSmallRoad = net;
                    return net;
                }
            }

            return null;
        }
        /// <summary>
        /// Prepares the context and queues a preview rebuild when the tool is enabled.
        /// </summary>
        private void PrimeAndShowPreviewOnEnable()
        {
            EnsureContextRecipeReady();

            if (!TryGetSharedRuntimeStamp(out var stamp))
            {
                ModRuntime.Warn("[ROAD-STAMP] Shared runtime stamp missing");
                return;
            }

            m_RuntimeStamp = stamp;

            m_PendingCreateShape = false;

            QueuePreviewRebuild();
        }
        #endregion

        #region Context & Metadata Configuration
        /// <summary>
        /// Ensures the baseline context recipe and placement flags are prepared.
        /// </summary>
        private void EnsureContextRecipeReady()
        {
            if (m_ContextRecipeReady)
                return;

            PrepareManualIntersectionLikeContextRecipe();
            m_ContextRecipeReady = true;
        }

        /// <summary>
        /// Prepares the foundational placement flags resembling manual intersection creation.
        /// </summary>
        private void PrepareManualIntersectionLikeContextRecipe()
        {
            m_DesiredPlacementFlags = Game.Objects.PlacementFlags.RoadEdge | Game.Objects.PlacementFlags.RoadSide;
        }

        /// <summary>
        /// Wraps the application of snapping metadata to the target entity in a safe try-catch block.
        /// </summary>
        private void PrepareRuntimeStampPlacementMetadata(Entity targetEntity)
        {
            try
            {
                ApplyStampSnapMetadataToEntity(targetEntity);
            }
            catch (Exception e)
            {
                ModRuntime.Warn($"PrepareRuntimeStampSnapMetadata error: {e.Message}");
            }
        }

        /// <summary>
        /// Applies detailed snapping metadata and placement flags to the ECS entity representing the stamp.
        /// </summary>
        private bool ApplyStampSnapMetadataToEntity(Entity targetEntity)
        {
            bool changed = false;

            if (targetEntity == Entity.Null || !EntityManager.Exists(targetEntity))
                return false;

            if (!EntityManager.TryGetComponent(targetEntity, out PlaceableObjectData placeable))
            {
                placeable = new PlaceableObjectData();
                EntityManager.AddComponentData(targetEntity, placeable);
                changed = true;
            }

            var oldFlags = placeable.m_Flags;

            placeable.m_Flags |= m_DesiredPlacementFlags;

            bool shouldTouchSnapMetadata = WritesSubNetSnapMetadata;

            bool isAnySnapActive = shouldTouchSnapMetadata && IsAnyGlobalSnapEnabled();

            if (shouldTouchSnapMetadata)
            {
                if (isAnySnapActive)
                    placeable.m_Flags |= Game.Objects.PlacementFlags.SubNetSnap;
                else
                    placeable.m_Flags &= ~Game.Objects.PlacementFlags.SubNetSnap;           
            }

            if (oldFlags != placeable.m_Flags || changed)
            {
                EntityManager.SetComponentData(targetEntity, placeable);
                changed = true;
            }

            if (EntityManager.HasBuffer<Game.Prefabs.SubNet>(targetEntity))
            {
                bool2 dynamicSubNetSnapping = new(isAnySnapActive, isAnySnapActive);

                DynamicBuffer<Game.Prefabs.SubNet> subNets = EntityManager.GetBuffer<Game.Prefabs.SubNet>(targetEntity);

                CompositionFlags suppressionFlags = BuildCommonSuppressionFlags();

                for (int i = 0; i < subNets.Length; i++)
                {
                    Game.Prefabs.SubNet subNet = subNets[i];

                    if (shouldTouchSnapMetadata)
                    {
                        if (subNet.m_Snapping.x != dynamicSubNetSnapping.x ||
                            subNet.m_Snapping.y != dynamicSubNetSnapping.y)
                        {
                            subNet.m_Snapping = dynamicSubNetSnapping;
                            changed = true;
                        }
                    }

                    if (suppressionFlags != default)
                    {
                        subNet.m_Upgrades.m_Left |= suppressionFlags.m_Left;
                        subNet.m_Upgrades.m_Right |= suppressionFlags.m_Right;

                        changed = true;
                    }

                    subNets[i] = subNet;
                }
            }
            return changed;
        }
        #endregion

        #region Mutation & Shape Generation
        /// <summary>
        /// Handles the execution of the queued shape creation process safely.
        /// </summary>
        private void HandleExecuteCreateShape()
        {
            if (!m_PendingCreateShape)
                return;

            if (m_IsCreatingShape)
                return;

            m_PendingCreateShape = false;
            m_IsCreatingShape = true;

            try
            {
                TryCommitRuntimeStampMutation();
            }
            finally
            {
                m_IsCreatingShape = false;
            }
        }

        /// <summary>
        /// Commits the newly generated geometry to the runtime stamp and updates the prefab system.
        /// </summary>
        private bool TryCommitRuntimeStampMutation()
        {
            if (m_RuntimeStamp == null)
                return false;

            if (!TryMutateTargetStamp())
                return false;

            Entity entity = m_PrefabSystem.GetEntity(m_RuntimeStamp);

            if (entity != Entity.Null && EntityManager.Exists(entity))
            {
                m_PrefabSystem.UpdatePrefab(m_RuntimeStamp, entity);
            }

            MarkRuntimeStampChanged();
            m_PendingObjectToolHandoff = true;
            m_PendingHandoffStamp = m_RuntimeStamp;
            return true;
        }

        /// <summary>
        /// Validates whether the runtime stamp entity has been fully constructed with required geometry and network buffers.
        /// </summary>
        protected virtual bool IsRuntimeStampEntityReady(Entity entity)
        {
            if (entity == Entity.Null || !EntityManager.Exists(entity))
                return false;

            if (!EntityManager.TryGetComponent(entity, out ObjectGeometryData geom))
                return false;

            if (geom.m_Size.x <= 0.1f || geom.m_Size.z <= 0.1f)
                return false;

            if (!EntityManager.HasBuffer<Game.Prefabs.SubNet>(entity))
                return false;

            DynamicBuffer<Game.Prefabs.SubNet> subNets = EntityManager.GetBuffer<Game.Prefabs.SubNet>(entity);
            if (subNets.Length == 0)
                return false;

            return true;
        }

        /// <summary>
        /// Retrieves and updates the current entity representation of the given stamp prefab.
        /// </summary>
        private bool TryResolveRuntimeStampEntity(AssetStampPrefab stamp, out Entity entity)
        {
            entity = Entity.Null;

            if (stamp == null) return false;

            entity = m_PrefabSystem.GetEntity(stamp);

            return entity != Entity.Null &&
                   EntityManager.Exists(entity);
        }
        #endregion

        #region Handoff & Tool Execution
        /// <summary>
        /// Processes a queued handoff operation, transferring the generated stamp to the object tool.
        /// </summary>
        private bool HandlePendingObjectToolHandoff()
        {
            if (!m_PendingObjectToolHandoff)
                return false;

            if (!TryResolvePendingHandoffEntity(out Entity refreshedEntity))
                return false;

            PrepareRuntimeStampPlacementMetadata(refreshedEntity);

            AssetStampPrefab stampToHandOff = m_PendingHandoffStamp;

            if (stampToHandOff == null)
                return false;

            ClearPendingHandoff();
            HandoffToObjectTool(stampToHandOff);
           
            return true;
        }

        /// <summary>
        /// Attempts to resolve and validate the pending handoff entity before transferring control.
        /// </summary>
        private bool TryResolvePendingHandoffEntity(out Entity refreshedEntity)
        {
            refreshedEntity = Entity.Null;

            if (m_PendingHandoffStamp == null)
            {
                ClearPendingHandoff();
                return false;
            }

            if (!TryResolveRuntimeStampEntity(m_PendingHandoffStamp, out refreshedEntity)) return false;
  
            if (!IsRuntimeStampEntityReady(refreshedEntity))
                return false;

            return true;
        }

        /// <summary>
        /// Hands off the constructed asset stamp to the active object tool system for preview and placement.
        /// </summary>
        protected void HandoffToObjectTool(AssetStampPrefab stamp)
        {
            if (m_ObjectToolSystem == null || m_ToolSystem == null || stamp == null)
                return;

            try
            {
                bool toolSwitchNeeded = m_ToolSystem.activeTool != m_ObjectToolSystem;
                bool stampChanged = m_LastHandedOffStamp != stamp;
                bool geometryChanged = m_LastHandedOffRevision != m_RuntimeStampRevision;

                if (!toolSwitchNeeded && !stampChanged && !geometryChanged)
                    return;

                if (toolSwitchNeeded)
                {
                    m_ToolSystem.selected = Entity.Null;
                    m_ToolSystem.activeTool = m_ObjectToolSystem;
                }

                if (stampChanged || geometryChanged)
                {
                    ModRuntime.TrySetField(m_ObjectToolSystem, "m_SelectedPrefab", null);
                    ModRuntime.TrySetField(m_ObjectToolSystem, "m_Prefab", null);

                    bool setOk = m_ObjectToolSystem.TrySetPrefab(stamp);
                    if (!setOk)
                        return;

                    m_ObjectToolSystem.InitializeRaycast();

                    m_LastHandedOffStamp = stamp;
                    m_LastHandedOffRevision = m_RuntimeStampRevision;
                }

                if (m_ObjectToolSystem.mode != ObjectToolSystem.Mode.Stamp)
                    m_ObjectToolSystem.mode = ObjectToolSystem.Mode.Stamp;

                if (OverridesObjectToolSnapMask)
                    ApplySnapMaskToActiveTool();
            }
            catch (Exception e)
            {
                ModRuntime.Warn($"HandoffToObjectTool error: {e}");
            }
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Clears out any pending handoff flags and cached stamp data.
        /// </summary>
        private void ClearPendingHandoff()
        {
            m_PendingObjectToolHandoff = false;
            m_PendingHandoffStamp = null;
        }
        private void MarkRuntimeStampChanged()
        {
            m_RuntimeStampRevision++;
        }
        private bool IsAnyGlobalSnapEnabled()
        {
            return MertToolState.SnapGeometryEnabled;
        }
        #endregion
    }
}