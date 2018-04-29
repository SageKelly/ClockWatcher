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
#if DEBUG
        private const string CONNECTION_STRING = "Data Source = VELVEETA\\DEVELOPMENT; Initial Catalog = TimeWatcherDebug; Integrated Security = true";
#else
        private const string CONNECTION_STRING = "Data Source = VELVEETA\\DEVELOPMENT; Initial Catalog = TimeWatcher; Integrated Security = true";
#endif

        #region Session
        public static Guid CreateSession(DateTimeOffset Created, Guid UserID)
        {
            string sqlString = "Dev.p_Sessions_Create";

            Dictionary<string, object> entry = new Dictionary<string, object>()
            {
                {"CREATED",Created },
                {"USER_ID",CheckGuid(UserID) }
            };
            return (Guid)MakeRequest(sqlString, entry)?[0][0];
        }

        public static Guid CreateSession(DateTimeOffset Created)
        {
            string sqlString = "Dev.p_Sessions_Create";

            Dictionary<string, object> entry = new Dictionary<string, object>()
            {
                {"CREATED",Created },
                {"USER_ID",null }
            };
            return (Guid)MakeRequest(sqlString, entry)?[0][0];
        }

        public static List<object[]> FindSession(Guid SessionId)
        {
            string sqlString = "Dev.p_Sessions_Read";
            return MakeRequest(sqlString, new Dictionary<string, object>() { { "SESSION_ID", SessionId } });
        }

        public static List<object[]> FindAllSessions()
        {
            string sqlString = "Dev.p_Sessions_ReadAll";
            return MakeRequest(sqlString, null);
        }

        public static List<object[]> UpdateSession(Guid SessionID, DateTimeOffset Created, DateTimeOffset Finished, Guid UserID)
        {
            string sqlString = "Dev.p_Sessions_Update";

            Dictionary<string, object> entry = new Dictionary<string, object>()
            {
                {"SESSION_ID",SessionID },
                {"CREATED",Created },
                {"FINISHED",Finished },
                {"USER_ID",CheckGuid(UserID )}
            };
            return MakeRequest(sqlString, entry);
        }

        public static Guid DeleteSession(Guid SessionID)
        {
            string sqlString = "Dev.p_Sessions_Delete";
            Dictionary<string, object> entry = new Dictionary<string, object>()
            {
                {"SESSION_ID",SessionID }
            };
            List<object[]> temp = MakeRequest(sqlString, entry);

            if (temp.Count > 0)
            {
                return (Guid)temp[0][0];
            }
            else
            {
                return Guid.Empty;
            }
        }

        #endregion

        #region TimeEntry
        public static Guid CreateTimeEntry(DateTimeOffset Created, DateTimeOffset Finished, string Comment, Guid SessionID)
        {
            string sqlString = "Dev.p_TimeEntries_Create";
            Dictionary<string, object> entry = new Dictionary<string, object>()
            {
                {"CREATED",Created },
                {"FINISHED",Finished },
                {"COMMENT",Comment},
                {"SESSION_ID",SessionID}
            };
            return (Guid)MakeRequest(sqlString, entry)[0][0];
        }

        public static object[] FindTimeEntry(Guid EntryID)
        {
            string sqlString = "Dev.p_TimeEntries_Read";
            List<object[]> result = MakeRequest(sqlString, new Dictionary<string, object>() { { "ENTRY_ID", EntryID } });
            if (result.Count > 0)
            {
                return result[0];
            }
            return null;
        }

        public static List<object[]> FindAllTimeEntries()
        {
            string sqlString = "Dev.p_TimeEntries_ReadAll";
            return MakeRequest(sqlString, null);
        }

        public static List<object[]> UpdateTimeEntry(Guid EntryID, DateTimeOffset Created, DateTimeOffset Finished, string Comment, Guid SessionID)
        {
            string sqlString = "Dev.p_TimeEntries_Update";
            Dictionary<string, object> entry = new Dictionary<string, object>()
            {
                {"ENTRY_ID",EntryID },
                {"CREATED",Created },
                {"FINISHED",Finished },
                {"COMMENT",Comment },
                {"SESSION_ID",SessionID}
            };
            return MakeRequest(sqlString, entry);
        }

        public static Guid DeleteTimeEntry(Guid EntryID)
        {
            string sqlString = "Dev.p_TimeEntries_Delete";
            Dictionary<string, object> entry = new Dictionary<string, object>()
            {
                {"ENTRY_ID",EntryID }
            };
            List<object[]> temp = MakeRequest(sqlString, entry);

            if (temp.Count > 0)
            {
                return (Guid)temp[0][0];
            }
            else
            {
                return Guid.Empty;
            }
        }

        public static List<object[]> FindTimeEntriesForSession(Guid SessionID)
        {
            string sqlString = "Dev.p_GetTimeEntriesForSession";

            Dictionary<string, object> parameters = new Dictionary<string, object>()
            {
                {"SESSION_ID", SessionID }
            };

            return MakeRequest(sqlString, parameters);
        }
        #endregion

        #region User
        public static Guid CreateUser(DateTimeOffset Created, string FirstName, string LastName)
        {
            string sqlString = "Dev.p_Users_Create";
            Dictionary<string, object> entry = new Dictionary<string, object>()
            {
                {"CREATED",Created },
                {"FIRST_NAME",FirstName },
                {"LAST_NAME",LastName }
            };
            return (Guid)MakeRequest(sqlString, entry)[0][0];
        }

        public static List<object[]> FindUser(Guid UserID)
        {
            string sqlString = "Dev.p_Users_Read";
            return MakeRequest(sqlString, new Dictionary<string, object>() { { "USER_ID", UserID } });
        }

        public static List<object[]> FindAllUsers()
        {
            string sqlString = "Dev.p_Users_ReadAll";
            return MakeRequest(sqlString, null);
        }

        public static List<object[]> UpdateUser(Guid UserID, DateTimeOffset Created, string FirstName, string LastName)
        {
            string sqlString = "Dev.p_Users_Update";
            Dictionary<string, object> entry = new Dictionary<string, object>()
            {
                {"USER_ID",UserID },
                {"CREATED",Created },
                {"FIRST_NAME",FirstName },
                {"LAST_NAME",LastName }
            };
            return MakeRequest(sqlString, entry);
        }

        public static Guid DeleteUser(Guid UserID)
        {
            string sqlString = "Dev.p_Users_Delete";
            Dictionary<string, object> entry = new Dictionary<string, object>()
            {
                {"USER_ID",UserID }
            };

            List<object[]> temp = MakeRequest(sqlString, entry);

            if (temp.Count > 0)
            {
                return (Guid)temp[0][0];
            }
            else
            {
                return Guid.Empty;
            }
        }
        #endregion


        private static object CheckGuid(Guid ID)
        {
            if (ID == Guid.Empty)
            {
                return null;
            }
            else return ID;
        }

        private static List<object[]> MakeRequest(string SQLString, Dictionary<string, object> Parameters)
        {
            List<object[]> result = null;
            using (SqlConnection conn = new SqlConnection(CONNECTION_STRING))
            {
                if (!string.IsNullOrEmpty(SQLString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(SQLString, conn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    if (Parameters != null)
                    {
                        foreach (KeyValuePair<string, object> param in Parameters)
                        {
                            cmd.Parameters.Add(new SqlParameter(param.Key, param.Value));
                        }
                    }


                    SqlDataReader sdr = cmd.ExecuteReader();

                    try
                    {
                        result = new List<object[]>();
                        if (sdr.Read())
                        {
                            result.Add(new object[sdr.FieldCount]);
                            sdr.GetValues(result[result.Count - 1]);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        throw e;
                    }
                    conn.Close();
                }
            }
            return result;
        }

    }
}
