using System;
using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;

namespace HotelManagementApp
{
    internal static class HotelDb
    {
        private static readonly string ConnectionString =
            ConfigurationManager.ConnectionStrings["HotelDb"]?.ConnectionString ??
            "server=localhost;database=hotel_management;uid=root;pwd=;SslMode=none;Allow User Variables=True;";

        public static DataTable ExecuteSelect(string sql, params MySqlParameter[] parameters)
        {
            var table = new DataTable();
            using (var connection = new MySqlConnection(ConnectionString))
            using (var command = new MySqlCommand(sql, connection))
            {
                if (parameters != null && parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }

                using (var adapter = new MySqlDataAdapter(command))
                {
                    adapter.Fill(table);
                }
            }

            return table;
        }

        public static int ExecuteNonQuery(string sql, params MySqlParameter[] parameters)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            using (var command = new MySqlCommand(sql, connection))
            {
                if (parameters != null && parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }

                connection.Open();
                return command.ExecuteNonQuery();
            }
        }
    }
}
