using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Diagnostics;

namespace HaloMMSiteMVC.Models
{
    public class Player
    {
        public Player(string name) //constructor if player is not in DB 
        {
            Name = name;
            
        }

        
        public string Name { get; set; }
        public List<int> GameIDs { get; set; }

        public List<Game> GameList { get; set; }

        

        public void PopulateGameIDList(string GT, List<int> GameIDList, bool customsFlag)
        {
            int numofGames;
            string fullhtml;
            int sigStartGameCount;
            int sigEndGameCount;
            int sigStartGameID;
            int sigEndGameID;
            int sigMidGameID = 0;
            int gameID;

            WebClient bungie = new WebClient(); //accesses bungie.net
            

            string matchHistoryP1 = "http://halo.bungie.net/stats/playerstatshalo3.aspx?player="; //first part of match history page string
            string matchHistoryP2 = "&ctl00_mainContent_bnetpgl_recentgamesChangePage="; //2nd part of match history page string. concatted to current page


            fullhtml = bungie.DownloadString(matchHistoryP1 + GT + matchHistoryP2 + 1); //first page of GT1s game history
            sigStartGameCount = fullhtml.IndexOf("&nbsp;<strong>",48000); //index of first char in HTML line that gives you total MM games --49242 is first char location on first page (varies)

            sigEndGameCount = fullhtml.IndexOf("</strong>", sigStartGameCount); //index of next char after final digit of total MM games --49260
            //fist char + length of that substring as start index, length of characters in number of MM games as endingChar - startingChar - length of "Intro" substring = number of MM games as string
            numofGames = int.Parse(fullhtml.Substring(sigStartGameCount + "&nbsp;<strong>".Length, (sigEndGameCount - sigStartGameCount - "&nbsp;<strong>".Length)));


            int historyPage = 1;
            int gamesThisPage = 25; //25 games on a full page
            fullhtml = bungie.DownloadString(matchHistoryP1 + GT + matchHistoryP2 + historyPage); //point webclient to first page of GT1s match history
            
            for (int i = 0; i < numofGames; i++)
            {
                

                sigStartGameID = fullhtml.IndexOf("GameStatsHalo3", sigMidGameID); //find gameID -- 55183 is first char location (varies)
                sigEndGameID = fullhtml.IndexOf("&amp;player", sigMidGameID);
                //MessageBox.Show(fullhtml.Substring(sigStartGameID + "GameStatsHalo3.aspx?gameid=".Length, sigEndGameID - "GameStatsHalo3.aspx?gameid=".Length - sigStartGameID));
                try
                {
                    int.TryParse(fullhtml.Substring(sigStartGameID + "GameStatsHalo3.aspx?gameid=".Length, sigEndGameID - "GameStatsHalo3.aspx?gameid=".Length - sigStartGameID), out gameID);
                }
                catch
                {
                    break;
                }


                sigMidGameID = sigEndGameID + 1;
               
                GameIDs.Add(gameID);
                gamesThisPage--; //increment count of games left on page
                if (gamesThisPage == 0) //once gamesThisPage == 0 we've got all the gameIDs from this page
                {
                    bungie.Dispose();

                    historyPage++;
                    fullhtml = bungie.DownloadString(matchHistoryP1 + GT + matchHistoryP2 + historyPage); //iterate to next gamehistory page
                    gamesThisPage = 25;
                    sigMidGameID = 0;
                   
                }

            }

            
            bungie.Dispose(); //releases webclient

        }


        public void GetMatchedGameDetails(List<int> matchedGamesList)
        {
            WebClient bungie = new WebClient(); //accesses bungie.net
            string fullhtml; //stores the html from the game page for searching
            int sigStartPos; //beginning of desired substring
            int sigEndPos; //end of desired substring
            
            
            string gameType, map, playlist, dateText;

            GameList = new List<Game>();
            foreach (int id in matchedGamesList)
            {
                string gameURL = "https://halo.bungie.net/Stats/GameStatsHalo3.aspx?gameid=" + id.ToString();
                
                fullhtml = bungie.DownloadString(gameURL); //url of a matched game
                //get gametype
                sigStartPos = fullhtml.IndexOf("\"first styled\">") + ("\"first styled\">").Length; //index of first substring in HTML line that gives you gametype

                sigEndPos = fullhtml.IndexOf(" on ", sigStartPos); //index of next char after gametype

                gameType = fullhtml.Substring(sigStartPos, sigEndPos - sigStartPos); //works


                //get map

                //sigEndPos + 4 for " on "
                map = fullhtml.Substring(sigEndPos + 4,fullhtml.IndexOf("</li>",sigEndPos) - (sigEndPos + 4)); //works

                

                //get playlist
                sigStartPos = fullhtml.IndexOf("Playlist - ", sigEndPos) + ("Playlist - ").Length;

                sigEndPos = fullhtml.IndexOf("&nbsp;</li>", sigStartPos);

                playlist = fullhtml.Substring(sigStartPos, sigEndPos - sigStartPos); //works

                
                //get dateText
                sigStartPos = fullhtml.IndexOf("<li>", sigEndPos + ("&nbsp;</ li >").Length) + ("<li>").Length;
                sigEndPos = fullhtml.IndexOf(",", sigStartPos);

                dateText = fullhtml.Substring(sigStartPos, sigEndPos - sigStartPos);

               

                GameList.Add(new Game(id, dateText, map, gameType, playlist));

                sigStartPos = 0;
                sigEndPos = 0;
                gameURL = "";
            }

        }

    }

    //if the url was [...]/movies/random need a MoviesController with an action named Random
}