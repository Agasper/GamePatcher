using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.Globalization;
using System.Resources;
using System.IO;
using System.Diagnostics;

namespace Patcher
{
    static class Program
    {
        public static ResourceManager resourceManager;
        public static string targetDirectory = "";

        [STAThread]
        static void Main(string[] args)
        {
            resourceManager = new ResourceManager("Patcher.Properties.Strings", System.Reflection.Assembly.GetExecutingAssembly());

            if (args.Length == 0)
            {
                //copy itself to temp and run
                string tempFolder = Path.Combine(Path.GetTempPath(), "Patcher_");
                if (!Directory.Exists(tempFolder))
                    Directory.CreateDirectory(tempFolder);

                List<string> filesToCopy = new List<string>();
                filesToCopy.Add(Application.ExecutablePath);
                filesToCopy.Add(Path.Combine(Application.StartupPath, "ICSharpCode.SharpZipLib.dll"));
                filesToCopy.Add(Path.Combine(Application.StartupPath, "RsyncNet.dll"));
                filesToCopy.AddRange(Directory.GetFiles(Path.Combine(Application.StartupPath, "ru")));
                filesToCopy.AddRange(Directory.GetFiles(Path.Combine(Application.StartupPath, "Patcher")));

                foreach (string file in filesToCopy)
                {
                    string target_path = Path.Combine(tempFolder, file.Substring(Application.StartupPath.Length + 1));

                    if (!Directory.Exists(Path.GetDirectoryName(target_path)))
                        Directory.CreateDirectory(Path.GetDirectoryName(target_path));

                    File.Copy(file, target_path, true);
                }

                Process process = new Process();
                process.StartInfo = new ProcessStartInfo(Path.Combine(tempFolder, Path.GetFileName(Application.ExecutablePath)), Application.StartupPath);
                process.Start();

                return;
            }
            else if (args[0].ToUpper() != "DEBUG")
            {
                targetDirectory = args[0];
                if (!Directory.Exists(targetDirectory))
                {
                    MessageBox.Show(string.Format(resourceManager.GetString("invalid_target_directory"), targetDirectory), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain());
        }
    }
}
