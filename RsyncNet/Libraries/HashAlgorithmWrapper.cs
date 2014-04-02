namespace RsyncNet.Libraries
{
    using System;
    using System.Security.Cryptography;

    public class HashAlgorithmWrapper<T> : IHashAlgorithm where T : HashAlgorithm
    {
        private readonly T _t;

        public HashAlgorithmWrapper(T t)
        {
            _t = t;
        }

        #region IHashAlgorithm Properties, indexers, events and operators

        public int HashSize
        {
            get { return _t.HashSize; }
        }

        #endregion

        #region IHashAlgorithm Methods

        public byte[] ComputeHash(byte[] buffer, int offset, int length)
        {
            return _t.ComputeHash(buffer, offset, length);
        }

        public void Clear()
        {
            _t.Clear();
        }

        #endregion
    }
}