namespace RsyncNet.Hash
{
    public class HashBlock
    {
        #region Properties, indexers, events and operators: public

        public uint Checksum { get; set; }
        public byte[] Hash { get; set; }
        public int Length { get; set; }
        public long Offset { get; set; }

        #endregion
    }
}