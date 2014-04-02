using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PatchBuilder.Properties;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using System.Security.Cryptography;

namespace PatchBuilder
{
    public partial class frmMain : Form
    {
        

        frmStatus statusForm;
        string sourceFolder;
        string patchFolder;

        public frmMain()
        {
            InitializeComponent();
        }
        

        private void frmMain_Load(object sender, EventArgs e)
        {
            statusForm = new frmStatus();
            numericUpDown1.Value = Settings.Default.v1;
            numericUpDown2.Value = Settings.Default.v2;
            sourceFolder = Path.Combine(Application.StartupPath, "source");
            patchFolder = Path.Combine(Application.StartupPath, "output");
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown2.Value = numericUpDown1.Value + 1;
            Settings.Default.v1 = (int)numericUpDown1.Value;
            Settings.Default.Save();
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown1.Value = numericUpDown2.Value - 1;
            Settings.Default.v2 = (int)numericUpDown2.Value;
            Settings.Default.Save();
        }

        private void btnMakePatch_Click(object sender, EventArgs e)
        {
            string folder1 = Path.Combine(sourceFolder, numericUpDown1.Value.ToString());
            string folder2 = Path.Combine(sourceFolder, numericUpDown2.Value.ToString());

            if (!Directory.Exists(folder1))
            {
                MessageBox.Show(string.Format("Version {0} folder does not exists. Path: {1}", numericUpDown1.Value, folder1), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(folder2))
            {
                MessageBox.Show(string.Format("Version {0} folder does not exists. Path: {1}", numericUpDown1.Value, folder2), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(patchFolder))
                Directory.CreateDirectory(patchFolder);

            btnMakePatch.Enabled = false;

            statusForm.Show(this);
            statusForm.UpdateText("Calculating files...");
            statusForm.UpdateStatus(0);

            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += new DoWorkEventHandler(MakePatch);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.RunWorkerAsync();
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
                statusForm.UpdateText("Cancelled");
            else
                statusForm.UpdateText("Done!");

            statusForm.UpdateStatus(100);

            btnMakePatch.Enabled = true;
        }

        void MakePatch(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            string folder1 = Path.Combine(sourceFolder, numericUpDown1.Value.ToString());
            string folder2 = Path.Combine(sourceFolder, numericUpDown2.Value.ToString());

            try
            {
                if (numericUpDown1.Value != int.Parse(File.ReadAllText(Path.Combine(Path.Combine(folder1, "patcher"), "version.txt")).Trim()))
                    throw new Exception("invalid value");
            }
            catch
            {
                if (MessageBox.Show(string.Format("File `.\\{0}\\patcher\\version.txt` does not exists or have incorrect value. Continue ?", numericUpDown1.Value), "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            try
            {
                if (numericUpDown2.Value != int.Parse(File.ReadAllText(Path.Combine(Path.Combine(folder2, "patcher"), "version.txt")).Trim()))
                    throw new Exception("invalid value");
            }
            catch
            {
                if (MessageBox.Show(string.Format("File `.\\{0}\\patcher\\version.txt` does not exists or have incorrect value. Continue ?", numericUpDown2.Value), "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            List<FileHelper.FileEntry> folderDiff = GetFolderDifference(folder1, folder2);
            if (statusForm.UpdateStatus(10))
            {
                e.Cancel = true;
                return;
            }

            statusForm.UpdateText("Creating patch...");

            using (FileStream fsOut = File.Create(Path.Combine(patchFolder, string.Format("{0}_{1}.patch", numericUpDown1.Value.ToString(), numericUpDown2.Value.ToString()))))
            {
                using (ZipOutputStream zipStream = new ZipOutputStream(fsOut))
                {
                    zipStream.SetLevel(9);

                    for (int i = 0; i < folderDiff.Count; i++)
                    {
                        FileHelper.FileEntry entry = folderDiff[i];

                        if (entry.entryType == FileHelper.FileEntry.EntryType.Added)
                        {
                            statusForm.UpdateText("Compress new file " + entry.filename);
                            FileHelper.CompessFile(zipStream, Path.Combine(folder2, entry.filename), entry.filename);
                        }

                        if (entry.entryType == FileHelper.FileEntry.EntryType.Modified)
                        {
                            statusForm.UpdateText("Create patch for " + entry.filename);
                            var patch = FileHelper.CreatePatchFor(Path.Combine(folder1, entry.filename), Path.Combine(folder2, entry.filename));
                            statusForm.UpdateText("Compress patch for " + entry.filename);
                            FileHelper.CompessFileFromData(zipStream, entry.filename, patch);
                        }

                        if (statusForm.UpdateStatus((int)(((float)i / folderDiff.Count) * 98)))
                        {
                            e.Cancel = true;
                            return;
                        }
                    }

                    FileHelper.CompessFileFromData(zipStream, "patch_info.txt", string.Format("{0}\n", FileHelper.blockSize) + FileHelper.FileEntryListToString(folderDiff));

                    zipStream.IsStreamOwner = true;
                    zipStream.Close();
                }
            }

            statusForm.UpdateStatus(100);
        }


        List<FileHelper.FileEntry> GetFolderDifference(string folder1, string folder2)
        {
            string[] files1 = Directory.GetFiles(folder1, "*.*", SearchOption.AllDirectories);
            string[] files2 = Directory.GetFiles(folder2, "*.*", SearchOption.AllDirectories);

            FileHelper.FilterRelativePathes(folder1, ref files1);
            FileHelper.FilterRelativePathes(folder2, ref files2);

            List<FileHelper.FileEntry> result = new List<FileHelper.FileEntry>();

            int cnt = 0;

            foreach (string v1_file in files1)
            {
                if (!files2.Contains(v1_file))
                    result.Add(new FileHelper.FileEntry(FileHelper.FileEntry.EntryType.Removed, v1_file, ""));
            }

            foreach (string v2_file in files2)
            {
                string md5_2 = FileHelper.GetMD5HashFromFile(Path.Combine(folder2, v2_file));

                if (!files1.Contains(v2_file))
                    result.Add(new FileHelper.FileEntry(FileHelper.FileEntry.EntryType.Added, v2_file, md5_2));
                else
                {
                    string md5_1 = FileHelper.GetMD5HashFromFile(Path.Combine(folder1, v2_file));

                    if (md5_1 != md5_2)
                        result.Add(new FileHelper.FileEntry(FileHelper.FileEntry.EntryType.Modified, v2_file, md5_2, md5_1));
                }

                statusForm.UpdateStatus((int)(100 * cnt++ / (float)files2.Length));
            }

            return result;
        }

       



    }
}
