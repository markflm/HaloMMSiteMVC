﻿using System;
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
    }
   }
