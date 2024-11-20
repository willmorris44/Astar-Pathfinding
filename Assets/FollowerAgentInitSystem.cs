// This system is responsible for initializing a follower entity when it is first instantiated.
// It only runs once for each entity and then never runs again.
// It adds the ManagedState component because it's full of pointers and stuff and can't be added in the baker.

using Pathfinding.ECS;
using Pathfinding;
using Unity.Collections;
using Unity.Entities;


public partial class FollowerAgentInitSystem : SystemBase {
    protected override void OnUpdate() {
        Entities.ForEach((Unity.Entities.Entity entity, in ManagedStateOptionsData opts) => {
            // managed state is a managed component (class, not a struct).

            // Create the ManagedState managed component and add it with the entity manager
            ManagedState state = new ManagedState {
                enableLocalAvoidance = opts.EnableLocalAvoidance,
                pathfindingSettings = new PathRequestSettings {
                    graphMask = opts.PathRequestOpts.AffectedGraphsMask,
                    tagPenalties = new int[32],
                    traversableTags = opts.PathRequestOpts.TraversableTags,
                    traversalProvider = null // the default PathRequestSettings uses null
                },
                enableGravity = opts.EnableGravity,
                rvoSettings = opts.RvoAgentOpts,
                pathTracer = new PathTracer(Allocator.Persistent)
            };
            // Copy the values from tag penalties to the settings (they're different types)
            for (int i = 0; i < opts.PathRequestOpts.TagPenalties.Length; i++) {
                state.pathfindingSettings.tagPenalties[i] = opts.PathRequestOpts.TagPenalties[i];
            }
            EntityManager.AddComponentObject(entity, state);

            // Remove the ManagedStateOptionsData component so this system never runs again
            // for the current entity (it's not needed anymore anyway)
            EntityManager.RemoveComponent<ManagedStateOptionsData>(entity);
        }).WithStructuralChanges().WithoutBurst().Run();
    }
}