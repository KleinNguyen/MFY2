#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SceneMusicTrack))]
public class SceneMusicTrackDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 4 + EditorGUIUtility.standardVerticalSpacing * 4;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty sceneName = property.FindPropertyRelative("sceneName");
        SerializedProperty clip = property.FindPropertyRelative("clip");
        SerializedProperty loop = property.FindPropertyRelative("loop");
        SerializedProperty continueCurrentTrack = property.FindPropertyRelative("continueCurrentTrack");

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        Rect sceneRect = new Rect(position.x, position.y, position.width, lineHeight);
        Rect clipRect = new Rect(position.x, sceneRect.yMax + spacing, position.width, lineHeight);
        Rect loopRect = new Rect(position.x, clipRect.yMax + spacing, position.width, lineHeight);
        Rect continueRect = new Rect(position.x, loopRect.yMax + spacing, position.width, lineHeight);

        List<string> sceneOptions = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled)
            {
                continue;
            }

            string name = System.IO.Path.GetFileNameWithoutExtension(scene.path);
            if (!string.IsNullOrEmpty(name) && !sceneOptions.Contains(name))
            {
                sceneOptions.Add(name);
            }
        }

        if (!string.IsNullOrEmpty(sceneName.stringValue) && !sceneOptions.Contains(sceneName.stringValue))
        {
            sceneOptions.Add(sceneName.stringValue);
        }

        if (sceneOptions.Count == 0)
        {
            EditorGUI.PropertyField(sceneRect, sceneName, new GUIContent("Scene Name"));
        }
        else
        {
            int selectedIndex = Mathf.Max(0, sceneOptions.IndexOf(sceneName.stringValue));
            int newIndex = EditorGUI.Popup(sceneRect, "Scene Name", selectedIndex, sceneOptions.ToArray());
            if (newIndex != selectedIndex)
            {
                sceneName.stringValue = sceneOptions[newIndex];
            }
        }

        EditorGUI.PropertyField(clipRect, clip, new GUIContent("Clip"));
        EditorGUI.PropertyField(loopRect, loop, new GUIContent("Loop"));
        EditorGUI.PropertyField(continueRect, continueCurrentTrack, new GUIContent("Continue Current Track"));

        EditorGUI.EndProperty();
    }
}
#endif
