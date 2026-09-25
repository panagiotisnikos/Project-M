using UnityEngine;

/// <summary>
/// Drop this on any ScreenSpaceOverlay Canvas root to make it respect the player's UI Scale
/// accessibility setting (GameSettings.UIScale). Canvas.scaleFactor scales the whole canvas
/// independently of whatever CanvasScaler mode that canvas already uses, so this is safe to
/// add to an existing panel without touching its layout code.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class UIScaleFollower : MonoBehaviour
{
    private Canvas canvas;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        Apply();
        GameSettings.Changed += Apply;
    }

    private void OnDestroy()
    {
        GameSettings.Changed -= Apply;
    }

    private void Apply()
    {
        if (canvas != null) canvas.scaleFactor = GameSettings.UIScale;
    }
}
