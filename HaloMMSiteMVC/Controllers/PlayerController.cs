using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HaloMMSiteMVC.Models; 

namespace HaloMMSiteMVC.Controllers
{
    public class PlayerController : Controller
    {
        // GET: Player
        //calls when url is site/player/player
        /*a view is an ActionResult, but ViewResult is more specific*/
        DataAccess db = new DataAccess();
        public ActionResult Player()
        {
            
            return View();
            //another type of action result -- return Content("sup bich");
        }

    

        public ActionResult SearchResults(string gt1, string gt2)
        {
            List<int> intGameIDs1 = new List<int>();
            List<int> intGameIDs2 = new List<int>();
            List<int> matchedIDs = new List<int>();
            //Grab GTs from textboxes
            //Check DB to see if players exist

            if (db.IsInDB(gt1)) //if they do exist
            {

                intGameIDs1 = (db.ImportGamesFromDB(gt1, intGameIDs1));

            }
            else
            {
                
            }
            Player playerOne = new Player(gt1, intGameIDs1);
            //create player object, run the bungie scraper
            if (db.IsInDB(gt2))
            {
                intGameIDs2 = (db.ImportGamesFromDB(gt2, intGameIDs2));

            }
            else
            {

            }

            Player playerTwo = new Player(gt2, intGameIDs2);
            //create player object, run the bungie scraper


            //if they do, pull IDs from DB with DatAccess class
            //if not, run player.populategamelist
            //Once both Player objects are populated, do list.intersect in controller to find matched games

            //run get game details
            matchedIDs = (intGameIDs1.Intersect(intGameIDs2)).ToList();
            playerOne.GetMatchedGameDetails(matchedIDs);
            return View(playerOne);
        }
    }
}