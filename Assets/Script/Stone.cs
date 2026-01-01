using UnityEngine;

public class Stone : MonoBehaviour
{
    public int hp = 3;
    [HideInInspector] public GridNode node;

    private void Start()
    {
        // Assign this stone to the grid node
        Vector2Int gridPos = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.z)
        );

        node = GridManager.Instance.GetNode(gridPos);
        if (node != null)
        {
            node.walkable = false; // Block the train
            node.stone = this;
        }
    }

    // Call this when train hits the stone
    public void Damage(int dmg)
    {
        hp -= dmg;

        if (hp <= 0)
        {
            Break();
        }
    }

    void Break()
    {
        if (node != null)
        {
            node.walkable = true;
            node.stone = null;
        }

        Destroy(gameObject);

        // Notify grid that it has changed
        GridManager.Instance.OnGridUpdated();
    }
}