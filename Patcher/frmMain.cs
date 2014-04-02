using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using System.Threading;
using System.Resources;
using System.IO;
using System.Web;
using System.Net;
using System.Diagnostics;

namespace Patcher
{
    public partial class frmMain : Form
    {
        bool drag = false;
        Point startDragPoint;

        Configuration configuration;
        string patcherDirectory;
        BackgroundWorker worker;
        AutoResetEvent patchDownloadEvent;
        long lastBytesReceived;
        DateTime lastProgressChanged;
        Exception downloadingException;

        public frmMain()
        {
            InitializeComponent();
            patcherDirectory = Path.Combine(Application.StartupPath, "patcher");
            worker = new BackgroundWorker();
        }

        #region CloseButton

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        private void btnClose_MouseLeave(object sender, EventArgs e)
        {
            btnClose.BackgroundImage = Properties.Resources.close_button;
        }

        private void btnClose_MouseEnter(object sender, EventArgs e)
        {
            btnClose.BackgroundImage = Properties.Resources.close_button_hover;
        }

        private void btnClose_MouseDown(object sender, MouseEventArgs e)
        {
            btnClose.BackgroundImage = Properties.Resources.close_button_active;
        }

        private void btnClose_MouseUp(object sender, MouseEventArgs e)
        {
            btnClose.BackgroundImage = Properties.Resources.close_button;
        }

        #endregion

        #region MinimizeButton

        private void btnMinimize_MouseUp(object sender, MouseEventArgs e)
        {
            btnMinimize.BackgroundImage = Properties.Resources.minimize_button;
        }

        private void btnMinimize_MouseDown(object sender, MouseEventArgs e)
        {
            btnMinimize.BackgroundImage = Properties.Resources.minimize_button_active;
        }

        private void btnMinimize_MouseEnter(object sender, EventArgs e)
        {
            btnMinimize.BackgroundImage = Properties.Resources.minimize_button_hover;
        }

        private void btnMinimize_MouseLeave(object sender, EventArgs e)
        {
            btnMinimize.BackgroundImage = Properties.Resources.minimize_button;
        }

        private void btnMinimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        #endregion

        #region FormDrag

        private void frmMain_MouseUp(object sender, MouseEventArgs e)
        {
            drag = false;
        }

