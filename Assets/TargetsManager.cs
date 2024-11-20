using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

public class TargetsManager : MonoBehaviour {
    public GameObject _target;
    static public GameObject target;

    void Start() {
        target = _target;
    }

    static public NativeArray<float3> GetTargetPositions(Allocator allocator) {
        var positions = new float3[] { new float3(target.transform.position.x, target.transform.position.y, target.transform.position.z) };
        return new NativeArray<float3>(positions, allocator);
    }
}
