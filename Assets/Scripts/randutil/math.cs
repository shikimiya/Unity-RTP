using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace randutil
{
    // MathRandomGeneratorは、暗号以外で使用するためのランダムジェネレーターです。
    public interface MathRandomGenerator
    {
        // Intnは[0：n）内のランダムな整数を返します。
        int Intn(int n);
        
        // Uint32は、ランダムな32ビットの符号なし整数を返します。
        uint Uint32();
        
        // Uint64は、ランダムな64ビットの符号なし整数を返します。
        ulong Uint64();
        
        // Generate Stringは、指定されたルーンのセットを使用してランダムな文字列を返します。
        //名前の衝突を避けるために一意のIDを生成するために使用できます。
        //
        //注意：これを暗号化の使用に使用しないでください。
        string String();
    }

    public static class MathRandomGeneratorExtended
    {
        // NewMath Random Generatorは、新しい数学的ランダムジェネレーターを作成します。
        //ランダムジェネレーターは暗号ランダムによってシードされます。
        public static mathRandomGenerator NewMathRandomGenerator()
        {
            var (seed, err) = crypto.CryptoUint64();

            if (err != null)
            {
                seed = Convert.ToUInt64(DateTime.UtcNow.ToBinary());
            }

            var m = new mathRandomGenerator
            {
                r = new Random(seed.GetHashCode()),
            };

            return m;
        }
    }

    public class mathRandomGenerator
    {
        public System.Random r;

        public Mutex mu;

        public mathRandomGenerator()
        {
            r = new Random();
            
            mu = new Mutex();
        }

        public int Intn(int n)
        {
            mu.WaitOne();

            var v = r.Next(n);
            
            mu.ReleaseMutex();

            return v;
        }

        public uint Uint32()
        {
            mu.WaitOne();

            var rng = new RNGCryptoServiceProvider();

            var b = new byte[4];

            var l = new List<byte>(4);

            for (var i = 0; i < 4; i++)
            {
                rng.GetBytes(b);

                var seed = BitConverter.ToInt32(b, 0);

                var pos = new Random(seed).Next(4);

                var c = b[pos];

                l.Add(c);
            }
            
            rng.Dispose();

            var u = BitConverter.ToUInt32(l.ToArray(), 0);
            
            mu.ReleaseMutex();

            return u;
        }

        public ulong Uint64()
        {
            mu.WaitOne();

            var rng = new RNGCryptoServiceProvider();

            var b = new byte[8];

            var l = new List<byte>(8);

            for (var i = 0; i < 8; i++)
            {
                rng.GetBytes(b);

                var seed = BitConverter.ToInt32(b, 0);

                var pos = new Random(seed).Next(8);

                var c = b[pos];

                l.Add(c);
            }
            
            rng.Dispose();

            var u = BitConverter.ToUInt64(l.ToArray(), 0);
            
            mu.ReleaseMutex();

            return u;
        }

        public string GenerateString(int n, string runes)
        {
            var sb = new StringBuilder();
            
            var rng = new RNGCryptoServiceProvider();

            var b = new byte[n];

            for (var i = 0; i < n; i++)
            {
                rng.GetBytes(b);

                var seed = BitConverter.ToInt32(b, 0);

                var pos = new Random(seed).Next(runes.Length);

                var c = runes.Substring(pos, 1);

                sb.Append(c);
            }
            
            rng.Dispose();

            var s = sb.ToString().Substring(0, n);

            return s;
        }
    }
}