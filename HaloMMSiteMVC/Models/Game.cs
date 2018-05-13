using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HaloMMSiteMVC.Models
{
    public class Game
    {
        public Game(int gameID, string date, string map, string gametype, string playlist)
        {

            GameID = gameID;
            Date = date;
            Map = map;
            Gametype = gametype;
            Playlist = playlist;



        }
        public int GameID { get;  }
        public string Date { get;  }

        public string Map { get;}

        public string Playlist { get; set; }

        public string Gametype { get; }

        public bool IsCustom { get; }
    }
}