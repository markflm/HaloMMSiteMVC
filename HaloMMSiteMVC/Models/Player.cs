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

        

        public void PopulateGameIDList(string GT, bool customsFlag)
        {
            int numofGames;
            string fullhtml;
            int sigStartGameCount;
            int sigEndGameCount;
            int sigStartGameID;
            int sigEndGameID;
            int sigMidGameID = 0;
            int gameID;
            string matchHistoryP2;

            WebClient bungie = new WebClient(); //accesses bungie.net

            if (customsFlag)
                matchHistoryP2 = "&cus=1&ctl00_mainContent_bnetpgl_recentgamesChangePage="; //the URL for customs
            else
                matchHistoryP2 = "&ctl00_mainContent_bnetpgl_recentgamesChangePage="; //URL for MM games


            string matchHistoryP1 = "http://halo.bungie.net/stats/playerstatshalo3.aspx?player="; //first part of match history page string
             //2nd part of match history page string. concatted to current page


            fullhtml = bungie.DownloadString(matchHistoryP1 + GT + matchHistoryP2 + 1); //first page of GT1s game history
            sigStartGameCount = fullhtml.IndexOf("&nbsp;<strong>"); //index of first char in HTML line that gives you total MM games

            sigEndGameCount = fullhtml.IndexOf("</strong>", sigStartGameCount); //index of next char after final digit of total MM games
            //fist char + length of that substring as start index, length of characters in number of MM games as endingChar - startingChar - length of "Intro" substring = number of MM games as string
            numofGames = int.Parse(fullhtml.Substring(sigStartGameCount + "&nbsp;<strong>".Length, (sigEndGameCount - sigStartGameCount - "&nbsp;<strong>".Length)));


            int historyPage = 1;
            int gamesThisPage = 25; //25 games on a full page
            fullhtml = bungie.DownloadString(matchHistoryP1 + GT + matchHistoryP2 + historyPage); //point webclient to first page of GT1s match history
            for (int i = 0; i < numofGames; i++)
            {
                sigStartGameID = fullhtml.IndexOf("GameStatsHalo3", sigMidGameID); //find gameID
                sigEndGameID = fullhtml.IndexOf("&amp;player", sigMidGameID);
                //MessageBox.Show(fullhtml.Substring(sigStartGameID + "GameStatsHalo3.aspx?gameid=".Length, sigEndGameID - "GameStatsHalo3.aspx?gameid=".Length - sigStartGameID));
                try
                {
                    int.TryParse(fullhtml.Substring(sigStartGameID + "GameStatsHalo3.aspx?gameid=".Length, sigEndGameID - "GameStatsHalo3.aspx?gameid=".Length - sigStartGameID), out gameID);
                }
                catch
                {
                    if (i <= numofGames * .95) //if the parse fails and <= 95% of the games haven't been collected, it's probably a corrupted bungie page, so:
                    {
                        bungie.Dispose();

                        historyPage++;
                        fullhtml = bungie.DownloadString(matchHistoryP1 + GT + matchHistoryP2 + historyPage); //iterate to next gamehistory page
                        gamesThisPage = 25;
                        sigMidGameID = 0;

                        numofGames = numofGames - gamesThisPage; //25 games aren't acccessible because the page is dead. take them off the total
                        continue; //next page
                    }
                    else //if numofGames > 95%, probably at the end of the game list. exit.
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
                try
                {


                    sigStartPos = fullhtml.IndexOf("\"first styled\">") + ("\"first styled\">").Length; //index of first substring in HTML line that gives you gametype

                    sigEndPos = fullhtml.IndexOf(" on ", sigStartPos); //index of next char after gametype

                    gameType = fullhtml.Substring(sigStartPos, sigEndPos - sigStartPos); //works


                    //get map

                    //sigEndPos + 4 for " on "
                    map = fullhtml.Substring(sigEndPos + 4, fullhtml.IndexOf("</li>", sigEndPos) - (sigEndPos + 4)); //works



                    //get playlist
                    sigStartPos = fullhtml.IndexOf("Playlist - ", sigEndPos) + ("Playlist - ").Length;

                    sigEndPos = fullhtml.IndexOf("&nbsp;</li>", sigStartPos);

                    playlist = fullhtml.Substring(sigStartPos, sigEndPos - sigStartPos); //works


                    //get dateText
                    sigStartPos = fullhtml.IndexOf("<li>", sigEndPos + ("&nbsp;</ li >").Length) + ("<li>").Length;
                    sigEndPos = fullhtml.IndexOf(",", sigStartPos);

                    dateText = fullhtml.Substring(sigStartPos, sigEndPos - sigStartPos);

                }

                catch //gameID wasn't found, dispose the webClient and go to next item in list
                {
                    bungie.Dispose();
                    continue;

                }

                GameList.Add(new Game(id, dateText, map, gameType, playlist));

                bungie.Dispose();
                sigStartPos = 0;
                sigEndPos = 0;
                gameURL = "";
            }

        }

    }

    //if the url was [...]/movies/random need a MoviesController with an action named Random
}