using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A red screen-edge vignette that fades in as the player's health drops and
/// pulses when critical. Builds its own overlay canvas so it needs no scene
/// wiring beyond a PlayerHealth reference.
/// </summary>
public class PlayerLowHealthVignette : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [Tooltip("Health fraction at or below which the vignette starts showing.")]
    [SerializeField] private float showBelowFraction = 0.35f;
    [SerializeField] private float maxAlpha = 0.62f;
    [SerializeField] private Color vignetteColor = new Color(0.65f, 0.05f, 0.05f);
    [SerializeField] private int sortingOrder = 500;

    private Image image;
    private float displayed;

    private void Awake()
    {
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null) playerHealth = GetComponentInParent<PlayerHealth>();

        var canvasGo = new GameObject("LowHealthVignetteCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        canvasGo.AddComponent<CanvasScaler>();

        var imgGo = new GameObject("Vignette");
        imgGo.transform.SetParent(canvasGo.transform, false);
        image = imgGo.AddComponent<Image>();
        image.raycastTarget = false;
        image.sprite = BuildVignetteSprite();
        image.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, 0f);

        var rt = image.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void Update()
    {
        if (playerHealth == null || image == null) return;

        float frac = playerHealth.MaxHealth > 0
            ? (float)playerHealth.CurrentHealth / playerHealth.MaxHealth
            : 1f;

        float targetAlpha = 0f;
        if (!playerHealth.IsDead && frac < showBelowFraction)
        {
            float t = 1f - Mathf.Clamp01(frac / showBelowFraction); // 0 at threshold -> 1 at empty
            targetAlpha = t * maxAlpha;
            // heartbeat pulse when critical
            if (frac < 0.15f)
                targetAlpha *= 0.75f + 0.25f * Mathf.Sin(Time.time * 6f);
        }

        displayed = Mathf.MoveTowards(displayed, targetAlpha, 2.5f * Time.deltaTime);
        image.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, displayed);
    }

    private static Sprite BuildVignetteSprite()
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float maxD = size * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) / maxD;
            float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.45f) / 0.55f));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
