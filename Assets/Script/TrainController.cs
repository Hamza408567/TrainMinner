using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class TrainController : MonoBehaviour
{
    [SerializeField] private PreComputedLoop preComputedLoop;
    [SerializeField] private ParticleSystem enginSmoke;

    [Header("Engine Settings")]
    public float speed = 3f;
    public bool clockwise = true;
    [SerializeField] private float baseEngineRotationSpeed = 8f; // Added: Base rotation speed
    [SerializeField] private float engineRotationSpeedMultiplier = 0.5f; // Added: How much speed affects rotation

    [Header("Buggy Settings")]
    public GameObject buggyPrefab;
    public int numberOfBuggies = 3;
    public float baseFollowDistance = 1.5f;
    public float distanceMultiplier = 1.0f;
    [SerializeField] private float baseBuggyRotationSmoothness = 10f; // Added: Base rotation speed
    [SerializeField] private float buggyRotationSpeedMultiplier = 0.3f; // Added: How much speed affects rotation
    [SerializeField] private float buggyMoveSmoothness = 12f;

    private List<GridNode> loop;
    private float enginePathDistance = 0f;
    private List<Transform> buggies = new List<Transform>();
    private float totalLoopLength = 0f;
    private List<float> segmentLengths = new List<float>();
    private List<float> cumulativeLengths = new List<float>();
    private bool isMoving = false;
    
    // Calculated properties
    private float CurrentEngineRotationSpeed => baseEngineRotationSpeed + (speed * engineRotationSpeedMultiplier);
    private float CurrentBuggyRotationSpeed => baseBuggyRotationSmoothness + (speed * buggyRotationSpeedMultiplier);

    // =============================
    void OnEnable()
    {
        GridManager.OnLoopUpdated += UpdateTrainPath;
    }

    void OnDisable()
    {
        GridManager.OnLoopUpdated -= UpdateTrainPath;
    }
    public void SetSpeedFromSlider(float value)
    {
        speed = value;
    }
    void Start()
    {
        
        loop = preComputedLoop.PrecomputedLoop;
        if (loop == null || loop.Count < 2)
        {
            Debug.LogError("No precomputed loop found!");
            enabled = false;
            return;
        }

        CalculateLoopLengths();
        
        // Start engine at first node
        transform.position = loop[0].worldPos;
        enginePathDistance = 0f;
        
        SpawnBuggies();
        
        Debug.Log($"Train initialized. Loop length: {totalLoopLength}, Clockwise: {clockwise}");
        Debug.Log($"Current rotation speeds - Engine: {CurrentEngineRotationSpeed:F1}, Buggy: {CurrentBuggyRotationSpeed:F1}");
    }

    void CalculateLoopLengths()
    {
        segmentLengths.Clear();
        cumulativeLengths.Clear();
        totalLoopLength = 0f;
        
        for (int i = 0; i < loop.Count; i++)
        {
            int nextIndex = (i + 1) % loop.Count;
            float segmentLength = Vector3.Distance(loop[i].worldPos, loop[nextIndex].worldPos);
            segmentLengths.Add(segmentLength);
            totalLoopLength += segmentLength;
            cumulativeLengths.Add(totalLoopLength);
        }
    }

    // Get position at a specific distance along the loop
    Vector3 GetPositionAtDistance(float distance)
    {
        // Normalize distance to loop length
        distance = Mathf.Repeat(distance, totalLoopLength);
        
        // Find which segment the distance falls into
        for (int i = 0; i < cumulativeLengths.Count; i++)
        {
            if (distance <= cumulativeLengths[i])
            {
                float segmentStartDistance = (i == 0) ? 0 : cumulativeLengths[i - 1];
                float distanceInSegment = distance - segmentStartDistance;
                
                int startNodeIndex = i;
                int endNodeIndex = (i + 1) % loop.Count;
                
                Vector3 startPos = loop[startNodeIndex].worldPos;
                Vector3 endPos = loop[endNodeIndex].worldPos;
                
                float t = distanceInSegment / segmentLengths[i];
                return Vector3.Lerp(startPos, endPos, t);
            }
        }
        
        return loop[0].worldPos; // Fallback
    }

    // Get the direction vector at a specific distance
    Vector3 GetDirectionAtDistance(float distance)
    {
        distance = Mathf.Repeat(distance, totalLoopLength);
        
        // Find which segment we're on
        for (int i = 0; i < cumulativeLengths.Count; i++)
        {
            if (distance <= cumulativeLengths[i])
            {
                int startNodeIndex = i;
                int endNodeIndex = (i + 1) % loop.Count;
                
                Vector3 startPos = loop[startNodeIndex].worldPos;
                Vector3 endPos = loop[endNodeIndex].worldPos;
                
                return (endPos - startPos).normalized;
            }
        }
        
        return (loop[1].worldPos - loop[0].worldPos).normalized;
    }

    // =============================
    void Update()
    {
        // Check input
        isMoving = Mouse.current.leftButton.isPressed || 
                   (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed);
        
        if (isMoving)
        {
            if (!enginSmoke.isPlaying) 
                enginSmoke.Play();
            
            MoveEngine();
            MoveBuggies();
        }
        else
        {
            if (enginSmoke.isPlaying) 
                enginSmoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    void MoveEngine()
    {
        // Calculate movement based on direction
        float directionFactor = clockwise ? 1f : -1f;
        float deltaDistance = speed * Time.deltaTime * directionFactor;
        
        // Update engine distance
        enginePathDistance += deltaDistance;
        
        // Ensure distance stays within loop bounds
        while (enginePathDistance < 0) enginePathDistance += totalLoopLength;
        while (enginePathDistance >= totalLoopLength) enginePathDistance -= totalLoopLength;
        
        // Get new position
        Vector3 targetPosition = GetPositionAtDistance(enginePathDistance);
        transform.position = targetPosition;
        
        // Get direction at this point
        Vector3 loopDirection = GetDirectionAtDistance(enginePathDistance);
        
        // Determine actual facing direction based on movement direction
        Vector3 facingDirection = clockwise ? loopDirection : -loopDirection;
        
        // Rotate engine with speed-dependent rotation speed
        if (facingDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(facingDirection, Vector3.up);
            float rotationSpeed = CurrentEngineRotationSpeed;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    void MoveBuggies()
    {
        for (int i = 0; i < buggies.Count; i++)
        {
            // Calculate buggy distance - always BEHIND the engine
            float followDistance = baseFollowDistance * distanceMultiplier * (i + 1);
            
            // Buggy distance depends on movement direction
            float buggyDistance;
            if (clockwise)
            {
                // Moving clockwise: buggies are behind (earlier in loop)
                buggyDistance = enginePathDistance - followDistance;
            }
            else
            {
                // Moving counter-clockwise: buggies are behind (later in loop)
                buggyDistance = enginePathDistance + followDistance;
            }
            
            // Wrap distance
            while (buggyDistance < 0) buggyDistance += totalLoopLength;
            while (buggyDistance >= totalLoopLength) buggyDistance -= totalLoopLength;
            
            // Get buggy target position
            Vector3 targetPosition = GetPositionAtDistance(buggyDistance);
            
            // Get direction at buggy position
            Vector3 loopDirection = GetDirectionAtDistance(buggyDistance);
            
            // Buggy faces same direction as engine
            Vector3 buggyFacingDirection = clockwise ? loopDirection : -loopDirection;
            
            // Smooth movement
            Transform buggy = buggies[i];
            buggy.position = Vector3.Lerp(
                buggy.position,
                targetPosition,
                buggyMoveSmoothness * Time.deltaTime
            );
            
            // Rotate buggy with speed-dependent rotation speed
            if (buggyFacingDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(buggyFacingDirection, Vector3.up);
                float rotationSpeed = CurrentBuggyRotationSpeed;
                buggy.rotation = Quaternion.Slerp(
                    buggy.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
    }

    void SpawnBuggies()
    {
        buggies.Clear();
        
        for (int i = 0; i < numberOfBuggies; i++)
        {
            // Calculate initial position for each buggy (behind engine)
            float followDistance = baseFollowDistance * distanceMultiplier * (i + 1);
            float buggyDistance;
            
            if (clockwise)
            {
                buggyDistance = enginePathDistance - followDistance;
            }
            else
            {
                buggyDistance = enginePathDistance + followDistance;
            }
            
            // Wrap distance
            while (buggyDistance < 0) buggyDistance += totalLoopLength;
            while (buggyDistance >= totalLoopLength) buggyDistance -= totalLoopLength;
            
            Vector3 spawnPosition = GetPositionAtDistance(buggyDistance);
            
            // Get direction for buggy
            Vector3 loopDirection = GetDirectionAtDistance(buggyDistance);
            Vector3 spawnDirection = clockwise ? loopDirection : -loopDirection;
            
            Quaternion spawnRotation = Quaternion.LookRotation(spawnDirection, Vector3.up);
            
            GameObject buggy = Instantiate(
                buggyPrefab,
                spawnPosition,
                spawnRotation,
                transform.parent
            );
            buggy.name = $"Buggy_{i + 1}";
            buggies.Add(buggy.transform);
        }
    }

    public void UpdateTrainPath()
    {
        var newLoop = preComputedLoop.PrecomputedLoop;
        if (newLoop == null || newLoop.Count < 2)
        {
            Debug.LogWarning("⚠ No valid loop after update");
            return;
        }
        
        // Store old positions for buggies
        List<Vector3> oldBuggyPositions = new List<Vector3>();
        foreach (var buggy in buggies)
        {
            oldBuggyPositions.Add(buggy.position);
        }
        
        // Update loop and recalculate
        loop = newLoop;
        CalculateLoopLengths();
        
        // Find closest point on new loop for engine
        float closestEngineDistance = FindClosestDistanceOnLoop(transform.position);
        enginePathDistance = closestEngineDistance;
        
        // Update buggies - find closest points on new loop
        for (int i = 0; i < buggies.Count; i++)
        {
            if (i < oldBuggyPositions.Count)
            {
                float buggyDistance = FindClosestDistanceOnLoop(oldBuggyPositions[i]);
                
                // Adjust to maintain proper following distance
                float followDistance = baseFollowDistance * distanceMultiplier * (i + 1);
                if (clockwise)
                {
                    buggyDistance = enginePathDistance - followDistance;
                }
                else
                {
                    buggyDistance = enginePathDistance + followDistance;
                }
                
                // Wrap distance
                while (buggyDistance < 0) buggyDistance += totalLoopLength;
                while (buggyDistance >= totalLoopLength) buggyDistance -= totalLoopLength;
                
                // Update position and rotation
                Vector3 newPosition = GetPositionAtDistance(buggyDistance);
                buggies[i].position = newPosition;
                
                Vector3 loopDirection = GetDirectionAtDistance(buggyDistance);
                Vector3 buggyDirection = clockwise ? loopDirection : -loopDirection;
                buggies[i].rotation = Quaternion.LookRotation(buggyDirection, Vector3.up);
            }
        }
        
        Debug.Log($"🚆 Train remapped to new path. Engine distance: {enginePathDistance:F2}");
    }

    float FindClosestDistanceOnLoop(Vector3 position)
    {
        float minDistance = float.MaxValue;
        float closestDistance = 0f;
        
        for (int i = 0; i < loop.Count; i++)
        {
            int nextIndex = (i + 1) % loop.Count;
            Vector3 start = loop[i].worldPos;
            Vector3 end = loop[nextIndex].worldPos;
            
            // Project point onto line segment
            Vector3 line = end - start;
            float lineLength = line.magnitude;
            Vector3 lineDir = line.normalized;
            Vector3 pointToStart = position - start;
            
            float t = Vector3.Dot(pointToStart, lineDir);
            t = Mathf.Clamp(t, 0, lineLength);
            
            Vector3 closestPoint = start + lineDir * t;
            float distanceToSegment = Vector3.Distance(position, closestPoint);
            
            if (distanceToSegment < minDistance)
            {
                minDistance = distanceToSegment;
                
                // Calculate distance along loop to this point
                float segmentStartDistance = (i == 0) ? 0 : cumulativeLengths[i - 1];
                closestDistance = segmentStartDistance + t;
            }
        }
        
        return closestDistance;
    }

    // Display current rotation speeds in the inspector
    void OnGUI()
    {
        if (Application.isPlaying)
        {
            GUI.Label(new Rect(10, 10, 300, 20), $"Engine Rotation Speed: {CurrentEngineRotationSpeed:F1}");
            GUI.Label(new Rect(10, 30, 300, 20), $"Buggy Rotation Speed: {CurrentBuggyRotationSpeed:F1}");
            GUI.Label(new Rect(10, 50, 300, 20), $"Train Speed: {speed:F1}");
        }
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (loop == null || loop.Count < 2) return;
        
        // Draw loop
        Gizmos.color = Color.green;
        for (int i = 0; i < loop.Count; i++)
        {
            int nextIndex = (i + 1) % loop.Count;
            Gizmos.DrawLine(loop[i].worldPos, loop[nextIndex].worldPos);
        }
        
        // Draw engine position and direction
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.3f);
        
        Vector3 engineDirection = GetDirectionAtDistance(enginePathDistance);
        if (!clockwise) engineDirection = -engineDirection;
        Gizmos.DrawRay(transform.position, engineDirection * 2f);
        
        // Draw buggy target positions
        Gizmos.color = Color.blue;
        for (int i = 0; i < buggies.Count; i++)
        {
            float followDistance = baseFollowDistance * distanceMultiplier * (i + 1);
            float buggyDistance;
            
            if (clockwise)
                buggyDistance = enginePathDistance - followDistance;
            else
                buggyDistance = enginePathDistance + followDistance;
            
            // Wrap distance
            while (buggyDistance < 0) buggyDistance += totalLoopLength;
            while (buggyDistance >= totalLoopLength) buggyDistance -= totalLoopLength;
            
            Vector3 buggyPos = GetPositionAtDistance(buggyDistance);
            Gizmos.DrawSphere(buggyPos, 0.2f);
            Gizmos.DrawLine(transform.position, buggyPos);
        }
    }
}