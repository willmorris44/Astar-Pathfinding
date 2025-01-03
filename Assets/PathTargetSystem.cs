using Pathfinding.ECS;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

struct PathTargetTag : IComponentData { }

public struct PathTargetComponent : IComponentData {
    public float checkCount;
}

[BurstCompile]
public partial struct PathTargetSystem : ISystem {

    [BurstCompile]
    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<DestinationPoint>();
    }

    public void OnUpdate(ref SystemState state) {
        var allTargets = TargetsManager.GetTargetPositions(Allocator.TempJob);

        var pathTargetJob = new PathTargetJob {
            targetPositions = allTargets
        };

        // Schedule the job and make sure to complete it before disposing
        state.Dependency = pathTargetJob.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();

        allTargets.Dispose();
    }
}

[BurstCompile]
[UpdateBefore(typeof(Pathfinding.ECS.AIMovementSystemGroup))]
public partial struct PathTargetJob : IJobEntity {
    [ReadOnly]
    public NativeArray<float3> targetPositions;

    void Execute(ref DestinationPoint destinationPoint, ref PathTargetComponent pathTargetComponent, in LocalTransform transform) {
        if (pathTargetComponent.checkCount > 100) {
            pathTargetComponent.checkCount = 0;
        } else {
            pathTargetComponent.checkCount += 1;
            return;
        }

        destinationPoint.destination = targetPositions[0];
    }
}