        private void frmMain_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Y > 32) return;
            drag = true;
            startDragPoint = new Point(e.X, e.Y);
        }

        void Drag(MouseEventArgs e)
        {
            if (drag)
            {
                Point p1 = new Point(e.X, e.Y);
                Point p2 = this.PointToScreen(p1);
                Point p3 = new Point(p2.X - this.startDragPoint.X,
                                     p2.Y - this.startDragPoint.Y);
                this.Location = p3;
            }
        }

        private void frmMain_MouseMove(object sender, MouseEventArgs e)
        {
            Drag(e);
        }

        private void lblTitle_MouseUp(object sender, MouseEventArgs e)
        {
            drag = false;
        }

        private void lblTitle_MouseMove(object sender, MouseEventArgs e)
        {
            Drag(e);
        }

        private void lblTitle_MouseDown(object sender, MouseEventArgs e)
        {
            drag = true;
            startDragPoint = new Point(e.X, e.Y);
        }

        #endregion

        private void frmMain_Load(object sender, EventArgs e)
        {
            patchDownloadEvent = new AutoResetEvent(false);

            lblVersion.Text = "v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            try
            {
                picPublisherLogo.Image = Bitmap.FromFile(Path.Combine(patcherDirectory, "publisher_logo.png"));
            }
            catch { }

            try
            {
                picGameLogo.Image = Bitmap.FromFile(Path.Combine(patcherDirectory, "game_logo.png"));
            }
            catch { }

            try
            {
                configuration = new Configuration(Path.Combine(patcherDirectory, "configuration.xml"));
            }
            catch(Exception ex)
            {
                MessageBox.Show(string.Format(Program.resourceManager.GetString("cant_load_configuration"), ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }

            lblTitle.Text = configuration.GameName + " - " + Program.resourceManager.GetString("launcher");
            lblStatus.Text = Program.resourceManager.GetString("initialization");
            
            webBrowser.Navigate(configuration.NewsUrl);

            btnPlay.MouseEnter += new EventHandler(ButtonHover);
            btnPlay.MouseLeave += new EventHandler(ButtonNormal);
            btnPlay.MouseDown += new MouseEventHandler(ButtonActive);
            btnPlay.MouseUp += new MouseEventHandler(ButtonNormal);

            btnCancel.MouseEnter += new EventHandler(ButtonHover);
            btnCancel.MouseLeave += new EventHandler(ButtonNormal);
            btnCancel.MouseDown += new MouseEventHandler(ButtonActive);
            btnCancel.MouseUp += new MouseEventHandler(ButtonNormal);

            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += new DoWorkEventHandler(Update);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(UpdateComplete);
            worker.RunWorkerAsync();
        }

        void SetTextStatus(string text)
        {
            if (lblStatus.InvokeRequired)
                lblStatus.Invoke(new Action<string>(SetTextStatus), text);
            else
                lblStatus.Text = text;
        }

        void SetProgressBar(int percent)
        {
            if (progressBar.InvokeRequired)
                progressBar.Invoke(new Action<int>(SetProgressBar), percent);
            else
            {
                progressBar.Value = percent;
                Windows7Taskbar.SetProgressValue(this.Handle, (ulong)percent, (ulong)progressBar.Maximum);
            }
        }

        void UpdateComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled)
            {
                btnPlay.Enabled = true;
                SetTextStatus(Program.resourceManager.GetString("update_done"));
                SetProgressBar(100);
                return;
            }

            SetTextStatus(Program.resourceManager.GetString("update_error"));
            SetProgressBar(0);
        }

        void Update(object sender, DoWorkEventArgs e)
        {
            if (Program.targetDirectory == "")
            {
                if (MessageBox.Show(Program.resourceManager.GetString("run_without_target_directory"), "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == System.Windows.Forms.DialogResult.Yes)
                {
                    Program.targetDirectory = Application.StartupPath;
                }
                else
                {
                    e.Cancel = true;
                    return;
                }
            }
            SetProgressBar(0);
            SetTextStatus(Program.resourceManager.GetString("checking_version"));
            BackgroundWorker worker = (BackgroundWorker)sender;

            int currentVersion = 0;
            int lastVersion = 0;

            try
            {
                currentVersion = Updater.GetCurrentVersion(Path.Combine(patcherDirectory, "version.txt"));
            }
            catch
            {
                e.Cancel = true;
                MessageBox.Show(Program.resourceManager.GetString("cant_get_current_version"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SetTextStatus(Program.resourceManager.GetString("checking_new_version"));
            if (worker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }

            try
            {
                WebClient webClientVer = new WebClient();
                lastVersion = int.Parse(webClientVer.DownloadString(new Uri(configuration.VersionUrl)));
            }
            catch (UriFormatException)
            {
                e.Cancel = true;
                MessageBox.Show("Configuration.check_version_url is incorrent. Url expected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            catch (FormatException)
            {
                e.Cancel = true;
                MessageBox.Show(string.Format("For url `{0}` server has invalid response. Integer is expected.", configuration.VersionUrl), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            catch (Exception ex)
            {
                e.Cancel = true;
                WebException webex = (WebException)ex;
                HttpWebResponse response = (HttpWebResponse)webex.Response;
                MessageBox.Show(Program.resourceManager.GetString("cant_get_new_version") + "\nResponse: " + response.StatusDescription, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (worker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }

            for (int i = currentVersion; i < lastVersion; i++)
            {
                SetTextStatus(Program.resourceManager.GetString("start_downloading"));
                WebClient webClient = new WebClient();
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(webClient_DownloadProgressChanged);
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(webClient_DownloadFileCompleted);
                string tempPatchFilename = Path.GetTempFileName();
                string patch_filename = string.Format("{0}_{1}.patch", i, i + 1);
                string patch_uri = configuration.PatchesDirectory + patch_filename;

                patchDownloadEvent.Reset();
                try
                {
                    webClient.DownloadFileAsync(new Uri(patch_uri), tempPatchFilename, patch_filename);
                }
                catch (UriFormatException)
                {
                    e.Cancel = true;
                    MessageBox.Show("Configuration.patches_directory is incorrent. Nothing can be downloaded from `" + patch_uri + "`. Url expected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                patchDownloadEvent.WaitOne();

                if (downloadingException != null)
                {
                    WebException webex = (WebException)downloadingException;
                    HttpWebResponse response = (HttpWebResponse)webex.Response;
                    e.Cancel = true;
                    MessageBox.Show(Program.resourceManager.GetString("cant_get_patch")+ "\nMessage: " + webex.Message + "\nRequest: " + (response != null ? response.ResponseUri.ToString() : "null") + "\nResponse: " + (response != null ? response.StatusDescription : "null"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    Updater.ApplyPatch(tempPatchFilename, new Action<string, int>(delegate(string text, int percent)
                    {
                        SetTextStatus(text);
                        SetProgressBar(percent);
                    }));
                }
                catch (PatcherException ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    e.Cancel = true;
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format(Program.resourceManager.GetString("unknown_exception"), ex.Message, ex.StackTrace), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    e.Cancel = true;
                    return;
                }

                File.Delete(tempPatchFilename);

            }

        }

        void webClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
                downloadingException = e.Error;
            else
                downloadingException = null;

            patchDownloadEvent.Set();
        }

        void webClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            SetTextStatus( string.Format(Program.resourceManager.GetString("downloading"), 
                e.UserState.ToString(),
                Updater.HumanReadableSizeFormat(e.BytesReceived),
                Updater.HumanReadableSizeFormat(e.TotalBytesToReceive),
                e.ProgressPercentage.ToString()));

            SetProgressBar(e.ProgressPercentage);

            lastBytesReceived = e.BytesReceived;
            lastProgressChanged = DateTime.Now;
        }


        private void ButtonHover(object sender, EventArgs e)
        {
            ((Button)sender).BackgroundImage = Properties.Resources.button_hover;
        }

        private void ButtonActive(object sender, EventArgs e)
        {
            ((Button)sender).BackgroundImage = Properties.Resources.button_active;
        }

        private void ButtonNormal(object sender, EventArgs e)
        {
            ((Button)sender).BackgroundImage = Properties.Resources.button;
        }


        private void picGameLogo_Click(object sender, EventArgs e)
        {
            if (configuration.GameUrl.Length > 0)
                System.Diagnostics.Process.Start(configuration.GameUrl);
        }

        private void picPublisherLogo_Click(object sender, EventArgs e)
        {
            if (configuration.PublisherUrl.Length > 0)
                System.Diagnostics.Process.Start(configuration.PublisherUrl);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            //if (worker.IsBusy)
            //{
            //    if (MessageBox.Show(Program.resource_manager.GetString("exit_confirmation"), "Message", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
            //    {
            //        worker.CancelAsync();
            //        btnCancel.Enabled = false;
            //    }
            //    return;
            //}

            this.Close();
        }

        private void btnPlay_Paint(object sender, PaintEventArgs e)
        {
            ButtonRepaint(sender, e);
        }

        private void btnCancel_Paint(object sender, PaintEventArgs e)
        {
            ButtonRepaint(sender, e);
        }

        private void ButtonRepaint(object sender, PaintEventArgs e)
        {
            var btn = (Button)sender;
            if (btn.Enabled)
                return;
            var drawBrush = new SolidBrush(Color.Gray);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(btn.Text, btn.Font, drawBrush, e.ClipRectangle, sf);
            drawBrush.Dispose();
            sf.Dispose();
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo(Path.Combine(Program.targetDirectory, configuration.GameExe));
            process.Start();
            this.Close();
        }



    }
}
