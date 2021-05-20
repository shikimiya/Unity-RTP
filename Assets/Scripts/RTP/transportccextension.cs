using System;
using System.Collections.Generic;
using System.Linq;

namespace rtp
{
    public static class TransportCCExtensionExtended
    {
        public const int transportCCExtensionSize = 2;
    }
    
    // TransportCCExtension is a extension payload format in
    // https://tools.ietf.org/html/draft-holmer-rmcat-transport-wide-cc-extensions-01
    // 0                   1                   2                   3
    // 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    // |       0xBE    |    0xDE       |           length=1            |
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    // |  ID   | L=1   |transport-wide sequence number | zero padding  |
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    public class TransportCCExtension
    {
        public ushort TransportSequence;
        
        // Marshal serializes the members to buffer
        public (List<byte>, string) Marshal()
        {
            var buf = new List<byte>(TransportCCExtensionExtended.transportCCExtensionSize);

            buf.AddRange(BitConverter.GetBytes(TransportSequence).Take(2).Reverse().ToList());

            return (buf, null);
        }
        
        // Unmarshal parses the passed byte slice and stores the result in the members
        public string Unmarshal(List<byte> rawData)
        {
            if (rawData.Count < TransportCCExtensionExtended.transportCCExtensionSize)
            {
                return error.errTooSmall;
            }

            TransportSequence = BitConverter.ToUInt16(rawData.GetRange(0, 2).ToArray().Reverse().ToArray(), 0);

            return null;
        }
    }
    
    
}