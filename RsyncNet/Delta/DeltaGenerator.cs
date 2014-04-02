namespace RsyncNet.Delta
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Hash;
    using Libraries;

    public class DeltaGenerator
    {
        private IEnumerable<HashBlock> _blockHashes;
        private int _blockSize;
        private bool _initialized;
        private Dictionary<ushort, List<HashBlock>> _remoteBlocksIndexTable;

        public DeltaGenerator(IRollingChecksum checksumProvider, IHashAlgorithm hashProvider)
        {
            if (checksumProvider == null) throw new ArgumentNullException("checksumProvider");
            if (hashProvider == null) throw new ArgumentNullException("hashProvider");
            _initialized = false;
            ChecksumProvider = checksumProvider;
            HashProvider = hashProvider;
        }

        #region Properties, indexers, events and operators: public

        public IRollingChecksum ChecksumProvider { get; set; }
        public IHashAlgorithm HashProvider { get; set; }

        #endregion

        #region Properties, indexers, events and operators: internal

        public DeltaStatistics Statistics { get; private set; }

        #endregion

        #region Methods: public

        public IEnumerable<IDelta> GetDeltas(Stream inputStream)
        {
            if (inputStream == null) throw new ArgumentNullException("inputStream");
            if (_initialized == false) throw new InvalidOperationException("Initialize must be called");

            ChecksumProvider.Reset();
            var slidingBuffer = new SlidingStreamBuffer(inputStream, _blockSize);
            slidingBuffer.Warmup();
            bool startingNewBlock = true;
            long offset = 0;
            var deltas = new List<IDelta>();
            var currentByteDelta = new ByteDelta();

#if DEBUG
            Statistics.Matching = 0;
            Statistics.PossibleMatches = 0;
            Statistics.NonMatching = 0;
#endif
            int currentBlockSize;
            while ((currentBlockSize = (int) slidingBuffer.GetNumBytesAvailable()) > 0)
            {
                // Deal with signed integer limits
                if (IsSignedIntLength(offset - currentByteDelta.Offset))
                {
                    currentByteDelta.Length = (int) (offset - currentByteDelta.Offset);
                    deltas.Add(currentByteDelta);
                    startingNewBlock = true;
                }

                if (startingNewBlock)
                {
                    currentByteDelta = new ByteDelta {Offset = offset};
                    ChecksumProvider.ProcessBlock(slidingBuffer.GetBuffer(), 0, currentBlockSize);
                }
                else if (currentBlockSize < _blockSize)
                    ChecksumProvider.TrimFront(); // remaining bytes < block_size, so read nothing new - just trim
                else
                    ChecksumProvider.RollByte(slidingBuffer[(int) (currentBlockSize - 1)]);
                        // at this point, sigGen needs the last byte of the current block

                uint currentBlockChecksum = ChecksumProvider.Value;
                ushort currentBlockChecksumHash = RollingChecksum.HashChecksum(currentBlockChecksum);
                if (_remoteBlocksIndexTable.ContainsKey(currentBlockChecksumHash))
                {
                    List<HashBlock> possibleRemoteBlockMatches = _remoteBlocksIndexTable[currentBlockChecksumHash];
                    if (possibleRemoteBlockMatches.Any(entry => entry.Checksum == currentBlockChecksum))
                    {
#if DEBUG
                        ++Statistics.PossibleMatches;
#endif
                        byte[] currentBlockHash = HashProvider.ComputeHash(slidingBuffer.GetBuffer(), 0,
                                                                           (int) currentBlockSize);
                        HashBlock matchingTargetBlock;
                        if ((matchingTargetBlock =
                             possibleRemoteBlockMatches.FirstOrDefault(
                                 entry => entry.Hash.SequenceEqual(currentBlockHash))) != null)
                        {
#if DEBUG
                            Statistics.Matching += 1;
#endif
                            if ((currentByteDelta.Length = (int) (offset - currentByteDelta.Offset)) > 0)
                            {
                                deltas.Add(currentByteDelta);
                            }
                            deltas.Add(new CopyDelta
                                           {
                                               Offset = matchingTargetBlock.Offset,
                                               Length = matchingTargetBlock.Length
                                           });
                            slidingBuffer.MoveForward((int) currentBlockSize);
                            offset += currentBlockSize;
                            startingNewBlock = true;
                            continue;
                        }
                    }
                }
#if DEBUG
                ++Statistics.NonMatching;
#endif
                slidingBuffer.MoveForward(1);
                ++offset;
                startingNewBlock = false;
            }
            Statistics.FileLength = offset;
            if (!startingNewBlock && (currentByteDelta.Length = (int) (offset - currentByteDelta.Offset)) > 0)
            {
                deltas.Add(currentByteDelta);
            }
#if DEBUG
            Statistics.FileLength = offset;
#endif
            return deltas;
        }

        public void Initialize(int blockSize, IEnumerable<HashBlock> blockHashes)
        {
            if (blockSize <= 0) throw new ArgumentException("blockSize must be greater than zero");
            _blockSize = blockSize;
            _blockHashes = blockHashes;
            _initialized = true;
            Statistics = new DeltaStatistics(_blockSize);
            _remoteBlocksIndexTable = new Dictionary<ushort, List<HashBlock>>();
            BuildIndexTable(_blockHashes);
        }

        #endregion

        #region Methods: private

        private void BuildIndexTable(IEnumerable<HashBlock> entries)
        {
            if (entries == null) return;
            foreach (HashBlock entry in entries)
            {
                ushort hash = RollingChecksum.HashChecksum(entry.Checksum);
                if (_remoteBlocksIndexTable.ContainsKey(hash))
                {
                    if (_remoteBlocksIndexTable[hash].Any(existingEntry => entry.Hash.SequenceEqual(existingEntry.Hash)))
                        continue;
                    _remoteBlocksIndexTable[hash].Add(entry);
                }
                else
                {
                    _remoteBlocksIndexTable[hash] = new List<HashBlock>(new[] {entry});
                }
            }
        }

        private static bool IsSignedIntLength(long l)
        {
            return l == 2147483647;
        }

        #endregion
    }
}