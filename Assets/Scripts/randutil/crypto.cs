using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace randutil
{
    public static class crypto
    {
        // GenerateCryptoRandomStringは、暗号化に使用するためのランダムな文字列を生成します。
        public static (string, string) GenerateCryptoRandomString(int n, string runes)
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

            return (s, null);
        }
        
        // CryptoUint64は、暗号化されたランダムuint64を返します。
        public static (ulong, string) CryptoUint64()
        {
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

            return (u, null);
        }
    }
}