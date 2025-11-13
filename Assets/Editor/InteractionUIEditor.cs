using UnityEditor;
using UnityEngine;

// Custom inspector for InteractionUI to add an Editor-only Preview button
[CustomEditor(typeof(InteractionUI))]
public class InteractionUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw the default inspector first
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Editor Preview", EditorStyles.boldLabel);

        // Get the preview memory property (declared in InteractionUI)
        SerializedProperty previewProp = serializedObject.FindProperty("editorPreviewMemory");
        if (previewProp != null)
        {
            EditorGUILayout.PropertyField(previewProp, new GUIContent("Preview Memory"));
        }
        else
        {
            EditorGUILayout.HelpBox("No editor preview property found on InteractionUI.", MessageType.Info);
        }

        InteractionUI ui = (InteractionUI)target;

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = previewProp != null && previewProp.objectReferenceValue != null;
        if (GUILayout.Button("Preview Memory"))
        {
            MemoryData mem = (MemoryData)previewProp.objectReferenceValue;
            ui.ShowMemory(mem);
            EditorUtility.SetDirty(ui);
        }
        GUI.enabled = true;

        if (GUILayout.Button("Close Preview"))
        {
            ui.HideMemory();
            EditorUtility.SetDirty(ui);
        }
        EditorGUILayout.EndHorizontal();

        // Navigation controls while previewing
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Prev Spread"))
        {
            ui.PrevPage();
            EditorUtility.SetDirty(ui);
        }
        if (GUILayout.Button("Next Spread"))
        {
            ui.NextPage();
            EditorUtility.SetDirty(ui);
        }
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }
}
