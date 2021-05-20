namespace codecs
{
    public partial class error
    {
        public const string errShortPacket = "packet is not large enough";

        public const string errNilPacket = "invalid nil packet";

        public const string errTooManyPDiff = "too many PDiff";

        public const string errTooManySpatialLayers = "too many spatial layers";

        public const string errUnhandledNALUType = "NALU Type is unhandled";
    }
}