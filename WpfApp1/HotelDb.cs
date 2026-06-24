using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace HotelManagementApp
{
    internal static class HotelDb
    {
        private static readonly string ConnectionString =
            ConfigurationManager.ConnectionStrings["HotelDb"]?.ConnectionString ??
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=hotel;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

        public static string GetConnectionString()
        {
            return ConnectionString;
        }

        public static DataTable ExecuteSelect(string sql, params SqlParameter[] parameters)
        {
            var table = new DataTable();
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                if (parameters != null && parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }

                using (var adapter = new SqlDataAdapter(command))
                {
                    adapter.Fill(table);
                }
            }

            return table;
        }

        public static int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
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
