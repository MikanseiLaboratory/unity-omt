using UnityEngine;

namespace OpenMediaTransport.Samples
{
    public sealed class OmtReceiverSample : MonoBehaviour
    {
        public OmtReceiver receiver;
        public Renderer target;

        void Awake()
        {
            if (receiver == null)
                receiver = GetComponent<OmtReceiver>();
            if (receiver == null)
                receiver = gameObject.AddComponent<OmtReceiver>();
            if (target == null)
                target = GetComponent<Renderer>();
            if (target != null)
                receiver.targetRenderer = target;
        }
    }
}
