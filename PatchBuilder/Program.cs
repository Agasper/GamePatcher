using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace PatchBuilder
{
    static class Program
    {
        public static frmMain mainForm;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            mainForm = new frmMain();
            Application.Run(mainForm);
        }
    }
}
