using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TrainController : MonoBehaviour
{
    [SerializeField] private PreComputedLoop preComputedLoop;
    [SerializeField] private ParticleSystem enginSmoke;

    [Header("Engine Settings")]
    public float speed = 3f;
    public bool clockwise = true;
    [SerializeField] private float engineRotationSpeed = 8f;

    [Header("Buggy Settings")]
    public GameObject buggyPrefab;
    public int numberOfBuggies = 3;

    public float baseFollowDistance = 1.5f;
    public float distanceMultiplier = 1f;

    [SerializeField] private float buggyMoveSmoothness = 12f;
    [SerializeField] private float buggyRotationSmoothness = 10f;

    // =============================
    private List<GridNode> loop;
    private List<float> segmentLengths = new();

    private float loopLength;
    private float engineDistance;

    private readonly List<Transform> buggies = new();
    private bool isMoving;

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
            Debug.LogError("❌ No valid loop found");
            enabled = false;
            return;
        }

        BuildLoopData();

        engineDistance = 0f;
        transform.position = loop[0].worldPos;

        SpawnBuggies();
    }

    // =============================
    void Update()
    {
        isMoving =
            Mouse.current?.leftButton.isPressed == true ||
            Touchscreen.current?.primaryTouch.press.isPressed == true;

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

    // =============================
    void MoveEngine()
    {
        float move = speed * Time.deltaTime;
        engineDistance += clockwise ? move : -move;
        engineDistance = Mathf.Repeat(engineDistance, loopLength);

        Vector3 pos = GetPositionAtDistance(engineDistance);
        Vector3 dir = GetDirectionAtDistance(engineDistance);

        transform.position = pos;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir, Vector3.up),
            engineRotationSpeed * Time.deltaTime
        );
    }

    // =============================
    void MoveBuggies()
    {
        for (int i = 0; i < buggies.Count; i++)
        {
            float offset =
                baseFollowDistance * distanceMultiplier * (i + 1);

            float buggyDistance = engineDistance - offset;
            buggyDistance = Mathf.Repeat(buggyDistance, loopLength);

            Vector3 targetPos = GetPositionAtDistance(buggyDistance);
            Vector3 forward = GetDirectionAtDistance(buggyDistance);

            Transform buggy = buggies[i];

            buggy.position = Vector3.Lerp(
                buggy.position,
                targetPos,
                buggyMoveSmoothness * Time.deltaTime
            );

            buggy.rotation = Quaternion.Slerp(
                buggy.rotation,
                Quaternion.LookRotation(forward, Vector3.up),
                buggyRotationSmoothness * Time.deltaTime
            );
        }
    }

    // =============================
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
    void UpdateTrainPath()
    {
        var newLoop = preComputedLoop.PrecomputedLoop;

        if (newLoop == null || newLoop.Count < 2)
        {
            Debug.LogWarning("⚠ Loop update ignored (invalid)");
            return;
        }

        loop = newLoop;
        BuildLoopData();

        engineDistance = FindClosestDistanceOnLoop(transform.position);

        Debug.Log("🚆 Train + buggies remapped to updated loop");
    }

    // =============================
    void BuildLoopData()
    {
        segmentLengths.Clear();
        loopLength = 0f;

        for (int i = 0; i < loop.Count; i++)
        {
            Vector3 a = loop[i].worldPos;
            Vector3 b = loop[(i + 1) % loop.Count].worldPos;

            float len = Vector3.Distance(a, b);
            segmentLengths.Add(len);
            loopLength += len;
        }
    }

    // =============================
    Vector3 GetPositionAtDistance(float distance)
    {
        float remaining = distance;

        for (int i = 0; i < segmentLengths.Count; i++)
        {
            if (segmentLengths[i] >= remaining)
            {
                Vector3 a = loop[i].worldPos;
                Vector3 b = loop[(i + 1) % loop.Count].worldPos;
                float t = remaining / segmentLengths[i];
                return Vector3.Lerp(a, b, t);
            }

            remaining -= segmentLengths[i];
        }

        return loop[^1].worldPos;
    }

    Vector3 GetDirectionAtDistance(float distance)
    {
        float remaining = distance;

        for (int i = 0; i < segmentLengths.Count; i++)
        {
            if (segmentLengths[i] >= remaining)
            {
                Vector3 a = loop[i].worldPos;
                Vector3 b = loop[(i + 1) % loop.Count].worldPos;
                return (b - a).normalized;
            }

            remaining -= segmentLengths[i];
        }

        return (loop[1].worldPos - loop[0].worldPos).normalized;
    }

    // =============================
    float FindClosestDistanceOnLoop(Vector3 pos)
    {
        float bestDist = float.MaxValue;
        float bestDistance = 0f;
        float accumulated = 0f;

        for (int i = 0; i < loop.Count; i++)
        {
            Vector3 a = loop[i].worldPos;
            Vector3 b = loop[(i + 1) % loop.Count].worldPos;

            Vector3 proj = ProjectPointOnLineSegment(a, b, pos);
            float d = Vector3.Distance(pos, proj);

            if (d < bestDist)
            {
                bestDist = d;
                bestDistance = accumulated + Vector3.Distance(a, proj);
            }

            accumulated += segmentLengths[i];
        }

        return bestDistance;
    }

    Vector3 ProjectPointOnLineSegment(Vector3 a, Vector3 b, Vector3 point)
    {
        Vector3 ab = b - a;
        float t = Vector3.Dot(point - a, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);
        return a + ab * t;
    }
}
