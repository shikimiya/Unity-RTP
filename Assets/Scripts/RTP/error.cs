namespace rtp
{
    public partial class error
    {
        public static string errHeaderSizeInsufficient = "RTP header size insufficient";

        public static string errHeaderSizeInsufficientForExtension = "RTP header size insufficient for extension";

        public static string errAudioLevelOverflow = "audio level overflow";

        public static string errTooSmall = "buffer too small";

        public static string errRFC8285OneByteHeaderIDRange =
            "header extension id must be between 1 and 14 for RFC 5285 one byte extensions";

        public static string errRFC8285OneByteHeaderSize =
            "header extension payload must be 16bytes or less for RFC 5285 one byte extensions";

        public static string errRFC8285TwoByteHeaderIDRange =
            "header extension id must be between 1 and 255 for RFC 5285 two byte extensions";

        public static string errRFC8285TwoByteHeaderSize =
            "header extension payload must be 255bytes or less for RFC 5285 two byte extensions";

        public static string errRFC3550HeaderIDRange = "header extension id must be 0 for non-RFC 5285 extensions";

    }
}