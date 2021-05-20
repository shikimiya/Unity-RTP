using randutil;

namespace rtp
{
    // Use global random generator to properly seed by crypto grade random.
    public static class rand
    {
        public static mathRandomGenerator globalMathRandomGenerator =
            randutil.MathRandomGeneratorExtended.NewMathRandomGenerator();
    }
}