using UnityEngine;

namespace OpenMediaTransport.Samples
{
    public sealed class OmtLoopbackSample : MonoBehaviour
    {
        public OmtSender sender;
        public OmtReceiver receiver;
        public Renderer target;
        public string sourceName = "Unity Loopback";

        void Awake()
        {
            if (sender == null)
                sender = gameObject.AddComponent<OmtSender>();
            if (receiver == null)
                receiver = gameObject.AddComponent<OmtReceiver>();
            sender.omtName = sourceName;
            sender.captureMethod = OmtCaptureMethod.GameView;
            receiver.omtName = sourceName;
            if (target != null)
                receiver.targetRenderer = target;
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 420, 120), GUI.skin.box);
            GUILayout.Label("OMT Loopback");
            GUILayout.Label("Sender connections: " + sender.connections);
            GUILayout.Label("Receiver: " + (receiver.isConnected ? receiver.width + "x" + receiver.height : "waiting"));
            GUILayout.Label("Dropped: " + receiver.droppedFrames);
            GUILayout.EndArea();
        }
    }
}
