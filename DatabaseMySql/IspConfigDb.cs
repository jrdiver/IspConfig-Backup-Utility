using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;
using sftpObjects;

namespace DatabaseMySql
{
    public class IspConfigDb
    {
        public string HostName;
        public string UserName;
        public string Password;
        public int Port;
        public string DatabaseName = "dbispconfig";
        internal MySqlConnection Connection;

        public Exception Ex;
        public bool HasError;
        public string ErrorLocation;


        public IspConfigDb(string hostName, string userName, string password, int port = 3306)
        {
            HostName = hostName;
            UserName = userName;
            Password = password;
            Port = port;

            Initialize();
        }

        private void Initialize()
        {
            string connectionString = "SERVER=" + HostName + ";" + "DATABASE=" + DatabaseName + ";" + "UID=" + UserName + ";" + "PASSWORD='" + Password + "'; SslMode=None; Port=" + Port + ";";

            try
            {
                Connection = new MySqlConnection(connectionString);
            }
            catch (Exception ex)
            {
                Ex = ex;
                HasError = true;
                ErrorLocation = "Database - Initialization";
            }
        }
        private bool OpenConnection()
        {
            if (Connection.State == ConnectionState.Open) return true;
            try
            {
                Connection.Open();
                return true;
            }
            catch (Exception ex)
            {
                Ex = ex;
                HasError = true;
                ErrorLocation = "Database - Opening Connection";
                return false;
            }
        }

        private bool CloseConnection()
        {
            try
            {
                Connection.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                Ex = ex;
                HasError = true;
                ErrorLocation = "Database - Closing Connection";
                return false;
            }
        }

        private DataTable BasicQuery(string query)
        {
            HasError = false;
            Ex = null;
            DataTable dt = new();
            try
            {
                //Open connection
                if (OpenConnection())
                {
                    //Create Command
                    MySqlCommand cmd = new(query, Connection);
                    //Create a data reader and Execute the command
                    dt = new DataTable();
                    dt.Load(cmd.ExecuteReader());

                    //close Connection
                    CloseConnection();

                }

            }
            catch (Exception ex)
            {
                Ex = ex;
                HasError = true;
                ErrorLocation = "Database - Running the Query: " + query;
                CloseConnection();
            }

            return dt;
        }

        public List<SortDefinition> GetWebDefinitions()
        {
            DataTable results = BasicQuery("SELECT domain, system_user, filename, backup_format, tstamp, filesize, web_backup.parent_domain_id, backup_type " +
                                           "FROM dbispconfig.web_domain " +
                                           "join web_backup on web_domain.domain_id = web_backup.parent_domain_id " +
                                           "where type like 'vhost' order by domain, filename");
            List<SortDefinition> definitions = new();

            foreach (DataRow row in results.Rows)
            {
                //Check if the site exits in the definitions
                if (!definitions.Exists(x => x.DomainName == row["domain"].ToString()))
                {
                    SortDefinition definition = new()
                    {
                        DomainName = row["domain"].ToString(), 
                        User = row["system_user"].ToString()
                    };
                    definitions.Add(definition);
                }
                
                SortDefinition currentDefinition = definitions.FirstOrDefault(x => x.DomainName == row["domain"].ToString());

                int.TryParse(row["filesize"].ToString(), out int fileSize);
                long.TryParse(row["tstamp"].ToString(), out long seconds);

                SortFileDefinition file = new()
                {
                    SourceFileName = row["filename"].ToString(),
                    BackupType = row["backup_type"].ToString(),
                    Format = row["backup_format"].ToString(),
                    ReportedSize = fileSize
                };
                file.LoadDateFromEpoch(seconds);


                currentDefinition?.Files.Add(file);
            }

            return definitions;
        }
    }
}
