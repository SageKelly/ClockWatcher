using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeKeeper
{
    public static class Gateway
    {
        private const string CONNECTION_STRING = "Data Source = VELVEETA/DEVELOPMENT; Initial Catalog = TimeWatcher; Integrated Security = true";

        public static object[] FindSession(Guid SessionId)
        {
            object[] result = null;
            using (SqlCommand cmd = new SqlCommand("Dev.Sessions_Read", new SqlConnection(CONNECTION_STRING)))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                SqlDataReader sdr = cmd.ExecuteReader();

                try
                {
                    if (sdr.Read())
                    {
                        result = new object[sdr.FieldCount];
                        sdr.GetValues(result);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw e;
                }
            }
            return result;
        }

        public static object[] FindTimeEntry(Guid EntryId)
        {
            object[] result = null;
            using (SqlConnection conn = new SqlConnection(CONNECTION_STRING))
            {
                SqlCommand cmd = new SqlCommand("Dev.TimeEntries_Read", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();

                SqlDataReader sdr = cmd.ExecuteReader();

                try
                {
                    if (sdr.Read())
                    {
                        result = new object[sdr.FieldCount];
                        sdr.GetValues(result);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw e;
                }
                finally
                {
                    conn.Close();
                }
            }
            return result;
        }

        public static object[] FindUser(Guid UserId)
        {
            object[] result = null;
            using (SqlConnection conn = new SqlConnection(CONNECTION_STRING))
            {
                SqlCommand cmd = new SqlCommand("Dev.Users_Read", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();

                SqlDataReader sdr = cmd.ExecuteReader();

                try
                {
                    if (sdr.Read())
                    {
                        result = new object[sdr.FieldCount];
                        sdr.GetValues(result);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw e;
                }
                finally
                {
                    conn.Close();
                }
            }
            return result;
        }

    }
}
