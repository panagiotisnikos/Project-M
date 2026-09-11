using UnityEngine;

/// <summary>
/// Keeps the main menu's looping music track scaled by GameAudioSettings.Master
/// * .Music, same as MusicManager does for the gameplay scene - so the options
/// menu's Music slider (and Master slider) actually affects the menu you'd most
/// likely be sitting on when you open Options in the first place.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MenuMusicController : MonoBehaviour
{
    [Tooltip("Volume at 100% Master/Music.")]
    [Range(0f, 1f)] [SerializeField] private float baseVolume = 0.18f;

    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    private void Update()
    {
        source.volume = baseVolume * GameAudioSettings.Master * GameAudioSettings.Music;
    }
}
