using UnityEditor;
using UnityEngine;

/// <summary>Authoring-workflow helper for CampSpawner: one click to drop a new named
/// spawn point as a child, instead of manually creating + parenting + renaming an empty.</summary>
[CustomEditor(typeof(CampSpawner))]
public class CampSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Authoring Tools", EditorStyles.boldLabel);

        CampSpawner spawner = (CampSpawner)target;

        if (GUILayout.Button("Add Spawn Point Here"))
            AddSpawnPoint(spawner);
    }

    private void AddSpawnPoint(CampSpawner spawner)
    {
        serializedObject.Update();
        SerializedProperty spawnPointsProp = serializedObject.FindProperty("spawnPoints");

        GameObject go = new GameObject($"SpawnPoint_{spawnPointsProp.arraySize}");
        Undo.RegisterCreatedObjectUndo(go, "Add Spawn Point");
        go.transform.SetParent(spawner.transform, false);
        go.transform.localPosition = Vector3.zero;

        spawnPointsProp.arraySize++;
        spawnPointsProp.GetArrayElementAtIndex(spawnPointsProp.arraySize - 1).objectReferenceValue = go.transform;

        serializedObject.ApplyModifiedProperties();

        Selection.activeGameObject = go;
    }
}
