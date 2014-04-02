namespace RsyncNet.Delta
{
    using System;

    public class DeltaStatistics
    {
        private readonly int _blockSize;

        public int BucketCollisions;
        public long FileLength;
        public int Matching;
        public int NonMatching;
        public int PossibleMatches;

        public DeltaStatistics(int blockSize)
        {
            _blockSize = blockSize;
        }

        #region Methods: public

        public void Dump()
        {
            Console.Out.WriteLine("Matched blocks: {0}, False positives due to weak checksum: {1}",
                                  Matching,
                                  PossibleMatches - Matching);
            Console.Out.WriteLine("Bytes saved: {0}, Bytes total: {1} => {2}% transfer size reduction.",
                                  Matching*_blockSize,
                                  FileLength,
                                  (int) (((float) Matching*_blockSize/FileLength)*100));
        }

        #endregion
    }
}