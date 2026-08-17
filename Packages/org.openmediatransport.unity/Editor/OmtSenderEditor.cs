using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenMediaTransport.Editor
{
    [CustomEditor(typeof(OmtSender))]
    [CanEditMultipleObjects]
    sealed class OmtSenderEditor : UnityEditor.Editor
    {
        SerializedProperty _omtName;
        SerializedProperty _quality;
        SerializedProperty _keepAlpha;
        SerializedProperty _captureMethod;
        SerializedProperty _sourceCamera;
        SerializedProperty _sourceTexture;
        SerializedProperty _frameRateN;
        SerializedProperty _frameRateD;

        void OnEnable()
        {
            _omtName = serializedObject.FindProperty("_omtName");
            _quality = serializedObject.FindProperty("_quality");
            _keepAlpha = serializedObject.FindProperty("_keepAlpha");
            _captureMethod = serializedObject.FindProperty("_captureMethod");
            _sourceCamera = serializedObject.FindProperty("_sourceCamera");
            _sourceTexture = serializedObject.FindProperty("_sourceTexture");
            _frameRateN = serializedObject.FindProperty("_frameRateN");
            _frameRateD = serializedObject.FindProperty("_frameRateD");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_omtName, new GUIContent("OMT Name"));
            EditorGUILayout.PropertyField(_quality);
            EditorGUILayout.PropertyField(_keepAlpha);
            EditorGUILayout.PropertyField(_captureMethod);
            var method = (OmtCaptureMethod)_captureMethod.enumValueIndex;
            if (method == OmtCaptureMethod.Camera)
                EditorGUILayout.PropertyField(_sourceCamera);
            if (method == OmtCaptureMethod.Texture)
                EditorGUILayout.PropertyField(_sourceTexture);
            EditorGUILayout.PropertyField(_frameRateN);
            EditorGUILayout.PropertyField(_frameRateD);

            var api = SystemInfo.graphicsDeviceType;
            if (api != GraphicsDeviceType.Direct3D11 && api != GraphicsDeviceType.Direct3D12 && api != GraphicsDeviceType.Metal)
                EditorGUILayout.HelpBox("OMT has been validated on D3D11, D3D12, and Metal. Current API: " + api, MessageType.Warning);
            if (method == OmtCaptureMethod.Camera && _sourceCamera.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Assign a Camera to capture.", MessageType.Warning);
            if (method == OmtCaptureMethod.Texture && _sourceTexture.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Assign a Texture or RenderTexture to capture.", MessageType.Warning);

            if (!serializedObject.isEditingMultipleObjects)
            {
                var sender = (OmtSender)target;
                EditorGUILayout.Space();
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.IntField("Connections", sender.connections);
                EditorGUI.EndDisabledGroup();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
