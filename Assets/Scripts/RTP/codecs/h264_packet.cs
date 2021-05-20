using System;
using System.Collections.Generic;
using System.Linq;

namespace codecs
{
    public class H264Payloader
    {
        //ペイロードは、1つ以上のバイト配列にわたってH264パケットをフラグメント化します
        public byte[][] Payload(int mtu, byte[] payload)
        {
            var payloads = new byte[][] { };

            if (payload.Length == 0)
            {
                return payloads;
            }

            Action<byte[]> func = delegate(byte[] nalu)
            {
                if (nalu.Length == 0)
                {
                    return;
                }

                var naluType = nalu[0] & H264PacketExtended.naluTypeBitmask;

                var naluRefIdc = nalu[0] & H264PacketExtended.naluRefIdcBitmask;

                if (naluType == 9 || naluType == 12)
                {
                    return;
                }
                
                // Single NALU
                if (nalu.Length <= mtu)
                {
                    var output = new byte[nalu.Length];

                    output = nalu;

                    var payloadsList = payloads.ToList();
                    payloadsList.Add(output);
                    payloads = payloadsList.ToArray();

                    return;
                } 
                
                // FU-A
                var maxFragmentSize = mtu - H264PacketExtended.fuaHeaderSize;

                var naluData = nalu;

                var naluDataIndex = 1;

                var naluDataLength = nalu.Length - naluDataIndex;

                var naluDataRemaining = naluDataLength;

                if (common.min(maxFragmentSize, naluDataRemaining) <= 0)
                {
                    return;
                }

                for (; naluDataRemaining > 0;)
                {
                    var currentFragmentSize = common.min(maxFragmentSize, naluDataRemaining);

                    var output = new byte[H264PacketExtended.fuaHeaderSize + currentFragmentSize];

                    output[0] = H264PacketExtended.fuaNALUType;

                    output[0] |= Convert.ToByte(naluRefIdc);

                    output[1] = Convert.ToByte(naluType);

                    if (naluDataRemaining == naluDataLength)
                    {
                        // Set start bit
                        output[1] |= 1 << 7;
                    }
                    else if ((naluDataRemaining - currentFragmentSize) == 0)
                    {
                        // Send end bit
                        output[1] |= 1 << 6;
                    }
                    
                    Array.Copy(naluData, naluDataIndex, output, H264PacketExtended.fuaHeaderSize, currentFragmentSize);

                    var payloadsList = payloads.ToList();
                    payloadsList.Add(output);
                    payloads = payloadsList.ToArray();

                    naluDataRemaining -= currentFragmentSize;

                    naluDataIndex += currentFragmentSize;
                }
            };
            
            H264PacketExtended.emitNalus(payload, func);

            return payloads;
        }
    } 

    // H264Packetは、RTPパケットのペイロードに格納されているH264ヘッダーを表します
    public class H264Packet
    {
        public bool IsAVC;

        public byte[] fuaBuffer;

        public byte[] doPackaging(byte[] nalu)
        {
            if (IsAVC)
            {
                var naluLength = new byte[4];

                var buf = BitConverter.GetBytes(nalu.Length).Take(4).Reverse().ToArray();

                Array.Copy(buf, 0, naluLength, 0, buf.Length);

                var naluLengthList = naluLength.ToList();
                naluLengthList.AddRange(nalu);
                naluLength = naluLengthList.ToArray();
            }

            var tmp = H264PacketExtended.annexbNALUStartCode();
            var tmpList = tmp.ToList();
            tmpList.AddRange(nalu);

            tmp = tmpList.ToArray();
            return tmp;
        }
        
        // IsDetectedFinalPacketInSequenceは、
        // 渡されたパケットのtrueを返し、マーカービットが設定されてパケットシーケンスの終わりを示します
        public bool IsDetectedFinalPacketInSequence(bool rtpPacketMarkerBit)
        {
            return rtpPacketMarkerBit;
        }
        
