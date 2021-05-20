using System;
using System.Linq;
using randutil;

namespace codecs
{
    // VP9PayloaderペイロードVP9パケット
    public class VP9Payloader
    {
        public ushort pictureID;

        public bool initailized;
        
        // InitialPictureIDFnは、ランダムな初期画像IDを返す関数です。
        public Func<ushort> InitialPictureIDfn;
        
        //ペイロードは、1つ以上のバイト配列にわたってVP9パケットをフラグメント化します
        public byte[][] Payload(int mtu, byte[] payload)
        {
            if (!initailized)
            {
                if (InitialPictureIDfn == null)
                {
                    InitialPictureIDfn = () =>
                    {
                        return Convert.ToUInt16(VP9PayloaderExtended.globalMathRandomGenerator.Intn(0x7FFF));
                    };
                }

                pictureID = Convert.ToUInt16(InitialPictureIDfn() & 0x7FFF);

                initailized = true;
            }

            if (payload == null)
            {
                return new byte[][]
                {

                };
            }

            var maxFragmentSize = mtu - VP9PayloaderExtended.vp9HeaderSize;

            var payloadDataRemaining = payload.Length;

            var payloadDataIndex = 0;

            if (common.min(maxFragmentSize, payloadDataRemaining) <= 0)
            {
                return new byte[][]
                {

                };
            }

            var payloads = new byte[][]
            {

            };

            for (; payloadDataRemaining > 0;)
            {
                var currentFragmentSize = common.min(maxFragmentSize, payloadDataRemaining);

                var output = new byte[VP9PayloaderExtended.vp9HeaderSize + currentFragmentSize];

                output[0] = 0x90; // F = I I = 1

                if (payloadDataIndex == 0)
                {
                    output[0] |= 0x08;
                }

                if (payloadDataRemaining == currentFragmentSize)
                {
                    output[0] |= 0x04; // E = 1
                }

                output[1] = Convert.ToByte((pictureID >> 8) | 0x80);

                output[2] = Convert.ToByte(pictureID);

                Array.Copy(payload, payloadDataIndex, output, VP9PayloaderExtended.vp9HeaderSize, currentFragmentSize);

                var payloadsList = payloads.ToList();
                payloadsList.Add(output);
                payloads = payloadsList.ToArray();

                
            }
            
            pictureID++;
            
            if (pictureID >= 0x8000)
            {
                pictureID = 0;
            }
            
            return payloads;
        }
    }

    // VP9Packetは、RTPパケットのペイロードに格納されているVP9ヘッダーを表します
    public class VP9Packet
    {
        // Required header
        public bool I; // PictureID is present

        public bool P; // Inter-picture predicted frame

        public bool L; // Layer indices is present

        public bool F; // Flexible mode

        public bool B; // Start of a frame

        public bool E; // End of frame

        public bool V; // Scalability structure (SS) data present
        
        // Recommended headers
        public ushort PictureID; // 7 or 16 bits, picture ID
        
        // Conditionally recommended headers
        public byte TID; // Temporal layer ID

        public bool U; // Switching up point

        public byte SID; // Spatial layer ID

        public bool D; // Inter-layer dependency used
        
        // Conditionally required headers
        public byte[] PDiff; // Reference index (F=1)

        public byte TL0PICIDX; // Temporal layer zero index (F=0)
        
        // Scalability structure headers
        public byte NS; // N_S + 1 indicates the number of spatial layers present in the VP9 stream

        public bool Y; // Each spatial layer's frame resolution present

        public bool G; // PG description present flag.

        public byte NG; // N_G indicates the number of pictures in a Picture Group (PG)

        public ushort[] Width;

        public ushort[] Height;

        public byte[] PGTID; // Temporal layer ID of pictures in a Picture Group

        public bool[] PGU; // Switching up point of pictures in a Picture Group

        public byte[][] PGPDiff; // // Reference indecies of pictures in a Picture Group

        public byte[] Payload;

        // IsDetectedFinalPacketInSequenceは、渡されたパケットのtrueを返し、
        // マーカービットが設定されてパケットシーケンスの終わりを示します
        public bool IsDetectedFinalPacketInSequence(bool rtpPacketMarkerBit)
        {
            return rtpPacketMarkerBit;
        }
        
        // Unmarshalは渡されたバイトスライスを解析し、
        // このメソッドが呼び出されたVP9Packetに結果を格納します
        public (byte[], string) Unmarshal(byte[] packet)
        {
            if (packet == null)
            {
                return (null, error.errNilPacket);
            }

            if (packet.Length < 1)
            {
                return (null, error.errShortPacket);
            }

            I = (packet[0] & 0x80) != 0;

            P = (packet[0] & 0x40) != 0;

            L = (packet[0] & 0x20) != 0;

            F = (packet[0] & 0x10) != 0;

            B = (packet[0] & 0x08) != 0;

            E = (packet[0] & 0x04) != 0;

            V = (packet[0] & 0x02) != 0;

            var pos = 1;

            string err = null;

            if (I)
            {
                (pos, err) = parsePictureID(packet, pos);

                if (err != null)
                {
                    return (null, err);
                }
            }

            if (L)
            {
                (pos, err) = parseLayerInfo(packet, pos);

                if (err != null)
                {
                    return (null, err);
                }
            }

            if (F && P)
            {
                (pos, err) = parseRefIndices(packet, pos);

                if (err != null)
                {
                    return (null, err);
                }
            }

            if (V)
            {
                (pos, err) = parseSSData(packet, pos);

                if (err != null)
                {
                    return (null, err);
                }
            }

            Payload = packet.Skip(pos).ToArray();

            return (Payload, null);
        }

