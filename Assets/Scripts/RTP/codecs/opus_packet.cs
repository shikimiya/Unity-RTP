namespace codecs
{
    // OpusPayloaderペイロードOpusパケット
    public class OpusPayloader
    {
        //ペイロードは、1つ以上のバイト配列にわたってOpusパケットをフラグメント化します
        public byte[][] Payload(int mtu, byte[] payload)
        {
            if (payload == null)
            {
                return new byte[][]
                {

                };
            }

            var output = new byte[payload.Length];

            output = payload;

            return new byte[][]
            {
                output
            };
        }
    }

    // OpusPacketは、RTPパケットのペイロードに格納されているOpusヘッダーを表します
    public class OpusPacket
    {
        public byte[] Payload;

        public bool IsDetectedFinalPacketInSequence(bool rtpPacket)
        {
            return true;
        }
        
        // Unmarshalは渡されたバイトスライスを解析し、このメソッドが呼び出されたOpusPacketに結果を格納します
        public (byte[], string) Unmarshal(byte[] packet)
        {
            if (packet == null)
            {
                return (null, error.errShortPacket);
            } 
            else if (packet.Length == 0)
            {
                return (null, error.errShortPacket);
            }

            Payload = packet;

            return (packet, null);
        }
    }

    // OpusPartitionHeadCheckerはOpusパーティションヘッドをチェックします
    public class OpusPartitionHeadChecker
    {
        public bool IsPartitionHead(byte[] packet)
        {
            var p = new OpusPacket();

            var (_, err) = p.Unmarshal(packet);

            if (err != null)
            {
                return false;
            }

            return true;
        }
    } 
}