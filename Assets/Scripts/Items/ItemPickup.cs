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
    private GameObject customVisual;

    private int lastClaimedFrame = -1;

    public ItemData Item => item;
    public int Count => count;
    public bool IsCollectable => Time.time - bornTime >= pickupDelay;

    /// <summary>
    /// Atomically claims this pickup for one collection attempt this frame.
    /// Guards against a real duplication bug: OnTriggerStay fires once per
    /// physics tick, and Destroy() is deferred to end-of-frame, so a frame with
    /// more than one FixedUpdate (a low-framerate or catch-up frame) could
    /// otherwise call PlayerInventory's pickup handling on the same
    /// still-alive pickup twice, granting its contents twice. Keyed on
    /// Time.frameCount rather than a one-shot flag so a *partial* pickup
    /// (inventory was full, some amount left on the ground) can still be
    /// retried on a later frame once space frees up.
    /// </summary>
    public bool TryClaimThisFrame()
    {
        if (Time.frameCount == lastClaimedFrame)
            return false;

        lastClaimedFrame = Time.frameCount;
        return true;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (meshRoot == null) meshRoot = transform;
        bobBaseY = meshRoot.localPosition.y;
        bornTime = Time.time;

        // Configure() may run before Awake() (loot spawns Instantiate then
        // Configure in the same frame) or after - apply whichever item is
        // already set so the visual swap isn't missed either way.
        if (item != null) ApplyWorldVisual();
    }

    public void Configure(ItemData data, int amount)
    {
        item = data;
        count = Mathf.Max(1, amount);
        ApplyWorldVisual();
    }

    /// <summary>
    /// Swaps in the item's own worldPrefab (if it has one) as a child of
    /// meshRoot - inherits the spin/bob for free - and hides the shared
    /// default mesh underneath it so only one visual shows. No worldPrefab =
    /// the default shared visual is left exactly as it was.
    /// </summary>
    private void ApplyWorldVisual()
    {
        if (item == null || item.worldPrefab == null || meshRoot == null || customVisual != null)
            return;

        foreach (var renderer in meshRoot.GetComponentsInChildren<Renderer>())
            renderer.enabled = false;

        customVisual = Instantiate(item.worldPrefab, meshRoot);
        customVisual.transform.localPosition = Vector3.zero;
        customVisual.transform.localRotation = Quaternion.identity;
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
