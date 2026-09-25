using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The options menu - Audio (Master/Music/SFX), Video (fullscreen/quality/
/// resolution), Controls (mouse sensitivity), Gameplay (invert Y, toasts).
/// Builds itself at runtime the same way InventoryUI does (same sprite kit:
/// ui_slate/ui_frame/ui_button), so it can be dropped into any scene/canvas
/// without hand-building UGUI hierarchies. Reads/writes GameAudioSettings and
/// GameSettings directly and applies changes live as the player drags/clicks.
///
/// Usage: call Open(onClose) from whichever panel is showing (pause menu, main
/// menu) - it hides itself and invokes onClose when the player hits Back, so
/// the caller can re-show its own panel without this component knowing about it.
/// </summary>
public class OptionsMenuUI : MonoBehaviour
{
    [SerializeField] private float panelWidth = 520f;

    private RectTransform panel;
    private GameObject canvasGo;
    private Action onClose;

    private static readonly Color Stone = new Color(0.14f, 0.14f, 0.16f, 1f);
    private static readonly Color StoneTint = new Color(0.62f, 0.63f, 0.68f, 1f);
    private static readonly Color Parch = new Color(0.87f, 0.83f, 0.74f);
    private static readonly Color ParchDim = new Color(0.60f, 0.57f, 0.50f);
    private static readonly Color Title = UIPalette.Lichen;
    private static readonly Color Accent = UIPalette.Teal;

    private TMP_FontAsset anton;
    private float cursorY;
    private const float RowHeight = 40f;
    private const float SectionGap = 14f;
    private const float SidePad = 26f;

    private (int width, int height)[] uniqueResolutions;
    private int[] resolutionIndexMap; // uniqueResolutions index -> first matching Screen.resolutions index

    private void Awake()
    {
        anton = LoadFont();
        Build();
        gameObject.SetActive(true);
        // The whole canvas (including the full-screen scrim) must be off, not just
        // the floating panel - the scrim's Image is raycastTarget=true by default,
        // so leaving it active blocked every click in the game, everywhere, all
        // the time (it sits at sortingOrder 420, above the rest of the UI).
        canvasGo.SetActive(false);
    }

    public void Open(Action closeCallback)
    {
        onClose = closeCallback;
        canvasGo.SetActive(true);
        RefreshAllControls();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        canvasGo.SetActive(false);
        onClose?.Invoke();
        onClose = null;
    }

    // ---------------------------------------------------------------- build
    private static TMP_FontAsset LoadFont()
    {
        var f = Resources.Load<TMP_FontAsset>("Fonts & Materials/Anton SDF");
#if UNITY_EDITOR
        if (f == null) f = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Anton SDF.asset");
#endif
        return f;
    }

    private Sprite Spr(string n)
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/UI/" + n + ".png");
#else
        return null;
#endif
    }

    private void Build()
    {
        BuildResolutionList();

        var slate = Spr("ui_slate");
        var frame = Spr("ui_frame");
        var buttonSpr = Spr("ui_button");
        var buttonHover = Spr("ui_button_hover");

        canvasGo = new GameObject("OptionsCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 420;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        canvasGo.AddComponent<GraphicRaycaster>();
        canvasGo.AddComponent<UIClickSound>();
        canvasGo.AddComponent<UIScaleFollower>(); // proves the UI Scale accessibility setting out end to end

        var scrim = NewRect("Scrim", canvasGo.transform, Vector2.zero, Vector2.one);
        scrim.offsetMin = Vector2.zero; scrim.offsetMax = Vector2.zero;
        var scrimImg = scrim.gameObject.AddComponent<Image>();
        scrimImg.color = new Color(0f, 0f, 0f, 0.55f);

        panel = NewRect("Panel", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        panel.sizeDelta = new Vector2(panelWidth, 720f);
        panel.anchoredPosition = Vector2.zero;
        var pImg = panel.gameObject.AddComponent<Image>();
        pImg.sprite = slate; pImg.type = Image.Type.Sliced; pImg.color = StoneTint;

        var fr = NewRect("Frame", panel, Vector2.zero, Vector2.one);
        fr.offsetMin = Vector2.zero; fr.offsetMax = Vector2.zero;
        var frImg = fr.gameObject.AddComponent<Image>();
        frImg.sprite = frame; frImg.type = Image.Type.Sliced; frImg.raycastTarget = false;

        var header = NewText("Header", panel, "OPTIONS", 24, Title, TextAlignmentOptions.TopLeft);
        header.rectTransform.anchorMin = new Vector2(0f, 1f); header.rectTransform.anchorMax = new Vector2(1f, 1f);
        header.rectTransform.pivot = new Vector2(0f, 1f);
        header.rectTransform.anchoredPosition = new Vector2(SidePad, -16f);
        header.rectTransform.sizeDelta = new Vector2(panelWidth - SidePad * 2f, 32f);
        header.characterSpacing = 8f; header.fontStyle = FontStyles.UpperCase;

        // scrollable body so the panel works even if a section list grows
        var scrollRt = NewRect("Body", panel, new Vector2(0f, 0f), new Vector2(1f, 1f));
        scrollRt.offsetMin = new Vector2(0f, 64f);
        scrollRt.offsetMax = new Vector2(0f, -58f);
        var scrollRect = scrollRt.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        var viewportRt = NewRect("Viewport", scrollRt, Vector2.zero, Vector2.one);
        viewportRt.offsetMin = Vector2.zero; viewportRt.offsetMax = Vector2.zero;
        viewportRt.gameObject.AddComponent<RectMask2D>();
        var contentRt = NewRect("Content", viewportRt, new Vector2(0f, 1f), new Vector2(1f, 1f));
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 900f); // grown as rows are added
        scrollRect.viewport = viewportRt;
        scrollRect.content = contentRt;

        cursorY = 0f;

        BuildAudioSection(contentRt);
        BuildVideoSection(contentRt);
        BuildControlsSection(contentRt);
        BuildGameplaySection(contentRt);
        BuildAccessibilitySection(contentRt);
        BuildKeyBindingsSection(contentRt);

        contentRt.sizeDelta = new Vector2(0f, cursorY + SectionGap);

        // Back button, pinned to the panel bottom (outside the scroll body)
        var backRt = NewRect("BackButton", panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        backRt.pivot = new Vector2(0.5f, 0f);
        backRt.sizeDelta = new Vector2(180f, 40f);
        backRt.anchoredPosition = new Vector2(0f, 16f);
        var backImg = backRt.gameObject.AddComponent<Image>();
        backImg.sprite = buttonSpr; backImg.type = Image.Type.Sliced;
        var backBtn = backRt.gameObject.AddComponent<Button>();
        backBtn.targetGraphic = backImg;
        SetSpriteState(backBtn, buttonSpr, buttonHover);
        backBtn.onClick.AddListener(Close);
        var backLabel = NewText("Label", backRt, "BACK", 16, Parch, TextAlignmentOptions.Center);
        backLabel.raycastTarget = false;
        if (anton != null) backLabel.font = anton;
        backLabel.fontStyle = FontStyles.UpperCase; backLabel.characterSpacing = 4f;
    }

    private void BuildResolutionList()
    {
        var seen = new List<(int, int)>();
        var firstIndex = new List<int>();
        var res = Screen.resolutions;
        for (int i = 0; i < res.Length; i++)
        {
            var pair = (res[i].width, res[i].height);
            if (!seen.Contains(pair)) { seen.Add(pair); firstIndex.Add(i); }
        }
        if (seen.Count == 0) { seen.Add((Screen.width, Screen.height)); firstIndex.Add(0); }
        uniqueResolutions = seen.ToArray();
        resolutionIndexMap = firstIndex.ToArray();
    }

    // ------------------------------------------------------------- sections
    private TMP_Text sectionAudio, masterValueText, musicValueText, sfxValueText, ambienceValueText;
    private Slider masterSlider, musicSlider, sfxSlider, ambienceSlider, sensitivitySlider;
    private TMP_Text sensitivityValueText;
    private TMP_Text fullscreenValueText, qualityValueText, resolutionValueText, vSyncValueText;
    private TMP_Text invertYValueText, toastsValueText;
    private int qualityIndex;
    private int resolutionCyclerIndex;

    private TMP_Text shakeValueText, reduceMotionValueText, holdToBlockValueText, uiScaleValueText;
    private Slider shakeSlider, uiScaleSlider;

    private readonly Dictionary<GameAction, TMP_Text> bindingRowTexts = new Dictionary<GameAction, TMP_Text>();
    private GameAction? listeningForAction;
    private static readonly KeyCode[] RebindableKeys = BuildRebindableKeySet();

    private void BuildAudioSection(Transform content)
    {
        AddSectionHeader(content, "AUDIO");
        masterSlider = AddSlider(content, "Master Volume", GameAudioSettings.Master, out masterValueText,
            v => { GameAudioSettings.SetMaster(v); masterValueText.text = Pct(v); });
        musicSlider = AddSlider(content, "Music Volume", GameAudioSettings.Music, out musicValueText,
            v => { GameAudioSettings.SetMusic(v); musicValueText.text = Pct(v); });
        sfxSlider = AddSlider(content, "SFX Volume", GameAudioSettings.Sfx, out sfxValueText,
            v => { GameAudioSettings.SetSfx(v); sfxValueText.text = Pct(v); });
        ambienceSlider = AddSlider(content, "Ambience Volume", GameAudioSettings.Ambience, out ambienceValueText,
            v => { GameAudioSettings.SetAmbience(v); ambienceValueText.text = Pct(v); });
    }

    private void BuildVideoSection(Transform content)
    {
        AddSectionHeader(content, "VIDEO");

        fullscreenValueText = AddToggleRow(content, "Fullscreen", GameSettings.Fullscreen,
            v => GameSettings.SetFullscreen(v));

        qualityIndex = Mathf.Clamp(GameSettings.QualityLevel, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        qualityValueText = AddCyclerRow(content, "Quality", QualitySettings.names[qualityIndex],
            dir =>
            {
                int count = QualitySettings.names.Length;
                qualityIndex = ((qualityIndex + dir) % count + count) % count;
                GameSettings.SetQualityLevel(qualityIndex);
                qualityValueText.text = QualitySettings.names[qualityIndex];
            });

        resolutionCyclerIndex = FindResolutionCyclerIndex();
        resolutionValueText = AddCyclerRow(content, "Resolution", ResolutionLabel(resolutionCyclerIndex),
            dir =>
            {
                int count = uniqueResolutions.Length;
                resolutionCyclerIndex = ((resolutionCyclerIndex + dir) % count + count) % count;
                GameSettings.SetResolutionIndex(resolutionIndexMap[resolutionCyclerIndex]);
                resolutionValueText.text = ResolutionLabel(resolutionCyclerIndex);
            });

        vSyncValueText = AddToggleRow(content, "VSync", GameSettings.VSyncEnabled,
            v => GameSettings.SetVSyncEnabled(v));
    }

    private void BuildControlsSection(Transform content)
    {
        AddSectionHeader(content, "CONTROLS");
        sensitivitySlider = AddRangedSlider(content, "Mouse Sensitivity", 0.5f, 10f, GameSettings.MouseSensitivity,
            out sensitivityValueText,
            v => { GameSettings.SetMouseSensitivity(v); sensitivityValueText.text = v.ToString("0.0"); });
    }

    private void BuildGameplaySection(Transform content)
    {
        AddSectionHeader(content, "GAMEPLAY");
        invertYValueText = AddToggleRow(content, "Invert Look Y", GameSettings.InvertY,
            v => GameSettings.SetInvertY(v));
        toastsValueText = AddToggleRow(content, "Show Pickup/Loadout Toasts", GameSettings.ShowToasts,
            v => GameSettings.SetShowToasts(v));
    }

    private void BuildAccessibilitySection(Transform content)
    {
        AddSectionHeader(content, "ACCESSIBILITY");

        shakeSlider = AddSlider(content, "Screen Shake", GameSettings.ShakeIntensity, out shakeValueText,
            v => { GameSettings.SetShakeIntensity(v); shakeValueText.text = Pct(v); });

        reduceMotionValueText = AddToggleRow(content, "Reduce Camera Motion", GameSettings.ReduceCameraMotion,
            v => GameSettings.SetReduceCameraMotion(v));

        // Deliberately a HOLD/TOGGLE cycler, not an ON/OFF toggle - "ON" would be
        // ambiguous about which mode it means.
        holdToBlockValueText = AddCyclerRow(content, "Block Input", GameSettings.HoldToBlock ? "HOLD" : "TOGGLE",
            dir =>
            {
                GameSettings.SetHoldToBlock(!GameSettings.HoldToBlock);
                holdToBlockValueText.text = GameSettings.HoldToBlock ? "HOLD" : "TOGGLE";
            });

        uiScaleSlider = AddRangedSlider(content, "UI Scale", 0.85f, 1.25f, GameSettings.UIScale, out uiScaleValueText,
            v => { GameSettings.SetUIScale(v); uiScaleValueText.text = v.ToString("0.00"); });
    }

    private void BuildKeyBindingsSection(Transform content)
    {
        AddSectionHeader(content, "KEY BINDINGS");

        foreach (var action in KeyBindings.AllActions)
        {
            var capturedAction = action;
            var valueText = AddCyclerlessBindRow(content, KeyBindings.Label(action), KeyBindings.Get(action).ToString(),
                () => BeginListening(capturedAction));
            bindingRowTexts[action] = valueText;
        }

        AddPlainButtonRow(content, "Reset Bindings to Default", () =>
        {
            KeyBindings.ResetToDefaults();
            RefreshKeyBindingRows();
        });
    }

    private void RefreshKeyBindingRows()
    {
        foreach (var kv in bindingRowTexts)
            kv.Value.text = KeyBindings.Get(kv.Key).ToString();
    }

    private void BeginListening(GameAction action)
    {
        listeningForAction = action;
        if (bindingRowTexts.TryGetValue(action, out var text)) text.text = "PRESS A KEY (ESC CANCELS)";
    }

    private void CancelListening()
    {
        if (listeningForAction == null) return;
        var action = listeningForAction.Value;
        listeningForAction = null;
        if (bindingRowTexts.TryGetValue(action, out var text)) text.text = KeyBindings.Get(action).ToString();
    }

    private void Update()
    {
        if (listeningForAction == null) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelListening();
            return;
        }

        foreach (var key in RebindableKeys)
        {
            if (Input.GetKeyDown(key))
            {
                KeyBindings.Rebind(listeningForAction.Value, key);
                listeningForAction = null;
                RefreshKeyBindingRows();
                return;
            }
        }
    }

    private static KeyCode[] BuildRebindableKeySet()
    {
        // Every keyboard KeyCode except Escape (reserved for "cancel rebind") and the
        // mouse/joystick codes (out of scope for V1 - see KeyBindings' class doc).
        var list = new List<KeyCode>();
        foreach (KeyCode kc in Enum.GetValues(typeof(KeyCode)))
        {
            if (kc == KeyCode.Escape) continue;
            string name = kc.ToString();
            if (name.StartsWith("Mouse") || name.StartsWith("Joystick")) continue;
            list.Add(kc);
        }
        return list.ToArray();
    }

    private void RefreshAllControls()
    {
        if (masterSlider != null) { masterSlider.SetValueWithoutNotify(GameAudioSettings.Master); masterValueText.text = Pct(GameAudioSettings.Master); }
        if (musicSlider != null) { musicSlider.SetValueWithoutNotify(GameAudioSettings.Music); musicValueText.text = Pct(GameAudioSettings.Music); }
        if (sfxSlider != null) { sfxSlider.SetValueWithoutNotify(GameAudioSettings.Sfx); sfxValueText.text = Pct(GameAudioSettings.Sfx); }
        if (ambienceSlider != null) { ambienceSlider.SetValueWithoutNotify(GameAudioSettings.Ambience); ambienceValueText.text = Pct(GameAudioSettings.Ambience); }
        if (sensitivitySlider != null) { sensitivitySlider.SetValueWithoutNotify(GameSettings.MouseSensitivity); sensitivityValueText.text = GameSettings.MouseSensitivity.ToString("0.0"); }
        if (fullscreenValueText != null) fullscreenValueText.text = GameSettings.Fullscreen ? "ON" : "OFF";
        if (vSyncValueText != null) vSyncValueText.text = GameSettings.VSyncEnabled ? "ON" : "OFF";
        if (invertYValueText != null) invertYValueText.text = GameSettings.InvertY ? "ON" : "OFF";
        if (toastsValueText != null) toastsValueText.text = GameSettings.ShowToasts ? "ON" : "OFF";
        if (shakeSlider != null) { shakeSlider.SetValueWithoutNotify(GameSettings.ShakeIntensity); shakeValueText.text = Pct(GameSettings.ShakeIntensity); }
        if (reduceMotionValueText != null) reduceMotionValueText.text = GameSettings.ReduceCameraMotion ? "ON" : "OFF";
        if (holdToBlockValueText != null) holdToBlockValueText.text = GameSettings.HoldToBlock ? "HOLD" : "TOGGLE";
        if (uiScaleSlider != null) { uiScaleSlider.SetValueWithoutNotify(GameSettings.UIScale); uiScaleValueText.text = GameSettings.UIScale.ToString("0.00"); }
        RefreshKeyBindingRows();
        CancelListening();
    }

    private int FindResolutionCyclerIndex()
    {
        int savedScreenResIndex = GameSettings.ResolutionIndex;
        if (savedScreenResIndex >= 0)
        {
            var res = Screen.resolutions;
            if (savedScreenResIndex < res.Length)
            {
                var wanted = (res[savedScreenResIndex].width, res[savedScreenResIndex].height);
                for (int i = 0; i < uniqueResolutions.Length; i++)
                    if (uniqueResolutions[i] == wanted) return i;
            }
        }
        var current = (Screen.width, Screen.height);
        for (int i = 0; i < uniqueResolutions.Length; i++)
            if (uniqueResolutions[i] == current) return i;
        return 0;
    }

    private string ResolutionLabel(int i) => uniqueResolutions[i].width + " x " + uniqueResolutions[i].height;
    private static string Pct(float v) => Mathf.RoundToInt(v * 100f) + "%";

    // --------------------------------------------------------------- rows
    private void AddSectionHeader(Transform content, string title)
    {
        if (cursorY > 0f) cursorY += SectionGap;

        var rt = NewRect("Section_" + title, content, new Vector2(0f, 1f), new Vector2(1f, 1f));
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(SidePad, -cursorY);
        rt.sizeDelta = new Vector2(-SidePad * 2f, 24f);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = title; t.fontSize = 15f; t.color = Title; t.alignment = TextAlignmentOptions.BottomLeft;
        if (anton != null) t.font = anton;
        t.characterSpacing = 6f; t.fontStyle = FontStyles.UpperCase;

        var rule = NewRect("Rule", content, new Vector2(0f, 1f), new Vector2(1f, 1f));
        rule.pivot = new Vector2(0f, 1f);
        rule.anchoredPosition = new Vector2(SidePad, -cursorY - 22f);
        rule.sizeDelta = new Vector2(-SidePad * 2f, 2f);
        var ruleImg = rule.gameObject.AddComponent<Image>();
        ruleImg.color = new Color(UIPalette.MossDim.r, UIPalette.MossDim.g, UIPalette.MossDim.b, 0.6f);
        ruleImg.raycastTarget = false;

        cursorY += 30f;
    }

    private RectTransform AddRowRoot(Transform content, string label)
    {
        var rowRt = NewRect("Row_" + label, content, new Vector2(0f, 1f), new Vector2(1f, 1f));
        rowRt.pivot = new Vector2(0f, 1f);
        rowRt.anchoredPosition = new Vector2(SidePad, -cursorY);
        rowRt.sizeDelta = new Vector2(-SidePad * 2f, RowHeight);

        var labelText = NewText("Label", rowRt, label, 15, Parch, TextAlignmentOptions.MidlineLeft);
        labelText.rectTransform.anchorMin = new Vector2(0f, 0f); labelText.rectTransform.anchorMax = new Vector2(0.48f, 1f);
        labelText.rectTransform.offsetMin = Vector2.zero; labelText.rectTransform.offsetMax = Vector2.zero;
        labelText.raycastTarget = false;

        cursorY += RowHeight + 6f;
        return rowRt;
    }

    private Slider AddSlider(Transform content, string label, float initial01, out TMP_Text valueText, Action<float> onChange)
        => AddRangedSliderInternal(content, label, 0f, 1f, initial01, out valueText, onChange, Pct(initial01));

    private Slider AddRangedSlider(Transform content, string label, float min, float max, float initial, out TMP_Text valueText, Action<float> onChange)
        => AddRangedSliderInternal(content, label, min, max, initial, out valueText, onChange, initial.ToString("0.0"));

    private Slider AddRangedSliderInternal(Transform content, string label, float min, float max, float initial,
        out TMP_Text valueText, Action<float> onChange, string initialText)
    {
        var rowRt = AddRowRoot(content, label);

        var sliderRt = NewRect("Slider", rowRt, new Vector2(0.48f, 0f), new Vector2(0.80f, 1f));
        sliderRt.offsetMin = Vector2.zero; sliderRt.offsetMax = Vector2.zero;

        var bgImg = sliderRt.gameObject.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.06f, 1f);

        var fillAreaRt = NewRect("FillArea", sliderRt, Vector2.zero, Vector2.one);
        fillAreaRt.offsetMin = new Vector2(2f, 2f); fillAreaRt.offsetMax = new Vector2(-2f, -2f);
        var fillRt = NewRect("Fill", fillAreaRt, Vector2.zero, new Vector2(1f, 1f));
        fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
        var fillImg = fillRt.gameObject.AddComponent<Image>();
        fillImg.color = UIPalette.Sage;
        fillImg.raycastTarget = false;

        var slider = sliderRt.gameObject.AddComponent<Slider>();
        slider.targetGraphic = null;
        slider.fillRect = fillRt;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min; slider.maxValue = max; slider.wholeNumbers = false;
        slider.SetValueWithoutNotify(initial);

        valueText = NewText("Value", rowRt, initialText, 14, ParchDim, TextAlignmentOptions.MidlineRight);
        valueText.rectTransform.anchorMin = new Vector2(0.82f, 0f); valueText.rectTransform.anchorMax = new Vector2(1f, 1f);
        valueText.rectTransform.offsetMin = Vector2.zero; valueText.rectTransform.offsetMax = Vector2.zero;
        valueText.raycastTarget = false;

        slider.onValueChanged.AddListener(v => onChange(v));

        return slider;
    }

    private TMP_Text AddToggleRow(Transform content, string label, bool initial, Action<bool> onChange)
    {
        var rowRt = AddRowRoot(content, label);
        bool state = initial;

        var btnRt = NewRect("Toggle", rowRt, new Vector2(0.58f, 0f), new Vector2(0.92f, 1f));
        btnRt.offsetMin = Vector2.zero; btnRt.offsetMax = Vector2.zero;
        var img = btnRt.gameObject.AddComponent<Image>();
        img.sprite = Spr("ui_button"); img.type = Image.Type.Sliced;
        var btn = btnRt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        SetSpriteState(btn, Spr("ui_button"), Spr("ui_button_hover"));

        var valueText = NewText("Value", btnRt, state ? "ON" : "OFF", 14, Accent, TextAlignmentOptions.Center);
        valueText.raycastTarget = false;
        if (anton != null) valueText.font = anton;
        valueText.characterSpacing = 3f;

        btn.onClick.AddListener(() =>
        {
            state = !state;
            valueText.text = state ? "ON" : "OFF";
            onChange(state);
        });

        return valueText;
    }

    private TMP_Text AddCyclerRow(Transform content, string label, string initialValue, Action<int> onStep)
    {
        var rowRt = AddRowRoot(content, label);

        var prevRt = NewRect("Prev", rowRt, new Vector2(0.48f, 0f), new Vector2(0.60f, 1f));
        prevRt.offsetMin = Vector2.zero; prevRt.offsetMax = Vector2.zero;
        BuildArrowButton(prevRt, "<", () => onStep(-1));

        var valueRt = NewRect("Value", rowRt, new Vector2(0.60f, 0f), new Vector2(0.84f, 1f));
        valueRt.offsetMin = Vector2.zero; valueRt.offsetMax = Vector2.zero;
        var valueText = NewText("Text", valueRt, initialValue, 14, Parch, TextAlignmentOptions.Center);
        valueText.raycastTarget = false;

        var nextRt = NewRect("Next", rowRt, new Vector2(0.84f, 0f), new Vector2(0.96f, 1f));
        nextRt.offsetMin = Vector2.zero; nextRt.offsetMax = Vector2.zero;
        BuildArrowButton(nextRt, ">", () => onStep(1));

        return valueText;
    }

    /// <summary>A label + a single button showing the current binding; click to start listening
    /// for a new key. No left/right arrows (rebinding isn't a cycle through fixed values).</summary>
    private TMP_Text AddCyclerlessBindRow(Transform content, string label, string initialValue, Action onClick)
    {
        var rowRt = AddRowRoot(content, label);

        var btnRt = NewRect("Bind", rowRt, new Vector2(0.48f, 0f), new Vector2(0.96f, 1f));
        btnRt.offsetMin = Vector2.zero; btnRt.offsetMax = Vector2.zero;
        var img = btnRt.gameObject.AddComponent<Image>();
        img.sprite = Spr("ui_button"); img.type = Image.Type.Sliced;
        var btn = btnRt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        SetSpriteState(btn, Spr("ui_button"), Spr("ui_button_hover"));

        var valueText = NewText("Value", btnRt, initialValue, 13, Parch, TextAlignmentOptions.Center);
        valueText.raycastTarget = false;
        if (anton != null) valueText.font = anton;
        valueText.characterSpacing = 2f;

        btn.onClick.AddListener(() => onClick());

        return valueText;
    }

    /// <summary>A full-width action button with no label column (e.g. "Reset Bindings").</summary>
    private void AddPlainButtonRow(Transform content, string label, Action onClick)
    {
        var rowRt = NewRect("Row_" + label, content, new Vector2(0f, 1f), new Vector2(1f, 1f));
        rowRt.pivot = new Vector2(0f, 1f);
        rowRt.anchoredPosition = new Vector2(SidePad, -cursorY);
        rowRt.sizeDelta = new Vector2(-SidePad * 2f, RowHeight);
        cursorY += RowHeight + 6f;

        var img = rowRt.gameObject.AddComponent<Image>();
        img.sprite = Spr("ui_button"); img.type = Image.Type.Sliced;
        var btn = rowRt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        SetSpriteState(btn, Spr("ui_button"), Spr("ui_button_hover"));
        btn.onClick.AddListener(() => onClick());

        var labelText = NewText("Label", rowRt, label, 14, Accent, TextAlignmentOptions.Center);
        labelText.raycastTarget = false;
        if (anton != null) labelText.font = anton;
        labelText.characterSpacing = 3f;
        labelText.fontStyle = FontStyles.UpperCase;
    }

    private void BuildArrowButton(RectTransform rt, string glyph, Action onClick)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = Spr("ui_button"); img.type = Image.Type.Sliced;
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        SetSpriteState(btn, Spr("ui_button"), Spr("ui_button_hover"));
        btn.onClick.AddListener(() => onClick());

        var label = NewText("Label", rt, glyph, 16, Accent, TextAlignmentOptions.Center);
        label.raycastTarget = false;
        if (anton != null) label.font = anton;
    }

    private static void SetSpriteState(Button btn, Sprite normal, Sprite hover)
    {
        btn.transition = Selectable.Transition.SpriteSwap;
        var state = btn.spriteState;
        state.highlightedSprite = hover != null ? hover : normal;
        state.pressedSprite = hover != null ? hover : normal;
        btn.spriteState = state;
    }

    // --------------------------------------------------------------- helpers
    private RectTransform NewRect(string name, Transform parent, Vector2 aMin, Vector2 aMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        return rt;
    }

    private TMP_Text NewText(string name, Transform parent, string text, float size, Color col, TextAlignmentOptions align)
    {
        var rt = NewRect(name, parent, Vector2.zero, Vector2.one);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = col; t.alignment = align;
        if (anton != null) t.font = anton;
        return t;
    }
}
