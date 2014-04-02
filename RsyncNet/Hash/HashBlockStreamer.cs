namespace RsyncNet.Hash
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Helpers;

    public class HashBlockStreamer
    {
        #region Methods: public

        public static HashBlock[] Destream(Stream inputStream)
        {
            uint count = inputStream.ReadUInt();
            var hashBlocks = new HashBlock[count];
            for (int i = 0; i < count; ++i)
            {
                hashBlocks[i] = new HashBlock {Hash = new byte[16]};
                inputStream.Read(hashBlocks[i].Hash, 0, 16);
                hashBlocks[i].Length = inputStream.ReadInt();
                hashBlocks[i].Offset = inputStream.ReadLong();
                hashBlocks[i].Checksum = inputStream.ReadUInt();
            }
            return hashBlocks;
        }

        public static void Stream(IEnumerable<HashBlock> hashBlocks, Stream outputStream)
        {
            outputStream.WriteUInt((uint) hashBlocks.Count());
            foreach (HashBlock block in hashBlocks)
            {
                outputStream.Write(block.Hash, 0, 16);
                outputStream.WriteInt(block.Length);
                outputStream.WriteLong(block.Offset);
                outputStream.WriteUInt(block.Checksum);
            }
        }

        #endregion
    }
}