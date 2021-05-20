using System;
using System.Linq;

namespace codecs
{
    public static class VP8PayloaderExtended
    {
        public const int vp8HeaderSize = 1;
    }

    // VP8PayloaderペイロードVP8パケット
    public class VP8Payloader
    {
        public ushort pictureID;

        //ペイロードは、1つ以上のバイト配列にわたってVP8パケットをフラグメント化します
        public byte[][] Payload(int mtu, byte[] payload)
        {
            var usingHeaderSize = VP8PayloaderExtended.vp8HeaderSize;

            if (pictureID == 0 || pictureID < 128)
            {
                usingHeaderSize = VP8PayloaderExtended.vp8HeaderSize + 2;
            }
            else
            {
                usingHeaderSize = VP8PayloaderExtended.vp8HeaderSize + 3;
            }

            var maxFragmentSize = mtu - usingHeaderSize;

            var payloadData = payload;

            var payloadDataRemaining = payload.Length;

            var payloadDataqIndex = 0;

            var payloads = new byte[][] { };

            //フラグメント/ペイロードのサイズが正しいことを確認します
            if (common.min(maxFragmentSize, payloadDataRemaining) <= 0)
            {
                return payloads;
            }

            var first = true;

            for (; payloadDataRemaining > 0;)
            {
                var currentFragmentSize = common.min(maxFragmentSize, payloadDataRemaining);

                var output = new byte[usingHeaderSize + currentFragmentSize];

                if (first)
                {
                    output[0] = 0x10;

                    first = false;
                }

                switch (usingHeaderSize)
                {
                    case VP8PayloaderExtended.vp8HeaderSize | VP8PayloaderExtended.vp8HeaderSize + 2:
                        output[0] |= 0x80;
                        output[1] |= 0x80;
                        output[2] |= Convert.ToByte(0x80 | (pictureID & 0x7F));
                        break;

                    case VP8PayloaderExtended.vp8HeaderSize + 3:
                        output[0] |= 0x80;
                        output[1] |= 0x80;
                        output[2] |= Convert.ToByte(0x80 | (pictureID >> 8) & 0x7F);
                        break;
                }

                Array.Copy(payloadData, payloadDataqIndex, output, usingHeaderSize, currentFragmentSize);

                var payloadsList = payloads.ToList();
                payloadsList.Add(output);
                payloads = payloadsList.ToArray();

                payloadDataRemaining -= currentFragmentSize;

                payloadDataqIndex += currentFragmentSize;
            }

            pictureID++;

            pictureID &= 0x7FFF;

            return payloads;
        }
    }
    
    // VP8Packetは、RTPパケットのペイロードに格納されているVP8ヘッダーを表します
    public class VP8Packet
    {
        // 必須ヘッダー
        public byte X; /* extended control bits present */

        public byte N;　/* when set to 1 this frame can be discarded */

        public byte S; /* start of VP8 partition */

        public byte PID; /* partition index */
        
        //拡張制御ビット
        public byte I; /* 1 if PictureID is present */

        public byte L; /* 1 if TL0PICIDX is present */

        public byte T; /* 1 if TID is present */

        public byte K; /* 1 if KEYIDX is present */
        
        //オプションの拡張子
        public ushort PictureID; /* 8 or 16 bits, picture ID */

        public byte TL0PICIDX; /* 8 bits temporal level zero index */

        public byte TID; /* 2 bits temporal layer index */

        public byte Y; /* 1 bit layer sync bit */

        public byte KEYIDX; /* 5 bits temporal key frame index */

        public byte[] Payload;
        
        // IsDetectedFinalPacketInSequenceは、渡されたパケットのtrueを返し、
        // マーカービットが設定されてパケットシーケンスの終わりを示します
        public bool IsDetectedFinalPacketInSequence(bool rtpPacketMarketBit)
        {
            return rtpPacketMarketBit;
        }
        
        // Unmarshalは渡されたバイトスライスを解析し、このメソッドが呼び出されたVP8Packetに結果を格納します
        public (byte[], string) Unmarshal(byte[] payload)
        {
            if (payload == null)
            {
                return (null, error.errNilPacket);
            }

            var payloadLen = payload.Length;

            if (payloadLen < 4)
            {
                return (null, error.errShortPacket);
            }

            var payloadIndex = 0;

            X = Convert.ToByte((payload[payloadIndex] & 0x80) >> 7);

            N = Convert.ToByte((payload[payloadIndex] & 0x20) >> 5);

            S = Convert.ToByte((payload[payloadIndex] & 0x10) >> 4);

            PID = Convert.ToByte(payload[payloadIndex] & 0x07);

            payloadIndex++;
            
            if (X == 1)
            {
                I = Convert.ToByte((payload[payloadIndex] & 0x80) >> 7);

                L = Convert.ToByte((payload[payloadIndex] & 0x40) >> 6);

                T = Convert.ToByte((payload[payloadIndex] & 0x20) >> 5);

                K = Convert.ToByte((payload[payloadIndex & 0x10]) >> 4);

                payloadIndex++;
            }

            if (I == 1) // PID present?
            {
                if ((payload[payloadIndex] & 0x80) > 0)// M == 1, PID is 16bit
                {
                    PictureID = Convert.ToUInt16((payload[payloadIndex] & 0x7F) << 8 | payload[payloadIndex + 1]);

                    payloadIndex += 2;
                }
                else
                {
                    PictureID = payload[payloadIndex];

                    payloadIndex++;
                }
            }

            if (payloadIndex >= payloadLen)
            {
                return (null, error.errShortPacket);
            }

            if (T == 1 || K == 1)
            {
                if (T == 1)
                {
                    TID = Convert.ToByte(payload[payloadIndex] >> 6);

                    Y = Convert.ToByte((payload[payloadIndex] >> 5) & 0x1);
                }

                if (K == 1)
                {
                    KEYIDX = Convert.ToByte(payload[payloadIndex] & 0x1F);
                }

                payloadIndex++;
            }

            if (payloadIndex >= payloadLen)
            {
                return (null, error.errShortPacket);
            }

            Payload = payload.Skip(payloadIndex).ToArray();

            return (Payload, null);
        }
    }

    // VP8PartitionHeadCheckerはVP8パーティションヘッドをチェックします
    public class VP8ParitionHeadChecker
    {
        // IsPartitionHeadは、これがVP8パーティションのヘッドであるかどうかを確認します
        public bool IsPartitionHead(byte[] packet)
        {
            var p = new VP8Packet();

            var (_, err) = p.Unmarshal(packet);

            if (err != null)
            {
                return false;
            }

            return p.S == 1;
        }
    }
}