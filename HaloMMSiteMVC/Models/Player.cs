using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;

namespace HaloMMSiteMVC.Models
{
    public class Player 
    {

        public Player(string name) //constructor if player is not in DB 
        {
            Name = name;
            GameIDs = new List<int>();
            GameList = new List<Game>();
            GamesFromDB = new List<Game>();
        }

        
        public string Name { get; set; }
        public List<int> GameIDs { get; set; }

        public List<Game> GameList { get; set; }

        public List<Game> GamesFromDB { get; set; } //stupid workaround for GameList changing variables dynamically after I assign them before the prop value changes

        public string EmblemURL
        {
            get; set;
        }

        public string EnemyEmblemURL
        {
            get; set;
        }

        public string EnemyName { get; set; } //to display playerTwo's GT on search results
        
        public bool CheckIfGTExists(string GT)
        {
            string matchHistoryP1 = "http://halo.bungie.net/stats/playerstatshalo3.aspx?player="; //first part of match history page string
            string matchHistoryP2 = "&ctl00_mainContent_bnetpgl_recentgamesChangePage="; //URL for MM games
            string wholeurl = matchHistoryP1 + GT + matchHistoryP2 + 1;
            WebClient bungie = new WebClient(); //accesses bungie.net
            string fullhtml;
            fullhtml = bungie.DownloadString(wholeurl);
            //bungie.Dispose();
            if (fullhtml.IndexOf("No Games Played") != -1) //if "no games played" exists then GT doesn't exist, if "no games played" doesn't exist indexof returns -1           
                return false;
            else
                return true;

        }
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
            //bungie.download
            string fullhtml; //stores the html from the game page for searching
            int sigStartPos; //beginning of desired substring
            int sigEndPos; //end of desired substring
            
            
            string gameType, map, playlist, dateText;
            string gameURL = "https://halo.bungie.net/Stats/GameStatsHalo3.aspx?gameid=";
           
            foreach (int id in matchedGamesList)
            {
                gameURL = "https://halo.bungie.net/Stats/GameStatsHalo3.aspx?gameid=" + id.ToString();

                fullhtml = bungie.DownloadString(gameURL); //url of a matched game

                //bungie.DownloadStringAsync(new Uri(gameURL));

                //bungie.DownloadStringCompleted += Bungie_DownloadStringCompleted; //this actually needed to call the event handler


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

        private void Bungie_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            string fullhtml; //stores the html from the game page for searching
            int sigStartPos; //beginning of desired substring
            int sigEndPos; //end of desired substring

            //List<Task<string>> to store HTML?
            string gameType, map, playlist, dateText;
            string gameURL = "https://halo.bungie.net/Stats/GameStatsHalo3.aspx?gameid=";
        }

        public async Task GetMatchedGameDetailsAsync(List<int> matchedGamesList)
        {
            HttpClient bungie = new HttpClient();

            
            int sigStartPos; //beginning of desired substring
            int sigEndPos; //end of desired substring
            string gameType, map, playlist, dateText;
            GameList = new List<Game>();
            List<string> matchedURLs = new List<string>();
            foreach (int id in matchedGamesList)
            {
                matchedURLs.Add("https://halo.bungie.net/Stats/GameStatsHalo3.aspx?gameid=" + id.ToString());
            }

            List<Task<string>> matchedPages = new List<Task<string>>();

            foreach (string url in matchedURLs)
            {
                matchedPages.Add(Task.Run(() => bungie.GetStringAsync(url)));

               
                

            }
            
            
            var results = await Task.WhenAll(matchedPages);
            //bungie.Dispose();

            int i = 0;
            foreach(string fullhtml in results)
            {
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

                GameList.Add(new Game(matchedGamesList[i], dateText, map, gameType, playlist));
                i++;


                bungie.Dispose();
                sigStartPos = 0;
                sigEndPos = 0;
                //gameURL = "";
            }
        }

        public void GetPlayerEmblem(string enemyPlayer)
        {
            
            WebClient bungie = new WebClient();

            string playerProfile = "http://halo.bungie.net/stats/playerstatshalo3.aspx?player=";
            string emblemURLLeadUp = "identityStrip_EmblemCtrl_imgEmblem\" src=\"/";
            
            string fullhtml;
            string emblemURL;
            fullhtml = bungie.DownloadString(playerProfile + this.Name);
            
            emblemURL = fullhtml.Substring(fullhtml.IndexOf(emblemURLLeadUp) + emblemURLLeadUp.Length, //start substring at shortest unique lead of characters before image + length of lead
                //length of substring = index of first space after emblem url,
                //minus index of start of url, minus 1 because url ends with "
                (fullhtml.IndexOf(" ", fullhtml.IndexOf(emblemURLLeadUp) + emblemURLLeadUp.Length)) - (fullhtml.IndexOf(emblemURLLeadUp) + emblemURLLeadUp.Length) - 1);

            this.EmblemURL = emblemURL.Replace("&amp;", "&");

            fullhtml = bungie.DownloadString(playerProfile + enemyPlayer);

            emblemURL = fullhtml.Substring(fullhtml.IndexOf(emblemURLLeadUp) + emblemURLLeadUp.Length, //start substring at shortest unique lead of characters before image + length of lead
                                                                                                       //length of substring = index of first space after emblem url,
                                                                                                       //minus index of start of url, minus 1 because url ends with "
                (fullhtml.IndexOf(" ", fullhtml.IndexOf(emblemURLLeadUp) + emblemURLLeadUp.Length)) - (fullhtml.IndexOf(emblemURLLeadUp) + emblemURLLeadUp.Length) - 1);

            this.EnemyEmblemURL = emblemURL.Replace("&amp;", "&");
        }
    }

    //if the url was [...]/movies/random need a MoviesController with an action named Random
}