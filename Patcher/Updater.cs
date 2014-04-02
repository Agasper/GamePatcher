using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using System.Security.Cryptography;
using RsyncNet.Delta;

namespace Patcher
{
    class Updater
    {
        public struct FileEntry
        {
            public enum EntryType
            {
                Added,
                Modified,
                Removed
            }

            public EntryType type;
            public string filename;
            public string md5New;
            public string md5Old;
        }


        public static int GetCurrentVersion(string filename)
        {
            return int.Parse(File.ReadAllText(filename, System.Text.Encoding.UTF8));
        }

        public static string HumanReadableSizeFormat(long bytes)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB" };
            int place = 0;
            if (bytes > 0)
                place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 2);
            return string.Format("{0:#.00}", num) + " " + suf[place];
        }

        public static void ApplyPatch(string filename, Action<string, int> progressCallback)
        {
            using (FileStream fs = File.OpenRead(filename))
            {
                using (ZipFile zf = new ZipFile(fs))
                {
                    progressCallback(Program.resourceManager.GetString("unpacking_patch_info"), 0);

                    List<FileEntry> entryes = ParsePatchInfo(ExtractData(zf, "patch_info.txt"));

                    for (int i = 0; i < entryes.Count; i++)
                    {
                        int percent = (int)(((float)i / entryes.Count) * 100);
                        FileEntry entry = entryes[i];
                        string entryFilename = Path.Combine(Program.targetDirectory, entry.filename);

                        if (entry.type == FileEntry.EntryType.Removed)
                        {
                            progressCallback(string.Format(Program.resourceManager.GetString("deleting_file"), entry.filename), percent);

                            if (File.Exists(entryFilename))
                                File.Delete(entryFilename);
                        }

                        if (entry.type == FileEntry.EntryType.Added)
                        {
                            progressCallback(string.Format(Program.resourceManager.GetString("adding_file"), entry.filename), percent);

                            ExtractFile(zf, entry.filename, entryFilename);
                        }

                        if (entry.type == FileEntry.EntryType.Modified)
                        {
                            progressCallback(string.Format(Program.resourceManager.GetString("checking_file"), entry.filename), percent);

                            string md5_old = GetMD5HashFromFile(entryFilename);

                            if (entry.md5Old != md5_old)
                                throw new PatcherException(string.Format(Program.resourceManager.GetString("invalid_patch"), entryFilename));

                            string patched_file = Path.GetTempFileName();

                            progressCallback(string.Format(Program.resourceManager.GetString("patching_file"), entry.filename), percent);

                            using (FileStream sourceStream = File.Open(entryFilename, FileMode.Open))
                            {
                                using (FileStream outStream = File.Open(patched_file, FileMode.Create))
                                {
                                    DeltaStreamer streamer = new DeltaStreamer();
                                    streamer.Receive(ExtractStream(zf, entry.filename), sourceStream, outStream);
                                    outStream.Close();
                                }
                            }

                            progressCallback(string.Format(Program.resourceManager.GetString("copying_file"), entry.filename), percent);

                            File.Copy(patched_file, entryFilename, true);
                            File.Delete(patched_file);
                        }

                        if (entry.type == FileEntry.EntryType.Added || entry.type == FileEntry.EntryType.Modified)
                        {
                            progressCallback(string.Format(Program.resourceManager.GetString("checking_file"), entry.filename), percent);

                            string md5New = GetMD5HashFromFile(entryFilename);

                            if (entry.md5New != md5New)
                                throw new PatcherException(string.Format(Program.resourceManager.GetString("patch_broke_client"), entryFilename));
                        }
                    }
                }
            }
        }

        static string GetMD5HashFromFile(string fileName)
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

        static Stream ExtractStream(ZipFile zf, string entry_name)
        {
            ZipEntry zipEntry = zf.GetEntry(entry_name);
            return zf.GetInputStream(zipEntry);
        }

        static string ExtractData(ZipFile zf, string entry_name)
        {
            ZipEntry zipEntry = zf.GetEntry(entry_name);
            Stream zipStream = zf.GetInputStream(zipEntry);

            byte[] buffer = new byte[zipEntry.Size];
            zipStream.Read(buffer, 0, buffer.Length);
            return System.Text.Encoding.UTF8.GetString(buffer);
        }

        static void ExtractFile(ZipFile zf, string entry_name, string filename)
        {
            ZipEntry zipEntry = zf.GetEntry(entry_name);
            Stream zipStream = zf.GetInputStream(zipEntry);

            if (!Directory.Exists(Path.GetDirectoryName(filename)))
                Directory.CreateDirectory(Path.GetDirectoryName(filename));

            byte[] buffer = new byte[4096];
            
            using (FileStream streamWriter = File.Create(filename)) {
                StreamUtils.Copy(zipStream, streamWriter, buffer);
            }
        }

        static List<FileEntry> ParsePatchInfo(string patch_info_data)
        {
            List<FileEntry> result = new List<FileEntry>();
            int block_size = 0;
            foreach (string line in patch_info_data.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Substring(0, 2) == "//")
                    continue;

                if (block_size == 0) //first line in patch
                {
                    block_size = int.Parse(line);
                    continue;
                }

                FileEntry entry = new FileEntry();
                string[] params_ = line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

                entry.filename = params_[1];

                if (params_[0].ToUpper() == "R")
                    entry.type = FileEntry.EntryType.Removed;

                if (params_[0].ToUpper() == "A")
                {
                    entry.type = FileEntry.EntryType.Added;
                    entry.md5New = params_[2];
                }

                if (params_[0].ToUpper() == "M")
                {
                    entry.type = FileEntry.EntryType.Modified;
                    entry.md5New = params_[2];
                    entry.md5Old = params_[3];
                }

                result.Add(entry);
            }
            return result;
        }
    }
}
