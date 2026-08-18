using System;
using UnityEngine;

namespace OpenMediaTransport
{
    public sealed class OmtResources : ScriptableObject
    {
        public ComputeShader encoderCompute;
        public ComputeShader decoderCompute;

        public static OmtResources LoadDefault()
        {
            var loaded = Resources.Load<OmtResources>("OmtResources");
            if (loaded != null)
                return loaded;

#if UNITY_EDITOR
            loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<OmtResources>(
                "Packages/org.openmediatransport.unity/Runtime/Resources/OmtResources.asset");
#endif
            return loaded;
        }

        public bool IsValid => encoderCompute != null && decoderCompute != null;
    }
}
