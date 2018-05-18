using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HaloMMSiteMVC.Models; 

namespace HaloMMSiteMVC.Controllers
{
    public class HomeController : Controller
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


        public ActionResult SearchResults(string gt1, string gt2, bool mmCheckBox, bool cusCheckBox)
        {
            List<int> intGameIDs1 = new List<int>();
            List<int> intGameIDs2 = new List<int>();
            List<int> matchedIDs = new List<int>();

            
            Player playerOne = new Player(gt1);
            Player playerTwo = new Player(gt2);
            //Grab GTs from textboxes
            //Check DB to see if players exist

            if (db.IsInDB(gt1)) //if they do exist
            {

                intGameIDs1 = (db.ImportGamesFromDB(gt1, intGameIDs1, mmCheckBox, cusCheckBox));
                playerOne.GameIDs = intGameIDs1;
               

            }
            else //run bungie scraper to get games
            {
                playerOne.GameIDs = intGameIDs1;
                if (mmCheckBox && cusCheckBox) //both true, wants both types of game
                {
                    playerOne.PopulateGameIDList(gt1, false);
                    db.AddPlayerToDB(gt1, playerOne.GameIDs, false); //runs "store MM games" proc

                    playerOne.GameIDs.Clear(); //clear MM games from list

                    playerOne.PopulateGameIDList(gt1, true); //get customs
                    db.AddPlayerToDB(gt1, playerOne.GameIDs, true); //store customs

                    db.ImportGamesFromDB(gt1, playerOne.GameIDs, true, false); //get MM games and add them back to list
                }
                else if (mmCheckBox && !cusCheckBox)
                {
                    playerOne.PopulateGameIDList(gt1, false);
                    db.AddPlayerToDB(gt1, playerOne.GameIDs, false); //runs "store MM games" proc

                }
                else
                {
                    playerOne.PopulateGameIDList(gt1, true);
                    db.AddPlayerToDB(gt1, playerOne.GameIDs, true); //runs "store custom games" proc
                }
            }
           
            //create player object, run the bungie scraper
            if (db.IsInDB(gt2))
            {
                intGameIDs2 = (db.ImportGamesFromDB(gt2, intGameIDs2, mmCheckBox, cusCheckBox));
                playerTwo.GameIDs = intGameIDs2;

            }
            else //run bungie scraper to get games
            {
                playerTwo.GameIDs = intGameIDs2;
                if (mmCheckBox && cusCheckBox) //both true, wants both types of game
                {
                    playerTwo.PopulateGameIDList(gt2, false);
                    db.AddPlayerToDB(gt2, playerTwo.GameIDs, false); //runs "store MM games" proc

                    playerTwo.GameIDs.Clear(); //clear MM games from list

                    playerTwo.PopulateGameIDList(gt2, true); //get customs
                    db.AddPlayerToDB(gt2, playerTwo.GameIDs, true); //store customs

                    db.ImportGamesFromDB(gt2, playerTwo.GameIDs, true, false); //get MM games and add them back to list
                }
                else if (mmCheckBox && !cusCheckBox)
                {
                    playerTwo.PopulateGameIDList(gt2, false);
                    db.AddPlayerToDB(gt2, playerTwo.GameIDs, false); //runs "store MM games" proc

                }
                else
                {
                    playerOne.PopulateGameIDList(gt2, true);
                    db.AddPlayerToDB(gt2, playerTwo.GameIDs, true); //runs "store custom games" proc
                }
            }



            //Once both Player objects are populated, do list.intersect in controller to find matched games

            //run get game details
            matchedIDs = (playerOne.GameIDs.Intersect(playerTwo.GameIDs)).ToList();
            playerOne.GetMatchedGameDetails(matchedIDs);


            playerOne.GameList.Sort((x, y) => DateTime.Compare(DateTime.Parse(x.Date), DateTime.Parse(y.Date))); //orders GameList by date of game ascending
            playerOne.GameList.Reverse(); //reverses the list (descending)

            return View(playerOne);
        }

       
    }
}