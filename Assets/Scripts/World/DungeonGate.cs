using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A generic one-way "shortcut" unlock: a solid blockout obstacle that opens permanently once
/// a Camp clears and/or a PointOfInterest completes, and stays open across a save/load. Not
/// dungeon-specific - any future shortcut/gate reuses this directly.
///
/// [DefaultExecutionOrder(1001)]: must run its Start() AFTER GameSaveController's ([1000]) -
/// a restored save already flipped the Camp/POI's own state silently (no Cleared/Completed
/// event fires on a restore, by design - see Camp.RestoreCleared/PointOfInterest.RestoreState),
/// so this gate has to check the already-restored state itself rather than wait for an event
/// that will never come on a loaded game. Live, same-session unlocks still go through the
/// normal event subscription.
/// </summary>
[DefaultExecutionOrder(1001)]
public class DungeonGate : MonoBehaviour
{
    [Header("Unlock Condition (assign one or both - either satisfies it)")]
    [SerializeField] private Camp unlockedByCamp;
    [SerializeField] private PointOfInterest unlockedByPOI;

    [Header("What physically blocks the path")]
    [Tooltip("Deactivated when the gate opens. Assign a separate blocking object (not this " +
             "GameObject itself - deactivating the object this script lives on would also " +
             "disable the script before it finishes opening).")]
    [SerializeField] private GameObject visualBlocker;

    [Header("Feedback Hook (optional - no code required)")]
    [SerializeField] private UnityEvent onOpened = new UnityEvent();

    public bool IsOpen { get; private set; }

    private void OnEnable()
    {
        if (unlockedByCamp != null) unlockedByCamp.Cleared += Open;
        if (unlockedByPOI != null) unlockedByPOI.Completed += Open;
    }

    private void OnDisable()
    {
        if (unlockedByCamp != null) unlockedByCamp.Cleared -= Open;
        if (unlockedByPOI != null) unlockedByPOI.Completed -= Open;
    }

    private void Start()
    {
        // Catches a save that loaded already-cleared/completed - see class doc for why this
        // can't just be an Awake()-time check.
        bool alreadySatisfied =
            (unlockedByCamp != null && unlockedByCamp.IsCleared) ||
            (unlockedByPOI != null && unlockedByPOI.IsCompleted);

        if (alreadySatisfied)
            Open();
    }

    public void Open()
    {
        if (IsOpen)
            return;

        IsOpen = true;

        if (visualBlocker != null)
            visualBlocker.SetActive(false);
        else
            Debug.LogWarning($"[DungeonGate] {gameObject.name} has no Visual Blocker assigned - nothing physically opened.");

        DevLog.Log($"[DungeonGate] {gameObject.name} opened.");
        onOpened?.Invoke();
    }
}
