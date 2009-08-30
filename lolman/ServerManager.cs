using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LanOfLegends.lolman
{
    /// <summary>This class is used for managing the server list</summary>
    class ServerManager
    {
        /// <summary>This is the file where the servers have to be written to</summary>
        string fileName;

        /// <summary>Initialises the server manager</summary>
        /// <param name="fileName">The file to wich the servers are written to</param>
        internal ServerManager(string fileName)
        {
            this.fileName = fileName;

            if (!File.Exists(fileName))
                File.Create(fileName);
        }

        /// <summary>Add a server to the server list</summary>
        /// <param name="url">The url to the serverinfo.txt</param>
        internal void AddServer(string url)
        {
            //Check for obvious error
            if (url == "url.to.server/serverinfo.txt")
                return;

            //Check for duplicates
            foreach (string s in this.GetServers())
                if (s == url)
                    return;

            //Append to the list
            File.AppendAllText(this.fileName, url + '\n');
        }

        /// <summary>Gets all servers in the server list</summary>
        /// <returns>Array of urls to the servers</returns>
        internal string[] GetServers()
        {
            List<string> servers = new List<string>();
            bool duplicates = false;

            foreach (string s in File.ReadAllLines(this.fileName))
            {
                if (servers.Contains(s))
                    duplicates = true;
                else
                    servers.Add(s);
            }

            if (duplicates)
                File.WriteAllLines(this.fileName, servers.ToArray());

            return servers.ToArray();
        }

        /// <summary>Removes a server url from the server list</summary>
        /// <param name="url">The url to the serverinfo.txt</param>
        internal void RemoveServer(string url)
        {
            List<string> servers = new List<string>();
            foreach (string s in File.ReadAllLines(this.fileName))
            {
                if (!servers.Contains(s))
                    if (url != s)
                        servers.Add(s);
            }

            File.WriteAllLines(this.fileName, servers.ToArray());
        }
    }
}
