using UnityEngine;

// This tag forces this script to run ABSOLUTELY LAST, overriding any Rigging bugs
[DefaultExecutionOrder(500)] 
public class HeadLock : MonoBehaviour
{
    [Tooltip("Drag bone_0 from your Armature here.")]
    public Transform rootBone; 

    void LateUpdate()
    {
        if (rootBone != null)
        {
            // Instantly glue the physical bone back to the transform arrows every single frame
            rootBone.position = transform.position;
        }
    }
}