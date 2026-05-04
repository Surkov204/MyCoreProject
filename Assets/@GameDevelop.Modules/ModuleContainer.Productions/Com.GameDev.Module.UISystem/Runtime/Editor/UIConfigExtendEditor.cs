#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Com.GameDev.Module.UISystem
{
    [CustomEditor(typeof(UIConfigExtend))]
    public class UIConfigExtendEditor : UnityEditor.Editor
    {
        private SerializedProperty uiList;

        private void OnEnable()
        {
            uiList = serializedObject.FindProperty("uiList");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawList();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawList()
        {
            EditorGUILayout.LabelField("UI List", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("+", GUILayout.Width(28)))
            {
                uiList.arraySize++;
                SerializedProperty element = uiList.GetArrayElementAtIndex(uiList.arraySize - 1);
                ResetElement(element);
            }

            if (GUILayout.Button("-", GUILayout.Width(28)))
            {
                if (uiList.arraySize > 0)
                    uiList.arraySize--;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            for (int i = 0; i < uiList.arraySize; i++)
            {
                SerializedProperty element = uiList.GetArrayElementAtIndex(i);
                DrawElement(element, i);
                EditorGUILayout.Space(8);
            }
        }

        private void DrawElement(SerializedProperty element, int index)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"Element {index}", EditorStyles.boldLabel);

            if (GUILayout.Button("↑", GUILayout.Width(24)))
            {
                MoveElement(index, index - 1);
            }

            if (GUILayout.Button("↓", GUILayout.Width(24)))
            {
                MoveElement(index, index + 1);
            }

            if (GUILayout.Button("X", GUILayout.Width(24)))
            {
                uiList.DeleteArrayElementAtIndex(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(element.FindPropertyRelative("prefab"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("canvasType"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("animationType"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("customSortingOrder"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("lifecycle"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("showOnInit"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("beforeShowAction"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("requireGraphicRaycaster"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("note"));

            EditorGUILayout.EndVertical();
        }

        private void ResetElement(SerializedProperty element)
        {
            element.FindPropertyRelative("prefab").objectReferenceValue = null;
            element.FindPropertyRelative("canvasType").enumValueIndex = (int)CanvasType.FullScreen;
            element.FindPropertyRelative("animationType").enumValueIndex = (int)UIAnimationType.FadeScale;
            element.FindPropertyRelative("customSortingOrder").intValue = UIConfigExtend.UseDefaultSortingOrder;
            element.FindPropertyRelative("lifecycle").enumValueIndex = (int)UILifecycle.SpawnOnDemandCached;
            element.FindPropertyRelative("showOnInit").boolValue = false;
            element.FindPropertyRelative("beforeShowAction").enumValueIndex = (int)UIBeforeShowAction.None;
            element.FindPropertyRelative("requireGraphicRaycaster").boolValue = true;
            element.FindPropertyRelative("note").stringValue = string.Empty;
        }

        private void MoveElement(int from, int to)
        {
            if (to < 0 || to >= uiList.arraySize)
                return;

            uiList.MoveArrayElement(from, to);
        }
    }
}

#endif