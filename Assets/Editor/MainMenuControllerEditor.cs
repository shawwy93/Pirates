using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(MainMenuController))]
public class MainMenuControllerEditor : Editor
{
    const string PreviewName = "~MenuMapPreview";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var ctrl = (MainMenuController)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Map Backdrop Preview", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Spawn a preview of the map backdrop so you can tune scale and rotation in edit mode. " +
            "Adjust the preview with the Scene-view Transform handles, then press Capture to save the values onto this controller.",
            MessageType.Info);

        GameObject preview = FindPreview(ctrl);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(preview == null ? "▶  Show Preview" : "↻  Refresh Preview"))
                ShowPreview(ctrl);

            GUI.enabled = preview != null;
            if (GUILayout.Button("⤴  Capture From Preview"))
                CapturePreview(ctrl, preview);

            if (GUILayout.Button("✕  Hide Preview"))
                HidePreview(ctrl);
            GUI.enabled = true;
        }

        if (preview != null)
            EditorGUILayout.HelpBox(
                "Preview active. It will NOT be saved with the scene — hit Capture to persist its scale/rotation onto the controller fields.",
                MessageType.None);
    }

    static GameObject FindPreview(MainMenuController ctrl)
    {
        for (int i = 0; i < ctrl.transform.childCount; i++)
        {
            var child = ctrl.transform.GetChild(i);
            if (child != null && child.name == PreviewName) return child.gameObject;
        }
        return null;
    }

    void ShowPreview(MainMenuController ctrl)
    {
        var so = new SerializedObject(ctrl);
        var prefabProp = so.FindProperty("mapBackdropPrefab");
        var scaleProp  = so.FindProperty("mapBackdropScale");
        var eulerProp  = so.FindProperty("mapBackdropLocalEuler");

        if (prefabProp == null || prefabProp.objectReferenceValue == null)
        {
            EditorUtility.DisplayDialog(
                "No Map Prefab",
                "Assign a prefab to 'Map Backdrop Prefab' first, or run " +
                "Pirates Plight ▶ Dress Main Menu to auto-wire SM_Item_Map_03.",
                "OK");
            return;
        }

        var existing = FindPreview(ctrl);
        if (existing != null) Object.DestroyImmediate(existing);

        var prefab = (GameObject)prefabProp.objectReferenceValue;
        var go     = (GameObject)PrefabUtility.InstantiatePrefab(prefab, ctrl.transform);
        go.name    = PreviewName;
        go.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

        go.transform.localPosition = new Vector3(0f, 1.6f, 2.5f);
        go.transform.localRotation = Quaternion.Euler(eulerProp.vector3Value);
        go.transform.localScale    = scaleProp.vector3Value;

        foreach (var col in go.GetComponentsInChildren<Collider>())
            col.enabled = false;

        Selection.activeGameObject = go;
        SceneView.lastActiveSceneView?.FrameSelected();
    }

    void CapturePreview(MainMenuController ctrl, GameObject preview)
    {
        if (preview == null) return;

        Undo.RecordObject(ctrl, "Capture Map Backdrop Preview");

        var so = new SerializedObject(ctrl);
        so.FindProperty("mapBackdropScale").vector3Value       = preview.transform.localScale;
        so.FindProperty("mapBackdropLocalEuler").vector3Value  = preview.transform.localRotation.eulerAngles;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(ctrl.gameObject.scene);

        Debug.Log($"[MainMenu] Captured preview → scale {preview.transform.localScale}, " +
                  $"euler {preview.transform.localRotation.eulerAngles}.");
    }

    void HidePreview(MainMenuController ctrl)
    {
        var preview = FindPreview(ctrl);
        if (preview != null) Object.DestroyImmediate(preview);
    }
}
