﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Xml;

namespace LanOfLegends.lolman
{
    struct ToDoFile
    {
        internal string path;
        internal string localUrl;
        internal Int64 length;
        internal string sha1sum;
        internal List<FilePart> parts;
    }

    struct FilePart
    {
        internal Int64 length;
        internal string sha1sum;
    }

    struct InstallChangedEventArgs
    {
        internal InstallChangedEventType type;
        internal string message;

        //For log
        internal InstallChangedEventArgs(InstallChangedEventType type, string message)
        {
            this.type = type;
            this.message = message;
            this.fileSize = 0;
            this.remoteName = string.Empty;
            this.localName = string.Empty;
            this.bytesDownloaded = 0;
            this.downloadTime = 0;
        }

        internal Int64 fileSize;
        internal string remoteName;
        internal string localName;
        internal Int64 bytesDownloaded;
        internal double downloadTime;
    }

    enum InstallChangedEventType
    {
        downloadStart,
        downloadProgressChanged,
        verifyingStart,
        completed,
        log,
        verifyingFailed
    }

    class GameInstaller
    {
        const int maxSimulDownloads = 3;
        const Int64 maxSimulSize = 16777216; //16 mb

        //The game to download
        internal LolGame game;

        //The remote directory where all files are stored in
        List<string> baseUrls = new List<string>();
        int mirrorWalker = 0;

        //All files that have to be downloaded
        List<ToDoFile> toDoFiles;

        //The bg that started this
        internal BackgroundWorker parent;

        bool downloadDone;

        //Hold the total size
        //public Int64 totalSize = 0;
        public Int64 sizeToDo = 0;

        internal GameInstaller(LolGame game)
        {
            if (!Directory.Exists(game.local))
            {
                MessageBoxResult result = MessageBox.Show(
                    string.Format(
                        "The folder {0} doesn't exists. Do you want to create it?",
                        game.local
                    ),
                    "Error", MessageBoxButton.OKCancel
                );
                if (result == MessageBoxResult.Cancel)
                    throw new Exception("Stopped downloading, dir doesn't exist"); ;
                Directory.CreateDirectory(game.local);
            }

            this.game = game;
            foreach (string url in game.urls)
            {
                this.baseUrls.Add(url.Substring(0, url.LastIndexOf('/') + 1));
            }

            //Error check
            this.game.local = (new FileInfo(this.game.local + Path.DirectorySeparatorChar)).FullName;
        }

        internal void ParseXML()
        {
            //log
            this.parent.ReportProgress(0, new InstallChangedEventArgs(InstallChangedEventType.log, "Start downloading XML..."));
            WebClient webClient = new WebClient();
            string xmlinfo = webClient.DownloadString(game.urls[0]);
            //log
            this.parent.ReportProgress(0, new InstallChangedEventArgs(InstallChangedEventType.log, "Downloading XML done!"));
            this.parent.ReportProgress(0, new InstallChangedEventArgs(InstallChangedEventType.log, "Start parsing XML..."));

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlinfo);
            XmlElement root = doc.DocumentElement;
            toDoFiles = new List<ToDoFile>();

            if (root.Name != "install")
                throw new Exception("XML root must be 'install'");
            if (root.Attributes["version"].Value != "1.1")
                throw new Exception("Version of XML to high, update your client!");
            foreach (XmlNode node in root.ChildNodes)
            {
                if (node.Name == "files")
                {
                    foreach (XmlNode child in node.ChildNodes)
                    {
                        ParseXMLRecursive(child, "");
                    }
                }
            }

            //log
            this.parent.ReportProgress(
                0,
                new InstallChangedEventArgs(
                    InstallChangedEventType.log,
                    string.Format(
                        "Parsing XML done! {0} files found.",
                        this.toDoFiles.Count
                    )
                )
            );
        }

        private void ParseXMLRecursive(XmlNode node, string directory)
        {
            if (node.Name == "file")
            {
                string fileName = Encoding.UTF8.GetString(Encoding.Default.GetBytes(node.Attributes["name"].Value));
                string path = directory + fileName;
                string localName = this.game.local + directory + fileName;
                Int64 length = Int64.Parse(node.Attributes["length"].Value);

                ToDoFile file = new ToDoFile();
                file.localUrl = localName;
                file.path = path;
                file.length = length;

                if (node.HasChildNodes)
                {
                    file.parts = new List<FilePart>();
                    file.sha1sum = null;
                    foreach (XmlNode part in node.ChildNodes)
                    {
                        FilePart filePart = new FilePart();
                        filePart.length = Int64.Parse(part.Attributes["length"].Value);
                        filePart.sha1sum = part.Attributes["sha1sum"].Value;
                        file.parts.Add(filePart);
                    }
                }
                else
                {
                    file.sha1sum = node.Attributes["sha1sum"].Value;
                }

                this.toDoFiles.Add(file);
            }

            if (node.Name == "folder")
            {
                string dirName = Encoding.UTF8.GetString(Encoding.Default.GetBytes(node.Attributes["name"].Value));
                Directory.CreateDirectory(this.game.local + directory + dirName);

                foreach (XmlNode child in node.ChildNodes)
                {
                    ParseXMLRecursive(child, directory + node.Attributes["name"].Value + "/");
                }
            }
        }