        // Unmarshalは渡されたバイトスライスを解析し、
        // このメソッドが呼び出されたH264Packetに結果を格納します
        public (byte[], string) Unmarshal(byte[] payload)
        {
            if (payload == null)
            {
                return (null, error.errNilPacket);
            } 
            else if (payload.Length <= 2)
            {
                return (null, $"{error.errShortPacket}: {payload.Length} <= 2");
            }
            
            // NALU Types
            // https://tools.ietf.org/html/rfc6184#section-5.4
            var naluType = payload[0] & H264PacketExtended.naluTypeBitmask;

            if (naluType > 0 && naluType < 24)
            {
                return (doPackaging(payload), null);
            }

            if (naluType == H264PacketExtended.stapaNALUType)
            {
                var currOffset = H264PacketExtended.stapHeaderSize;

                var result = new byte[] { };

                for (;currOffset < payload.Length;)
                {
                    var naluSize = BitConverter.ToUInt16(payload.Skip(currOffset).Take(2).Reverse().ToArray(), 0);

                    currOffset += H264PacketExtended.stapNALULengthSize;

                    if (payload.Length < currOffset + naluSize)
                    {
                        return (null, $"{error.errShortPacket} STAP-A declared size({naluSize}) is larger than buffer({payload.Length - currOffset})");
                    }

                    var resultList = result.ToList();
                    resultList.AddRange(doPackaging(payload.Skip(currOffset).Take(naluSize).ToArray()));
                    currOffset += naluSize;
                }

                return (result, null);
            }

            if (naluType == H264PacketExtended.fuaNALUType)
            {
                if (payload.Length < H264PacketExtended.fuaHeaderSize)
                {
                    return (null, error.errShortPacket);
                }

                if (fuaBuffer == null)
                {
                    fuaBuffer = new byte[] { };
                }

                var fuaBufferList = fuaBuffer.ToList();
                fuaBufferList.AddRange(payload.Skip(H264PacketExtended.fuaHeaderSize).ToArray());
                fuaBuffer = fuaBufferList.ToArray();

                if ((payload[1] & H264PacketExtended.fuEndBitmask) != 0)
                {
                    var naluRefIdc = payload[0] & H264PacketExtended.naluRefIdcBitmask;

                    var fragemntedNaluType = payload[1] & H264PacketExtended.naluTypeBitmask;

                    var nalu = new List<byte>
                    {
                        Convert.ToByte(naluRefIdc | fragemntedNaluType),
                    };
                    
                    nalu.AddRange(fuaBuffer);
                    fuaBuffer = null;
                    return (doPackaging(nalu.ToArray()), null);
                }

                return (new byte[] { }, null);
            }

            return (null, $"{error.errUnhandledNALUType}: {naluType}");
        }
        
    }

    public static class H264PacketExtended
    {
        public const int stapaNALUType = 24;

        public const int fuaNALUType = 28;

        public const int fubNALUType = 29;

        public const int fuaHeaderSize = 2;

        public const int stapHeaderSize = 1;

        public const int stapNALULengthSize = 2;

        public const int naluTypeBitmask = 0x1F;

        public const int naluRefIdcBitmask = 0x60;

        public const int fuStartBitmask = 0x80;

        public const int fuEndBitmask = 0x40;

        public static byte[] annexbNALUStartCode()
        {
            return new byte[]
            {
                0x00, 0x00, 0x00, 0x01
            };
        }

        public static void emitNalus(byte[] nals, Action<byte[]> emit)
        {
            Func<byte[], int, (int, int)> nextInd = delegate(byte[] nalu, int start)
            {
                var zeroCount = 0;

                var tmpNalu = nalu.Skip(start).ToArray();

                for (var i = 0; i < tmpNalu.Length; i++)
                {
                    if (tmpNalu[i] == 0)
                    {
                        zeroCount++;

                        continue;
                    }
                    else if (tmpNalu[i] == 1)
                    {
                        if (zeroCount >= 2)
                        {
                            return (start + i - zeroCount, zeroCount - 1);
                        }
                    }

                    zeroCount = 0;
                }

                return (-1, -1);
            };

            var (nextIndStart, nextIndLen) = nextInd(nals, 0);

            if (nextIndStart == -1)
            {
                emit(nals);
            }
            else
            {
                for (;nextIndStart != -1;)
                {
                    var prevStart = nextIndStart + nextIndLen;

                    (nextIndStart, nextIndLen) = nextInd(nals, prevStart);

                    if (nextIndStart != -1)
                    {
                        emit(nals.Skip(prevStart).Take(nextIndStart - prevStart).ToArray());
                    }
                    else
                    {
                        // Emit until end of stream, no end indicator found
                        emit(nals.Skip(prevStart).ToArray());
                    }
                }
            }
        }
    } 

    // H264PartitionHeadCheckerはH264パーティションヘッドをチェックします
    public class H264PartitionHeadChecker
    {
        public bool IsPartitionHead(byte[] packet)
        {
            if (packet == null || packet.Length < 2)
            {
                return false;
            }

            if ((packet[0] & H264PacketExtended.naluTypeBitmask) == H264PacketExtended.fuaNALUType
             || (packet[0] & H264PacketExtended.naluTypeBitmask) == H264PacketExtended.fubNALUType)
            {
                return (packet[1] & H264PacketExtended.fuStartBitmask) != 0;
            }

            return true;
        }
    }
}