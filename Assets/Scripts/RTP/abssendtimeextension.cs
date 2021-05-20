using System;
using UnityEngine;

namespace rtp
{
    public static class AbsSendTimeExtensionExtended
    {
        public const int absSendTimeExtensionSize = 3;
        
        // NewAbsSendTimeExtensionは、time.Timeから新しいAbsSendTimeExtensionを作成します。
        public static AbsSendTimeExtension NewAbsSendTimeExtension(DateTime sendTime)
        {
            return new AbsSendTimeExtension
            {
                Timestamp = toNtpTime(sendTime) >> 14,
            };
        }

        public static ulong toNtpTime(DateTime t)
        {
            ulong s = 0;

            ulong f = 0;
            
            var u = new DateTimeOffset().ToUnixTimeMilliseconds();

            s = Convert.ToUInt64(u / 1e9);

            s += 0x83AA7E80;// unixエポックとntpepochの間の秒単位のオフセット

            f = Convert.ToUInt64(u % 1e9);

            f <<= 32;

            f /= Convert.ToUInt64(1e9);

            s <<= 32;
            
            return s | f;
        }

        public static DateTime toTime(ulong t)
        {
            var s = t >> 32;

            var f = t & 0xFFFFFFFF;

            f *= Convert.ToUInt64(1e9);

            f >>= 32;

            s -= 0x83AA7E80;

            var u = s * 1e9 + f;

            var dto = new DateTimeOffset();
            
            var dt = new DateTime(dto.ToUnixTimeMilliseconds());

            return dt;
        }
    }
    
    // AbsSendTimeExtension is a extension payload format in
    // http://www.webrtc.org/experiments/rtp-hdrext/abs-send-time
    public class AbsSendTimeExtension
    {
        public ulong Timestamp;
        
        //マーシャルはメンバーをバッファにシリアル化します。
        public (byte[], string) Marshal()
        {
            return (new byte[]
            {
                Convert.ToByte((Timestamp & 0xFF0000) >> 16),
                Convert.ToByte((Timestamp & 0xFF00) >> 8),
                Convert.ToByte(Timestamp & 0xFF),
            }, null);
        }
        
        // Unmarshalは渡されたバイトスライスを解析し、結果をメンバーに格納します。
        public string Unmarshal(byte[] rawData)
        {
            if (rawData.Length < AbsSendTimeExtensionExtended.absSendTimeExtensionSize)
            {
                return error.errAudioLevelOverflow;
            }

            Timestamp = Convert.ToUInt64(rawData[0]) << 16
                        | Convert.ToUInt64(rawData[1]) << 8
                        | Convert.ToUInt64(rawData[2]);

            return null;
        }
        
        //受信時間に従って絶対送信時間を推定します。
        //送信遅延が64秒より大きい場合、推定時間は間違っていることに注意してください。
        public DateTime Estimate(DateTime receive)
        {
            var receiveNTP = AbsSendTimeExtensionExtended.toNtpTime(receive);

            var ntp = receiveNTP & 0xFFFFFFC000000000 | (Timestamp & 0xFFFFFF) << 14;

            if (receiveNTP < ntp)
            {
                //受信時間は常に送信時間より遅くなければなりません
                ntp -= 0x1000000 << 14;
            }

            return AbsSendTimeExtensionExtended.toTime(ntp);
        }
    }
}