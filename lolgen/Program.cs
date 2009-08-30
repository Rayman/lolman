using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;

namespace LanOfLegends.lolgen
{
    class MainClass
    {
        public static void Main()
        {
            Console.Write("Enter the dir you want to scan: ");
            string dir = Console.ReadLine();
            Console.Write("Enter the name you want to give: ");
            string name = Console.ReadLine();

            try
            {
                DirectorySummer summer = new DirectorySummer(dir, name);
                summer.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.WriteLine("Done!");
            Console.ReadLine();
        }
    }

    class DirectorySummer
    {
        Int64 size;
        Int64 progress = 0;
        double progressPercentage = 0;
        DirectoryInfo info;
        //2^24 = 16777216 = 16 Mb
        const Int64 maxSumSize = 16777216;

        string name; //The name of the game
        List<string> gameHash = new List<string>();

        public DirectorySummer(string dir, string name)
        {
            if (!Directory.Exists(dir))
            {
                throw new Exception("Directory doesn't exist");
            }
            else
            {
                this.info = new DirectoryInfo(dir);
                Console.Write("Getting size...");
                this.size = getDirectorySize(this.info);
                Console.WriteLine(" {0} bytes", this.size);
                this.name = name;
            }
        }

        public void Start()
        {
            StringBuilder infoxml = new StringBuilder();
            infoxml.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n");
            infoxml.Append("<install type=\"game\" version=\"1.1\">\n");
            infoxml.Append("\t<name>" + this.name + "</name>\n");
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
            double percentage = Math.Round(100.0 * this.progress / this.size, 1);
            if (percentage > this.progressPercentage)
            {
                this.progressPercentage = percentage;
                Console.WriteLine();
                Console.Write(this.progressPercentage);
                Console.Write('%');
            }
            else
            {
                Console.Write('.');
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
