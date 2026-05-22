using UnityEngine;

// This tag forces the script to run AFTER the Animation Rigging system moves the bones
[DefaultExecutionOrder(100)] 
public class SnakeAntiPhasing : MonoBehaviour
{
    [Tooltip("Drag all your moving body bones here (e.g., bone_1 down to the tail).")]
    public Transform[] bodyBones;
    
    [Tooltip("How thick the snake is. If bones get closer than this, they push apart.")]
    public float boneRadius = 0.5f;

    void LateUpdate()
    {
        if (bodyBones.Length == 0) return;

        // Loop through all the bones
        for (int i = 0; i < bodyBones.Length; i++)
        {
            // Compare each bone with every other bone further down the chain.
            // We start at 'i + 2' so it ignores its direct neighbor (which is supposed to be connected!)
            for (int j = i + 2; j < bodyBones.Length; j++)
            {
                float distance = Vector3.Distance(bodyBones[i].position, bodyBones[j].position);
                float minSafeDistance = boneRadius * 2;

                // If they are closer than the safe distance, they are phasing!
                if (distance < minSafeDistance)
                {
                    // Calculate the direction to push them apart
                    Vector3 pushDirection = (bodyBones[i].position - bodyBones[j].position).normalized;
                    
                    // Calculate exactly how far they are phased into each other
                    float overlap = minSafeDistance - distance;

                    // Instantly push both bones away from each other by half the overlap amount
                    bodyBones[i].position += pushDirection * (overlap * 0.5f);
                    bodyBones[j].position -= pushDirection * (overlap * 0.5f);
                }
            }
        }
    }
}