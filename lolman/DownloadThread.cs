using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading;

namespace LanOfLegends.lolman
{
    class DownloadThread
    {
        internal ManualResetEvent mre;

        internal string remoteUrl;
        internal string localUrl;
        internal Int64 filesize;
        internal List<FilePart> parts;
        internal string sha1sum;

        internal BackgroundWorker parent;

        internal void StartDownload()
        {
            //Update GUI
            InstallChangedEventArgs prog = new InstallChangedEventArgs();
            prog.remoteName = remoteUrl;
            prog.localName = localUrl;
            prog.type = InstallChangedEventType.downloadStart;
            prog.fileSize = filesize;
            prog.bytesDownloaded = 0;
            this.parent.ReportProgress(0, prog);

            //Measure speed
            DateTime startTime = DateTime.Now;

            //Register all stuff to the client
            WebClient client = new WebClient();
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
            client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadFileCompleted);
            //client.DownloadDataCompleted += new DownloadDataCompletedEventHandler(DownloadDataCompleted);

            //Wait till the download is finished
            ManualResetEvent waiter = new ManualResetEvent(false);

            //client.DownloadDataAsync(new Uri(remoteUrl), new object[] { waiter, prog });
            client.DownloadFileAsync(new Uri(remoteUrl), localUrl, new object[] { waiter, prog });
            waiter.WaitOne();

            DateTime stopTime = DateTime.Now;
            TimeSpan time = stopTime - startTime;

            //Update GUI
            prog.type = InstallChangedEventType.verifyingStart;
            prog.downloadTime = time.TotalSeconds;
            prog.fileSize = this.filesize;
            this.parent.ReportProgress(0, prog);

            SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();
            prog.type = InstallChangedEventType.completed;

            if (this.sha1sum == null)
            {
                //TODO, find someting for this...
            }
            else
            {
                FileStream fs = File.OpenRead(this.localUrl);
                string hash = Convert.ToBase64String(sha1.ComputeHash(fs));
                fs.Close();
                if (hash != sha1sum)
                {
                    prog.type = InstallChangedEventType.verifyingFailed;
                    File.Delete(this.localUrl);
                }
            }

            this.parent.ReportProgress(0, prog);
        }

        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            InstallChangedEventArgs prog = (InstallChangedEventArgs)((object[])e.UserState)[1];
            prog.bytesDownloaded = e.BytesReceived;
            prog.type = InstallChangedEventType.downloadProgressChanged;
            this.parent.ReportProgress(0, prog);
        }

        void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            ManualResetEvent waiter = (ManualResetEvent)((object[])e.UserState)[0];
            waiter.Set();
        }
    }
}
