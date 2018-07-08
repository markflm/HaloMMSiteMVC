using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Configuration;
using System.Data.Common;
using System.Web.Configuration;
using System.Data.SqlTypes;
using System.Data;



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

        public void AddGameIDToDB(string PlayerName, int GameID, int TaskID, bool IsCustom, DateTime insertTime)
        {
            using (SqlConnection conn = new SqlConnection(cs))
            using (SqlCommand command = new SqlCommand("", conn))
            {
                conn.Open();
                command.Parameters.AddWithValue("@Name", "pholder");
                command.Parameters.AddWithValue("@GameID", 123);
                command.Parameters.AddWithValue("@TaskID", 123);
                command.Parameters.AddWithValue("@Time", "time");
                if (IsCustom)
                    command.Parameters.AddWithValue("@IsCustom", 1);
                else
                    command.Parameters.AddWithValue("@IsCustom", 0);

                command.CommandText = "INSERT INTO dbo.debugingAsync (Player, GameID, Iscustom, TaskID, insertTime)" +
                                       "VALUES (@Name, @GameID, @IsCustom, @TaskID, @Time)";
                command.Parameters["@Name"].Value = PlayerName;
                command.Parameters["@GameID"].Value = GameID;
                command.Parameters["@TaskID"].Value = TaskID;
                command.Parameters["@Time"].Value = insertTime.ToLongTimeString();
                command.ExecuteNonQuery();

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


        public void AddGameDetailsDataTable(DataTable dataTable)
        {
            using (var conn = new SqlConnection(cs))
            {
                conn.Open();

                var transaction = conn.BeginTransaction(); //make the insert a transaction, so all rows copy or none do
                using (var sqlBulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepNulls, transaction))
                {
                    

                    sqlBulk.DestinationTableName = "GameDetailsTemp";
                    sqlBulk.WriteToServer(dataTable);

                }
                transaction.Commit();
                conn.Dispose();
            }


        }

        //pull in matched game details from database
        public DataTable ImportGameDetailsTEMP(Player playerOne, List<int> matchedIDs) //eventually need to upgrade to storing each game only once
                                                                                       //and not requiring the player name to return records
        {
            using (SqlConnection conn = new SqlConnection(cs))
            using (SqlCommand command = new SqlCommand("", conn))
            {
                conn.Open();

                var query = "SELECT * FROM GameDetailsTemp WHERE GameID IN ({0}) AND Player = @Player";
                var gameIDParameterList = new List<string>();
                var index = 0;
                foreach (int id in matchedIDs)
                {
                    var paramName = "@idParam" + index;
                    command.Parameters.AddWithValue(paramName, id);
                    gameIDParameterList.Add(paramName);
                    index++;
                }

                command.CommandText = String.Format(query, string.Join(",", gameIDParameterList));
                command.Parameters.AddWithValue("@Player", playerOne.Name);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    var resultTable = new DataTable();
                    resultTable.Load(reader);


                    conn.Dispose();
                    return resultTable;
                }

            }
        }


        public DataTable ImportGameDetails(List<int> matchedIDs)
        {
            List<int> overflowList = new List<int>();
            List<int> secondOverflowList = new List<int>();
            List<int> thirdOverflowList = new List<int>();
            int gameCount = 0;
            DataTable resultTable = new DataTable();

            if (matchedIDs.Count > 2099) //sql can only take 2100 parameters in a select like this, if there are more than 2099 matched games, need to break up list
            {
                gameCount = matchedIDs.Count;
                while (matchedIDs.Count >= 2050)
                {
                    overflowList.Add(matchedIDs[2049]); //add gameID to overflow list

                    matchedIDs.RemoveAt(2049); //remove it from original list
                }

                if (overflowList.Count > 2099)  //if more than ~4200 matched games (only seen with this guiv shot and eli the ninja)
                {
                    while (overflowList.Count >= 2050)
                    {
                        secondOverflowList.Add(overflowList[2049]);

                        overflowList.RemoveAt(2049);
                    }

                    if (secondOverflowList.Count > 2099) //actually, they've got over 6300 games
                    {
                        while (secondOverflowList.Count >= 2050)
                        {
                            thirdOverflowList.Add(secondOverflowList[2049]);

                            secondOverflowList.RemoveAt(2049);
                        }
                    }

                }

            }
               

            using (SqlConnection conn = new SqlConnection(cs))
            using (SqlCommand command = new SqlCommand("", conn))
            {
                conn.Open();

                var query = "SELECT DISTINCT * FROM GameDetails WHERE GameID IN ({0})";
                var gameIDParameterList = new List<string>();
                var index = 0;
                foreach (int id in matchedIDs)
                {
                    var paramName = "@idParam" + index;
                    command.Parameters.AddWithValue(paramName, id);
                    gameIDParameterList.Add(paramName);
                    index++;
                }

                command.CommandText = String.Format(query, string.Join(",", gameIDParameterList));

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    
                    resultTable.Load(reader);


                  
                   
                }


                if (overflowList.Count > 0)
                {
                    gameIDParameterList.Clear();
                    index = 0;
                    command.Parameters.Clear();
                    command.CommandText = "";
                    foreach (int id in overflowList)
                    {
                        var paramName = "@idParam" + index;
                        command.Parameters.AddWithValue(paramName, id);
                        gameIDParameterList.Add(paramName);
                        index++;
                    }

                    command.CommandText = String.Format(query, string.Join(",", gameIDParameterList));

                    using (SqlDataReader reader = command.ExecuteReader())
                    {

                        resultTable.Load(reader);




                    }

                    if (secondOverflowList.Count > 0)
                    {
                        gameIDParameterList.Clear();
                        index = 0;
                        command.Parameters.Clear();
                        command.CommandText = "";
                        foreach (int id in secondOverflowList)
                        {
                            var paramName = "@idParam" + index;
                            command.Parameters.AddWithValue(paramName, id);
                            gameIDParameterList.Add(paramName);
                            index++;
                        }

                        command.CommandText = String.Format(query, string.Join(",", gameIDParameterList));

                        using (SqlDataReader reader = command.ExecuteReader())
                        {

                            resultTable.Load(reader);




                        }

                        if (thirdOverflowList.Count > 0)
                        {
                            gameIDParameterList.Clear();
                            index = 0;
                            command.Parameters.Clear();
                            command.CommandText = "";
                            foreach (int id in thirdOverflowList)
                            {
                                var paramName = "@idParam" + index;
                                command.Parameters.AddWithValue(paramName, id);
                                gameIDParameterList.Add(paramName);
                                index++;
                            }

                            command.CommandText = String.Format(query, string.Join(",", gameIDParameterList));

                            using (SqlDataReader reader = command.ExecuteReader())
                            {

                                resultTable.Load(reader);




                            }
                        }

                    }
                }

                

            }

            return resultTable;
           
            
        }

        public void InsertPlayerDataTable(DataTable dataTable)
        {
            using (var conn= new SqlConnection(cs))
            {
                conn.Open();

                var transaction = conn.BeginTransaction(); //make the insert a transaction, so all rows copy or none do
                using (var sqlBulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepNulls, transaction)) //enables "check permanent table for gameIDs, then insert new ones and truncate temp table"
                {
                    
                    sqlBulk.DestinationTableName = "GameIDs";
                    sqlBulk.WriteToServer(dataTable);


                }
                transaction.Commit();
                conn.Dispose();

            }
           
          
        }


        //inserts new games from GameDetailsTemp into GameDetails and deletes them from GameDetailsTemp
        public void RunGameDetailMigrateProc(Player playerOne, Player playerTwo)
        {
            using (SqlConnection conn = new SqlConnection(cs))
            {
                using (SqlCommand cmd = new SqlCommand("uspMigrateGameDetails", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Player1", playerOne.Name);
                    cmd.Parameters.AddWithValue("@Player2", playerTwo.Name);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                }


            }


        }
        public void SelectSuggestedPlayers(bool prosOnly)
        {
            //select 15 random names from GameIDs

        }
    }
}