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

    public bool HasCharge { get; private set; }

    /// <summary>Fires whenever the charge is granted or spent (UI hook).</summary>
    public event System.Action<bool> Changed;

    private CanvasGroup badgeGroup;

    private void Awake()
    {
        BuildBadge();
        RefreshBadge();
    }

    /// <summary>Grants the charge if not already held. No-op if one is already
    /// banked - V1 deliberately caps at a single charge, no stockpiling.</summary>
    public void Grant()
    {
        if (HasCharge) return;

        HasCharge = true;
        CombatAudio.Play(grantedSfx, transform.position, sfxVolume);
        Debug.Log("[HearthEmber] Ember kindled - it will spare you from one killing blow.");
        RefreshBadge();
        Changed?.Invoke(HasCharge);
    }

    /// <summary>Spends the charge if held. Returns whether it fired.</summary>
    public bool TryConsume()
    {
        if (!HasCharge) return false;

        HasCharge = false;
        CombatAudio.Play(consumedSfx, transform.position, sfxVolume);
        if (CameraShake.Instance != null) CameraShake.Instance.AddTrauma(consumedTrauma);
        Debug.Log("[HearthEmber] The ember burned out to spare you.");
        RefreshBadge();
        Changed?.Invoke(HasCharge);
        return true;
    }

    /// <summary>Silent restore from a save file - no SFX/shake, just state + UI.</summary>
    public void SetCharge(bool value)
    {
        HasCharge = value;
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
        badgeText.color = new Color(1f, 0.55f, 0.2f);
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
        if (badgeGroup != null) badgeGroup.alpha = HasCharge ? 1f : 0f;
    }
}
