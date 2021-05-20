using System.Collections.Generic;

namespace codecs
{
    public class G722Payloader
    {
        public List<List<byte>> Payload(int mtu, List<byte> payload)
        {
            var output = new List<List<byte>>();

            if (payload == null || mtu <= 0)
            {
                return output;
            }

            for (; payload.Count > mtu;)
            {
                var o = new List<byte>(mtu);

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