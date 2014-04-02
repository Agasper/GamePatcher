namespace RsyncNet.Libraries
{
    using System;

    public class RollingChecksum : IRollingChecksum
    {
        private byte[] _block;
        private int _blockSize;

        private uint _r1;
        private uint _r2;
        private int _roundRobinOffset;

        public RollingChecksum()
        {
            Reset();
        }

        #region IRollingChecksum Properties, indexers, events and operators

        public uint Value
        {
            get { return (_r1 & 0xffff) | (_r2 << 16); }
        }

        #endregion

        #region Methods: public

        public static ushort HashChecksum(long checksum)
        {
            return (ushort) (((checksum >> 16) + (checksum & 0xFFFF)) & 0xFFFF);
        }

        #endregion

        #region IRollingChecksum Methods

        public void ProcessBlock(byte[] block, int index, int blockSize)
        {
            _block = new byte[blockSize];
            _blockSize = blockSize;
            Array.Copy(block, index, _block, 0, blockSize);
            _r1 = 0;
            _r2 = 0;
            int i;
            for (i = 0; i < blockSize - 4; i += 4)
            {
                int p = index + i;
                _r2 +=
                    4*(_r1 + block[p]) +
                    3*(uint) block[p + 1] +
                    2*(uint) block[p + 2] +
                    block[p + 3];
                _r1 +=
                    block[p] +
                    (uint) block[p + 1] +
                    block[p + 2] +
                    block[p + 3];
            }
            for (; i < blockSize; ++i)
            {
                _r1 += block[index + i];
                _r2 += _r1;
            }
            _roundRobinOffset = 0;
        }

        public void Reset()
        {
            _blockSize = 0;
            _roundRobinOffset = 0;
        }

        public void RollByte(byte b)
        {
            _r1 -= _block[_roundRobinOffset];
            _r2 -= (uint)_blockSize *_block[_roundRobinOffset];
            _r1 += b;
            _r2 += _r1;
            _block[_roundRobinOffset] = b;
            _roundRobinOffset = ++_roundRobinOffset%_blockSize;
        }

        public void TrimFront()
        {
            _r1 -= _block[_roundRobinOffset];
            _r2 -= (uint)_blockSize*_block[_roundRobinOffset];
            _roundRobinOffset = ++_roundRobinOffset%_blockSize;
            --_blockSize;
        }

        #endregion
    }
}