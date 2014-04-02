using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using RsyncNet.Hash;
using RsyncNet.Libraries;
using RsyncNet.Delta;

namespace PatchBuilder
{
    class FileHelper
    {

        public const int blockSize = 1024;

        public struct FileEntry
        {
            public enum EntryType
            {
                Added,
                Modified,
                Removed
            }

            public EntryType entryType;
            public string filename;
            public string newMD5;
            public string oldMD5;

            public FileEntry(EntryType type, string filename, string new_md5 = "", string old_md5 = "")
            {
                this.entryType = type;
                this.filename = filename;
                this.newMD5 = new_md5;
                this.oldMD5 = old_md5;
            }
        }

        public static string FileEntryListToString(List<FileEntry> list)
        {
            string result = "";
            foreach (FileEntry entry in list)
            {
                if (entry.entryType == FileEntry.EntryType.Removed)
                    result += string.Format("{0}\t{1}\n", entry.entryType.ToString()[0], entry.filename);
                else if (entry.entryType == FileEntry.EntryType.Added)
                    result += string.Format("{0}\t{1}\t{2}\n", entry.entryType.ToString()[0], entry.filename, entry.newMD5);
                else if (entry.entryType == FileEntry.EntryType.Modified)
                    result += string.Format("{0}\t{1}\t{2}\t{3}\n", entry.entryType.ToString()[0], entry.filename, entry.newMD5, entry.oldMD5);
            }

            return result;
        }

        

        public static void FilterRelativePathes(string folder, ref string[] files)
        {
            for (int i = 0; i < files.Length; i++)
            {
                files[i] = files[i].Substring(folder.Length);
                if (files[i][0] == Path.DirectorySeparatorChar)
                    files[i] = files[i].Substring(1);
            }
        }

        public static string GetMD5HashFromFile(string fileName)
        {
            FileStream file = new FileStream(fileName, FileMode.Open);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }

        public static byte[] CreatePatchFor(string filename1, string filename2)
        {
            // Compute hashes
            HashBlock[] hashBlocksFromReceiver;
            using (FileStream sourceStream = File.Open(filename1, FileMode.Open))
            {
                hashBlocksFromReceiver = new HashBlockGenerator(new RollingChecksum(),
                                                                new HashAlgorithmWrapper<MD5>(MD5.Create()),
                                                                blockSize).ProcessStream(sourceStream).ToArray();
            }

            // Compute deltas
            MemoryStream deltaStream = new MemoryStream();
            using (FileStream fileStream = File.Open(filename2, FileMode.Open))
            {
                DeltaGenerator deltaGen = new DeltaGenerator(new RollingChecksum(), new HashAlgorithmWrapper<MD5>(MD5.Create()));
                deltaGen.Initialize(blockSize, hashBlocksFromReceiver);
                IEnumerable<IDelta> deltas = deltaGen.GetDeltas(fileStream);
                deltaGen.Statistics.Dump();
                fileStream.Seek(0, SeekOrigin.Begin);
                DeltaStreamer streamer = new DeltaStreamer();
                streamer.Send(deltas, fileStream, deltaStream);
            }

            return deltaStream.ToArray();
        }

        public static void CompessFile(ZipOutputStream zipStream, string filename, string zipFilename)
        {
            FileInfo fi = new FileInfo(filename);

            ZipEntry newZipEntry = new ZipEntry(zipFilename);
            newZipEntry.Size = fi.Length;

            zipStream.PutNextEntry(newZipEntry);
            
            byte[] buffer = new byte[4096];
            using (FileStream streamReader = File.OpenRead(filename))
            {
                StreamUtils.Copy(streamReader, zipStream, buffer);
            }

            zipStream.CloseEntry();
        }

        public static void CompessFileFromData(ZipOutputStream zipStream, string filename, string data)
        {
            CompessFileFromData(zipStream, filename, System.Text.Encoding.UTF8.GetBytes(data));
        }

        public static void CompessFileFromData(ZipOutputStream zipStream, string filename, byte[] data)
        {
            ZipEntry newZipEntry = new ZipEntry(filename);
            newZipEntry.Size = data.Length;

            zipStream.PutNextEntry(newZipEntry);

            zipStream.Write(data, 0, data.Length);

            zipStream.CloseEntry();
        }
    }
}
