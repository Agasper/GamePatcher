namespace RsyncNet.Libraries
{
    public interface IRollingChecksum
    {
        #region Properties, indexers, events and operators: public

        uint Value { get; }

        #endregion

        #region Methods: public

        void ProcessBlock(byte[] block, int index, int blockSize);
        void Reset();
        void RollByte(byte b);
        void TrimFront();

        #endregion
    }
}