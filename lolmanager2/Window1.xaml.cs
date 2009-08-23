//If debug is defined, the error's will not get catched
#define debug

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Net;

namespace lolmanager2
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        //For managing the server list
        const string serverListFileName = "serverlist.txt";
        private ServerManager serverManager = new ServerManager(serverListFileName);
        DataTable serverTable;

        //These hold the stuff for the 'downloads' page
        DataTable downloadTable;
        GameInstaller gameInstaller;

        //This is uses for stuff that has to be done in the background,
        //such as reloading the servers list or downloading a game
        BackgroundWorker bgWorker = new BackgroundWorker();

        //This is for determining transfer speed
        Queue<FileSpeedInfo> lastFilesSpeed = new Queue<FileSpeedInfo>();
        const int fileSpeedQueueLength = 8000; //miliseconds

        struct FileSpeedInfo
        {
            internal Int64 fileSize;
            internal DateTime timeStamp;
        }

        public Window1()
        {
            InitializeComponent();

            //Init the download table
            downloadTable = new DataTable("Downloads Table");
            DataColumn primary = downloadTable.Columns.Add("Name", typeof(string));
            downloadTable.Columns.Add("Progress", typeof(string));
            downloadTable.Columns.Add("Status", typeof(string));
            downloadTable.PrimaryKey = new DataColumn[] { primary };
            ListViewDownloads.DataContext = downloadTable;

            //Init the server list
            serverTable = new DataTable("Server List");
            DataColumn primary2 = serverTable.Columns.Add("Url", typeof(string));
            serverTable.Columns.Add("Status", typeof(string));
            serverTable.PrimaryKey = new DataColumn[] { primary2 };
            listViewServerList.DataContext = serverTable;

            RefreshServers();
        }

        #region Server List

        void RefreshServers()
        {
            if (bgWorker.IsBusy)
            {
                MessageBox.Show("Error, an other process is already running");
                return;
            }

            bgWorker = new BackgroundWorker();
            bgWorker.DoWork += new DoWorkEventHandler(RefreshServerList);

            serverTable.Rows.Clear();

            foreach (string s in this.serverManager.GetServers())
            {
                serverTable.Rows.Add(s, "Waiting for Update...");
            }

            serverTable.AcceptChanges();

            bgWorker.RunWorkerAsync();
        }

        void RefreshServerList(object sender, DoWorkEventArgs e)
        {
            WebClient client = new WebClient();

            foreach (string s in this.serverManager.GetServers())
            {
                string status = "Online";
                string result;
                try
                {
                    result = client.DownloadString(s);
                }
                catch (Exception ex)
                {
                    status = ex.Message;
                }
                serverTable.Rows.Find(s)["Status"] = status;
            }
        }

        private void ButtonAddServer_Click(object sender, RoutedEventArgs e)
        {
            this.serverManager.AddServer(textBoxAddServer.Text);
            RefreshServers();
        }

        private void ButtonRefreshServers_Click(object sender, RoutedEventArgs e)
        {
            RefreshServers();
        }

        private void ButtonRemoveServer_Click(object sender, RoutedEventArgs e)
        {
            DataRowView row = listViewServerList.SelectedItem as DataRowView;
            if (row != null)
            {
                this.serverManager.RemoveServer(row.Row["Url"] as string);
                serverTable.Rows.Remove(row.Row);
            }
        }

        #endregion
        #region Downloads

        private void buttonAddDownload(object sender, RoutedEventArgs e)
        {
            //Empy the download table
            downloadTable.Clear();
            try
            {
                gameInstaller = new GameInstaller(textBoxFrom.Text, textBoxTo.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            this.bgWorker = new BackgroundWorker();
            this.bgWorker.WorkerReportsProgress = true;
            this.bgWorker.DoWork += new DoWorkEventHandler(GameDownloadStart);
            this.bgWorker.ProgressChanged += new ProgressChangedEventHandler(GameDownloadProgressChanged);
            this.bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(GameDownloadCompleted);            
            this.bgWorker.RunWorkerAsync();    
        }

        void GameDownloadStart(object sender, DoWorkEventArgs e)
        {
#if (!debug)
            try{
#endif
            gameInstaller.parent = (BackgroundWorker)sender;
            gameInstaller.ParseXML();
            gameInstaller.FastScanMissing();
            gameInstaller.DownloadToDo();
#if (!debug)
            }catch (Exception ex) { MessageBox.Show(ex.Message); }
#endif
        }

        void GameDownloadCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("Download completed!");
        }

        void GameDownloadProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            InstallChangedEventArgs drp = (InstallChangedEventArgs)e.UserState;
            switch (drp.type)
            {
                case InstallChangedEventType.log:
                    textBoxLog.Text += "\n" + drp.message;
                    break;

                case InstallChangedEventType.downloadStart:
                    downloadTable.Rows.Add(drp.remoteName, drp.bytesDownloaded, "Connecting");
                    textBoxDownloads.Text = "Downloading...";
                    object lastItem = ListViewDownloads.Items[ListViewDownloads.Items.Count - 1];
                    ListViewDownloads.ScrollIntoView(lastItem);
                    break;    
                
                case InstallChangedEventType.downloadProgressChanged:
                    DataRow row = downloadTable.Rows.Find(drp.remoteName);
                    if (row != null)
                    {
                        row["Progress"] = string.Format(
                            "{0}% {1}/{2}",
                            Math.Round(
                                100.0 * (double)drp.bytesDownloaded / (double)drp.fileSize,
                                1
                            ),
                            drp.bytesDownloaded,
                            drp.fileSize
                        );
                        row["Status"] = "Downloading";
                    }
                    break;

                case InstallChangedEventType.verifyingStart:
                    DataRow row2 = downloadTable.Rows.Find(drp.remoteName);
                    row2["Status"] = "Verifying";

                    //Calculate the download speed
                    FileSpeedInfo info = new FileSpeedInfo();
                    info.fileSize = drp.fileSize;
                    info.timeStamp = DateTime.Now;

                    this.lastFilesSpeed.Enqueue(info);
                    Int64 sumSize = 0;

                    //Remove entries older than 10 sec
                    DateTime now = DateTime.Now;
                    int entriesToRemove = 0;
                    foreach (FileSpeedInfo f in this.lastFilesSpeed)
                    {
                        if ((now - f.timeStamp).TotalMilliseconds > fileSpeedQueueLength)
                        {
                            entriesToRemove++;
                        }
                        else
                        {
                            sumSize += f.fileSize;
                        }
                    }
                    for (int i = 0; i < entriesToRemove; i++)
                        this.lastFilesSpeed.Dequeue();

                    textBoxDownloadSpeed.Text = string.Format("{0} kB/s", Math.Round((double)sumSize / fileSpeedQueueLength * 1000 / 1024, 0));
                    break;

                case InstallChangedEventType.verifyingFailed:
                    DataRow row3 = downloadTable.Rows.Find(drp.remoteName);
                    row3["Status"] = "Hash Failed!";
                    break;

                case InstallChangedEventType.completed:
                    DataRow row4 = downloadTable.Rows.Find(drp.remoteName);
                    row4["Status"] = "Completed";
                    break;

                default:
                    throw new Exception("Not implemented !!!!");
            }
        }

        #endregion

    }
}
