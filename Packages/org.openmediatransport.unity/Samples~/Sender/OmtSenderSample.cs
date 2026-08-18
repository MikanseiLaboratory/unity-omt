using UnityEngine;

namespace OpenMediaTransport.Samples
{
    public sealed class OmtSenderSample : MonoBehaviour
    {
        public OmtSender sender;
        public OmtCaptureMethod method = OmtCaptureMethod.GameView;
        public Camera sourceCamera;
        public Texture sourceTexture;

        void Awake()
        {
            if (sender == null)
                sender = GetComponent<OmtSender>();
            if (sender == null)
                sender = gameObject.AddComponent<OmtSender>();
            sender.captureMethod = method;
            if (sourceCamera != null)
                sender.sourceCamera = sourceCamera;
            if (sourceTexture != null)
                sender.sourceTexture = sourceTexture;
            if (string.IsNullOrEmpty(sender.omtName))
                sender.omtName = "Unity Sender";
        }
    }
}
