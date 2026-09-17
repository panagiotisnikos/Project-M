using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Reads/writes the one save file (JSON, Application.persistentDataPath).
/// Same shape as GameSettings/GameAudioSettings - a static class, no MonoBehaviour
/// needed, single source of truth. Actually applying a SaveData to the live scene
/// is GameSaveController's job; this class only ever touches the file.
/// </summary>
public static class SaveSystem
{
    private const string FileName = "savegame.json";

    private static string FilePath =>
        Path.Combine(Application.persistentDataPath, FileName);

    public static bool HasSave => File.Exists(FilePath);

    public static void Save(SaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(FilePath, json);
            Debug.Log($"[SaveSystem] Saved to {FilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
        }
    }

    /// <summary>Returns null if there is no save file or it failed to parse.</summary>
    public static SaveData Load()
    {
        if (!HasSave)
            return null;

        try
        {
            string json = File.ReadAllText(FilePath);
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Load failed: {e.Message}");
            return null;
        }
    }

    public static void DeleteSave()
    {
        if (HasSave)
            File.Delete(FilePath);
    }
}
