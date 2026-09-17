using System;

/// <summary>
/// The generic "has this gameplay session ended, and how" signal. Exists so
/// GameUIController doesn't need to know about any specific objective/quest
/// system to show a victory screen - it was previously polling
/// DemoObjectiveManager.IsDemoCompleted directly every frame, which meant the
/// whole game-flow depended on one specifically-named, single-linear-objective
/// class. Any future win-condition source (a second boss, a quest system,
/// multiple objectives) reports through here instead.
///
/// Deliberately just two flags and an event, not a state machine - pause and
/// death already work fine as their own simple flags (see GameUIController),
/// and this doesn't need to model more than "did the session end in victory".
/// </summary>
public static class GameSession
{
    public static bool HasEnded { get; private set; }
    public static bool Won { get; private set; }

    /// <summary>Fired once, the moment a victory is reported.</summary>
    public static event Action Victory;

    public static void ReportVictory()
    {
        if (HasEnded)
            return;

        HasEnded = true;
        Won = true;
        Victory?.Invoke();
    }

    /// <summary>Call at the start of a fresh gameplay session (scene load) - these
    /// are static fields, so they'd otherwise leak across a scene reload within
    /// the same play session.</summary>
    public static void Reset()
    {
        HasEnded = false;
        Won = false;
    }
}
