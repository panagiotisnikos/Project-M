using UnityEngine;

/// <summary>
/// Rolls a loot table when the thing it's on dies, spawning ItemPickups.
/// Auto-hooks EnemyHealth.Died if present; otherwise call Drop() manually.
/// </summary>
public class LootDropper : MonoBehaviour
{
    [System.Serializable]
    public struct Entry
    {
        public ItemData item;
        [Min(1)] public int min;
        [Min(1)] public int max;
        [Range(0f, 1f)] public float chance;
    }

    [SerializeField] private ItemPickup pickupPrefab;
    [SerializeField] private Entry[] table;
    [SerializeField] private float scatter = 0.6f;

    private EnemyHealth enemyHealth;
    private bool dropped;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth != null) enemyHealth.Died += Drop;
    }

    private void OnDestroy()
    {
        if (enemyHealth != null) enemyHealth.Died -= Drop;
    }

    public void Drop()
    {
        if (dropped || pickupPrefab == null || table == null) return;
        dropped = true;

        foreach (var e in table)
        {
            if (e.item == null || Random.value > e.chance) continue;
            int n = Random.Range(e.min, Mathf.Max(e.min, e.max) + 1);

            Vector2 o = Random.insideUnitCircle * scatter;
            Vector3 pos = transform.position + new Vector3(o.x, 0.4f, o.y);
            var p = Instantiate(pickupPrefab, pos, Quaternion.identity);
            p.Configure(e.item, n);
            p.Toss(new Vector3(o.x, 0f, o.y).normalized);
        }
    }
}
