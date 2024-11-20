// place this onto an agent to make it an agent that can be controlled and moved on A*PFP's nav mesh.

using Unity.Entities;
using UnityEngine;
using Pathfinding.ECS;
using Pathfinding;
using Unity.Mathematics;
using Pathfinding.Util;
using Unity.Collections;

public enum AgentMovePlaneType { XY, XZ, Rot };


// Note that TagPenalties won't show up on the editor - this needs to be set elsewhere
// This struct exists because PathRequestSettings is not blittable, so I had to make this instead.
[System.Serializable]
public struct PathRequestOptsData {
    public GraphMask AffectedGraphsMask;
    [HideInInspector]
    public FixedList128Bytes<int> TagPenalties; // 128 == 4 "bytes per int" * 32 "tags"
    public int TraversableTags;

    public static readonly PathRequestOptsData Default = new PathRequestOptsData {
        AffectedGraphsMask = GraphMask.everything,
        TagPenalties = new FixedList128Bytes<int>(),
        TraversableTags = -1
    };
}

[System.Serializable]
public struct ManagedStateOptionsData : IComponentData {
    // managed state
    public bool EnableLocalAvoidance;
    public PathRequestOptsData PathRequestOpts;
    public bool EnableGravity;
    public Pathfinding.ECS.RVO.RVOAgent RvoAgentOpts;

    public static readonly ManagedStateOptionsData Default = new ManagedStateOptionsData {
        EnableLocalAvoidance = true,
        PathRequestOpts = PathRequestOptsData.Default,
        EnableGravity = true,
        RvoAgentOpts = Pathfinding.ECS.RVO.RVOAgent.Default // rvo managed state (default RVO settings)
    };
}

public class FollowerAgentAuthoring : MonoBehaviour {
    // default destination
    public float3 DefaultDestination = float3.zero;

    // agent shape stuff
    public float AgentHeight = 2f;
    public float AgentRadius = 0.5f;

    // movement stuff
    public float RotationSpeed = 600f;
    public float MoveSpeed = 5f;
    public float MaxRotationSpeed = 720f;
    public float MaxOnSpotRotationSpeed = 720f;
    public float SlowdownTime = 0.5f;
    public float DesiredWallDistance = 0.5f;
    public bool AllowRotatingOnSpot = true;
    public float LeadInRadiusWhenApproachingDestination = 1f;
    public MovementPlaneSource MovePlaneSource = MovementPlaneSource.Graph;
    public float StopDistance = 0.2f;
    public float RotationSmoothing = 0f;
    public LayerMask GroundMask = -1;
    public bool IsStopped = false;

    // repath policy - added after 5.0.9
    public Pathfinding.ECS.AutoRepathPolicy RepathPolicy = Pathfinding.ECS.AutoRepathPolicy.Default;

    // orientation
    public OrientationMode AgentOrientation = OrientationMode.ZAxisForward;

    // movement plane
    public AgentMovePlaneType MovePlane = AgentMovePlaneType.XZ;

    // managed state
    public ManagedStateOptionsData ManagedOpts = ManagedStateOptionsData.Default;

    // todo - there is probably a better way to display these, but it's okay for now.
    [Tooltip("Corresponds with layers I think? Max count is 32.")]
    public System.Collections.Generic.List<int> TagPenalties = new System.Collections.Generic.List<int>(32);
}

public class FollowerAgentBaker : Baker<FollowerAgentAuthoring> {
    public override void Bake(FollowerAgentAuthoring authoring) {
        Unity.Entities.Entity agentEntity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponent<PathTargetComponent>(agentEntity);

        // add various required components
        AddComponent<MovementState>(agentEntity);
        AddComponent<MovementControl>(agentEntity); // i think these get set automatically
        AddComponent<SearchState>(agentEntity);
        AddComponent<MovementStatistics>(agentEntity); // does lastPos need to be set in initialize or spawn system?
        AddComponent<ResolvedMovement>(agentEntity);
        AddComponent<SimulateMovement>(agentEntity);
        AddComponent<SimulateMovementRepair>(agentEntity);
        AddComponent<SimulateMovementControl>(agentEntity);
        AddComponent<SimulateMovementFinalize>(agentEntity);
        AddComponent<SyncPositionWithTransform>(agentEntity);
        AddComponent<SyncRotationWithTransform>(agentEntity);

        // agent shape
        AddComponent(agentEntity, new AgentCylinderShape { height = authoring.AgentHeight, radius = authoring.AgentRadius });

        // default destination point
        AddComponent(agentEntity, new DestinationPoint { destination = new float3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity) });

        // movement plane
        NativeMovementPlane plane;
        if (authoring.MovePlane == AgentMovePlaneType.XY) {
            plane = new NativeMovementPlane(SimpleMovementPlane.XYPlane);
        }
        else if (authoring.MovePlane == AgentMovePlaneType.XZ) {
            plane = new NativeMovementPlane(SimpleMovementPlane.XZPlane);
        }
        else {
            plane = new NativeMovementPlane(authoring.transform.rotation);
        }
        AddComponent(agentEntity, new AgentMovementPlane { value = plane });

        // movement settings
        AddComponent(agentEntity, new MovementSettings {
            stopDistance = authoring.StopDistance,
            rotationSmoothing = authoring.RotationSmoothing,
            groundMask = authoring.GroundMask,
            isStopped = authoring.IsStopped,
            follower = new Pathfinding.PID.PIDMovement {
                rotationSpeed = authoring.RotationSpeed,
                speed = authoring.MoveSpeed,
                maxRotationSpeed = authoring.MaxRotationSpeed,
                maxOnSpotRotationSpeed = authoring.MaxOnSpotRotationSpeed,
                slowdownTime = authoring.SlowdownTime,
                desiredWallDistance = authoring.DesiredWallDistance,
                allowRotatingOnSpot = authoring.AllowRotatingOnSpot,
                leadInRadiusWhenApproachingDestination = authoring.LeadInRadiusWhenApproachingDestination
            }
        });

        // managed state options
        for (int i = 0; i < 32; i++) {
            if (i >= authoring.TagPenalties.Count) { break; }
            authoring.ManagedOpts.PathRequestOpts.TagPenalties[i] = authoring.TagPenalties[i];
        }
        AddComponent(agentEntity, authoring.ManagedOpts); // this is temporary (the init system will convert it to a managed component)
        if (authoring.ManagedOpts.EnableGravity) { AddComponent<GravityState>(agentEntity); }

        // added after 5.0.9
        AddComponent(agentEntity, authoring.RepathPolicy);

        // added after 5.0.9
        AddSharedComponent(agentEntity, new AgentMovementPlaneSource { value = authoring.MovePlaneSource });

        // orientation
        if (authoring.AgentOrientation == OrientationMode.YAxisForward) { AddComponent<OrientationYAxisForward>(agentEntity); }
    }
}