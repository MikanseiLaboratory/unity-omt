using UnityEngine;

namespace OpenMediaTransport.Samples
{
    public sealed class Spin : MonoBehaviour
    {
        public Vector3 degreesPerSecond = new Vector3(0f, 45f, 20f);

        void Update()
        {
            transform.Rotate(degreesPerSecond * Time.deltaTime, Space.World);
        }
    }
}
