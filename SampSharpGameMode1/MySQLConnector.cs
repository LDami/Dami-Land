using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

using MySql.Data;
using MySql.Data.MySqlClient;

namespace SampSharpGameMode1
{
    public class MySQLConnector
    {
        private static MySQLConnector _instance = null;

        public MySQLConnector()
        {

        }
        public static MySQLConnector Instance()
        {
            if (_instance == null)
                _instance = new MySQLConnector();
            return _instance;
        }

        private MySqlConnection mySqlConnection = null;

        private MySqlDataReader reader = null;
        private int readRows;
        private int rowsAffected;
        public int RowsAffected { get => rowsAffected; private set => rowsAffected = value; }

        /// <summary>
        ///     Connects to the server configured in ConfigurationManager
        /// </summary>
        /// <returns>true if connection succeed, else false</returns>
        public Boolean Connect()
        {
            if (mySqlConnection == null)
            {
                string connstring = string.Format(
                    "Server=" + ConfigurationManager.AppSettings["mysql_host"] + 
                    "; Port=" + ConfigurationManager.AppSettings["mysql_port"] + 
                    "; database=" + ConfigurationManager.AppSettings["mysql_db"] + 
                    "; UID=" + ConfigurationManager.AppSettings["mysql_user"] + 
                    "; password=" + ConfigurationManager.AppSettings["mysql_pass"], 
                    ConfigurationManager.AppSettings["mysql_db"]);

                try
                {
                    mySqlConnection = new MySqlConnection(connstring);
                    mySqlConnection.Open();
                    Logger.WriteLineAndClose("MySQLConnector.cs - MySQLConnector.Connect:I: Connected to the database");
                    return true;
                }
                catch (MySqlException e)
                {
                    Logger.WriteLineAndClose("MySQLConnector.cs - MySQLConnector.Connect:E: Unable to connect to the database: " + e.Code);
                    mySqlConnection = null;
                    return false;
                }
            }
            else return false;
        }

        public void Close()
        {
            mySqlConnection.Close();
        }

        public string GetState()
        {
            return mySqlConnection.State.ToString();
        }

        /// <summary>
        ///     Executes a SQL query
        /// </summary>
        /// <param name="query">The query string to execute</param>
        /// <param name="parameters">Dictionary(string, object) of parameters</param>
        /// <returns>Last inserted id</returns>
        public long Execute(string query, Dictionary<string, object> parameters)
        {
            if (!query.StartsWith("SELECT"))
            {
                rowsAffected = 0;
                /*
                Console.WriteLine("MySQLConnector.cs - MySQLConnector.Execute:I: Executing query: {0}", query);
                Console.WriteLine("With params:");
                foreach (KeyValuePair<string, object> kvp in parameters)
                {
                    Console.WriteLine("\t- {0} = {1}", kvp.Key, kvp.Value);
                }
                */
                var cmd = new MySqlCommand(query, mySqlConnection);
                foreach (KeyValuePair<string, object> kvp in parameters)
                    cmd.Parameters.AddWithValue(kvp.Key, kvp.Value);

                rowsAffected = cmd.ExecuteNonQuery();
                return cmd.LastInsertedId;
            }
            else
                return -1;
        }

        /// <summary>
        ///     Opens a SQL recordset
        /// </summary>
        /// <param name="query">The query string to execute</param>
        /// <param name="parameters">Dictionary(string, object) of parameters</param>
        public void OpenReader(string query, Dictionary<string, object> parameters)
        {
            if (query.StartsWith("SELECT"))
            {
                readRows = 0;
                /*
                Console.WriteLine("MySQLConnector.cs - MySQLConnector.OpenReader:I: Fetching query: {0}", query);
                Console.WriteLine("With params:");
                foreach (KeyValuePair<string, object> kvp in parameters)
                {
                    if (query.Substring(query.IndexOf(kvp.Key) - 5, 5).Contains("LIKE"))
                        Console.WriteLine("\t- {0} = %{1}%", kvp.Key, kvp.Value);
                    else
                        Console.WriteLine("\t- {0} = {1}", kvp.Key, kvp.Value);
                }
                */
                var cmd = new MySqlCommand(query, mySqlConnection);
                foreach (KeyValuePair<string, object> kvp in parameters)
                {
                    if(query.Substring(query.IndexOf(kvp.Key) - 5, 5).Contains("LIKE"))
                        cmd.Parameters.AddWithValue(kvp.Key, "%" + kvp.Value + "%");
                    else
                        cmd.Parameters.AddWithValue(kvp.Key, kvp.Value);
                }

                reader = cmd.ExecuteReader();
                //Console.WriteLine("MySQLConnector.cs - MySQLConnector.OpenReader:I: Reader has rows: {0}", reader.HasRows);
            }
        }

        /// <summary>
        ///     Closes the last opened recordset
        /// </summary>
        public void CloseReader()
        {
            //Console.WriteLine("MySQLConnector.cs - MySQLConnector.CloseReader:I: Read rows: {0}", readRows);
            if (!reader.IsClosed)
                reader.Close();
        }

        /// <summary>
        ///     Get the next row of the opened SQL recordset
        /// </summary>
        /// <returns>Dictionary(column-name: string, value: string) of the row</returns>
        public Dictionary<string, string> GetNextRow()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (reader != null)
            {
                if (reader.Read())
                {
                    readRows++;
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        if(reader.IsDBNull(i))
                            result.Add(reader.GetName(i), "[null]");
                        else
                            result.Add(reader.GetName(i), reader.GetString(i));
                    }
                }
            }
            return result;
        }

        public class Field
        {
            private static Dictionary<string, string> field = new Dictionary<string, string>()
            {
                /* Race */
                {"race_id", "Race ID"},
                {"race_name", "Race Name"},
                {"race_creator", "Race Creator"},
                {"race_type", "Race Type"},
                /* Derby */
                {"derby_id", "Derby ID"},
                {"derby_name", "Derby Name"},
                {"derby_creator", "Derby Creator"},
            };
            
            public static string GetFieldName(string name)
            {
                if (field.ContainsKey(name))
                    return field[name];
                else
                {
                    Console.WriteLine("MySQLConnector.cs - MySQLConnector.Field.GetFieldName:W: No field name for " + name);
                    Console.WriteLine("Returning empty string");
                    return "";
                }
            }
        }
    }
}
