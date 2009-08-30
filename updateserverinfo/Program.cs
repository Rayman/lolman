using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LanOfLegends.updateserverinfo
{
    class Program
    {
        static void Main(string[] args)
        {
            Program p = new Program();
        }

        List<string> files = new List<string>();

        public Program()
        {
            Console.WriteLine("Searching for games...");
            DirectoryInfo dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            SearchRecursive(dir, "./");

            //Write to file
            TextWriter tw = new StreamWriter("server.info.txt");

            //This is so the manager can recognise it
            tw.Write("lol.dirlist\0");

            //Add all entries
            foreach (string file in this.files)
                tw.Write(file + '\0');

            //Close the reader
            tw.Close();
            Console.WriteLine("Done!");
            Console.ReadLine();
        }

        public void SearchRecursive(DirectoryInfo dir, string path)
        {
            foreach (FileInfo file in dir.GetFiles())
            {
                if (file.Name == "lol.info.xml")
                {
                    string fileName = path + file.Name;
                    this.files.Add(fileName);
                    Console.WriteLine(fileName);
                }
            }

            foreach (DirectoryInfo child in dir.GetDirectories())
            {
                SearchRecursive(child, path + child.Name + "/");
            }
        }
    }
}
