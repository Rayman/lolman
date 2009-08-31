﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;

namespace LanOfLegends.lolgen2
{
    class DirectorySummer : BackgroundWorker
    {
        Int64 size;
        Int64 progress = 0;
        int progressPercentage = 0;
        DirectoryInfo info;
        //2^24 = 16777216 = 16 Mb
        const Int64 maxSumSize = 16777216;
        string message;

        string name; //The name of the game
        List<string> gameHash = new List<string>();

        Icon ico = null;

        public DirectorySummer(string dir, string name)
        {
            this.name = name;
            InitSummer(dir);
        }

        public DirectorySummer(string dir, string name, Icon ico)
        {
            this.name = name;
            this.ico = ico;
            InitSummer(dir);
        }

        public void InitSummer(string dir)
        {
            if (!Directory.Exists(dir))
            {
                throw new Exception("Directory doesn't exist");
            }
            else
            {
                this.info = new DirectoryInfo(dir);
                this.message = "Getting size...";
                this.size = getDirectorySize(this.info);
                this.message += string.Format(" {0} bytes", this.size);
            }

            this.DoWork += new DoWorkEventHandler(Start);
            this.WorkerReportsProgress = true;
        }

        void Start(object sender, DoWorkEventArgs e)
        {
            StringBuilder infoxml = new StringBuilder();
            infoxml.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n");
            infoxml.Append("<install type=\"game\" version=\"1.1\">\n");
            infoxml.Append("\t<name>" + this.name + "</name>\n");

            //Icon as string
            MemoryStream ms = new MemoryStream();
            ico.Save(ms);

            //Compress if smaller
            byte[] icon = ms.ToArray();
            ms.Close();
            byte[] iconCompressed = GzipUtils.Compress(icon);
            if (icon.Length < iconCompressed.Length)
            {
                string base64Icon = Convert.ToBase64String(GzipUtils.Compress(icon));
                infoxml.Append("\t<icon>" + base64Icon + "</icon>\n");
            }
            else
            {
                string base64Icon = Convert.ToBase64String(GzipUtils.Compress(iconCompressed));
                infoxml.Append("\t<icon compression=\"gzip\">" + base64Icon + "</icon>\n");
            }

            infoxml.Append("\t<files>\n");

            ProcessDirectory(this.info, 2, ref infoxml);

            //Calculate the infohash
            var sha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider();
            this.gameHash.Sort();
            StringBuilder concat = new StringBuilder();
            foreach (string s in this.gameHash)
                concat.Append(s);
            var hash = Convert.ToBase64String(sha1.ComputeHash(Encoding.Default.GetBytes(concat.ToString())));

            infoxml.Append("\t</files>\n");
            infoxml.Append("\t<infohash>" + hash + "</infohash>\n");
            infoxml.Append("</install>\n");

            StreamWriter sw = new StreamWriter(this.info.FullName + "/lol.info.xml");
            sw.Write(infoxml.ToString());
            sw.Close();
        }

        protected void ProcessDirectory(DirectoryInfo dir, int depth, ref StringBuilder infoxml)
        {
            SHA1 summer = new SHA1CryptoServiceProvider();

            foreach (FileInfo file in dir.GetFiles())
            {
                //Dont add this file :)
                if (file.Name == "lol.info.xml")
                    continue;

                //Open the file to sha1 it's contents
                this.message = file.FullName;
                Stream s = File.OpenRead(file.FullName);

                //If biggen than 1.5 * maxsumsize, it must be splitted in parts
                if (file.Length * 2 < maxSumSize * 3)
                {
                    string base64hash = Convert.ToBase64String(summer.ComputeHash(s));

                    //Write it to the file
                    for (int i = 0; i < depth; i++) infoxml.Append('\t');
                    infoxml.AppendFormat("<file name=\"{0}\" length=\"{1}\" sha1sum=\"{2}\" />\n",
                                         file.Name,
                                         file.Length,
                                         base64hash
                                         );

                    //Write progress to console
                    this.progress += file.Length;
                    this.ReportProgress();

                    //For the infohash
                    this.gameHash.Add(string.Format("{0}\0{1}\0{2}\n", file.Name, file.Length, base64hash));
                }
                else
                {
                    //Write stuff to file
                    for (int i = 0; i < depth; i++) infoxml.Append('\t');
                    infoxml.AppendFormat("<file name=\"{0}\" length=\"{1}\">\n",
                                         file.Name,
                                         file.Length
                                         );
                    //enumerate all parts
                    byte[] buffer = new byte[maxSumSize];
                    BinaryReader br = new BinaryReader(s);
                    int pos = 0;

                    //For the infohash
                    this.gameHash.Add(string.Format("{0}\0{1}\0", file.Name, file.Length));

                    do
                    {
                        int read = br.Read(buffer, 0, (int)maxSumSize);
                        pos += read;
                        if (read == 0)
                            break;
                        string hash = Convert.ToBase64String(summer.ComputeHash(buffer));

                        //Write to file
                        for (int i = 0; i <= depth; i++) infoxml.Append('\t');
                        infoxml.AppendFormat("<part length=\"{0}\" sha1sum=\"{1}\" />\n",
                                             read,
                                             hash
                                             );
                        //Write progress to console
                        this.progress += read;
                        this.ReportProgress();

                        //For the infohash
                        this.gameHash.Add(hash);
                    }
                    while (true);
                    br.Close();

                    for (int i = 0; i < depth; i++) infoxml.Append('\t');
                    infoxml.AppendFormat("</file>\n");

                    //For the infohash
                    this.gameHash.Add("\n");
                }
            }

            foreach (DirectoryInfo child in dir.GetDirectories())
            {
                for (int i = 0; i < depth; i++) infoxml.Append('\t');
                infoxml.AppendFormat("<folder name=\"{0}\">\n", child.Name);
                ProcessDirectory(child, depth + 1, ref infoxml);
                for (int i = 0; i < depth; i++) infoxml.Append('\t');
                infoxml.Append("</folder>\n");
            }
        }

        protected void ReportProgress()
        {
            int newProgress = (int)(100 * this.progress / this.size);

            if (this.progressPercentage != newProgress)
            {
                this.progressPercentage = newProgress;
                this.ReportProgress(this.progressPercentage, (object)this.message);
            }
        }

        protected static Int64 getDirectorySize(DirectoryInfo dir)
        {
            Int64 size = 0;
            foreach (DirectoryInfo child in dir.GetDirectories())
            {
                size += getDirectorySize(child);
            }
            foreach (FileInfo file in dir.GetFiles())
            {
                size += file.Length;
            }

            return size;
        }
    }
}
