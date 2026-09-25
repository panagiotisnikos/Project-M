using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One quiet line of text that fades in and out near the bottom of the screen. Shared by
/// every StationHint; a newer message simply replaces the current one.
/// </summary>
public class HintToast : MonoBehaviour
{
    private static HintToast instance;

    private CanvasGroup group;
    private TMP_Text text;
    private Coroutine routine;

    public static void Show(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        if (instance == null) Build();
        instance.Play(message);
    }

    private static void Build()
    {
        var go = new GameObject("HintToast");
        instance = go.AddComponent<HintToast>();

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 320;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        instance.group = go.AddComponent<CanvasGroup>();
        instance.group.alpha = 0f;
        instance.group.blocksRaycasts = false;
        instance.group.interactable = false;

        var textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        var rect = (RectTransform)textGO.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 190f); // just above the controls strip
        rect.sizeDelta = new Vector2(1000f, 40f);

        instance.text = textGO.AddComponent<TextMeshProUGUI>();
        instance.text.alignment = TextAlignmentOptions.Center;
        instance.text.fontSize = 21f;
        instance.text.fontStyle = FontStyles.Italic;
        instance.text.color = new Color(0.87f, 0.83f, 0.74f);
        instance.text.raycastTarget = false;

        var shadow = textGO.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);
    }

    private void LateUpdate()
    {
        // Never talk over a menu, the death screen or the ending.
        if (GameUIController.IsPaused || InventoryUI.IsOpen || CraftingUI.IsOpen || ProgressionUI.IsOpen)
            group.alpha = 0f;
    }

    private void Play(string message)
    {
        text.text = message;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        yield return Fade(1f, 0.5f);
        yield return new WaitForSeconds(4.5f);
        yield return Fade(0f, 1.2f);
        routine = null;
    }

    private IEnumerator Fade(float to, float duration)
    {
        float from = group.alpha;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
    }
}
