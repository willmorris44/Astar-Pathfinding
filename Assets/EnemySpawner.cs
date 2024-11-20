using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class EnemySpawner : MonoBehaviour {
    public int count; 

    void Start() {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<EnemyDirectory>().Build(entityManager);
        if (query.HasSingleton<EnemyDirectory>()) {
            var enemyPrefab = query.GetSingleton<EnemyDirectory>().monkeyPrefab;

            for (var i = 0; i < count; i++) {
                var x = Random.Range(-50f, 50f);
                var z = Random.Range(-50f, 50f);
                var position = transform.position + new Vector3(x, 0, z);

                Entity enemy = entityManager.Instantiate(enemyPrefab);
                LocalTransform localTransform = new LocalTransform {
                    Position = position,
                    Rotation = Quaternion.identity,
                    Scale = 100f
                };
                entityManager.SetComponentData(enemy, localTransform);
            }
        }
    }
}