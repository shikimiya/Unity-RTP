using System;
using System.Collections.Generic;
using System.Linq;

namespace rtp
{
    // Extension RTP Header extension
    public struct Extension
    {
        public byte id;
        public List<byte> payload;
    }

    public static class RtpConstMember
    {
        public const int headerLength = 4;

        public const byte versionShift = 6;

        public const byte versionMask = 0x3;

        public const byte paddingShift = 5;

        public const byte paddingMask = 0x1;

        public const byte extensionShift = 4;

        public const byte extensionMask = 0x1;

        public const byte ccMask = 0xF;

        public const byte markerShift = 7;

        public const byte markerMask = 0x1;

        public const ushort extensionProfileOneByte = 0xBEDE;

        public const ushort extensionProfileTwoByte = 0x1000;

        public const ushort extensionIDReserved = 0xF;

        public const byte ptMask = 0x7F;

        public const byte seqNumOffset = 2;

        public const byte seqNumLength = 2;

        public const byte timestampOffset = 4;

        public const byte timestampLength = 4;

        public const byte ssrcOffset = 8;

        public const byte ssrcLength = 4;

        public const byte csrcOffset = 12;

        public const byte csrcLength = 4;
    }

    public class Header
    {
        public byte version;
        public bool padding;
        public bool extension;
        public bool marker;
        public int payloadOffset;
        public byte payloadType;
        public ushort sequenceNumber;
        public uint timeStamp;
        public uint SSRC;
        public List<uint> CSRC;
        public ushort extensionProfile;
        public List<Extension> extensions;

        public string Unmarshal(List<byte> rawPacket)
        {
            if (rawPacket.Count < RtpConstMember.headerLength)
            {
                return error.errHeaderSizeInsufficient;
            }
            
            /*
			 *  0                   1                   2                   3
			 *  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
			 * +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
			 * |V=2|P|X|  CC   |M|     PT      |       sequence number         |
			 * +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
			 * |                           timestamp                           |
			 * +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
			 * |           synchronization source (SSRC) identifier            |
			 * +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
			 * |            contributing source (CSRC) identifiers             |
			 * |                             ....                              |
			 * +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
			 */

            version = Convert.ToByte((rawPacket[0] >> RtpConstMember.versionShift) & RtpConstMember.versionMask);

            padding = ((rawPacket[0] >> RtpConstMember.paddingShift) & RtpConstMember.paddingMask) > 0;

            extension = ((rawPacket[0] >> RtpConstMember.extensionShift) & RtpConstMember.extensionMask) > 0;

            var nCSRC = rawPacket[0] & RtpConstMember.ccMask;

            if (CSRC.Capacity < nCSRC || CSRC == null)
            {
                CSRC = new List<uint>(nCSRC);
            }
            else
            {
                CSRC = CSRC.GetRange(0, nCSRC);
            }

            var currOffset = RtpConstMember.csrcOffset + (nCSRC * RtpConstMember.csrcLength);

            if (rawPacket.Count < currOffset)
            {
                return $"size {rawPacket.Count} < {currOffset}: {error.errHeaderSizeInsufficient}";
            }

            marker = ((rawPacket[1] >> RtpConstMember.markerShift) & RtpConstMember.markerMask) > 0;

            payloadType = Convert.ToByte(rawPacket[1] & RtpConstMember.ptMask);

            sequenceNumber =
                BitConverter.ToUInt16(
                    rawPacket.GetRange(RtpConstMember.seqNumOffset, RtpConstMember.seqNumLength).ToArray().Reverse()
                        .ToArray(), 0);
            
            timeStamp = 
                BitConverter.ToUInt32(rawPacket.GetRange(RtpConstMember.timestampOffset, RtpConstMember.timestampLength).ToArray().Reverse().ToArray(), 0);

            SSRC = BitConverter.ToUInt32(
                rawPacket.GetRange(RtpConstMember.ssrcOffset, RtpConstMember.ssrcLength).ToArray().Reverse().ToArray(),
                0);

            for (var i = 0; i < CSRC.Count; i++)
            {
                var offset = RtpConstMember.csrcOffset + (i * RtpConstMember.csrcLength);

                CSRC[i] = BitConverter.ToUInt32(rawPacket.GetRange(offset, 4).ToArray().Reverse().ToArray(), 0);
            }

            if (extensions != null)
            {
                extensions = extensions.GetRange(0,1);
            }

            if (extension)
            {
                var expected = currOffset + 4;

                if (rawPacket.Count < expected)
                {
                    return $"size {rawPacket} < {expected}: {error.errHeaderSizeInsufficientForExtension}";
                }
                
                extensionProfile =
                    BitConverter.ToUInt16(rawPacket.GetRange(currOffset, 2).ToArray().Reverse().ToArray(), 0);

                currOffset += 2;

                var extensionLength =
                    BitConverter.ToUInt16(rawPacket.GetRange(currOffset, 2).ToArray().Reverse().ToArray(), 0);

                currOffset += 2;

                expected = currOffset + extensionLength;

                if (rawPacket.Count < expected)
                {
                    return $"size {rawPacket.Count} < {expected}: {error.errHeaderSizeInsufficientForExtension}";
                }

                switch (extensionProfile)
                {
                    case RtpConstMember.extensionProfileOneByte:
                        var end = currOffset + extensionLength;

                        for (; currOffset < end;)
                        {
                            if (rawPacket[currOffset] == 0x00)
                            {
                                currOffset++;
                                continue;
                            }

                            var extid = rawPacket[currOffset] >> 4;

                            var len = rawPacket[currOffset] & ~0xF0 + 1;

                            currOffset++;

                            if (extid == RtpConstMember.extensionIDReserved)
                            {
                                break;
                            }
                            
                            var extension = new Extension()
                            {
                                id = Convert.ToByte(extid),
                                payload = rawPacket.GetRange(currOffset, len),
                            };
                            
                            extensions.Add(extension);

                            currOffset += len;
                        }
                        break;
                    
                    case RtpConstMember.extensionProfileTwoByte:
                        end = currOffset + extensionLength;

                        for (; currOffset < end;)
                        {
                            if (rawPacket[currOffset] == 0x00)
                            {
                                currOffset++;
                                continue;
                            }

                            var extid = rawPacket[currOffset];

                            currOffset++;

                            var len = rawPacket[currOffset];

                            currOffset++;

                            var ex = new Extension
                            {
                                id = extid,
                                payload = rawPacket.GetRange(currOffset, len),
                            };
                            
                            extensions.Add(ex);

                            currOffset += len;
                        }

                        break;
                    
                    default:
                        if (rawPacket.Count < currOffset + extensionLength)
                        {
                            return $"{error.errHeaderSizeInsufficientForExtension}: {rawPacket.Count} < {currOffset + extensionLength}";
                        }

                        var ex2 = new Extension
                        {
                            id = 0, 
                            payload = rawPacket.GetRange(currOffset, extensionLength)
                        };

                        extensions.Add(ex2);

                        currOffset += extensions[0].payload.Count;
                        break;
                }
            }

            payloadOffset = currOffset;

            return null;
        }

