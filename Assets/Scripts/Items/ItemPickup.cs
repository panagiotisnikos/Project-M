using UnityEngine;

/// <summary>
/// An item lying in the world. Walk over it to auto-collect (PlayerInventory
/// handles the trigger). Spawned by loot drops and by dropping from the pack.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private ItemData item;
    [SerializeField, Min(1)] private int count = 1;
    [SerializeField] private Transform meshRoot;
    [SerializeField] private float spinSpeed = 45f;
    [SerializeField] private float bobHeight = 0.12f;
    [SerializeField] private float bobSpeed = 2f;

    [Tooltip("Seconds after spawning before this can be auto-collected. Without this, loot " +
             "that spawns already inside the player's pickup radius (a chest, a close-range " +
             "kill) vanishes into the inventory before it's visibly landed - this guarantees " +
             "the toss/pop is always seen first.")]
    [SerializeField] private float pickupDelay = 0.55f;

    private Rigidbody body;
    private float bobBaseY;
    private float bornTime;

    public ItemData Item => item;
    public int Count => count;
    public bool IsCollectable => Time.time - bornTime >= pickupDelay;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (meshRoot == null) meshRoot = transform;
        bobBaseY = meshRoot.localPosition.y;
        bornTime = Time.time;
    }

    public void Configure(ItemData data, int amount)
    {
        item = data;
        count = Mathf.Max(1, amount);
    }

    public void SetCount(int amount) => count = Mathf.Max(1, amount);

    public void Toss(Vector3 direction)
    {
        if (body == null) body = GetComponent<Rigidbody>();
        direction.y = 0f;
        body.AddForce(direction.normalized * 2.2f + Vector3.up * 3f, ForceMode.Impulse);
    }

    private void Update()
    {
        // gentle idle spin + bob once it has settled
        if (Time.time - bornTime < 0.6f || meshRoot == null) return;
        meshRoot.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        var lp = meshRoot.localPosition;
        lp.y = bobBaseY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        meshRoot.localPosition = lp;
    }
}
