using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace OpenMediaTransport.Tests
{
    public class OmtPlayModeTests
    {
        [UnityTest]
        public IEnumerator SenderAndReceiverCanBeEnabledAndDisabled()
        {
            var resources = OmtResources.LoadDefault();
            if (resources == null || !resources.IsValid)
                Assert.Ignore("OmtResources are not available in this test host.");

            var senderGo = new GameObject("OMT Sender Test");
            var receiverGo = new GameObject("OMT Receiver Test");
            try
            {
                var sender = senderGo.AddComponent<OmtSender>();
                sender.SetResources(resources);
                sender.omtName = "UnityPlayModeTest";
                sender.captureMethod = OmtCaptureMethod.GameView;

                var receiver = receiverGo.AddComponent<OmtReceiver>();
                receiver.SetResources(resources);
                receiver.omtName = "omt://127.0.0.1:6522";

                yield return new WaitForSeconds(0.5f);
                sender.enabled = false;
                receiver.enabled = false;
                yield return null;
                Assert.Pass();
            }
            finally
            {
                Object.DestroyImmediate(senderGo);
                Object.DestroyImmediate(receiverGo);
            }
        }
    }
}
