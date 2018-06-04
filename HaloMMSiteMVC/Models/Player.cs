using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;
using System.Data;

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



        public async Task<DataTable> GetMatchedGameDetails(List<int> matchedGamesList)
        {
            ServicePointManager.DefaultConnectionLimit = 12;
            ServicePointManager.Expect100Continue = false;
            //WebClient bungie = new WebClient(); //accesses bungie.net
            HttpClient bungie = new HttpClient();
            DataTable detailTable = new DataTable();

            bungie.Timeout = new TimeSpan(0, 20, 0);

            detailTable.Columns.Add("GameID");
            detailTable.Columns.Add("Map");
            detailTable.Columns.Add("Playlist");
            detailTable.Columns.Add("GameType");
            detailTable.Columns.Add("GameDate");

            int sigStartPos; //beginning of desired substring
            int sigEndPos; //end of desired substring
            int failedPages = 0; //keep track of tasks that fail to download
            int successfulPages = 0;
            string result;

            

            //set Accept headers
            //bungie.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml,application/json");
            //set User agent
            //bungie.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; EN; rv:11.0) like Gecko");
            //bungie.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Charset", "ISO-8859-1");

            string gameType, map, playlist, dateText, gidString;
            int gid;
            string gameURL = "http://halo.bungie.net/Stats/GameStatsHalo3.aspx?gameid=";

            List<Task<string>> tasks = new List<Task<string>>();

            List<string> debugTasks = new List<string>();


            //for (int i = 0; i < 400; i++)
            //{
            //    Uri siteLink = new Uri(gameURL + matchedGamesList[i]); //GT = name of player, passed to method.
            //                                          //creates url like
            //                                          //

            //    tasks.Add(bungie.GetStringAsync(siteLink));
            //    debugTasks.Add(tasks.Last().Id.ToString() + " " + siteLink.ToString());
        //}
            foreach (int id in matchedGamesList)
            {
                Uri siteLink = new Uri(gameURL + id); //GT = name of player, passed to method.
                                                      //creates url like
                                                      //

                tasks.Add(bungie.GetStringAsync(siteLink));
                debugTasks.Add(tasks.Last().Id.ToString() + " " + siteLink.ToString());
            }

            
            while (tasks.Count > 0)
            {
                var taskComplete = await Task.WhenAny(tasks);

                tasks.Remove(taskComplete);

                try
                {
                    result = taskComplete.Result;

                }
                catch
                {
                    failedPages++;
                    //Task.FromException(e);
                    taskComplete.Dispose();
                    continue; //if try fails, means task failed to download string. skip task
                }
                //tasks.Remove(taskComplete);
                successfulPages++;
                try
                {
                    //get gameID
                    sigStartPos = result.IndexOf("gameid=") + ("gameid=").Length;

                    sigEndPos = result.IndexOf("\"", sigStartPos);

                    gidString = result.Substring(sigStartPos, sigEndPos - sigStartPos);

                    gid = int.Parse(gidString); //to match up with SQL db datatype


                    //get gametype
                    sigStartPos = result.IndexOf("\"first styled\">") + ("\"first styled\">").Length; //index of first substring in HTML line that gives you gametype

                    sigEndPos = result.IndexOf(" on ", sigStartPos); //index of next char after gametype

                    gameType = result.Substring(sigStartPos, sigEndPos - sigStartPos); //works


                    //get map

                    //sigEndPos + 4 for " on "
                    map = result.Substring(sigEndPos + 4, result.IndexOf("</li>", sigEndPos) - (sigEndPos + 4)); //works



                    //get playlist
                    sigStartPos = result.IndexOf("Playlist - ", sigEndPos) + ("Playlist - ").Length;

                    sigEndPos = result.IndexOf("&nbsp;</li>", sigStartPos);

                    playlist = result.Substring(sigStartPos, sigEndPos - sigStartPos); //works


                    //get dateText
                    sigStartPos = result.IndexOf("<li>", sigEndPos + ("&nbsp;</ li >").Length) + ("<li>").Length;
                    sigEndPos = result.IndexOf(",", sigStartPos);

                    dateText = result.Substring(sigStartPos, sigEndPos - sigStartPos);

                }

                catch //gameID wasn't found, dispose the webClient and go to next item in list
                {
                    
                    continue;

                }

                GameList.Add(new Game(gid, dateText, map, gameType, playlist));

                detailTable.Rows.Add(gid, map, playlist, gameType, dateText); //add to data table for quicker storage in DB


                sigStartPos = 0;
                sigEndPos = 0;
               
            }

            return detailTable;

        }



        public async Task<DataTable> PopulateGameIDListAsync(string GT, bool customsFlag)
        {
            WebClient bungie = new WebClient();
            HttpClient IDDownloader = new HttpClient();
            DataTable dataTable = new DataTable();

            //model table after GameIDs table in SQL db
            dataTable.Columns.Add("RowID");
            dataTable.Columns.Add("Player");
            dataTable.Columns.Add("GameID");
            dataTable.Columns.Add("IsCustom");

          

            string matchHistoryP2;
            string matchHistoryP1;
            string fullhtml;
            int sigStartGameCount;
            int sigEndGameCount;
            int numofGames;
            int approxNumOfPages;
            int sigStartGameID;
            int sigEndGameID;
            int sigMidGameID;
            int sigStartGameType;
            int sigEndGameType;
            int sigStartDate;
            int sigEndDate;
            int sigStartMap;
            int sigEndMap;
            int sigStartPlaylist;
            int sigEndPlaylist;
            string taskResult;
            int gameID = 0;
            string gametype, map, playlist, date;




            //set Accept headers
            //IDDownloader.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml,application/json");
            //set User agent
            //IDDownloader.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; EN; rv:11.0) like Gecko");
            //IDDownloader.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Charset", "ISO-8859-1");

            if (customsFlag)
                matchHistoryP2 = "&cus=1&ctl00_mainContent_bnetpgl_recentgamesChangePage="; //the URL for customs
            else
                matchHistoryP2 = "&ctl00_mainContent_bnetpgl_recentgamesChangePage="; //URL for MM games


            matchHistoryP1 = "http://halo.bungie.net/stats/playerstatshalo3.aspx?player="; //first part of match history page string
                                                                                           //2nd part of match history page string. concatted to current page


            fullhtml = bungie.DownloadString(matchHistoryP1 + GT + matchHistoryP2 + 1); //first page of GT1s game history
            sigStartGameCount = fullhtml.IndexOf("&nbsp;<strong>"); //index of first char in HTML line that gives you total MM games

            sigEndGameCount = fullhtml.IndexOf("</strong>", sigStartGameCount); //index of next char after final digit of total MM games
            //fist char + length of that substring as start index, length of characters in number of MM games as endingChar - startingChar - length of "Intro" substring = number of MM games as string
            numofGames = int.Parse(fullhtml.Substring(sigStartGameCount + "&nbsp;<strong>".Length, (sigEndGameCount - sigStartGameCount - "&nbsp;<strong>".Length)));
            approxNumOfPages = (numofGames / 25) + 1; //25 games a page, +1 to make sure a page isn't missed due to integer division
            bungie.Dispose();


            List<Task<string>> tasks = new List<Task<string>>();
            

            List<string> taskIDandSiteLink = new List<string>();
            for (int i = 1; i <= approxNumOfPages; i++)
            {
                Uri siteLink = new Uri(matchHistoryP1 + GT + matchHistoryP2 + i); //GT = name of player, passed to method.
                                                                              //creates url like
                                                                              //http://halo.bungie.net/stats/playerstatshalo3.aspx?player=infury&ctl00_mainContent_bnetpgl_recentgamesChangePage=1

                tasks.Add(IDDownloader.GetStringAsync(siteLink));

                taskIDandSiteLink.Add(tasks.Last().Id + " " + siteLink.ToString()); //list of taskIDs and what page they should download
            }

            while (tasks.Count > 0)
            {

                var taskComplete = await Task.WhenAny(tasks);

                
                tasks.Remove(taskComplete); //remove task from list

                try
                {
                    taskResult = taskComplete.Result;
                }
                catch
                {
                    Debug.Print(taskComplete.Id.ToString());
                    taskComplete.Dispose();
                    taskResult = "";
                    continue;
                }

                
                sigMidGameID = 0;
                sigStartGameID = 0;
                sigEndGameID = 0;

                if (taskResult.IndexOf("No games found for this player.") != -1  || 
                    taskResult.IndexOf("It seems that you have encountered a problem with our site.") != -1)
                    continue; //if index of above IS NOT negative one, then it's a corrupted page or a customs page that doesn't exist.
                              //skip this task and await the next one


                for (int x = 0; x < 25; x++) //25 GameIDs per page
                {
                    sigStartGameID = taskResult.IndexOf("GameStatsHalo3", sigMidGameID); //find gameID
                    sigEndGameID = taskResult.IndexOf("&amp;player", sigMidGameID);

                    try
                    {
                        int.TryParse(taskResult.Substring(sigStartGameID + "GameStatsHalo3.aspx?gameid=".Length, sigEndGameID - "GameStatsHalo3.aspx?gameid=".Length - sigStartGameID), out gameID);
                        GameIDs.Add(gameID);
                        
                        //get gametype for this row
                        sigStartGameType = taskResult.IndexOf("\">", sigEndGameID);
                        sigEndGameType = taskResult.IndexOf("</", sigEndGameID);
                        gametype = taskResult.Substring(sigStartGameType + "\">".Length, sigEndGameType - "\">".Length - sigStartGameID);

                        //get date for this row
                        sigStartDate = taskResult.IndexOf("</td><td>", sigEndGameType);
                        sigEndDate = taskResult.IndexOf("M", sigStartDate);
                        date = taskResult.Substring(sigStartDate + "</td><td>".Length, sigEndDate);

                        //get map for this row

                        //get playlist for this row












                        dataTable.Rows.Add(x, GT, gameID, customsFlag);

                    }
                    catch
                    {
                        x = 0;
                        break; //if parse fails before x = 25, taskResult page didn't have a full 25 games iterate to next Task
                    }


                    sigMidGameID = sigEndGameID + 1; //increment index by 1 to find next instance of a GameID in the html
                    
                }
                
               


                //taskComplete.Dispose(); //problem exists with or without disposing the task





            }

            IDDownloader.Dispose();
            return dataTable;
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
}

    //if the url was [...]/movies/random need a MoviesController with an action named Random
