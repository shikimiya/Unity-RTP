
using System.Text.RegularExpressions;
using UnityEngine;

namespace randutil
{

    public class crypto_test : MonoBehaviour
    {
        public void Start()
        {
            TestCryptoRandomGenerator();

            TestCryptoUint64();
        }
        
        public void TestCryptoRandomGenerator()
        {
            var isLetter = @"^[a-zA-Z]+$";

            for (var i = 0; i < 10000; i++)
            {
                var (s, err) = crypto.GenerateCryptoRandomString(10, RandExtended.runesAlpha);

                if (err != null)
                {
                    Debug.LogError(err);
                }

                if (s.Length != 10)
                {
                    Debug.LogError("Generator returned invalid length");
                }

                if (!Regex.IsMatch(s, isLetter))
                {
                    Debug.LogError($"Generator returned unexpected character: {s}");
                }
            }
        }

        public void TestCryptoUint64()
        {
            ulong min = 0xFFFFFFFFFFFFFFFF;

            ulong max = 0;

            for (var i = 0; i < 10000; i++)
            {
                var (r, err) = crypto.CryptoUint64();

                if (err != null)
                {
                    Debug.LogError(err);
                }

                if (r < min)
                {
                    min = r;
                }

                if (r > max)
                {
                    max = r;
                }
            }

            if (min > 0x1000000000000000)
            {
                Debug.LogError("Value around lower boundary was not generated");
            }

            if (max < 0xF000000000000000)
            {
                Debug.LogError("Value around upper boundary was not generated");
            }
        }
    }
}