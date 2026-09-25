using UnityEngine;

/// <summary>
/// Hides this GameObject in web (WebGL) builds - for controls that mean nothing in a browser
/// tab, like "Quit" (Application.Quit is a no-op on the web; the player just closes the tab).
/// Any surrounding layout group closes the gap automatically.
/// </summary>
public class HideOnWeb : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    private void Awake()
    {
        gameObject.SetActive(false);
    }
#endif
}
