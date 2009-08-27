﻿//If debug is defined, the error's will not get catched
#define debug

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Net;
using System.Xml;
using System.Threading;
using System.Collections.ObjectModel;
using System.Collections;
using System.Linq;
using System.Windows.Controls;

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
        ObservableCollection<ServerGameList> _serverList = new ObservableCollection<ServerGameList>();
        SynchronisedObservableCollection<ServerGameList> serverList;

        //For managing the game list
        ObservableCollection<LolGame> _gameList = new ObservableCollection<LolGame>();
        SynchronisedObservableCollection<LolGame> gameList;

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

        struct ServerGameList
        {
            public string url { get; set; }
            public List<string> dirs;
            public string status { get; set; }
        }

        class LolGame
        {
            public Int64 size { get; set; }
            public string sizeHumanReadable
            {
                get
                {
                    if (size < 1024)
                        return size.ToString() + " B";
                    else if (size < 1024 * 1024)
                        return Math.Round((double)size / 1024, 1).ToString() + " kB";
                    else
                        return Math.Round((double)size / 1024 / 1024, 1).ToString() + " MB";
                }
            }
            public string name { get; set; }

            public string status { get; set; }
            public string infohash { get; set; }
            public List<string> urls;
            public string local { get; set; }
            public string statusInfo { get { return urls.Count.ToString() + " servers"; } }
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
            serverList = new SynchronisedObservableCollection<ServerGameList>(this._serverList);
            listViewServerList.DataContext = serverList;

            gameList = new SynchronisedObservableCollection<LolGame>(this._gameList);
            ListViewGameList.DataContext = this.gameList;

            RefreshServers();
        }

        #region Server List

        void RefreshServers()
        {
            RefreshServers(null);
        }

        void RefreshServers(RunWorkerCompletedEventHandler whenDone)
        {
            if (bgWorker.IsBusy)
            {
                MessageBox.Show("Error, an other process is already running");
                return;
            }

            bgWorker = new BackgroundWorker();
            bgWorker.DoWork += new DoWorkEventHandler(RefreshServerList);
            bgWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs args)
            {
                textBoxLog.Text += "\nRefreshing server list completed!";
            };

            //If whendone is set, add an eventhandler
            if (whenDone != null)
                bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(whenDone);

            //Empty all rows
            serverList.Clear();
            foreach (string url in this.serverManager.GetServers())
            {
                ServerGameList list = new ServerGameList();
                list.url = url;
                list.status = "Waiting for Update...";
                serverList.Add(list);
            }

            bgWorker.RunWorkerAsync();
            textBoxLog.Text += "\nStarted refreshing server list...";
        }

        void RefreshServerList(object sender, DoWorkEventArgs e)
        {
            WebClient client = new WebClient();

            foreach (string s in this.serverManager.GetServers())
            {
                //Holds the webresponse
                string result = null;

                //Init a new game list for the server
                ServerGameList serverGameList = new ServerGameList();
                serverGameList.url = s;
                serverGameList.dirs = new List<string>();
                serverGameList.status = "Online";

                try
                {
                    result = client.DownloadString(s);
                    string[] dirs = result.Split('\0');
                    if (dirs.Length == 0)
                        serverGameList.status = "Error, not a lol server";
                    else
                    {
                        if (dirs[0] != "lol.dirlist")
                            serverGameList.status = "Error, not a lol server";
                        else
                        {
                            //Add all dirs
                            for (int i = 1; i < dirs.Length; i++)
                                if (dirs[i].Length == 0)
                                    continue;
                                else
                                    serverGameList.dirs.Add(dirs[i]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    serverGameList.status = ex.Message;
                }
                finally
                {


                    //Find the url to remove
                    ServerGameList? itemToRemove = null;
                    foreach (ServerGameList list in this.serverList)
                    {
                        if (list.url == s)
                        {
                            itemToRemove = list;
                            break;
                        }
                    }
                    serverList.Remove(itemToRemove.Value);
                    serverList.Add(serverGameList);
                }
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
            //Get the selection
            string urlToRemove = ((ServerGameList)listViewServerList.SelectedItem).url;

            //Remove it from the serverlist.txt
            this.serverManager.RemoveServer(urlToRemove);

            //Remove it from the list
            ServerGameList? itemToRemove = null;
            foreach (ServerGameList sgl in this.serverList)
            {
                if (sgl.url == urlToRemove)
                {
                    itemToRemove = sgl;
                    break;
                }
            }
            if (itemToRemove != null)
                serverList.Remove(itemToRemove.Value);
        }

        /// <summary>Hold the last column of the ServerList that was sorted</summary>
        private GridViewColumn lastServerListColumnSorted;

        /// <summary>This is for sorting the server list</summary>
        private void OnServerListHeaderClick(object sender, RoutedEventArgs e)
        {
            GridViewColumn column = ((GridViewColumnHeader)e.OriginalSource).Column;
            if (lastServerListColumnSorted != null)
            {
                lastServerListColumnSorted.HeaderTemplate = null;
            }
            SortDescriptionCollection sorts = listViewServerList.Items.SortDescriptions;
            RenderSort(sorts, column, GetSortDirection(sorts, column, lastServerListColumnSorted), ref lastServerListColumnSorted);
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
        #region Games

        private void ButtonRefreshGameList_Click(object sender, RoutedEventArgs e)
        {
            RefreshServers(new RunWorkerCompletedEventHandler(ServerRefreshCompleted));
        }

        void ServerRefreshCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bgWorker = new BackgroundWorker();
            bgWorker.DoWork += new DoWorkEventHandler(DownloadAllGameInfo);
            bgWorker.RunWorkerAsync();
        }

        void DownloadAllGameInfo(object sender, DoWorkEventArgs e)
        {

            WebClient client = new WebClient();

            foreach (ServerGameList server in this.serverList)
            {
                if (server.status != null && server.status != "Online")
                    continue;

                foreach (string dir in server.dirs)
                {
                    LolGame game = new LolGame();
                    game.status = "OK";

                    string url = server.url.Substring(0, server.url.LastIndexOf("/") + 1) + dir;
                    XmlElement root;

                    game.urls = new List<string>();
                    game.urls.Add(url);

                    Int64 size = 0;

                    try
                    {
                        string result = client.DownloadString(new Uri(url));

                        //Parse the xml
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(result);
                        root = doc.DocumentElement;

                        if (root.Name != "install")
                        {
                            throw new Exception("XML root must be 'install'");
                        }
                        if (root.Attributes["version"].Value != "1.1")
                        {
                            throw new Exception("Error, version of XML file is wrong");
                        }

                        //Count the total size
                        foreach (XmlNode node in root.ChildNodes)
                        {
                            if (node.Name == "name")
                                game.name = node.InnerText;

                            if (node.Name == "files")
                            {
                                foreach (XmlNode child in node.ChildNodes)
                                {
                                    ParseXMLRecursive(child, ref size);
                                }
                            }

                            if (node.Name == "infohash")
                                game.infohash = node.InnerText;
                        }
                    }
                    catch (Exception ex)
                    {
                        game.status = ex.Message;
                    }

                    if (game.infohash == null)
                    {
                        game.status = "Error, no infohash";
                    }

                    game.size = size;

                    //Add the game to the gamelist
                    //If it is already there, add the infohash
                    LolGame previous = null;
                    if (game.status == "OK")
                    {
                        foreach (LolGame g in this.gameList)
                        {
                            if (g.infohash == game.infohash)
                            {
                                previous = g;
                                break;
                            }
                        }
                    }

                    if (previous != null)
                    {
                        this.gameList.Remove(previous);
                        previous.urls.Add(game.urls[0]);
                        this.gameList.Add(previous);
                    }
                    else
                        this.gameList.Add(game);
                }
            }
        }

        private void ParseXMLRecursive(XmlNode node, ref Int64 size)
        {
            if (node.Name == "file")
                size += Int64.Parse(node.Attributes["length"].Value);
            if (node.Name == "folder")
                foreach (XmlNode child in node.ChildNodes)
                    ParseXMLRecursive(child, ref size);
        }

        private void ButtonAddToQueue_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Not implemented!");
        }

        /// <summary>Hold the last column of the gamelist that was sorted</summary>
        private GridViewColumn lastGameListColumnSorted;

        /// <summary>This is for sorting the game list</summary>
        private void OnGameListHeaderClick(object sender, RoutedEventArgs e)
        {
            GridViewColumn column = ((GridViewColumnHeader)e.OriginalSource).Column;
            if (lastGameListColumnSorted != null)
            {
                lastGameListColumnSorted.HeaderTemplate = null;
            }
            SortDescriptionCollection sorts = ListViewGameList.Items.SortDescriptions;
            RenderSort(sorts, column, GetSortDirection(sorts, column, lastGameListColumnSorted), ref lastGameListColumnSorted);
        }

        #endregion

        #region Static Methods for ListView Sorting

        /// <summary>
        /// Gets the sort direction of the column of the listview
        /// </summary>
        /// <param name="sorts">SortDescriptions of the items of the listview</param>
        /// <param name="column">The column that was clicked on</param>
        /// <param name="lastGameListColumnSorted">The last column that was sorted</param>
        /// <returns>ListSortDirection of the sort direction</returns>
        private static ListSortDirection GetSortDirection(SortDescriptionCollection sorts, GridViewColumn column, GridViewColumn lastColumnSorted)
        {
            if (column != null && column == lastColumnSorted && sorts[0].Direction == ListSortDirection.Ascending)
            {
                return ListSortDirection.Descending;
            }
            return ListSortDirection.Ascending;
        }

        /// <summary>
        /// Renders the triangle next to the columns header
        /// </summary>
        /// <param name="sorts">The SortDescriptions of the Items of the ListView</param>
        /// <param name="column">The column that was clicked on</param>
        /// <param name="direction">The Sort Direction</param>
        /// <param name="lastColumnSorted">The last column that was sorted</param>
        private static void RenderSort(SortDescriptionCollection sorts, GridViewColumn column, ListSortDirection direction, ref GridViewColumn lastColumnSorted)
        {
            if (column == null) return;
            column.HeaderTemplate = (DataTemplate)App.Current.FindResource("HeaderTemplate" + direction);

            System.Windows.Data.Binding columnBinding = column.DisplayMemberBinding as System.Windows.Data.Binding;
            if (columnBinding != null)
            {
                sorts.Clear();
                sorts.Add(new SortDescription(columnBinding.Path.Path, direction));
                lastColumnSorted = column;
            }
        }

        #endregion
    }
}