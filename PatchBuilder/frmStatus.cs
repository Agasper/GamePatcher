using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PatchBuilder
{
    public partial class frmStatus : Form
    {
        bool stop = false;

        public frmStatus()
        {
            InitializeComponent();
        }

        private void frmStatus_Load(object sender, EventArgs e)
        {

        }

        public bool UpdateStatus(int percent)
        {
            if (progressBarTotal.InvokeRequired)
                progressBarTotal.Invoke(new Func<int, bool>(UpdateStatus), percent);
            else
            {
                progressBarTotal.Value = percent;

                this.Text = string.Format("Completed {0}%", percent);

                if (percent == 0 || percent == 100)
                    btnCancel.Enabled = true;

                if (percent >= 100)
                    btnCancel.Text = "Close";
                else
                    btnCancel.Text = "Cancel";
            }
            return stop;
        }

        public bool UpdateText(string text)
        {
            if (label.InvokeRequired)
                label.Invoke( new Func<string, bool>(UpdateText), text);
            else
                label.Text = text;
            return stop;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (progressBarTotal.Value >= 100)
            {
                this.Hide();
                return;
            }
            stop = true;
            btnCancel.Enabled = false;
        }
    }
}
