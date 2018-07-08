using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HaloMMSiteMVC.Models;
using System.Threading.Tasks;
using System.Data;

namespace HaloMMSiteMVC.Controllers
{
    public class HomeController : AsyncController
    {

        //TODO: Handling if GT doesn't exist on bungie
        //  Check what happens if button is clicked during a bungie scrape
        //  Handling same GT twice
        //  Mouse-hover-over-icon 
        // Gamescrape progress bar
        //Heinz Almighty's 2nd page of custom games is broken on bungie.net. no systematic work-around
        // GET: Player
        //calls when url is site/player/player
        /*a view is an ActionResult, but ViewResult is more specific*/
        DataAccess db = new DataAccess();

        public ActionResult Player()
        {

            return View();

            //another type of action result -- return Content("sup bich");
        }


        public async Task<ActionResult> SearchResults(string gt1, string gt2, bool mmCheckBox, bool cusCheckBox)
        {
            List<int> intGameIDs1 = new List<int>();
            List<int> intGameIDs2 = new List<int>();
            List<int> matchedIDs = new List<int>();
            List<int> gamesFromDB = new List<int>();
            List<int> gamesToFetch = new List<int>();
            List<Game> fullGamesFromDB = new List<Game>();
            List<Game> fullGamesToDB = new List<Game>();
            DataTable playerInsertTable = new DataTable();
            DataTable detailInsertTable = new DataTable();
            DataTable detailRetrievalTable = new DataTable();



            Player playerOne = new Player((gt1.Trim()).ToUpper()); //trim any leading/trailing spaces since these would produce valid URLs
            Player playerTwo = new Player((gt2.Trim()).ToUpper());  //toUpper so string comparison below works


            if (playerOne.Name == playerTwo.Name)
            {
                ViewBag.Error = "Please enter two different Gamertags";
                playerOne.Name = null;
                return View(playerOne);

            }
            else if (!(playerOne.CheckIfGTExists(playerOne.Name)))
            {
                ViewBag.Error = playerOne.Name + " has no Bungie.net profile. Make sure this is the correct spelling";
                playerOne.Name = null;
                return View(playerOne);
            }
            else if (!(playerTwo.CheckIfGTExists(playerTwo.Name)))
            {
                ViewBag.Error = playerTwo.Name + " has no Bungie.net profile. Make sure this is the correct spelling";
                playerOne.Name = null;
                return View(playerOne);
            }


            //testing

            //await playerOne.PopulateGameIDListAsync(playerOne.Name, false);

            //testing
            //Grab GTs from textboxes
            //Check DB to see if players exist
            if (mmCheckBox && cusCheckBox)
            {
                if (db.IsInDBMM(playerOne.Name) && db.IsInDBCustom(playerOne.Name)) //if both boxes checked and both types of games in DB, import to GameIDs
                {
                    intGameIDs1 = (db.ImportGamesFromDB(playerOne.Name, intGameIDs1, mmCheckBox, cusCheckBox));
                    playerOne.GameIDs = intGameIDs1;
                    playerOne.IsInDBCustoms = true;
                    playerOne.IsInDBMM = true; //for deciding which, if any, gameIDs need to be moved 

                }
                else if (db.IsInDBMM(playerOne.Name)) //if both types of games not in DB, check for one type and import
                {   //has MM games in DB, but not customs

                    playerInsertTable = await playerOne.PopulateGameIDListAsync(playerOne.Name, true); //grab customs from bungie

                    db.InsertPlayerDataTable(playerInsertTable); //store customs in GameID table

                    db.AddGameDetailsDataTable(playerOne.GameDetailsTable); //inserts newly scraped custom games into GameDetailsTemp landing table

                    playerOne.GameIDs.Clear(); //clear player's gameIDs so we don't have duplicates when we import games from DB
                    intGameIDs1 = (db.ImportGamesFromDB(playerOne.Name, intGameIDs1, mmCheckBox, cusCheckBox));

                    playerOne.GameIDs = intGameIDs1;


                }
                else if (db.IsInDBCustom(playerOne.Name)) //check for the other type, import
                {
                    //has custom games in DB, but not MM

                    playerInsertTable = await playerOne.PopulateGameIDListAsync(playerOne.Name, false); //grab MM games from bungie

                    db.InsertPlayerDataTable(playerInsertTable); //store MM games in GameID table

                    db.AddGameDetailsDataTable(playerOne.GameDetailsTable); //inserts newly scraped MM  games into GameDetailsTemp landing table

                    playerOne.GameIDs.Clear(); //clear player's gameIDs so we don't have duplicates when we import games from DB
                    intGameIDs1 = (db.ImportGamesFromDB(playerOne.Name, intGameIDs1, mmCheckBox, cusCheckBox));

                    playerOne.GameIDs = intGameIDs1;
                }
                else
                {
                    //run populate method for both
                    playerInsertTable = await playerOne.PopulateGameIDListAsync(playerOne.Name, false);
                    playerInsertTable.Merge(await playerOne.PopulateGameIDListAsync(playerOne.Name, true)); //attempts to merge custom and mm datatable into one 


                    db.InsertPlayerDataTable(playerInsertTable);

                    db.AddGameDetailsDataTable(playerOne.GameDetailsTable);

                    //after this runs should have gameIDs and GameDetails in the player object, don't need to select anything from DB yet.
                }

            }   //player 2 stuff

            if (mmCheckBox && cusCheckBox)
            {
                if (db.IsInDBMM(playerTwo.Name) && db.IsInDBCustom(playerTwo.Name)) //if both boxes checked and both types of games in DB, import to GameIDs
                {
                    intGameIDs2 = (db.ImportGamesFromDB(playerTwo.Name, intGameIDs2, mmCheckBox, cusCheckBox));
                    playerTwo.GameIDs = intGameIDs2;

                }
                else if (db.IsInDBMM(playerTwo.Name)) //if both types of games not in DB, check for one type and import
                {   //has MM games in DB, but not customs

                    playerInsertTable = await playerTwo.PopulateGameIDListAsync(playerTwo.Name, true); //grab customs from bungie

                    db.InsertPlayerDataTable(playerInsertTable); //store customs in GameID table

                    db.AddGameDetailsDataTable(playerTwo.GameDetailsTable); //inserts newly scraped custom games into GameDetailsTemp landing table

                    playerTwo.GameIDs.Clear(); //clear player's gameIDs so we don't have duplicates when we import games from DB
                    intGameIDs2 = (db.ImportGamesFromDB(playerTwo.Name, intGameIDs2, mmCheckBox, cusCheckBox));

                    playerTwo.GameIDs = intGameIDs2;


                }
                else if (db.IsInDBCustom(playerTwo.Name)) //check for the other type, import
                {
                    //has custom games in DB, but not MM

                    playerInsertTable = await playerTwo.PopulateGameIDListAsync(playerTwo.Name, false); //grab MM games from bungie

                    db.InsertPlayerDataTable(playerInsertTable); //store MM games in GameID table

                    db.AddGameDetailsDataTable(playerTwo.GameDetailsTable); //inserts newly scraped MM  games into GameDetailsTemp landing table

                    playerTwo.GameIDs.Clear(); //clear player's gameIDs so we don't have duplicates when we import games from DB
                    intGameIDs2 = (db.ImportGamesFromDB(playerTwo.Name, intGameIDs2, mmCheckBox, cusCheckBox));

                    playerTwo.GameIDs = intGameIDs2;
                }
                else
                {
                    //run populate method for both
                    playerInsertTable = await playerTwo.PopulateGameIDListAsync(playerTwo.Name, false);
                    playerInsertTable.Merge(await playerTwo.PopulateGameIDListAsync(playerTwo.Name, true)); //attempts to merge custom and mm datatable into one 


                    db.InsertPlayerDataTable(playerInsertTable);

                    db.AddGameDetailsDataTable(playerTwo.GameDetailsTable);

                    //after this runs should have gameIDs and GameDetails in the player object, don't need to select anything from DB yet.
                }
            }

            else if (mmCheckBox) //if both boxes aren't checked, see if MM is checked
            {
                if (db.IsInDBMM(playerOne.Name)) //if in DB, import
                {
                    intGameIDs1 = (db.ImportGamesFromDB(playerOne.Name, intGameIDs1, mmCheckBox, cusCheckBox));
                    playerOne.GameIDs = intGameIDs1;
                }
                else
                {
                    //run populate method for MM

                    playerInsertTable = await playerOne.PopulateGameIDListAsync(playerOne.Name, false);
                    db.InsertPlayerDataTable(playerInsertTable);
                    db.AddGameDetailsDataTable(playerOne.GameDetailsTable);



                }

                //player 2 stuff

                if (db.IsInDBMM(playerTwo.Name)) //if in DB, import
                {
                    intGameIDs2 = (db.ImportGamesFromDB(playerTwo.Name, intGameIDs2, mmCheckBox, cusCheckBox));
                    playerTwo.GameIDs = intGameIDs2;
                }
                else
                {
                    //run populate method for MM

                    playerInsertTable = await playerTwo.PopulateGameIDListAsync(playerTwo.Name, false);
                    db.InsertPlayerDataTable(playerInsertTable);
                    db.AddGameDetailsDataTable(playerTwo.GameDetailsTable);



                }
            }
            else if (cusCheckBox) //if both boxes aren't checked AND MM isn't checked, see if custom is
            {
                if (db.IsInDBCustom(playerOne.Name)) //if in DB, import
                {
                    intGameIDs1 = (db.ImportGamesFromDB(playerOne.Name, intGameIDs1, mmCheckBox, cusCheckBox));
                    playerOne.GameIDs = intGameIDs1;
                }
                else
                {
                    //run populate method for custom
                    playerInsertTable = await playerOne.PopulateGameIDListAsync(playerOne.Name, true);
                    db.InsertPlayerDataTable(playerInsertTable);
                    db.AddGameDetailsDataTable(playerOne.GameDetailsTable);
                }

                //player 2 stuff
                if (db.IsInDBCustom(playerTwo.Name)) //if in DB, import
                {
                    intGameIDs2 = (db.ImportGamesFromDB(playerTwo.Name, intGameIDs2, mmCheckBox, cusCheckBox));
                    playerTwo.GameIDs = intGameIDs2;
                }
                else
                {
                    //run populate method for custom
                    playerInsertTable = await playerTwo.PopulateGameIDListAsync(playerTwo.Name, true);
                    db.InsertPlayerDataTable(playerInsertTable);
                    db.AddGameDetailsDataTable(playerTwo.GameDetailsTable);
                }


            }

            else
            {
                ViewBag.Error = "Select Matchmaking games, Custom games or both to begin your search";
                playerOne.Name = null;
                return View(playerOne);
            }



            //Once both Player objects are populated, do list.intersect in controller to find matched games
            matchedIDs = (playerOne.GameIDs.Intersect(playerTwo.GameIDs)).ToList(); //matched GameIDs

            if (matchedIDs.Count > 0)
            {
                //moves games into permanent GameDetails page and deletes those games from GameDetailsTemp
                db.RunGameDetailMigrateProc(playerOne, playerTwo); //running proc first then selecting only from permanenet table might fix duplicates?

                detailRetrievalTable = db.ImportGameDetails(matchedIDs); //import from DB
                foreach (DataRow dr in detailRetrievalTable.Rows)
                {
                    //add games imported from permanent table to GameList
                    playerOne.GameList.Add(new Game(int.Parse(dr["GameID"].ToString()), dr["GameDate"].ToString(), dr["Map"].ToString(), dr["GameType"].ToString(), dr["Playlist"].ToString()));
                    //gamesFromDB.Add(int.Parse(dr["GameID"].ToString())); //for the following .Except
                }

                //gamesToFetch = (matchedIDs.Except(gamesFromDB)).ToList(); //isolate matched IDs that weren't returned from DB



                //think this is causing problems with duplicates when players who have more than 2.1k games together search for the first time.
                //if (gamesToFetch.Count > 0)
                //{
                //    detailRetrievalTable = db.ImportGameDetailsTEMP(playerOne, gamesToFetch);

                //    foreach (DataRow dr in detailRetrievalTable.Rows)
                //    {
                //        //add games improted from temp table to GameList
                //        playerOne.GameList.Add(new Game(int.Parse(dr["GameID"].ToString()), dr["GameDate"].ToString(), dr["Map"].ToString(), dr["GameType"].ToString(), dr["Playlist"].ToString()));

                //    }
                //}






                playerOne.GameList.Sort((x, y) => DateTime.Compare(DateTime.Parse(x.Date), DateTime.Parse(y.Date))); //orders GameList by date of game ascending
                playerOne.GameList.Reverse(); //reverses the list (descending)

                foreach (Game g in playerOne.GameList)
                {
                    g.Date = g.Date.Substring(0, g.Date.IndexOf(" ")); //remove timestamp (hh:mm:ss) from game date, but keeps games in true chronological order

                }

                ViewBag.Error = "";
                playerOne.EnemyName = playerTwo.Name;
                playerOne.GetPlayerEmblem(playerTwo.Name);

                return View(playerOne);
            }

            else
            {
                //moves games into permanent GameDetails page and deletes those games from GameDetailsTemp
                db.RunGameDetailMigrateProc(playerOne, playerTwo);
                ViewBag.Error = playerOne.Name + " and " + playerTwo.Name + " have no games together on Bungie.net.";
                playerOne.Name = null;

                return View(playerOne);
            }
           

        }
    }
}
 