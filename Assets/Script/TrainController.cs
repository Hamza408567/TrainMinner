using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TrainController : MonoBehaviour
{
    [SerializeField] private PreComputedLoop preComputedLoop;
    [SerializeField] private ParticleSystem enginSmoke;
    [Header("Engine Settings")] public float speed = 3f;
    public bool clockwise = true;
    [SerializeField] private float engineRotationSpeed = 8f;

    [Header("Buggy Settings")] public GameObject buggyPrefab;
    public int numberOfBuggies = 3;

    public float baseFollowDistance = 1.5f;
    public float distanceMultiplier = 1.0f;

    [SerializeField] private float buggyMoveSmoothness = 12f;
    [SerializeField] private float buggyRotationSmoothness = 10f;

    private List<GridNode> loop;
    private int currentIndex;

    private List<Transform> buggies = new List<Transform>();
    private List<Vector3> positionHistory = new List<Vector3>();
    private bool isMoving = false;

    // =============================
    void OnEnable()
    {
        GridManager.OnLoopUpdated += UpdateTrainPath;
    }

    void OnDisable()
    {
        GridManager.OnLoopUpdated -= UpdateTrainPath;
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

        transform.position = loop[0].worldPos;
        currentIndex = clockwise ? 1 : loop.Count - 1;

        positionHistory.Clear();
        positionHistory.Add(transform.position);

        SpawnBuggies();
    }

    public void UpdateTrainPath()
    {
        var newLoop = preComputedLoop.PrecomputedLoop;
        if (newLoop == null || newLoop.Count < 2)
        {
            Debug.LogWarning("⚠ No valid loop after update");
            return;
        }

        loop = newLoop;

        // Find closest node to engine
        float minDist = float.MaxValue;
        int closestIndex = 0;
        for (int i = 0; i < loop.Count; i++)
        {
            float d = Vector3.Distance(transform.position, loop[i].worldPos);
            if (d < minDist)
            {
                minDist = d;
                closestIndex = i;
            }
        }

        if (clockwise)
            currentIndex = (closestIndex + 1) % loop.Count;
        else
            currentIndex = (closestIndex - 1 + loop.Count) % loop.Count;

        // ===============================
        // Map buggies positions onto new loop
        // ===============================
        List<Vector3> newHistory = new List<Vector3>();
        newHistory.Add(transform.position); // start with engine

        foreach (var buggy in buggies)
        {
            // Find closest point on new loop for buggy
            float minBuggyDist = float.MaxValue;
            Vector3 closestPoint = transform.position;

            for (int i = 0; i < loop.Count - 1; i++)
            {
                Vector3 start = loop[i].worldPos;
                Vector3 end = loop[i + 1].worldPos;
                Vector3 proj = ProjectPointOnLineSegment(start, end, buggy.position);
                float dist = Vector3.Distance(buggy.position, proj);
                if (dist < minBuggyDist)
                {
                    minBuggyDist = dist;
                    closestPoint = proj;
                }
            }

            newHistory.Add(closestPoint);
        }

        // Replace history smoothly
        positionHistory = newHistory;

        Debug.Log("🚆 Train and buggies remapped to new path smoothly");
    }

// Projects a point onto a line segment
    private Vector3 ProjectPointOnLineSegment(Vector3 a, Vector3 b, Vector3 point)
    {
        Vector3 ab = b - a;
        float t = Vector3.Dot(point - a, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);
        return a + ab * t;
    }

    // =============================
    void Update()
    {
        // Check if left mouse button or primary touch is pressed
        isMoving = Mouse.current.leftButton.isPressed ||
                   (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed);


        if (isMoving)
        {
            enginSmoke.Play();
            MoveEngine();
            RecordHistory();
            MoveBuggies();
        }
        else
        {

            enginSmoke.Stop();
        }
    }

    void MoveEngine()
    {
        Vector3 target = loop[currentIndex].worldPos;

        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime
        );

        Vector3 moveDir = target - transform.position;
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion rot = Quaternion.LookRotation(moveDir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                rot,
                engineRotationSpeed * Time.deltaTime
            );
        }

        // Reached node
        if (Vector3.Distance(transform.position, target) < 0.05f)
        {
            if (clockwise)
            {
                currentIndex = (currentIndex + 1) % loop.Count;
            }
            else
            {
                currentIndex = (currentIndex - 1 + loop.Count) % loop.Count;
            }
        }
    }


    // =============================
    void RecordHistory()
    {
        if (positionHistory.Count == 0 ||
            Vector3.Distance(positionHistory[^1], transform.position) > 0.02f)
        {
            positionHistory.Add(transform.position);
        }

        // Distance-based history trimming (NO jitter)
        float maxDistance =
            baseFollowDistance * distanceMultiplier * (numberOfBuggies + 2);

        float dist = 0f;
        for (int i = positionHistory.Count - 1; i > 0; i--)
        {
            dist += Vector3.Distance(positionHistory[i], positionHistory[i - 1]);
            if (dist > maxDistance)
            {
                positionHistory.RemoveRange(0, i);
                break;
            }
        }
    }
    void SpawnBuggies()
    {
        buggies.Clear();

        for (int i = 0; i < numberOfBuggies; i++)
        {
            GameObject obj = Instantiate(
                buggyPrefab,
                transform.position,
                transform.rotation,
                transform.parent
            );

            obj.name = $"Buggy_{i + 1}";
            buggies.Add(obj.transform);
        }
    }
    // =============================
    void MoveBuggies()
    {
        for (int i = 0; i < buggies.Count; i++)
        {
            float distance =
                baseFollowDistance * distanceMultiplier * (i + 1);

            Vector3 targetPos = GetPositionAlongHistory(distance);

            Transform buggy = buggies[i];

            // Smooth & stable movement
            buggy.position = Vector3.Lerp(
                buggy.position,
                targetPos,
                buggyMoveSmoothness * Time.deltaTime
            );

            // Face movement direction
            Vector3 moveDir = targetPos - buggy.position;
            if (moveDir.sqrMagnitude > 0.0001f)
            {
                Quaternion rot = Quaternion.LookRotation(moveDir.normalized, Vector3.up);
                buggy.rotation = Quaternion.Slerp(
                    buggy.rotation,
                    rot,
                    buggyRotationSmoothness * Time.deltaTime
                );
            }
        }
    }

    // =============================
    Vector3 GetPositionAlongHistory(float distance)
    {
        float remaining = distance;

        for (int i = positionHistory.Count - 1; i > 0; i--)
        {
            float seg = Vector3.Distance(
                positionHistory[i],
                positionHistory[i - 1]
            );

            if (seg >= remaining)
            {
                float t = remaining / seg;
                return Vector3.Lerp(
                    positionHistory[i],
                    positionHistory[i - 1],
                    t
                );
            }

            remaining -= seg;
        }

        return positionHistory[0];
    }

    // =============================
   
}
