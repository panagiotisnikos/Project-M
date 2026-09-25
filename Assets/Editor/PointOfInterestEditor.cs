using UnityEditor;
using UnityEngine;

/// <summary>
/// Authoring-workflow helpers for PointOfInterest: one click to preview/resize the
/// discovery trigger in Edit mode, plus quick Play-mode test buttons so content can be
/// iterated on without physically walking up to it every time.
/// </summary>
[CustomEditor(typeof(PointOfInterest))]
public class PointOfInterestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Authoring Tools", EditorStyles.boldLabel);

        PointOfInterest poi = (PointOfInterest)target;

        if (GUILayout.Button("Add/Resize Discovery Trigger"))
            ConfigureDiscoveryTrigger(poi);

        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Play Mode Testing", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Force Discover"))
                    poi.Discover();

                if (GUILayout.Button("Force Complete"))
                    poi.Complete();
            }

            EditorGUILayout.LabelField($"State: {poi.State}   RegionEligible: {poi.IsRegionEligible}");
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void ConfigureDiscoveryTrigger(PointOfInterest poi)
    {
        float radius = serializedObject.FindProperty("discoveryRadius").floatValue;

        SphereCollider sphere = poi.GetComponent<SphereCollider>();
        if (sphere == null)
            sphere = Undo.AddComponent<SphereCollider>(poi.gameObject);

        Undo.RecordObject(sphere, "Configure Discovery Trigger");
        sphere.isTrigger = true;
        sphere.center = Vector3.zero;
        sphere.radius = radius;

        EditorUtility.SetDirty(sphere);
    }
}
