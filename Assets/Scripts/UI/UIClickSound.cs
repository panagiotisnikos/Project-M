using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays a click SFX on every Button under this GameObject, without wiring each
/// one by hand. Drop on a Canvas root (or any parent of a set of buttons) -
/// finds every Button in children (including inactive panels) once at Awake and
/// appends a listener; existing Inspector-wired onClick calls are untouched.
/// New buttons added later under the same root need this re-run (or their own
/// listener) since the scan only happens once.
/// </summary>
public class UIClickSound : MonoBehaviour
{
    [SerializeField] private AudioClip clickSfx;
    [Range(0f, 1f)] [SerializeField] private float volume = 0.45f;

    private void Awake()
    {
        foreach (var button in GetComponentsInChildren<Button>(true))
        {
            button.onClick.AddListener(PlayClick);
        }
    }

    private void PlayClick()
    {
        CombatAudio.PlayUI(clickSfx, volume);
    }
}
