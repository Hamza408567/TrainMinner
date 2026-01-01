using System.Collections.Generic;
using UnityEngine;

public class TrainController : MonoBehaviour
{
    public float speed = 3f;
    public bool clockwise = true; // ✅ Toggle clockwise or counter-clockwise

    private List<GridNode> loop;
    private int currentIndex = 0;

    [SerializeField] private float rotationSpeed = 5f; // How fast the train rotates

    void Start()
    {
        // Use precomputed loop from GridManager
        loop = GridManager.Instance.PrecomputedLoop;

        if (loop == null || loop.Count < 2)
        {
            Debug.LogError("No precomputed loop found for the train!");
            enabled = false;
            return;
        }

        // Start at first node
        transform.position = loop[0].worldPos;
        currentIndex = clockwise ? 1 : loop.Count - 1; // move towards next node
    }

    void Update()
    {
        if (loop == null || loop.Count < 2)
            return;

        Vector3 target = loop[currentIndex].worldPos;

        // Move towards target
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        // Rotate train to face movement direction
        Vector3 direction = (target - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Check if reached current target
        if (Vector3.Distance(transform.position, target) < 0.05f)
        {
            if (clockwise)
            {
                currentIndex++;
                if (currentIndex >= loop.Count)
                    currentIndex = 1; // wrap around
            }
            else
            {
                currentIndex--;
                if (currentIndex < 0)
                    currentIndex = loop.Count - 2; // wrap around
            }
        }
    }
}