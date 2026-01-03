using System;
using UnityEngine;

public class SpawnRailsAligned : MonoBehaviour
{
    [SerializeField] private PreComputedLoop preComputedLoop;
    [SerializeField] private GameObject straightRailPrefab;

    // 4 corner prefabs
    [SerializeField] private GameObject cornerBL; // Bottom-Left
    [SerializeField] private GameObject cornerBR; // Bottom-Right
    [SerializeField] private GameObject cornerTL; // Top-Left
    [SerializeField] private GameObject cornerTR; // Top-Right
    [SerializeField] private Transform railParent; // optional, organize rails under a parent
    private void Awake()
    {
        preComputedLoop = GetComponent<PreComputedLoop>();
    }

    public void SpawnRails()
    {
        if (preComputedLoop.PrecomputedLoop == null || preComputedLoop.PrecomputedLoop.Count < 3)
        {
            Debug.LogError("Need at least 3 nodes in loop to spawn rails!");
            return;
        }

        // Clear previous rails
        ClearRails();

        int count = preComputedLoop.PrecomputedLoop.Count;

        for (int i = 0; i < count - 1; i++)
        {
            GridNode prev = preComputedLoop.PrecomputedLoop[(i - 1 + count) % count];
            GridNode current = preComputedLoop.PrecomputedLoop[i];
            GridNode next = preComputedLoop.PrecomputedLoop[(i + 1) % count];

            Vector3 dir1 = (current.worldPos - prev.worldPos).normalized; // incoming
            Vector3 dir2 = (next.worldPos - current.worldPos).normalized; // outgoing

            GameObject railPrefabToUse;
            Quaternion rotation = Quaternion.identity;

// ✅ FORCE FIRST RAIL TO BE BR
            if (i == 0)
            {
                railPrefabToUse = cornerBR;
            }
            else if (Vector3.Dot(dir1, dir2) > 0.99f)
            {
                railPrefabToUse = straightRailPrefab;
                rotation = Quaternion.LookRotation(dir2, Vector3.up);
            }
            else
            {
                railPrefabToUse = GetCornerPrefab(dir1, dir2);
            }

            // Instantiate rail
            Vector3 position = current.worldPos;
            GameObject rail = Instantiate(railPrefabToUse, position, rotation, railParent);
            rail.name = $"Rail_{i}_{railPrefabToUse.name}";
        }

        Debug.Log("✅ Rails aligned along loop (4 corner prefabs)!");
    }

// Picks the correct corner prefab based on turn direction
    private GameObject GetCornerPrefab(Vector3 dir1, Vector3 dir2)
    {
        // Only XZ plane
        dir1.y = 0;
        dir2.y = 0;

        // Round small float errors
        dir1.x = Mathf.Round(dir1.x);
        dir1.z = Mathf.Round(dir1.z);
        dir2.x = Mathf.Round(dir2.x);
        dir2.z = Mathf.Round(dir2.z);

        // ----- Bottom-Left -----
        // Coming from right (x>0) to forward (z>0) OR from back (z<0) to left (x<0)
        if ((dir1.x > 0 && dir2.z > 0) || (dir1.z < 0 && dir2.x < 0))
            return cornerBL;

        // ----- Bottom-Right -----
        // Coming from left (x<0) to forward (z>0) OR from back (z<0) to right (x>0)
        if ((dir1.x < 0 && dir2.z > 0) || (dir1.z < 0 && dir2.x > 0))
            return cornerBR;

        // ----- Top-Left -----
        // Coming from right (x>0) to back (z<0) OR from forward (z>0) to left (x<0)
        if ((dir1.x > 0 && dir2.z < 0) || (dir1.z > 0 && dir2.x < 0))
            return cornerTL;

        // ----- Top-Right -----
        // Coming from left (x<0) to back (z<0) OR from forward (z>0) to right (x>0)
        if ((dir1.x < 0 && dir2.z < 0) || (dir1.z > 0 && dir2.x > 0))
            return cornerTR;

        // Fallback
        return cornerBL;
    }



    public void ClearRails()
    {
        if (railParent == null)
        {
            Debug.LogWarning("Rail parent is not assigned!");
            return;
        }

        for (int i = railParent.childCount - 1; i >= 0; i--)
            Destroy(railParent.GetChild(i).gameObject);
        Debug.Log("🧹 Rails cleared");
    }
//------------------------------------------------------
    
}
