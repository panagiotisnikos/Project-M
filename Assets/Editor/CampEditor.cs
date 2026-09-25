using UnityEditor;
using UnityEngine;

/// <summary>
/// Authoring-workflow helpers for Camp: "build the environment visually, then one click
/// wires up enemy registration and encounter bounds" - the two steps that would otherwise
/// need manual Inspector fiddling or a trip into Play mode to verify.
/// </summary>
[CustomEditor(typeof(Camp))]
public class CampEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Authoring Tools", EditorStyles.boldLabel);

        Camp camp = (Camp)target;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Rescan Children For Enemies"))
            {
                Undo.RecordObject(camp, "Rescan Camp Enemies");
                camp.RefreshEnemies();
                EditorUtility.SetDirty(camp);
            }

            if (GUILayout.Button("Add Encounter Bounds Trigger"))
            {
                AddEncounterBoundsTrigger(camp);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void AddEncounterBoundsTrigger(Camp camp)
    {
        SerializedProperty boundsProp = serializedObject.FindProperty("encounterBounds");
        if (boundsProp.objectReferenceValue != null)
        {
            EditorUtility.DisplayDialog("Camp", "Encounter Bounds is already assigned.", "OK");
            return;
        }

        BoxCollider box = camp.GetComponent<BoxCollider>();
        if (box == null)
            box = Undo.AddComponent<BoxCollider>(camp.gameObject);

        box.isTrigger = true;

        Bounds combined = new Bounds(camp.transform.position, Vector3.zero);
        bool any = false;
        foreach (var childRenderer in camp.GetComponentsInChildren<Renderer>())
        {
            if (!any) { combined = childRenderer.bounds; any = true; }
            else combined.Encapsulate(childRenderer.bounds);
        }

        if (!any)
        {
            // No renderers to measure yet (pure blockout, or nothing placed) - a
            // generic default box the author can resize by hand afterward.
            combined = new Bounds(camp.transform.position, new Vector3(10f, 4f, 10f));
        }
        else
        {
            combined.Expand(2f); // pad so the trigger reads as "the whole area", not flush with geometry
        }

        box.center = camp.transform.InverseTransformPoint(combined.center);
        Vector3 lossy = camp.transform.lossyScale;
        box.size = new Vector3(
            Mathf.Approximately(lossy.x, 0f) ? combined.size.x : combined.size.x / lossy.x,
            Mathf.Approximately(lossy.y, 0f) ? combined.size.y : combined.size.y / lossy.y,
            Mathf.Approximately(lossy.z, 0f) ? combined.size.z : combined.size.z / lossy.z);

        boundsProp.objectReferenceValue = box;
        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(camp);
    }
}
