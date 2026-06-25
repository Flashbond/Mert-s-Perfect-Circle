using Colossal.Entities;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using MertsToolBox.Management;
using Unity.Collections;
using Unity.Entities;

namespace MertsToolBox.Systems
{

    public partial class HelixFakeGroundSystem : SystemBase
    {
        private EntityQuery m_TempElevatedEdgesQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            RequireForUpdate<Temp>();

            // İçinde "Game.Net.Elevation" barındıran tüm geçici yolları yakalıyoruz
            m_TempElevatedEdgesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] {
                    ComponentType.ReadOnly<Temp>(),
                    ComponentType.ReadOnly<Edge>(),
                    ComponentType.ReadOnly<Game.Net.Elevation>() // Hedefimiz!
                }
            });
        }

        protected override void OnUpdate()
        {
            if (!MertToolState.HelixCleanupRequested) return;

            var entities = m_TempElevatedEdgesQuery.ToEntityArray(Allocator.TempJob);
            int i = 0;
            // Yakaladığımız tüm havadaki sarmal yolları "Zemin" moduna sokuyoruz
            for (i = 0; i < entities.Length; i++)
            {
                Entity edgeEntity = entities[i];
               
                if (!EntityManager.TryGetComponent(edgeEntity, out PlaceableObjectData placeable))
                {
                    placeable = new PlaceableObjectData();
                    placeable.m_Flags |= Game.Objects.PlacementFlags.Attached;
                    EntityManager.SetComponentData(edgeEntity, placeable);
                
                }
              
           
            }
            entities.Dispose();
        }
    }
}