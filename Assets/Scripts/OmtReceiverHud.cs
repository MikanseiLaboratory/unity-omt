using UnityEngine;

namespace OpenMediaTransport.Samples
{
    public sealed class OmtReceiverHud : MonoBehaviour
    {
        public OmtReceiver receiver;

        void Awake()
        {
            if (receiver == null)
                receiver = GetComponent<OmtReceiver>();
        }

        void OnGUI()
        {
            if (receiver == null)
                return;

            GUILayout.BeginArea(new Rect(16, 16, 460, 360), GUI.skin.box);
            GUILayout.Label("OMT Receiver");
            GUILayout.Label(string.IsNullOrEmpty(receiver.omtName) ? "No source selected" : receiver.omtName);
            GUILayout.Label(receiver.isConnected
                ? receiver.width + "x" + receiver.height
                : "waiting");
            GUILayout.Label("Dropped: " + receiver.droppedFrames);
            GUILayout.Space(8);
            GUILayout.Label("Sources");
            foreach (var name in OmtFinder.sourceNames)
            {
                if (GUILayout.Button(name))
                    receiver.omtName = name;
            }
            GUILayout.EndArea();
        }
    }
}
