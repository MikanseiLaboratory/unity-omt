using UnityEngine;

namespace OpenMediaTransport.Samples
{
    public sealed class OmtLoopbackDriver : MonoBehaviour
    {
        public OmtSender sender;
        public OmtReceiver receiver;
        public string sourceName = "Unity Loopback";

        string _status = "waiting for discovery";

        void Awake()
        {
            if (sender == null)
                sender = GetComponent<OmtSender>();
            if (receiver == null)
                receiver = GetComponent<OmtReceiver>();
            if (sender != null && !string.IsNullOrEmpty(sourceName))
                sender.omtName = sourceName;
        }

        System.Collections.IEnumerator Start()
        {
            if (sender == null || receiver == null)
            {
                _status = "missing OmtSender or OmtReceiver";
                yield break;
            }

            var deadline = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < deadline)
            {
                foreach (var name in OmtFinder.sourceNames)
                {
                    if (name.IndexOf(sourceName, System.StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    receiver.omtName = name;
                    _status = "connected to " + name;
                    yield break;
                }

                _status = "waiting for " + sourceName;
                yield return new WaitForSeconds(0.25f);
            }

            _status = "source not found — use Receiver Select, or type omt://127.0.0.1:6400";
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 420, 110), GUI.skin.box);
            GUILayout.Label("OMT Loopback");
            GUILayout.Label(_status);
            if (sender != null)
                GUILayout.Label("Sender connections: " + sender.connections);
            if (receiver != null)
            {
                GUILayout.Label(receiver.isConnected
                    ? "Receiver: " + receiver.width + "x" + receiver.height + "  dropped " + receiver.droppedFrames
                    : "Receiver: waiting");
            }
            GUILayout.EndArea();

            if (receiver != null && receiver.texture != null)
                GUI.DrawTexture(new Rect(16, 140, 480, 270), receiver.texture, ScaleMode.ScaleToFit);
        }
    }
}