        //マーシャルはヘッダーをバイトにシリアル化します。
        public (byte[], string) Marshal()
        {
            var buf = new List<byte>();

            var (n, err) = MarshalTo();

            if (err != null)
            {
                return (null, err);
            }

            return (buf.Take(n).ToArray(), null);
        }
        
        // MarshalToはヘッダーをシリアル化し、バッファーに書き込みます。
        public (int, string) MarshalTo(byte[] buf)
        {
            var size = MarshalSize();

            if (size > buf.Length)
            {
                return (0, "err short buffer");
            }
            
            //最初のバイトには、バージョン、パディングビット、拡張ビット、およびcsrcサイズが含まれます。
            buf[0] = Convert.ToByte((version & RtpConstMember.versionShift) | CSRC.Count);

            if (padding)
            {
                buf[0] |= 1 << RtpConstMember.paddingShift;
            }

            if (extension)
            {
                buf[0] |= 1 << RtpConstMember.extensionShift;
            }
            
            // 2番目のバイトには、マーカービットとペイロードタイプが含まれます。
            buf[1] = payloadType;

            if (marker)
            {
                buf[1] |= 1 << RtpConstMember.markerShift;
            }

            var tmp = BitConverter.GetBytes(sequenceNumber).Take(2).Reverse().ToArray();
            Array.Copy(tmp, 0, buf, 2, tmp.Length);

            tmp = BitConverter.GetBytes(timeStamp).Take(4).Reverse().ToArray();
            Array.Copy(tmp, 0, buf, 4, buf.Length);

            tmp = BitConverter.GetBytes(SSRC).Take(4).Reverse().ToArray();
            Array.Copy(tmp, 0, buf, 8, tmp.Length);

            var n = 12;

            foreach (var csrc in CSRC)
            {
                tmp = BitConverter.GetBytes(csrc).Take(4).Reverse().ToArray();
                Array.Copy(tmp, 0, buf, n, tmp.Length);
                n += 4;
            }

            if (extension)
            {
                var extHeaderPos = n;

                tmp = BitConverter.GetBytes(extensionProfile).Take(2).Reverse().ToArray();
                Array.Copy(tmp, 0, );
            }
        }

