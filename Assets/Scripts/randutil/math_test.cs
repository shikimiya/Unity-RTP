
using System.Text.RegularExpressions;
using UnityEngine;

namespace randutil
{

    public class math_test : MonoBehaviour
    {
        public void Start()
        {
            TestMathRandomGenerator();

            TestIntn();

            TestUint64();

            TestUint32();
        }
        
        public void TestMathRandomGenerator()
        {
            var g = MathRandomGeneratorExtended.NewMathRandomGenerator();
            
            var isLetter = @"^[a-zA-Z]+$";

            for (var i = 0; i < 10000; i++)
            {
                var s = g.GenerateString(10, RandExtended.runesAlpha);

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

        public void TestIntn()
        {
            var g = MathRandomGeneratorExtended.NewMathRandomGenerator();

            var min = 100;

            var max = 0;

            for (var i = 0; i < 10000; i++)
            {
                var r = g.Intn(100);

                if (r < 0 || r >= 100)
                {
                    Debug.LogError($"Out of range Intn(100): {r}");
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

            if (min > 1)
            {
                Debug.LogError("Value around lower boundary was not generated");
            }

            if (max < 90)
            {
                Debug.LogError("Value around upper boundary was not generated");
            }
        }

        public void TestUint64()
        {
            var g = MathRandomGeneratorExtended.NewMathRandomGenerator();

            ulong min = 0xFFFFFFFFFFFFFFFF;

            ulong max = 0;

            for (var i = 0; i < 10000; i++)
            {
                var r = g.Uint64();

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

        public void TestUint32()
        {
            var g = MathRandomGeneratorExtended.NewMathRandomGenerator();

            uint min = 0xFFFFFFFF;

            uint max = 0;

            for (var i = 0; i < 10000; i++)
            {
                var r = g.Uint32();

                if (r < min)
                {
                    min = r;
                }

                if (r > max)
                {
                    max = r;
                }
            }

            if (min > 0x10000000)
            {
                Debug.LogError("Value around lower boundary was not generated");
            }

            if (max < 0xF0000000)
            {
                Debug.LogError("Value around upper boundary was not generated");
            }
        }
    }

}