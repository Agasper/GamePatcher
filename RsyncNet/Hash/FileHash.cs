namespace RsyncNet.Hash
{
    using System.Collections.Generic;

    public class FileHash
    {
        #region Properties, indexers, events and operators: public

        public IEnumerable<HashBlock> Hash { get; set; }
        public string Path { get; set; }

        #endregion
    }
}