        public int MarshalSize()
        {
            var size = 12 + CSRC.Count * RtpConstMember.csrcLength;

            if (extension)
            {
                var extSize = 4;

                switch (extensionProfile)
                {
                    case RtpConstMember.extensionProfileOneByte:
                        foreach (var extension in extensions)
                        {
                            extSize += 1 + extension.payload.Count;
                        }

                        break;
                    
                    case RtpConstMember.extensionProfileTwoByte:
                        foreach (var extension in extensions)
                        {
                            extSize += 2 + extension.payload.Count;
                        }

                        break;

                    default:
                        extSize += extensions[0].payload.Count;
                        break;
                }

                size += ((extSize + 3) / 4) * 4;
            }

            return size;
        }

        // SetExtensionはRTPヘッダー拡張を設定します
        public string SetExtension(byte id, List<byte> payload)
        {
            if (extension)
            {
                switch (extensionProfile)
                {
                    // RFC 8285 RTP One Byte Header Extension
                    case RtpConstMember.extensionProfileOneByte:
                        if (id < 1 || id > 14)
                        {
                            return $"{error.errRFC8285OneByteHeaderIDRange} actual({id})";
                        }

                        if (payload.Count > 16)
                        {
                            return $"{error.errRFC8285OneByteHeaderSize} actual({payload.Count})";
                        }

                        break;
                    // RFC 8285 RTP2バイトヘッダー拡張
                    case RtpConstMember.extensionProfileTwoByte:
                        if (id < 1 || id > 255)
                        {
                            return $"{error.errRFC8285TwoByteHeaderIDRange} actual({id})";
                        }

                        if (payload.Count > 255)
                        {
                            return $"{error.errRFC8285TwoByteHeaderIDRange} actual({payload.Count})";
                        }

                        break;
                    default: // RFC3550 Extension
                        if (id != 0)
                        {
                            return $"{error.errRFC3550HeaderIDRange} actual({id})";
                        }

                        break;
                }

                //存在する場合は既存のものを更新し、存在しない場合は新しい拡張機能を追加します
                for (var i = 0; i < extensions.Count; i++)
                {
                    if (extensions[i].id == id)
                    {
                        extensions[i].payload = payload;
                        return null;
                    }
                }

                extensions.Add(new Extension
                {
                    id = id,
                    payload = payload,
                });

                return null;
            }

            extension = true;

            var len = payload.Count;

            if (len <= 16)
            {
                extensionProfile = RtpConstMember.extensionProfileOneByte;
            }

            if (len > 16 && len < 256)
            {
                extensionProfile = RtpConstMember.extensionProfileTwoByte;
            }
            
            extensions.Add(new Extension
            {
                id = id,
                payload = payload,
            });

            return null;
        }
        
        // GetExtensionIDsは拡張ID配列を返します
        public List<byte> GetExtensionsIDs()
        {
            if (!extension)
            {
                return null;
            }

            if (extensions.Count == 0)
            {
                return null;
            }

            var ids = new List<byte>(extensions.Count);

            foreach (var ex in extensions)
            {
                ids.Add(ex.id);
            }

            return ids;
        }
        
        // GetExtensionはRTPヘッダー拡張を返します
        public List<byte> GetExtension(byte id)
        {
            if (!extension)
            {
                return null;
            }

            foreach (var ex in extensions)
            {
                if (ex.id == id)
                {
                    return ex.payload;
                }
            }

            return null;
        }
        
        // DelExtensionはRTPヘッダー拡張を削除します
        public string DelExtension(byte id)
        {
            if (!extension)
            {
                return error.;
            }

            for (var i = 0; i < extensions.Count; i++)
            {
                if (extensions[i].id == id)
                {
                    extensions = extensions.Take(i).ToList();
                    extensions.AddRange(extensions.Skip(i + 1).ToList());
                    return null;
                }
            }
            
            return error.errHeader
        }
    }

    public class Packet
    {
        public Header header;

        public List<byte> Raw;

        public List<byte> Payload;

        public Packet()
        {
            header = new Header();
            
            Raw = new List<byte>();
            
            Payload = new List<byte>();
        }

        public string Unmarshal(List<byte> rawPacket)
        {
            var err = header.Unmarshal(rawPacket);

            if (err != null)
            {
                return err;
            }

            Payload = rawPacket.Skip(header.payloadOffset).ToList();

            Raw = rawPacket;

            return null;
        }

        // MarshalToはパケットをシリアル化し、バッファに書き込みます。
        public (int, string) MarshalTo(byte[] buf)
        {
            var (n, err) = Header.MarshalTo(buf)
        }

        // MarshalSizeは、マーシャリングされたパケットのサイズを返します。
        public int MarshalSize()
        {
            return Header.MarshalSize() + Payload.Count();
        }
    }
}
