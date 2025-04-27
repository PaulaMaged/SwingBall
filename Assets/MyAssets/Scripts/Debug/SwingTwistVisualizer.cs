using UnityEngine;

/// <summary>
/// Attach this to a GameObject to visualize the swing (twist-removed) rotation of a bone in real time.
/// </summary>
[ExecuteInEditMode]
public class SwingTwistVisualizer : MonoBehaviour
{
    [Header("Bone Settings")]
    [Tooltip("Target bone whose rotation will be decomposed.")]
    public Transform bone;

    [Header("Twist Axis")]
    [Tooltip("Local axis of the bone around which twisting will be removed.")]
    public Vector3 twistAxis = Vector3.up;

    [Header("Visualization")]
    [Tooltip("Optional proxy transform to show the swing-only orientation.")]
    public Transform swingProxy;

    void OnValidate()
    {
        // Fallback to self if no bone assigned
        if (bone == null)
            bone = transform;
    }

    void Update()
    {
        if (bone == null || swingProxy == null)
            return;

        // Compute swing (twist removed)
        Quaternion swing = RemoveTwist(bone.localRotation, twistAxis.normalized);

        // Apply to proxy
        swingProxy.localRotation = swing;
    }

    /// <summary>
    /// Strips out the twist component around the given axis from quaternion q.
    /// </summary>
    /// <param name="q">Original rotation (in local space of the bone).</param>
    /// <param name="axis">Normalized axis to remove twist around.</param>
    /// <returns>Quaternion containing only the swing component.</returns>
    public static Quaternion RemoveTwist(Quaternion q, Vector3 axis)
    {
        // Extract vector part
        Vector3 r = new Vector3(q.x, q.y, q.z);
        // Project onto twist axis
        Vector3 proj = Vector3.Dot(r, axis) * axis;

        // Reconstruct the twist quaternion
        Quaternion twist = new Quaternion(proj.x, proj.y, proj.z, q.w);
        twist = Quaternion.Normalize(twist);

        // Swing is the residual
        Quaternion swing = q * Quaternion.Inverse(twist);
        return swing;
    }
}
