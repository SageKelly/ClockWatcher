using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeKeeper
{
    public static class SessionIdentityMap
    {
        private static Dictionary<Guid, object[]> LoadedSessions;
        private static Dictionary<Guid, bool> FullyLoadedSessions;

        static SessionIdentityMap()
        {
            LoadedSessions = new Dictionary<Guid, object[]>();
            FullyLoadedSessions = new Dictionary<Guid, bool>();
        }

        public static Guid CreateSession(DateTimeOffset Created)
        {
            Guid SessionID = Gateway.CreateSession(Created);
            LoadedSessions.Add(SessionID, new object[] { Created });
            FullyLoadedSessions.Add(SessionID, true);
            return SessionID;
        }

        public static object[] LoadSession(Guid SessionID)
        {
            if (!LoadedSessions.Keys.Contains(SessionID))
            {
                LoadedSessions.Add(SessionID, Gateway.FindSession(SessionID)[0]);
                FullyLoadedSessions.Add(SessionID, true);
            }
            return LoadedSessions[SessionID];
        }

        public static List<object[]> LoadAllSessions()
        {
            List<object[]> results = Gateway.FindAllSessions();

            foreach (object[] session in results)
            {
                Guid SessionID;
                if (Guid.TryParse(session[0].ToString(), out SessionID) && !LoadedSessions.Keys.Contains((Guid)session[0]))
                {

                    LoadedSessions.Add(SessionID, session);
                    if (FullyLoadedSessions.Keys.Contains(SessionID))
                    {
                        FullyLoadedSessions[SessionID] = true;
                    }
                    else
                    {
                        FullyLoadedSessions.Add(SessionID, true);
                    }
                }
                else
                {
                    throw new Exception("Loaded Session had invalid SessionID");
                }
            }
            return results;
        }

        public static bool UpdateSession(Guid SessionID, DateTimeOffset Created, DateTimeOffset Finished, Guid UserID)
        {
            object[] results = Gateway.UpdateSession(SessionID, Created, Finished, UserID)[0];

            LoadedSessions[SessionID] = results;

            return results != null;
        }

    }
}
