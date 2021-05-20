
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace randutil
{
    public static class RandExtended
    {
        public const string runesAlpha = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    }

    public class rand_test : MonoBehaviour
    {
        public void Start()
        {
            TestRandomGeneratorCollision();
        }
        
        
        
        public void TestRandomGeneratorCollision()
        {
            var g = MathRandomGeneratorExtended.NewMathRandomGenerator();
            

            var testCases = new Dictionary<string, Func<string>>
            {
                {"MathRandom", () => { return g.GenerateString(10, RandExtended.runesAlpha); }},
                
                {"CryptoRandom", () =>
                {
                    var (s, err) = crypto.GenerateCryptoRandomString(10, RandExtended.runesAlpha);

                    if (err != null)
                    {
                        Debug.LogError(err);
                    }

                    return s;
                }}
            };

            const int N = 100;

            const int iteration = 100;

            foreach (var testCase in testCases)
            {
                var tc = testCase;

                for (var iter = 0; iter < iteration; iter++)
                {
                    var mu = new Mutex();
                        
                    var rands = new List<string>(N);

                    for (var i = 0; i < N; i++)
                    {
                        Task t1 = Task.Run(() =>
                        {
                            var r = tc.Value.Invoke();

                            mu.WaitOne();
                                
                            rands.Add(r);
                                
                            mu.ReleaseMutex();
                        });

                        t1.Wait();
                    }

                    if (rands.Count != N)
                    {
                        Debug.LogError("Failed to generate randoms");
                    }

                    for (var i = 0; i < N; i++)
                    {
                        for (var j = i + 1; j < N; j++)
                        {
                            if (rands[i] == rands[j])
                            {
                                Debug.Log($"generateRandString caused collision: {rands[i]} == {rands[j]}");
                            }
                        }
                    }
                    
                }
            }
        }
    }
}
