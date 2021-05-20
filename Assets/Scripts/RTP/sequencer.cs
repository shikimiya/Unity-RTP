using System;
using System.Threading;

namespace rtp
{
    // Sequencer は、RTPパケットを構築するための連続したシーケンス番号を生成します
    public interface Sequencer
    {
        ushort NextSequenceNumber();

        ulong RollOverCount();
    }

    public static class sequencerExtended
    {
        // NewRandomSequencerは、ランダムなシーケンス番号から始まる新しいSequencerを返します
        public static Sequencer NewRandomSequencer()
        {
            return new sequencer
            {
                sequenceNumber = Convert.ToUInt16(rand.globalMathRandomGenerator.Intn(UInt16.MaxValue)),
            };
        }
        
        // NewFixedSequencerは、特定のシーケンス番号から始まる新しいSequencerを返します
        public static Sequencer NewFieldSequencer(ushort s)
        {
            return new sequencer
            {
                sequenceNumber = Convert.ToUInt16(s - 1), // -1（最初のシーケンス番号の前に1が付いているため）
            };
        }
    }

    public class sequencer : Sequencer
    {
        public ushort sequenceNumber;

        public ulong rollOverCount;

        public Mutex mutex;
        
        // NextSequenceNumberは、RTPパケットを構築するための新しいシーケンス番号をインクリメントして返します
        public ushort NextSequenceNumber()
        {
            mutex.WaitOne();

            try
            {
                sequenceNumber++;

                if (sequenceNumber == 0)
                {
                    rollOverCount++;
                }

                return sequenceNumber;
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
        
        // RollOver Countは、16ビットシーケンス番号がラップした回数を返します
        public ulong RollOverCount()
        {
            mutex.WaitOne();

            try
            {
                return rollOverCount;
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
    }
}