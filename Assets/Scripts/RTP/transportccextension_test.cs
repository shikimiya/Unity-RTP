
using System.Collections.Generic;
using UnityEngine;

namespace rtp
{
    public class transportccextension_test : MonoBehaviour
    {
        public void Start()
        {
            TestTransportCCExtensionTooSmall();

            TestTransportCCExtension();

            TestTransportCCExtensionExtraBytes();
        }

        public void TestTransportCCExtensionTooSmall()
        {
            var t1 = new TransportCCExtension();

            var rawData = new List<byte>();

            var err = t1.Unmarshal(rawData);

            if (err != error.errTooSmall)
            {
                Debug.LogError("err != errTooSmall");
            }
        }

        public void TestTransportCCExtension()
        {
            var t1 = new TransportCCExtension();

            var rawData = new List<byte>
            {
                0x00, 0x02,
            };

            var err = t1.Unmarshal(rawData);

            if (err != null)
            {
                Debug.LogError($"Unmarshal error on extension data");
            }

            var t2 = new TransportCCExtension
            {
                TransportSequence = 2,
            };

            if (t1.TransportSequence != t2.TransportSequence)
            {
                Debug.LogError($"Unmarshal error on extension data");
            }

            var (dstData, _) = t2.Marshal();

            for (int i = 0; i < rawData.Count; i++)
            {
                if (dstData[i] != rawData[i])
                {
                    Debug.LogError($"Marshal failed");
                }
            }
        }

        public void TestTransportCCExtensionExtraBytes()
        {
            var t1 = new TransportCCExtension();

            var rawData = new List<byte>
            {
                0x00, 0x02, 0x00, 0xff, 0xff,
            };

            var err = t1.Unmarshal(rawData);

            if (err != null)
            {
                Debug.LogError($"Unmarshal error on extension data");
            }

            var t2 = new TransportCCExtension
            {
                TransportSequence = 2,
            };

            if (t1.TransportSequence != t2.TransportSequence)
            {
                Debug.LogError($"Unmarshal failed");
            }
        }
    }
}