        public (int, string) parsePictureID(byte[] packet, int pos)
        {
            if (packet.Length <= pos)
            {
                return (pos, error.errShortPacket);
            }

            PictureID = Convert.ToByte(packet[pos] & 0x7F);

            if ((packet[pos] & 0x80) != 0)
            {
                pos++;

                if (packet.Length <= pos)
                {
                    return (pos, error.errShortPacket);
                }

                PictureID = Convert.ToByte((PictureID << 8) | packet[pos]);
            }

            pos++;

            return (pos, null);
        }

        public (int, string) parseLayerInfo(byte[] packet, int pos)
        {
            string err = null;
            
            (pos, err) = parseLayerInfoCommon(packet, pos);

            if (err != null)
            {
                return (pos, err);
            }

            if (F)
            {
                return (pos, err);
            }

            return parseLayerInfoNonFlexibleMode(packet, pos);
        }

        public (int, string) parseLayerInfoCommon(byte[] packet, int pos)
        {
            if (packet.Length <= pos)
            {
                return (pos, error.errShortPacket);
            }

            TID = Convert.ToByte(packet[pos] >> 5);

            U = (packet[pos] & 0x10) != 0;

            SID = Convert.ToByte((packet[pos] >> 1) & 0x7);

            D = (packet[pos] & 0x01) != 0;

            if (SID > VP9PayloaderExtended.maxSpatialLayers)
            {
                return (pos, error.errTooManySpatialLayers);
            }

            pos++;

            return (pos, null);
        }

        public (int, string) parseLayerInfoNonFlexibleMode(byte[] packet, int pos)
        {
            if (packet.Length <= pos)
            {
                return (pos, error.errShortPacket);
            }

            TL0PICIDX = packet[pos];

            pos++;

            return (pos, null);
        }

        public (int, string) parseRefIndices(byte[] packet, int pos)
        {
            for (;;)
            {
                if (packet.Length <= pos)
                {
                    return (pos, error.errShortPacket);
                }

                var PDiffList = PDiff.ToList();
                PDiffList.Add(Convert.ToByte(packet[pos] >> 1));
                PDiff = PDiffList.ToArray();

                if ((packet[pos] & 0x01) == 0)
                {
                    break;
                }

                if (PDiff.Length >= VP9PayloaderExtended.maxVP9RefPics)
                {
                    return (pos, error.errTooManyPDiff);
                }

                pos++;
            }

            pos++;

            return (pos, null);
        }

        public (int, string) parseSSData(byte[] packet, int pos)
        {
            if (packet.Length <= pos)
            {
                return (pos, error.errShortPacket);
            }
            
            NS = Convert.ToByte(packet[pos] >> 5);

            Y = (packet[pos] & 0x10) != 0;

            G = ((packet[pos] >> 1) & 0x7) != 0;

            pos++;

            var ns = NS + 1;

            NG = 0;

            if (Y)
            {
                Width = new ushort[ns];

                Height = new ushort[ns];

                for (var i = 0; i < ns; i++)
                {
                    Width[i] = Convert.ToUInt16(packet[pos] << 8 | packet[pos + 1]);

                    pos += 2;

                    Height[i] = Convert.ToUInt16(packet[pos] << 8 | packet[pos + 1]);

                    pos += 2;
                }
            }

            if (G)
            {
                NG = packet[pos];

                pos++;
                
            }

            for (var i = 0; i < NG; i++)
            {
                var PGTIDList = PGTID.ToList();
                PGTIDList.Add(Convert.ToByte(packet[pos] >> 5));
                PGTID = PGTIDList.ToArray();

                var PGUList = PGU.ToList();
                PGUList.Add((packet[pos] & 0x10) != 0);
                PGU = PGUList.ToArray();

                var R = (packet[pos] >> 2) & 0x3;

                pos++;

                var PGPDiffList = PGPDiff.ToList();
                PGTIDList.AddRange(new byte[] { });
                PGPDiff = PGPDiffList.ToArray();

                for (var j = 0; j < R; j++)
                {
                    var tmp = PGPDiff[i].ToList();
                    tmp.Add(packet[pos]);
                    PGPDiff[i] = tmp.ToArray();
                    pos++;
                }
            }

            return (pos, null);
        }
    }

    // VP9PartitionHeadCheckerはVP9パーティションヘッドをチェックします
    public class VP9PartitionHeadChecker
    {
        // IsPartitionHeadは、これがVP9パーティションのヘッドであるかどうかを確認します
        public bool IsPartitionHead(byte[] packet)
        {
            var p = new VP9Packet();

            var (_, err) = p.Unmarshal(packet);

            if (err != null)
            {
                return false;
            }

            return p.B;
        }
    }

    public static class VP9PayloaderExtended
    {
        public const int vp9HeaderSize = 3;//フレキシブルモード15ビット画像ID 
            
        public const int maxSpatialLayers = 5;

        public const int maxVP9RefPics = 3;

        //グローバルランダムジェネレーターを使用して、暗号グレードのランダムで適切にシードします。
        public static mathRandomGenerator globalMathRandomGenerator = randutil.MathRandomGeneratorExtended.NewMathRandomGenerator();
    }
}