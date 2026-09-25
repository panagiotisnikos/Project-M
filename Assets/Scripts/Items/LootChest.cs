using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// A one-time openable world chest. Walk up, press the interact key; it pops its
/// lid and rolls its LootDropper table (scattered ItemPickups) exactly like an
/// enemy's death drop, just triggered by interaction instead of EnemyHealth.Died.
/// Builds its own small "Press E" prompt so it needs no scene UI wiring.
/// </summary>
[RequireComponent(typeof(LootDropper))]
public class LootChest : MonoBehaviour
{
    [Header("Interact")]
    [SerializeField] private float interactRange = 2.2f;
    [SerializeField] private Transform player;

    [Header("Lid")]
    [SerializeField] private Transform lid;
    [SerializeField] private float lidOpenAngle = -100f;
    [SerializeField] private float lidOpenDuration = 0.45f;

    [Header("Audio")]
    [SerializeField] private AudioClip openSfx;
    [Range(0f, 1f)] [SerializeField] private float openVolume = 0.5f;

    [Header("Feedback Hook (optional - e.g. wire to a PointOfInterest's Complete())")]
    [SerializeField] private UnityEvent onOpened = new UnityEvent();

    /// <summary>Fired the moment this chest is opened - the code-side mirror of onOpened.</summary>
    public event System.Action Opened;

    private LootDropper dropper;
    private bool isOpen;
    private bool playerInRange;

    private CanvasGroup promptGroup;
    private TMP_Text promptText;

    private void Awake()
    {
        dropper = GetComponent<LootDropper>();

        if (player == null)
        {
            var pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null) player = pm.transform;
        }

        BuildPrompt();
    }

    private void Update()
    {
        if (isOpen || player == null)
        {
            SetPromptVisible(false);
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        playerInRange = dist <= interactRange;
        SetPromptVisible(playerInRange && !GameUIController.IsPaused && !InventoryUI.IsOpen);

        if (playerInRange && !GameUIController.IsPaused && !InventoryUI.IsOpen && Input.GetKeyDown(KeyBindings.Get(GameAction.Interact)))
        {
            Open();
        }
    }

    private void Open()
    {
        if (isOpen) return;
        isOpen = true;

        SetPromptVisible(false);
        dropper.Drop();

        CombatAudio.Play(openSfx, transform.position, openVolume);

        onOpened?.Invoke();
        Opened?.Invoke();

        if (lid != null)
            StartCoroutine(OpenLidRoutine());
    }

    private IEnumerator OpenLidRoutine()
    {
        Quaternion start = lid.localRotation;
        Quaternion end = Quaternion.Euler(lidOpenAngle, 0f, 0f);
        float t = 0f;
        while (t < lidOpenDuration)
        {
            t += Time.deltaTime;
            lid.localRotation = Quaternion.Slerp(start, end, t / lidOpenDuration);
            yield return null;
        }
        lid.localRotation = end;
    }

    // ---- self-contained "Press E" prompt --------------------------------
    private void BuildPrompt()
    {
        var canvasGO = new GameObject("LootChestPrompt");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 350;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(canvasGO.transform, false);
        var rect = textGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -140f);
        rect.sizeDelta = new Vector2(420f, 50f);

        promptText = textGO.AddComponent<TextMeshProUGUI>();
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.fontSize = 30f;
        promptText.color = UIPalette.Teal;
        promptText.fontStyle = FontStyles.Bold;

        var shadow = textGO.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);

        promptGroup = canvasGO.AddComponent<CanvasGroup>();
        promptGroup.alpha = 0f;
        promptGroup.blocksRaycasts = false;
        promptGroup.interactable = false;

        RefreshPromptText();
    }

    private void OnEnable()
    {
        KeyBindings.Changed += RefreshPromptText;
    }

    private void OnDisable()
    {
        KeyBindings.Changed -= RefreshPromptText;
    }

    private void RefreshPromptText()
    {
        if (promptText != null) promptText.text = $"Press {KeyBindings.Get(GameAction.Interact)} to open";
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptGroup == null) return;
        promptGroup.alpha = visible ? 1f : 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(UIPalette.Teal.r, UIPalette.Teal.g, UIPalette.Teal.b, 0.5f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
