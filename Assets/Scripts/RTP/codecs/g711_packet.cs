using System.Collections.Generic;

namespace codecs
{
    // G711PayloaderペイロードG711パケット
    public class G711Payloader
    {
        //ペイロードは、1つ以上のバイト配列にわたってG711パケットをフラグメント化します
        public List<List<byte>> Patload(int mtu, List<byte> payload)
        {
            var output = new List<List<byte>>();

            if (payload == null || mtu <= 0)
            {
                return output;
            }

            for (;payload.Count > mtu;)
            {
                var o = new List<byte>();

                o = payload.GetRange(0, mtu);

                payload = payload.GetRange(mtu, payload.Count - mtu);
                
                output.Add(o);
            }

            var ou = new List<byte>(payload.Count);

            ou = payload;
            
            output.Add(ou);

            return (output);
        }
        
        
    }
}