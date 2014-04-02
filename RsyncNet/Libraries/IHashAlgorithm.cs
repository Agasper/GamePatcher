namespace RsyncNet.Libraries
{
    public interface IHashAlgorithm
    {
        #region Properties, indexers, events and operators: public

        int HashSize { get; }

        #endregion

        #region Methods: public

        byte[] ComputeHash(byte[] buffer, int offset, int length);
        void Clear();

        #endregion
    }
}