        internal void FastScanMissing()
        {
            this.parent.ReportProgress(0, new InstallChangedEventArgs(InstallChangedEventType.log, "Start fast file scan"));
            List<ToDoFile> todo = new List<ToDoFile>();
            foreach (ToDoFile file in this.toDoFiles)
            {
                if (!File.Exists(file.localUrl) || (new FileInfo(file.localUrl)).Length != file.length)
                {
                    todo.Add(file);
                    this.sizeToDo += file.length;
                }
            }

            this.toDoFiles = todo;

            //log
            this.parent.ReportProgress(
                0,
                new InstallChangedEventArgs
                {
                    type = InstallChangedEventType.log,
                    message = string.Format(
                        "Scanning done! {0} files that should be downloaded.",
                        this.toDoFiles.Count
                    ),
                    fileSize = this.sizeToDo
                }
            );
        }

        internal void DownloadToDo()
        {
            this.parent.ReportProgress(0, new InstallChangedEventArgs(InstallChangedEventType.log, "Start downloading missing files..."));

            BackgroundWorker[] backgroundWorkers = new BackgroundWorker[maxSimulDownloads];
            ManualResetEvent[] resetEvents = new ManualResetEvent[maxSimulDownloads];
            this.downloadDone = false;

            for (int i = 0; i < backgroundWorkers.Length; i++)
            {
                backgroundWorkers[i] = new BackgroundWorker();
                backgroundWorkers[i].DoWork += new DoWorkEventHandler(ThreadStartDownload);
                backgroundWorkers[i].RunWorkerCompleted += new RunWorkerCompletedEventHandler(ThreadDownloadCompleted);
                resetEvents[i] = new ManualResetEvent(false);
                backgroundWorkers[i].RunWorkerAsync(resetEvents[i]);
                Thread.Sleep(200);
            }

            while (true)
            {
                WaitHandle.WaitAny(resetEvents);
                if (downloadDone)
                {
                    //No new downloads have to be started, but the old ones have to be finished
                    WaitHandle.WaitAll(resetEvents);
                    Thread.Sleep(400);//For gui events that are late

                    //log
                    this.parent.ReportProgress(0, new InstallChangedEventArgs(InstallChangedEventType.log, "Downloading missing files done!"));
                    return;
                }
                for (int i = 0; i < backgroundWorkers.Length; i++)
                {
                    if (!backgroundWorkers[i].IsBusy)
                    {
                        backgroundWorkers[i] = new BackgroundWorker();
                        backgroundWorkers[i].DoWork += new DoWorkEventHandler(ThreadStartDownload);
                        backgroundWorkers[i].RunWorkerCompleted += new RunWorkerCompletedEventHandler(ThreadDownloadCompleted);
                        resetEvents[i] = new ManualResetEvent(false);
                        backgroundWorkers[i].RunWorkerAsync(resetEvents[i]);
                    }
                }
            }
        }

        void ThreadStartDownload(object sender, DoWorkEventArgs e)
        {
            DownloadThread th = new DownloadThread();
            th.mre = (ManualResetEvent)e.Argument;

            th.parent = this.parent;

            lock (this.toDoFiles)
            {
                if (this.toDoFiles.Count == 0)
                {
                    this.downloadDone = true;
                    e.Result = th;
                    return;
                }

                //Get a random mirror
                this.mirrorWalker = ++this.mirrorWalker % this.baseUrls.Count;
                th.remoteUrl = this.baseUrls[this.mirrorWalker] + this.toDoFiles[0].path;

                th.localUrl = this.toDoFiles[0].localUrl;
                th.filesize = this.toDoFiles[0].length;
                th.sha1sum = this.toDoFiles[0].sha1sum;
                th.parts = this.toDoFiles[0].parts;
                this.toDoFiles.RemoveAt(0);
            }

            th.StartDownload();
            e.Result = th;
        }

        void ThreadDownloadCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Set the manualresetevent so the GameInstaller stops waiting and searches for an other file to download
            DownloadThread th = (DownloadThread)e.Result;
            ManualResetEvent mre = th.mre;
            mre.Set();
        }
    }
}
