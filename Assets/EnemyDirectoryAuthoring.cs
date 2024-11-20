using Unity.Entities;
using UnityEngine;

public class EnemyDirectoryAuthoring : MonoBehaviour {
    public GameObject monkeyPrefab;

    class Baker : Baker<EnemyDirectoryAuthoring> {
        public override void Bake(EnemyDirectoryAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new EnemyDirectory {
                monkeyPrefab = GetEntity(authoring.monkeyPrefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}

public struct EnemyDirectory : IComponentData {
    public Unity.Entities.Entity monkeyPrefab;
}