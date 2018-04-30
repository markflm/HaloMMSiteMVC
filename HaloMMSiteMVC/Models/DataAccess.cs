using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Configuration;
using System.Data.Common;
using System.Web.Configuration;



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
        public bool IsInDB(string PlayerName)
        {
            using (SqlConnection conn = new SqlConnection(cs))
            using (SqlCommand command = new SqlCommand("", conn))
            {
                command.CommandText = "SELECT Player FROM GameIDs WHERE Player = (@Name)";
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

        public List<int> ImportGamesFromDB(string PlayerName, List<int> GameIDs)
        {
            using (SqlConnection conn = new SqlConnection(cs))
            using (SqlCommand command = new SqlCommand("", conn))
            {
                command.CommandText = "SELECT GameID FROM GameIDs WHERE Player = (@Name)";
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
        public void AddPlayerToDB(string PlayerName, List<int> GameIDs)
        {
            using (SqlConnection conn = new SqlConnection(cs))
            using (SqlCommand command = new SqlCommand("", conn))
            {
                conn.Open();
                foreach (int gid in GameIDs)
                {
                    command.CommandText = "INSERT INTO dbo.GameIDs (Player, GameID) " +
                            "VALUES (@Name, @GameID)";
                    command.Parameters.AddWithValue("@Name", PlayerName);
                    command.Parameters.AddWithValue("@GameID", gid);

                    command.ExecuteNonQuery();
                }
                conn.Dispose();
               
               

            }

        }
    }
   }
