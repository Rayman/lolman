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
        public int priority { get; set; }
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
        const string queueFileName = "gamequeue.txt";
        const string doneFileName = "gamefinished.txt";

        internal GameQueueManager()
        {
            if (!File.Exists(queueFileName))
                File.Create(queueFileName);
            if (!File.Exists(doneFileName))
                File.Create(doneFileName);
        }

        internal void AddToQueue(string infoHash, string localPath)
        {
            if (!Directory.Exists(localPath))
                throw new Exception("Directory doesn't exists");
            localPath = (new DirectoryInfo(localPath + Path.DirectorySeparatorChar)).FullName;

            foreach (string line in File.ReadAllText(queueFileName).Split('\0'))
            {
                if (line.Length == 0)
                    continue;
                string hash = line.Substring(0, line.IndexOf('\t'));
                string localName = line.Substring(hash.Length, line.Length - hash.Length);

                //Duplicate
                if (hash == infoHash)
                    return;
            }

            //No duplicates, add it
            File.AppendAllText(queueFileName, infoHash + '\t' + localPath + '\0');
        }

        internal IEnumerable<LolGame> GetQueue(IEnumerable<LolGame> gameList)
        {
            return GetGames(gameList, queueFileName);
        }

        internal IEnumerable<LolGame> GetDone(IEnumerable<LolGame> gameList)
        {
            return GetGames(gameList, doneFileName);
        }

        private IEnumerable<LolGame> GetGames(IEnumerable<LolGame> gameList, string fileName)
        {
            foreach (string line in File.ReadAllText(fileName).Split('\0'))
            {
                if (line.Length == 0)
                    continue;
                string hash = line.Substring(0, line.IndexOf('\t'));
                string localName = line.Remove(0, line.IndexOf('\t') + 1);

                //Search for infohash
                IEnumerable<LolGame> selection = from s in gameList
                                                 where s.infohash == hash
                                                 select s;
                if (selection.Count<LolGame>() != 1)
                {
                    LolGame item = new LolGame();
                    item.name = "Error, no or multiple games";
                    item.status = "Error";
                    yield return item;
                }
                else
                {
                    LolGame game = selection.First<LolGame>();
                    game.local = localName;
                    yield return game;
                }
            }
        }

        internal void MoveUp(string infoHash)
        {
            string[] lines = File.ReadAllText(queueFileName).Split('\0');
            string previous = null;
            TextWriter tw = new StreamWriter(queueFileName);

            foreach (string line in lines)
            {
                if (line.Length == 0)
                    continue;
                string hash = line.Substring(0, line.IndexOf('\t'));
                string localName = line.Remove(0, line.IndexOf('\t') + 1);

                if (hash == infoHash)
                    tw.Write(hash + '\t' + localName + '\0');
                else
                {
                    if (previous != null)
                        tw.Write(previous);
                    previous = hash + '\t' + localName + '\0';
                }
            }

            if (previous != null)
                tw.Write(previous);
            tw.Close();
        }

        internal void Remove(string infoHash)
        {
            string[] lines = File.ReadAllText(queueFileName).Split('\0');
            TextWriter tw = new StreamWriter(queueFileName);

            foreach (string line in lines)
            {
                if (line.Length == 0)
                    continue;
                string hash = line.Substring(0, line.IndexOf('\t'));
                if (hash != infoHash)
                    tw.Write(line + '\0');

            }

            tw.Close();
        }
    }
}
