using UnityEditor;
using UnityEngine;

namespace OpenMediaTransport.Editor
{
    [CustomEditor(typeof(OmtReceiver))]
    [CanEditMultipleObjects]
    sealed class OmtReceiverEditor : UnityEditor.Editor
    {
        SerializedProperty _omtName;
        SerializedProperty _quality;
        SerializedProperty _preview;
        SerializedProperty _targetTexture;
        SerializedProperty _targetRenderer;
        SerializedProperty _targetMaterialProperty;

        void OnEnable()
        {
            _omtName = serializedObject.FindProperty("_omtName");
            _quality = serializedObject.FindProperty("_quality");
            _preview = serializedObject.FindProperty("_preview");
            _targetTexture = serializedObject.FindProperty("_targetTexture");
            _targetRenderer = serializedObject.FindProperty("_targetRenderer");
            _targetMaterialProperty = serializedObject.FindProperty("_targetMaterialProperty");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.DelayedTextField(_omtName, new GUIContent("OMT Name"));
            var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(70));
            if (EditorGUI.DropdownButton(rect, new GUIContent("Select"), FocusType.Keyboard))
                ShowSourceMenu(rect);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(_quality);
            EditorGUILayout.PropertyField(_preview);
            EditorGUILayout.PropertyField(_targetTexture);
            EditorGUILayout.PropertyField(_targetRenderer);
            if (_targetRenderer.objectReferenceValue != null)
                TexturePropertyDropdown();
            else
                EditorGUILayout.PropertyField(_targetMaterialProperty, new GUIContent("Property"));

            if (!serializedObject.isEditingMultipleObjects)
            {
                var receiver = (OmtReceiver)target;
                EditorGUILayout.Space();
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Toggle("Connected", receiver.isConnected);
                EditorGUILayout.IntField("Width", receiver.width);
                EditorGUILayout.IntField("Height", receiver.height);
                EditorGUILayout.IntField("Dropped", receiver.droppedFrames);
                EditorGUI.EndDisabledGroup();
            }

            serializedObject.ApplyModifiedProperties();
        }

        void ShowSourceMenu(Rect rect)
        {
            var menu = new GenericMenu();
            var sources = OmtFinder.EnumerateSourceNames();
            if (sources.Count == 0)
                menu.AddDisabledItem(new GUIContent("No source available"));
            else
            {
                foreach (var name in sources)
                    menu.AddItem(new GUIContent(name.Replace("/", "\\")), false, () =>
                    {
                        serializedObject.Update();
                        _omtName.stringValue = name;
                        serializedObject.ApplyModifiedProperties();
                    });
            }
            menu.DropDown(rect);
        }

        void TexturePropertyDropdown()
        {
            var renderer = _targetRenderer.objectReferenceValue as Renderer;
            if (renderer == null)
                return;
            var shader = renderer.sharedMaterial != null ? renderer.sharedMaterial.shader : null;
            if (shader == null)
            {
                EditorGUILayout.PropertyField(_targetMaterialProperty, new GUIContent("Property"));
                return;
            }

            var names = new System.Collections.Generic.List<string>();
            var count = ShaderUtil.GetPropertyCount(shader);
            for (var i = 0; i < count; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                    names.Add(ShaderUtil.GetPropertyName(shader, i));
            }
            if (names.Count == 0)
            {
                EditorGUILayout.PropertyField(_targetMaterialProperty, new GUIContent("Property"));
                return;
            }
            var current = Mathf.Max(0, names.IndexOf(_targetMaterialProperty.stringValue));
            var next = EditorGUILayout.Popup("Property", current, names.ToArray());
            _targetMaterialProperty.stringValue = names[next];
        }
    }
}
