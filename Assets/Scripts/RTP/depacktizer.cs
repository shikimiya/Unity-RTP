using System.Collections.Generic;

namespace rtp
{
    // DepacketizerはRTPペイロードをデパケット化し、ペイロードからRTP固有のデータを削除します
    public interface Depacketizer
    {
        bool IsDetectedFinalPacketInSequence(bool rtpPacketMarketBit);

        (List<byte>, string) Unmarshal(List<byte> packet);
    }
}