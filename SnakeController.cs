using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class SnakeController : MonoBehaviour
{
    [Header("Targeting & AI")]
    public Transform enemy;
    
    [Header("Obstacle Avoidance (The Eyes)")]
    public LayerMask obstacleLayer; 
    [Tooltip("How far the snake sees straight ahead.")]
    public float centerWhiskerLength = 4.0f;
    [Tooltip("How far the snake sees to its left and right.")]
    public float sideWhiskerLength = 3.0f;
    [Tooltip("The angle of the side whiskers (Default is 45 degrees).")]
    public float whiskerAngle = 45f;
    public float avoidanceStrength = 5.0f;
    public float whiskerHeightOffset = 0.5f;

    [Header("Realistic Slither Movement")]
    public float moveSpeed = 3.5f;
    public float turnSpeed = 3f;
    [Range(10f, 90f)] public float maxTurnAngle = 35f; 
    public float swayAmount = 0.4f;
    public float slitherSpeed = 5f;
    public float stopDistance = 3.0f;

    [Header("Combat Rigs")]
    public Rig stingerRig; 
    public Transform tailIKTarget;
    public Transform tailRestPoint;
    
    [Header("Advanced Strike Tuning")]
    public float attackRange = 3.5f;
    public float safeLungeDistance = 1.5f; 
    public float lungeSpeed = 12f;   
    public float strikeSpeed = 15f;  
    public float retractSpeed = 5f;
    public float attackCooldown = 1.5f;
    public bool continuousAttacks = true;

    private bool isAttacking = false;
    private float nextAttackTime = 0f;

    void Start()
    {
        if (stingerRig != null) stingerRig.weight = 0f;
    }

    void Update()
    {
        if (enemy == null) return;

        float distanceToEnemy = Vector3.Distance(transform.position, enemy.position);
        
        Vector3 dirToEnemy = (enemy.position - transform.position).normalized;
        dirToEnemy.y = 0;
        float angleToEnemy = Vector3.Angle(transform.forward, dirToEnemy);

        if (!isAttacking)
        {
            if (distanceToEnemy > stopDistance)
            {
                SlitherTowardsEnemy(dirToEnemy, angleToEnemy);
            }
            else
            {
                FaceEnemyStationary(dirToEnemy);
            }

            tailIKTarget.position = tailRestPoint.position;
            tailIKTarget.rotation = tailRestPoint.rotation;
        }

        // Trigger Combat
        if (distanceToEnemy <= attackRange && !isAttacking && Time.time >= nextAttackTime && angleToEnemy <= maxTurnAngle)
        {
            StartCoroutine(ComboStrikeRoutine());
        }
    }

    void SlitherTowardsEnemy(Vector3 baseDirection, float angleToEnemy)
    {
        // --- WHISKER OBSTACLE AVOIDANCE ---
        Vector3 rayOrigin = transform.position + (Vector3.up * whiskerHeightOffset);
        
        // Use the custom angle for the side whiskers
        Vector3 leftWhisker = Quaternion.Euler(0, -whiskerAngle, 0) * transform.forward;
        Vector3 rightWhisker = Quaternion.Euler(0, whiskerAngle, 0) * transform.forward;

        RaycastHit hit;
        bool avoiding = false;

        // Draw the whiskers in the Scene View
        Debug.DrawRay(rayOrigin, transform.forward * centerWhiskerLength, Color.red);
        Debug.DrawRay(rayOrigin, leftWhisker * sideWhiskerLength, Color.blue);
        Debug.DrawRay(rayOrigin, rightWhisker * sideWhiskerLength, Color.green);

        // Center Whisker Check
        if (Physics.Raycast(rayOrigin, transform.forward, out hit, centerWhiskerLength, obstacleLayer))
        {
            baseDirection += hit.normal * avoidanceStrength;
            avoiding = true;
        }
        // Left Whisker Check
        else if (Physics.Raycast(rayOrigin, leftWhisker, out hit, sideWhiskerLength, obstacleLayer))
        {
            baseDirection += transform.right * avoidanceStrength;
            avoiding = true;
        }
        // Right Whisker Check
        else if (Physics.Raycast(rayOrigin, rightWhisker, out hit, sideWhiskerLength, obstacleLayer))
        {
            baseDirection -= transform.right * avoidanceStrength;
            avoiding = true;
        }

        baseDirection.Normalize(); 

        if (baseDirection != Vector3.zero)
        {
            float currentAngle = Vector3.Angle(transform.forward, baseDirection);
            if (currentAngle > maxTurnAngle)
            {
                baseDirection = Vector3.RotateTowards(transform.forward, baseDirection, maxTurnAngle * Mathf.Deg2Rad, 0f);
            }

            float activeMoveSpeed = moveSpeed;
            if (currentAngle > 15f || avoiding)
            {
                activeMoveSpeed = moveSpeed * 1.5f; 
            }

            Quaternion targetRotation = Quaternion.LookRotation(baseDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

            Vector3 swayDirection = Vector3.Cross(Vector3.up, baseDirection);
            float wave = Mathf.Sin(Time.time * slitherSpeed) * swayAmount;
            
            Vector3 moveDirection = (baseDirection + (swayDirection * wave)).normalized;
            transform.position += moveDirection * activeMoveSpeed * Time.deltaTime;
        }
    }

    void FaceEnemyStationary(Vector3 dirToEnemy)
    {
        if (dirToEnemy != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dirToEnemy);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }

    IEnumerator ComboStrikeRoutine()
    {
        isAttacking = true;

        Vector3 startPos = transform.position;
        Vector3 lungeTarget = transform.position + (transform.forward * safeLungeDistance);
        lungeTarget.y = transform.position.y;

        float percent = 0;
        while (percent < 1)
        {
            percent += Time.deltaTime * lungeSpeed;
            float eased = Mathf.SmoothStep(0, 1, percent);

            transform.position = Vector3.Lerp(startPos, lungeTarget, eased);
            
            Vector3 aimDir = (enemy.position - transform.position).normalized;
            aimDir.y = 0;
            if (aimDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(aimDir);
            
            yield return null;
        }

        percent = 0;
        while (percent < 1)
        {
            percent += Time.deltaTime * strikeSpeed;
            float eased = Mathf.SmoothStep(0, 1, percent);

            Vector3 strikePos = enemy.position + (Vector3.up * 1.0f); 
            
            stingerRig.weight = eased;
            tailIKTarget.position = Vector3.Lerp(tailRestPoint.position, strikePos, eased);
            yield return null;
        }

        yield return new WaitForSeconds(0.15f);

        percent = 0;
        while (percent < 1)
        {
            percent += Time.deltaTime * retractSpeed;
            float eased = Mathf.SmoothStep(0, 1, percent);

            stingerRig.weight = 1f - eased;
            tailIKTarget.position = Vector3.Lerp(enemy.position + (Vector3.up * 1.0f), tailRestPoint.position, eased);
            yield return null;
        }

        stingerRig.weight = 0f; 
        nextAttackTime = continuousAttacks ? Time.time + attackCooldown : Mathf.Infinity;
        isAttacking = false;
    }
}