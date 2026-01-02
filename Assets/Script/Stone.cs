using System;
using System.Collections.Generic;
using UnityEngine;

public class Stone : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHp = 6;
    private int currentHp;
   // [SerializeField] private List<GameObject> damageVariants;
    public List<GameObject> damageVariant;

    [HideInInspector] public GridNode node;

    // =============================
    void Awake()
    {
        currentHp = maxHp;
        UpdateVariant();
    }

    void Start()
    {
        // Assign this stone to the grid node
        Vector2Int gridPos = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.z)
        );

        node = GridManager.Instance.GetNode(gridPos);
        if (node != null)
        {
            node.walkable = false;
            node.stone = this;
        }
    }

    // =============================
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("cutter"))
        {
            Debug.LogError("damage");
            Damage(1); // default cutter damage
        }
    }
    
    // =============================
    public void Damage(int dmg)
    {
        if (currentHp <= 0) return;

        currentHp -= dmg;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        UpdateVariant();

        if (currentHp <= 0)
        {
            Break();
        }
    }

    // =============================
    void UpdateVariant()
    {
        if (damageVariant == null || damageVariant.Count == 0)
            return;

        int damageIndex = maxHp - currentHp;
        damageIndex = Mathf.Clamp(damageIndex, 0, damageVariant.Count - 1);

        for (int i = 0; i < damageVariant.Count; i++)
        {
            damageVariant[i].SetActive(i == damageIndex);
        }
    }

    // =============================
    void Break()
    {
        if (node != null)
        {
            node.walkable = true;
            node.stone = null;
        }

        Destroy(gameObject);

       // GridManager.Instance.OnGridUpdated();
    }
}
