using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only convenience window for the enemy framework's stated goal: creating a new
/// archetype should be "duplicate/create configuration -> assign model/animations ->
/// configure behaviour -> place prefab -> play", not a manual multi-step asset-wiring
/// chore. Picks an existing enemy prefab to clone (for its rig/Animator/collider setup),
/// makes a matching EnemyData asset (copying the source's current numbers as a starting
/// point if it has one), wires the two together, and hands control back to the Inspector
/// for the actual archetype design work.
///
/// Menu: Project M > Create Enemy Archetype...
/// </summary>
public class EnemyArchetypeWizard : EditorWindow
{
    private GameObject sourcePrefab;
    private string archetypeName = "";
    private const string PrefabFolder = "Assets/Prefabs";
    private const string DataFolder = "Assets/Data/Enemies";

    [MenuItem("Project M/Create Enemy Archetype...")]
    private static void Open()
    {
        var w = GetWindow<EnemyArchetypeWizard>(true, "Create Enemy Archetype");
        w.minSize = new Vector2(420, 200);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Clone an existing enemy prefab as the starting point.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("Its rig, Animator, collider and NavMeshAgent setup are reused as-is - " +
            "only the archetype's EnemyData (stats/behaviour) is new.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(10);

        sourcePrefab = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Source Prefab", "An existing enemy prefab with EnemyAI + EnemyHealth (e.g. Enemy_Stalker, Enemy_Brute, or any archetype already built)."),
            sourcePrefab, typeof(GameObject), false);

        archetypeName = EditorGUILayout.TextField(
            new GUIContent("New Archetype Name", "Used for both the prefab and the EnemyData asset - e.g. \"Ambusher\" becomes Enemy_Ambusher.prefab / .asset."),
            archetypeName);

        EditorGUILayout.Space(6);

        string error = Validate();
        if (!string.IsNullOrEmpty(error))
            EditorGUILayout.HelpBox(error, MessageType.Warning);
        else
            EditorGUILayout.HelpBox(
                $"Will create:\n" +
                $"  {DataFolder}/Enemy_{Safe(archetypeName)}.asset\n" +
                $"  {PrefabFolder}/Enemy_{Safe(archetypeName)}.prefab (clone of {(sourcePrefab != null ? sourcePrefab.name : "-")})",
                MessageType.Info);

        EditorGUILayout.Space(10);

        using (new EditorGUI.DisabledScope(!string.IsNullOrEmpty(error)))
        {
            if (GUILayout.Button("Create Archetype", GUILayout.Height(32)))
            {
                Create();
                Close();
            }
        }
    }

    private string Validate()
    {
        if (sourcePrefab == null)
            return "Pick a source prefab to clone.";

        if (sourcePrefab.GetComponent<EnemyAI>() == null || sourcePrefab.GetComponent<EnemyHealth>() == null)
            return "Source prefab needs both EnemyAI and EnemyHealth components.";

        if (string.IsNullOrWhiteSpace(archetypeName))
            return "Give the new archetype a name.";

        string prefabPath = $"{PrefabFolder}/Enemy_{Safe(archetypeName)}.prefab";
        string dataPath = $"{DataFolder}/Enemy_{Safe(archetypeName)}.asset";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null || AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath) != null)
            return $"Enemy_{Safe(archetypeName)} already exists - pick a different name.";

        return null;
    }

    private static string Safe(string name) => name.Trim().Replace(" ", "");

    private void Create()
    {
        string safeName = Safe(archetypeName);
        string prefabPath = $"{PrefabFolder}/Enemy_{safeName}.prefab";
        string dataPath = $"{DataFolder}/Enemy_{safeName}.asset";

        Directory.CreateDirectory(PrefabFolder);
        Directory.CreateDirectory(DataFolder);

        // --- EnemyData: start from the source's current numbers if it has any, else defaults ---
        var sourceAI = sourcePrefab.GetComponent<EnemyAI>();
        var newData = ScriptableObject.CreateInstance<EnemyData>();
        if (sourceAI.Data != null)
            EditorUtility.CopySerialized(sourceAI.Data, newData);
        newData.enemyName = archetypeName;
        AssetDatabase.CreateAsset(newData, dataPath);

        // --- Prefab: clone the source so the rig/Animator/collider/NavMeshAgent carry over ---
        string sourcePath = AssetDatabase.GetAssetPath(sourcePrefab);
        AssetDatabase.CopyAsset(sourcePath, prefabPath);
        AssetDatabase.ImportAsset(prefabPath);

        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        root.name = $"Enemy_{safeName}";
        var ai = root.GetComponent<EnemyAI>();
        var so = new SerializedObject(ai);
        so.FindProperty("enemyData").objectReferenceValue = newData;
        so.ApplyModifiedProperties();
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var createdData = AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath);
        EditorGUIUtility.PingObject(createdData);
        Selection.activeObject = createdData;

        Debug.Log(
            $"[EnemyArchetypeWizard] Created {dataPath} + {prefabPath} (cloned from {sourcePrefab.name}).\n" +
            "Next: configure its stats/attacks in the Inspector (now selected), reassign the model/" +
            "Animator on the prefab if it needs different visuals/animations, then drag the prefab " +
            "into a scene (or a Camp / CampSpawner slot) and press Play."
        );
    }
}
