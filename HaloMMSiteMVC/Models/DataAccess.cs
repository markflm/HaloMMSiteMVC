using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Configuration;
using System.Data.Common;
using System.Web.Configuration;
using System.Data.SqlTypes;



namespace HaloMMSiteMVC.Models
{
    public class DataAccess
    {

        public DataAccess()
        {

        }

        private string cs = WebConfigurationManager.ConnectionStrings["H3MMDBEntities"].ConnectionString;


        //pre: requires Player object to be instantiated in order to pass its name as a string
        //post: returns true if player's name (gamertag) currently exists in the database
        public bool IsInDBMM(string PlayerName)
        {
            using (SqlConnection conn = new SqlConnection(cs))
            using (SqlCommand command = new SqlCommand("", conn))
            {
                command.CommandText = "SELECT Player FROM GameIDs WHERE Player = (@Name) AND IsCustom = 0"; //check if a MM game is recorded
                command.Parameters.AddWithValue("@Name", PlayerName); //using params prevents SQL injection apparently

                conn.Open();
                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())//returns true if there's a record to be read
                        return true;
                    else
                    {
                        conn.Dispose();
                        return false;
                    }

                }

            }
        }

        public bool IsInDBCustom(string PlayerName)
        {
            using (SqlConnection conn = new SqlConnection(cs))
            using (SqlCommand command = new SqlCommand("", conn))
            {
                command.CommandText = "SELECT Player FROM GameIDs WHERE Player = (@Name) AND IsCustom = 1"; //check if a MM game is recorded
                command.Parameters.AddWithValue("@Name", PlayerName); //using params prevents SQL injection apparently

                conn.Open();
                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())//returns true if there's a record to be read
                        return true;
                    else
                    {
                        conn.Dispose();
                        return false;
                    }

                }

            }
        }

        public List<int> ImportGamesFromDB(string PlayerName, List<int> GameIDs, bool getMM, bool getCus)
        {
            using (SqlConnection conn = new SqlConnection(cs))
            using (SqlCommand command = new SqlCommand("", conn))
            {
                if (getMM && getCus) //if both MM and custom boxes are checked
                    command.CommandText = "SELECT GameID FROM GameIDs WHERE Player = (@Name)"; //query with no condition for IsCustom
                else if (getMM) //only MM box checked
                    command.CommandText = "SELECT GameID FROM GameIDs WHERE Player = (@Name) AND IsCustom = 0";
                else  //only custom box checked
                    command.CommandText = "SELECT GameID FROM GameIDs WHERE Player = (@Name) AND IsCustom = 1";

                command.Parameters.AddWithValue("@Name", PlayerName);

                int d;
                conn.Open();
                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read()) //adds all a Player's gameIDs to their (integer) GameIDs list
                    {
                        d = (int)reader["GameID"];
                        GameIDs.Add(d);
                    }
                }
                conn.Dispose();

                return GameIDs;

            }

        }

        //pre: Player not already present in SQL DB and list of GameIDs has been scraped from Bungie
        //post: Player and their gameIDs added to DB for future access 
        public void AddPlayerToDB(string PlayerName, List<int> GameIDs, bool IsCustomsList)
        {
            using (SqlConnection conn = new SqlConnection(cs))
            using (SqlCommand command = new SqlCommand("", conn))
            {
                conn.Open();
                command.Parameters.AddWithValue("@Name", "pholder");
                command.Parameters.AddWithValue("@GameID", 123);
                if (IsCustomsList)
                    command.Parameters.AddWithValue("@IsCustom", 1);
                else
                    command.Parameters.AddWithValue("@IsCustom", 0);
                foreach (int gid in GameIDs)
                {
                    command.CommandText = "INSERT INTO dbo.GameIDs (Player, GameID, IsCustom) " +
                            "VALUES (@Name, @GameID, @IsCustom)";

                    command.Parameters["@Name"].Value = PlayerName;
                    command.Parameters["@GameID"].Value = gid;


                    command.ExecuteNonQuery();
                }
                conn.Dispose();



            }

        }

        //add details of matched games to table for quick access later.
        public void AddGameDetails(List<Game> GameList)
        {
            using (SqlConnection conn = new SqlConnection(cs))
            using (SqlCommand command = new SqlCommand("", conn))
            {
                conn.Open();
                command.Parameters.AddWithValue("@Playlist", "pholder");
                command.Parameters.AddWithValue("@GameID", 123);
                command.Parameters.AddWithValue("@Map", "pholder");
                command.Parameters.AddWithValue("@Date", "pholder");
                command.Parameters.AddWithValue("@Gametype", "pholder");

                foreach (Game game in GameList)
                {
                    command.CommandText = "INSERT INTO dbo.GameDetails VALUES(@GameID, @Map, @Playlist, @Gametype, @Date)";


                    command.Parameters["@Playlist"].Value = game.Playlist;
                    command.Parameters["@GameID"].Value = game.GameID;
                    command.Parameters["@Map"].Value = game.Map;
                    command.Parameters["@Date"].Value = game.Date;
                    command.Parameters["@Gametype"].Value = game.Gametype;



                    command.ExecuteNonQuery();
                }
                conn.Dispose();



            }



        }

        //pull in matched game details from database
        public List<int> ImportGameDetails(Player playerOne, List<int> MatchedIDs)
        {
            
            List<int> pulledFromDB = new List<int>();
            using (SqlConnection conn = new SqlConnection(cs))
            using (SqlCommand command = new SqlCommand("", conn))
            {

                command.CommandText = "SELECT * from GameDetails Where GameID = @GameID";

                command.Parameters.AddWithValue("@GameID", 123);


                conn.Open();
                foreach (int gid in MatchedIDs)
                {
                    command.Parameters["@GameID"].Value = gid;


                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                           
                            Game import = new Game(gid, reader["GameDate"].ToString(), reader["Map"].ToString(), reader["Playlist"].ToString(), reader["GameType"].ToString());


                           playerOne.GameList.Add(import); //add fully detailed game object to GameList
                           playerOne.GamesFromDB.Add(import); //workaround

                           pulledFromDB.Add(gid); //add ID to list of successfully pulled gameIDs
                        }
                    }
                }
                conn.Dispose();

                return pulledFromDB;

            }
        }
    }
}