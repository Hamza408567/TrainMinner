using System.Collections.Generic;
using UnityEngine;

public class TrainController : MonoBehaviour
{
    public float speed = 3f;

    private List<GridNode> loop;
    private int index;

    void Start()
    {
        loop = TrainLoopFinder.Instance.FindLoop();

        if (loop == null || loop.Count < 2)
        {
            Debug.LogError("No loop to move on!");
            enabled = false;
            return;
        }

        transform.position = loop[0].worldPos;
        index = 1;
    }

    void Update()
    {
        Vector3 target = loop[index].worldPos;
        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, target) < 0.05f)
        {
            index++;

            if (index >= loop.Count)
                index = 1; // loop forever
        }
    }
}