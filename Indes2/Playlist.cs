using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indes2
{
    class Playlist
    {
        private List<string> VideoNames = new List<string>();
        private int nextIndex;
        private int nextIndexLive;

        public Playlist()
        {
            NextIndex = 0;
            NextIndexLive = 0;
        }
        public void AddVideo(String name)
        {
            this.VideoNames.Add(name);
        }
        public void DelVideo(String name)
        {
            this.VideoNames.RemoveAt(GetIndex(name));
        }

        public List<string> GetVideoList()
        {
            return this.VideoNames;
        }

        public int Count()
        {
            return VideoNames.Count;
        }
        public int GetIndex(String name)
        {
            return VideoNames.FindIndex(a => a == name);
        }
        public bool CheckIfPlaylistDone()
        {
            if(NextIndex >= Count())
            {
                return true;
            }
            return false;
        }
        public bool CheckIfPlaylistNotNull()
        {
            if (Count() == 0)
            {
                return false;
            }
            return true;
        }

        public bool CheckIfPlaylistLiveDone()
        {
            if (NextIndexLive >= Count())
            {
                return true;
            }
            return false;
        }


        public int NextIndex { get => nextIndex; set => nextIndex = value; }
        public int NextIndexLive { get => nextIndexLive; set => nextIndexLive = value; }
    }
}
