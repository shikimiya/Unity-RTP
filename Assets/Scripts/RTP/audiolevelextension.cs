using System;

namespace rtp
{

    public static class AudioLevelExtensionExtended
    {
        public const int audioLevelExtensionSize = 1;
    }

    // AudioLevelExtension is a extension payload format described in
    // https://tools.ietf.org/html/rfc6464
    //
    // Implementation based on:
    // https://chromium.googlesource.com/external/webrtc/+/e2a017725570ead5946a4ca8235af27470ca0df9/webrtc/modules/rtp_rtcp/source/rtp_header_extensions.cc#49
    //
    // One byte format:
    // 0                   1
    // 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    // |  ID   | len=0 |V| level       |
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    //
    // Two byte format:
    // 0                   1                   2                   3
    // 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    // |      ID       |     len=1     |V|    level    |    0 (pad)    |
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    public class AudioLevelExtension
    {
        public byte Level;

        public bool Voice;
        
        // Marshal serializes the members to buffer
        public (byte[], string) Marshal() 
        {
            if (Level > 127)
            {
                return (null, error.errAudioLevelOverflow);
            }

            byte voice = 0x00;

            if (Voice)
            {
                voice = 0x80;
            }

            if (Voice)
            {
                voice = 0x80;
            }

            var buf = new byte[AudioLevelExtensionExtended.audioLevelExtensionSize];

            buf[0] = Convert.ToByte(voice | Level);

            return (buf, null);
        }
        
        // Unmarshalは渡されたバイトスライスを解析し、結果をメンバーに格納します
        public string Unmarshal(byte[] rawData)
        {
            if (rawData.Length < AudioLevelExtensionExtended.audioLevelExtensionSize)
            {
                return error.errAudioLevelOverflow;
            }

            Level = Convert.ToByte(rawData[0] & 0x7F);

            Voice = (rawData[0] & 0x80) != 0;

            return null;
        }
    } 
}