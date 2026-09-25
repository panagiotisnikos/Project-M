using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Discrete, keyboard-rebindable player actions. Mouse buttons (attack, block) and
/// analog axes (movement, camera look) are not covered - see KeyBindings' class doc.</summary>
public enum GameAction
{
    Interact,
    Dodge,
    ToggleInventory,
    Pause,
    ToggleControlsOverlay,
}

/// <summary>
/// Persisted (PlayerPrefs) keyboard rebinding for discrete player actions. Mirrors GameSettings'
/// shape/pattern. A component that owns a rebindable key calls KeyBindings.Get(action) at the
/// Input.GetKeyDown/GetKey call site instead of reading a local [SerializeField] KeyCode.
///
/// Scope: keyboard discrete actions only. Attack and Block are mouse buttons (0/1) and Movement/
/// Camera are analog axes - rebinding those cleanly needs a bindable-input-type abstraction
/// (KeyCode vs MouseButton vs Axis), not just a KeyCode swap, so V1 leaves them out rather than
/// building that generalization speculatively. Adding it later means adding an InputBinding
/// struct here and switching Get() callers accordingly - this enum-keyed dictionary shape is
/// already the right foundation for that, keyboard or controller.
/// </summary>
public static class KeyBindings
{
    private const string KeyPrefix = "keybind_";

    public static readonly GameAction[] AllActions = (GameAction[])Enum.GetValues(typeof(GameAction));

    private static readonly Dictionary<GameAction, KeyCode> Defaults = new Dictionary<GameAction, KeyCode>
    {
        { GameAction.Interact, KeyCode.E },
        { GameAction.Dodge, KeyCode.Space },
        { GameAction.ToggleInventory, KeyCode.Tab },
        { GameAction.Pause, KeyCode.Escape },
        { GameAction.ToggleControlsOverlay, KeyCode.F1 },
    };

    private static readonly Dictionary<GameAction, KeyCode> current = Load();

    public static event Action Changed;

    private static Dictionary<GameAction, KeyCode> Load()
    {
        var d = new Dictionary<GameAction, KeyCode>();
        foreach (var action in AllActions)
        {
            string saved = PlayerPrefs.GetString(KeyPrefix + action, Defaults[action].ToString());
            d[action] = Enum.TryParse(saved, out KeyCode kc) ? kc : Defaults[action];
        }
        return d;
    }

    public static KeyCode Get(GameAction action) => current.TryGetValue(action, out var kc) ? kc : Defaults[action];
    public static KeyCode GetDefault(GameAction action) => Defaults[action];

    /// <summary>Rebinds an action. If the key is already used elsewhere, the two actions swap
    /// keys (standard rebind UX) rather than leaving a silent duplicate binding.</summary>
    public static void Rebind(GameAction action, KeyCode key)
    {
        if (IsBound(key, out GameAction existing) && existing != action)
        {
            KeyCode displaced = current[action];
            current[existing] = displaced;
            PlayerPrefs.SetString(KeyPrefix + existing, displaced.ToString());
        }

        current[action] = key;
        PlayerPrefs.SetString(KeyPrefix + action, key.ToString());
        Changed?.Invoke();
    }

    public static void ResetToDefaults()
    {
        foreach (var action in AllActions)
        {
            current[action] = Defaults[action];
            PlayerPrefs.SetString(KeyPrefix + action, Defaults[action].ToString());
        }
        Changed?.Invoke();
    }

    public static bool IsBound(KeyCode key, out GameAction boundTo)
    {
        foreach (var kv in current)
        {
            if (kv.Value == key)
            {
                boundTo = kv.Key;
                return true;
            }
        }
        boundTo = default;
        return false;
    }

    public static string Label(GameAction action)
    {
        switch (action)
        {
            case GameAction.Interact: return "Interact";
            case GameAction.Dodge: return "Dodge / Roll";
            case GameAction.ToggleInventory: return "Toggle Inventory";
            case GameAction.Pause: return "Pause";
            case GameAction.ToggleControlsOverlay: return "Show Controls Help";
            default: return action.ToString();
        }
    }
}
