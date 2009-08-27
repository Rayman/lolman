using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;

namespace lolmanager2
{
    struct GameQueueItem
    {
        public string infoHash;
        public string name { get; set; }
        public Int64 progress;
        public Int64 size;
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
        public string status { get; set; }
        public string local { get; set; }
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

    class GameQueueManager
    {
        string fileName;

        internal GameQueueManager(string fileName)
        {
            this.fileName = fileName;

            if (!File.Exists(fileName))
                File.Create(fileName);
        }

        internal void AddGame(string infoHash)
        {
            //Check for duplicates
            foreach (string s in this.GetQueue())
                if (s == infoHash)
                    return;

            //Append to the list
            File.AppendAllText(this.fileName, infoHash + '\n');
        }

        internal string[] GetQueue()
        {
            List<string> games = new List<string>();
            bool duplicates = false;

            foreach (string s in File.ReadAllLines(this.fileName))
            {
                if (games.Contains(s))
                    duplicates = true;
                else
                    games.Add(s);
            }

            if (duplicates)
                File.WriteAllLines(this.fileName, games.ToArray());

            return games.ToArray();
        }

        internal void RemoveGame(string hash)
        {
            List<string> servers = new List<string>();
            foreach (string s in File.ReadAllLines(this.fileName))
            {
                if (!servers.Contains(s))
                    if (hash != s)
                        servers.Add(s);
            }

            File.WriteAllLines(this.fileName, servers.ToArray());
        }

        internal IEnumerable<LolGame> GetQueue(IEnumerable<LolGame> gameList)
        {
            foreach (string infoHash in this.GetQueue())
            {
                //Search for infohash
                IEnumerable<LolGame> game = from s in gameList
                                            where s.infohash == infoHash
                                            select s;
                if (game.Count<LolGame>() != 1)
                {
                    LolGame item = new LolGame();
                    item.name = "Error, multiple selections";
                    yield return item;
                }
                else
                {
                    yield return game.First<LolGame>();
                }
            }
        }
    }
}
