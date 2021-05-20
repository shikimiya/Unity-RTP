using System.Collections.Generic;

namespace rtp
{
    // PartitionHeadCheckerは、パケットがキーフレームであるかどうかをチェックするインターフェースです
    public interface PartitionHeadChecker
    {
        bool IsPartitionHead(List<byte> b);
    }
}