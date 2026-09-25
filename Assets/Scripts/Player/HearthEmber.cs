using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A single-charge "second chance" the player can only hold after resting at
/// the Refuge's fire (see RefugeZone) - Project M's one lightweight refuge
/// benefit. Deliberately NOT a Valheim-style passive stat buff that ticks
/// down over time: it's a single consumable safety net spent automatically
/// the instant it would otherwise matter (a killing blow), so its value is
/// legible exactly when it pays off, and the player must go home and warm up
/// again before it can save them a second time - "prepare before you push
/// your luck," not "grind outside a little faster."
/// </summary>
public class HearthEmber : MonoBehaviour
{
    [Header("Feel")]
    [SerializeField] private AudioClip grantedSfx;
    [SerializeField] private AudioClip consumedSfx;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.5f;
    [SerializeField] private float consumedTrauma = 0.6f;

    [Header("Capacity")]
    [Tooltip("Charges available without any progression unlock.")]
    [Min(1)] [SerializeField] private int baseMaxCharges = 1;
    [Tooltip("Optional - if assigned and purchased at an Attunement Shrine (see 'Ember Reserve'), " +
             "capacity becomes baseMaxCharges + reserveBonusCharges. A direct poll of " +
             "ProgressionSystem.HasUnlock, not an event - see ProgressionUnlock's own comment for why.")]
    [SerializeField] private ProgressionUnlock reserveUnlock;
    [Min(1)] [SerializeField] private int reserveBonusCharges = 1;

    public int Charges { get; private set; }
    public int MaxCharges => baseMaxCharges + (reserveUnlock != null && ProgressionSystem.HasUnlock(reserveUnlock) ? reserveBonusCharges : 0);

    /// <summary>Fires whenever a charge is granted or spent (UI hook). Argument is the new count.</summary>
    public event System.Action<int> Changed;

    private CanvasGroup badgeGroup;

    private void Awake()
    {
        BuildBadge();
        RefreshBadge();
    }

    /// <summary>Grants one charge if under capacity. No-op once at MaxCharges - V1 deliberately
    /// caps stockpiling at whatever capacity has been earned, not unlimited banking.</summary>
    public void Grant()
    {
        if (Charges >= MaxCharges) return;

        Charges++;
        CombatAudio.Play(grantedSfx, transform.position, sfxVolume);
        DevLog.Log($"[HearthEmber] Ember kindled ({Charges}/{MaxCharges}) - it will spare you from a killing blow.");
        RefreshBadge();
        Changed?.Invoke(Charges);
    }

    /// <summary>Spends one charge if any are held. Returns whether it fired.</summary>
    public bool TryConsume()
    {
        if (Charges <= 0) return false;

        Charges--;
        CombatAudio.Play(consumedSfx, transform.position, sfxVolume);
        if (CameraShake.Instance != null) CameraShake.Instance.AddTrauma(consumedTrauma);
        DevLog.Log($"[HearthEmber] The ember burned out to spare you ({Charges}/{MaxCharges} left).");
        RefreshBadge();
        Changed?.Invoke(Charges);
        return true;
    }

    /// <summary>Silent restore from a save file - no SFX/shake, just state + UI.</summary>
    public void SetCharges(int value)
    {
        Charges = Mathf.Clamp(value, 0, MaxCharges);
        RefreshBadge();
    }

    // ---- tiny self-built corner badge (same self-contained-UI approach as LootChest's prompt) ----
    private TMP_Text badgeText;

    private void BuildBadge()
    {
        var canvasGo = new GameObject("HearthEmberBadge");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        canvasGo.AddComponent<CanvasScaler>();

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(canvasGo.transform, false);
        var rect = textGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(24f, 24f);
        rect.sizeDelta = new Vector2(260f, 40f);

        badgeText = textGo.AddComponent<TextMeshProUGUI>();
        badgeText.text = "EMBER READY";
        badgeText.alignment = TextAlignmentOptions.MidlineLeft;
        badgeText.fontSize = 22f;
        badgeText.color = UIPalette.Firefly;
        badgeText.fontStyle = FontStyles.Bold;

        var shadow = textGo.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);

        badgeGroup = canvasGo.AddComponent<CanvasGroup>();
        badgeGroup.blocksRaycasts = false;
        badgeGroup.interactable = false;
    }

    private void RefreshBadge()
    {
        if (badgeGroup != null) badgeGroup.alpha = Charges > 0 ? 1f : 0f;
        if (badgeText != null) badgeText.text = MaxCharges > 1 ? $"EMBER READY ({Charges}/{MaxCharges})" : "EMBER READY";
    }
}
