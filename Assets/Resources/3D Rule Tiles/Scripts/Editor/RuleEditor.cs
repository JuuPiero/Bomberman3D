using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(Rule))]
public class RuleEditor : PropertyDrawer
{

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUILayout.LabelField("----------------------Rule-------------------------");
        GUILayoutOption[] guiLayoutOptions = new GUILayoutOption[2];
        guiLayoutOptions[0] = GUILayout.Width(40);
        guiLayoutOptions[1] = GUILayout.Height(20);

        EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(property.FindPropertyRelative("TL"), GUIContent.none, guiLayoutOptions);
        GUILayout.Space(-15);
        EditorGUILayout.PropertyField(property.FindPropertyRelative("T"), GUIContent.none, guiLayoutOptions);
        GUILayout.Space(-15);
        EditorGUILayout.PropertyField(property.FindPropertyRelative("TR"), GUIContent.none, guiLayoutOptions);
        EditorGUILayout.LabelField("| Tile: ", GUILayout.Width(50));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("tile"), GUIContent.none);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(property.FindPropertyRelative("L"), GUIContent.none, guiLayoutOptions);
        GUILayout.Space(12.5f);
        EditorGUILayout.PropertyField(property.FindPropertyRelative("R"), GUIContent.none, guiLayoutOptions);
        EditorGUILayout.LabelField("| Rotate: ", GUILayout.Width(75));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("rotation"), GUIContent.none);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(property.FindPropertyRelative("BL"), GUIContent.none, guiLayoutOptions);
        GUILayout.Space(-15);
        EditorGUILayout.PropertyField(property.FindPropertyRelative("B"), GUIContent.none, guiLayoutOptions);
        GUILayout.Space(-15);
        EditorGUILayout.PropertyField(property.FindPropertyRelative("BR"), GUIContent.none, guiLayoutOptions);
        EditorGUILayout.LabelField("| Position Offset: ", GUILayout.Width(125));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("position_offset"), GUIContent.none);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.LabelField("---------------------------------------------------");

    }

}
