using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeKeeper
{
    public static class SessionMapper
    {

        private const int SESSION_ID = 0;
        private const int CREATED = 1;
        private const int FINISHED = 2;
        private const int USER_ID = 3;

        private const int UPDATE_OFFSET = 2;


        private static Session MapToSession(object[] Entity, bool hasUser = false)
        {
            return new Session(
                (Guid)Entity[SESSION_ID],
                (DateTimeOffset)Entity[CREATED],
                (DateTimeOffset)Entity[FINISHED],
                hasUser ? (Guid)Entity[USER_ID] : Guid.Empty);
        }

        public static Session CreateSession()
        {
            DateTimeOffset Created = DateTimeOffset.Now;
            Guid SessionID = SessionIdentityMap.CreateSession(Created);

            Session result = new Session(SessionID, Created);

            return result;
        }

        public static Session FindSession(Session CurrentSession)
        {
            return FindSession(CurrentSession.SessionID);

        }

        public static Session FindSession(Guid SessionID)
        {
            Session results = MapToSession(SessionIdentityMap.LoadSession(SessionID), true);

            results.Times = TimeEntryMapper.FindTimeEntriesForSession(results);

            return results;

        }

        public static List<Session> FindAllSessions()
        {
            List<object[]> sessions = SessionIdentityMap.LoadAllSessions();
            List<Session> results = new List<Session>();

            foreach (object[] session in sessions)
            {
                results.Add(MapToSession(session));
            }
            return results;
        }

        public static bool UpdateSession(Session CurrentSession)
        {
            return SessionIdentityMap.UpdateSession(CurrentSession.SessionID, CurrentSession.Created, CurrentSession.Finished, CurrentSession.UserID);
        }

    }
}
