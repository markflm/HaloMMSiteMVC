﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HaloMMSiteMVC.Models;
using System.Threading.Tasks;

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



            //Grab GTs from textboxes
            //Check DB to see if players exist

            if (db.IsInDBMM(playerOne.Name) && db.IsInDBCustom(playerOne.Name))
            {
                intGameIDs1 = (db.ImportGamesFromDB(playerOne.Name, intGameIDs1, mmCheckBox, cusCheckBox));
                playerOne.GameIDs = intGameIDs1;

            }
            else if ((!(db.IsInDBMM(playerOne.Name)) && mmCheckBox) && !(cusCheckBox)) //if MM games aren't found in DB, MM box is checked and Custom Box isn't checked
            { //get MM games only
                playerOne.PopulateGameIDList(playerOne.Name, false);
                db.AddPlayerToDB(playerOne.Name, playerOne.GameIDs, false); //runs "store MM games" proc

            }
            else if ((!(db.IsInDBCustom(playerOne.Name)) && cusCheckBox) && !(mmCheckBox)) //if custom games aren't found in DB, customs box is checked and MM box isn't
            {
                playerOne.PopulateGameIDList(playerOne.Name, true); //get customs
                db.AddPlayerToDB(playerOne.Name, playerOne.GameIDs, true); //store customs

            }
            else //run bungie scraper to get games
            {
                playerOne.GameIDs = intGameIDs1;
                if (mmCheckBox && cusCheckBox) //both true, wants both types of game
                {
                    playerOne.PopulateGameIDList(playerOne.Name, false);
                    db.AddPlayerToDB(playerOne.Name, playerOne.GameIDs, false); //runs "store MM games" proc

                    playerOne.GameIDs.Clear(); //clear MM games from list

                    playerOne.PopulateGameIDList(playerOne.Name, true); //get customs
                    db.AddPlayerToDB(playerOne.Name, playerOne.GameIDs, true); //store customs

                    db.ImportGamesFromDB(playerOne.Name, playerOne.GameIDs, true, false); //get MM games and add them back to list
                }
                else if (mmCheckBox && !cusCheckBox)
                {
                    playerOne.PopulateGameIDList(playerOne.Name, false);
                    db.AddPlayerToDB(playerOne.Name, playerOne.GameIDs, false); //runs "store MM games" proc

                }
                else
                {
                    playerOne.PopulateGameIDList(playerOne.Name, true);
                    db.AddPlayerToDB(playerOne.Name, playerOne.GameIDs, true); //runs "store custom games" proc
                }
            }
           
            //create player object, run the bungie scraper
            if (db.IsInDBMM(playerTwo.Name) && db.IsInDBCustom(playerTwo.Name))
            {
                intGameIDs2 = (db.ImportGamesFromDB(playerTwo.Name, intGameIDs2, mmCheckBox, cusCheckBox));
                playerTwo.GameIDs = intGameIDs2;

            }
            else if((!(db.IsInDBMM(playerTwo.Name)) && mmCheckBox) && !(cusCheckBox)) //if MM games aren't found in DB, MM box is checked and Custom Box isn't checked
            { //get MM games only
                playerTwo.PopulateGameIDList(playerTwo.Name, false);
                db.AddPlayerToDB(playerTwo.Name, playerTwo.GameIDs, false); //runs "store MM games" proc

            }
            else if ((!(db.IsInDBCustom(playerTwo.Name)) && cusCheckBox) && !(mmCheckBox)) //if custom games aren't found in DB, customs box is checked and MM box isn't
            {
                playerTwo.PopulateGameIDList(playerTwo.Name, true); //get customs
                db.AddPlayerToDB(playerTwo.Name, playerTwo.GameIDs, true); //store customs

            }
            else //run bungie scraper to get games
            {
                playerTwo.GameIDs = intGameIDs2;
                if (mmCheckBox && cusCheckBox) //both true, wants both types of game
                {
                    playerTwo.PopulateGameIDList(playerTwo.Name, false);
                    db.AddPlayerToDB(playerTwo.Name, playerTwo.GameIDs, false); //runs "store MM games" proc

                    playerTwo.GameIDs.Clear(); //clear MM games from list

                    playerTwo.PopulateGameIDList(playerTwo.Name, true); //get customs
                    db.AddPlayerToDB(playerTwo.Name, playerTwo.GameIDs, true); //store customs

                    db.ImportGamesFromDB(playerTwo.Name, playerTwo.GameIDs, true, false); //get MM games and add them back to list
                }
                else if (mmCheckBox && !cusCheckBox)
                {
                    playerTwo.PopulateGameIDList(playerTwo.Name, false);
                    db.AddPlayerToDB(playerTwo.Name, playerTwo.GameIDs, false); //runs "store MM games" proc

                }
                else
                {
                    playerOne.PopulateGameIDList(playerTwo.Name, true);
                    db.AddPlayerToDB(playerTwo.Name, playerTwo.GameIDs, true); //runs "store custom games" proc
                }
            }



            //Once both Player objects are populated, do list.intersect in controller to find matched games

            //run get game details
            matchedIDs = (playerOne.GameIDs.Intersect(playerTwo.GameIDs)).ToList(); //matched GameIDs
            //import from DB
            gamesFromDB = db.ImportGameDetails(playerOne, matchedIDs);
            //isolate matched IDs that weren't returned from DB
            gamesToFetch = matchedIDs.Except(gamesFromDB).ToList();
            //grab those from bungie
            fullGamesFromDB = playerOne.GamesFromDB;
            if (gamesToFetch.Count > 0) //if > 0 there are matched Games not in DB
            {
                //these are the games present in playerOne's GameList before the bungie fetch
                                                      // i.e. games that don't need to be added to the DB

                playerOne.GetMatchedGameDetails(gamesToFetch); //adds remaining games to playerOne's GameList

                fullGamesToDB = playerOne.GameList.Except(fullGamesFromDB).ToList(); //isolates fetched games for addition to DB

                //add those to DB
                db.AddGameDetails(fullGamesToDB);
            }
            //else: (all matched games were in DB -- they're already added to playerOne.GameList from ImportGameDetails
            
            

            //await playerOne.GetMatchedGameDetailsAsync(matchedIDs);



            playerOne.GameList.Sort((x, y) => DateTime.Compare(DateTime.Parse(x.Date), DateTime.Parse(y.Date))); //orders GameList by date of game ascending
            playerOne.GameList.Reverse(); //reverses the list (descending)

            ViewBag.Error = "";
            playerOne.EnemyName = playerTwo.Name;
            playerOne.GetPlayerEmblem(playerTwo.Name);
            return View(playerOne);
        }

        public async Task BungieAccessAsync(List<int> matchedIDs)
        {

        }
  

        public ActionResult SearchInfo()
        {

            return View();
        }
    }
}