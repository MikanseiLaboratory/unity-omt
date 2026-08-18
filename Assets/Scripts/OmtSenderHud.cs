using UnityEngine;

namespace OpenMediaTransport.Samples
{
    public sealed class OmtSenderHud : MonoBehaviour
    {
        public OmtSender sender;

        void Awake()
        {
            if (sender == null)
                sender = GetComponent<OmtSender>();
        }

        void OnGUI()
        {
            if (sender == null)
                return;
            GUILayout.BeginArea(new Rect(16, 16, 420, 80), GUI.skin.box);
            GUILayout.Label("OMT Sender: " + sender.omtName);
            GUILayout.Label("Connections: " + sender.connections);
            GUILayout.EndArea();
        }
    }
}
