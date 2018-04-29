using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeKeeper
{
    public class TimeEntryMapper
    {

        private const int ENTRY_ID = 0;
        private const int CREATED = 1;
        private const int FINISHED = 2;
        private const int COMMENT = 3;
        private const int SESSION_ID = 4;
        

        public static TimeEntry MapToTimeEntry(object[] Entity)
        {
            TimeEntry result = null;
            if (Entity != null && Entity.Length == 5)
            {
                result = new TimeEntry(
                    (Guid)Entity[ENTRY_ID],
                    (DateTimeOffset)Entity[CREATED],
                    (DateTimeOffset)Entity[FINISHED],
                    (string)Entity[COMMENT],
                    (Guid)Entity[SESSION_ID]
                    );
            }
            return result;
        }

        public static TimeEntry CreateTimeEntry(Session CurrentSession)
        {
            TimeEntry result = null;

            DateTimeOffset Created = DateTimeOffset.Now;

            Guid EntryID = TimeEntryIdentityMap.CreateTimeEntry(Created, CurrentSession.SessionID);

            result = new TimeEntry(EntryID, Created, CurrentSession.SessionID);
            return result;
        }

        public static TimeEntry FindTimeEntry(TimeEntry Entry)
        {
            return MapToTimeEntry(TimeEntryIdentityMap.LoadTimeEntry(Entry.EntryID));
        }

        public static List<TimeEntry> FindTimeEntriesForSession(Session CurrentSession)
        {
            List<object[]> timeEntries = TimeEntryIdentityMap.LoadSessionTimeEntries(CurrentSession.SessionID);

            List<TimeEntry> results = new List<TimeEntry>();

            foreach (object[] entry in timeEntries)
            {
                results.Add(MapToTimeEntry(entry));
            }

            return results;
        }

        public static bool UpdateTimeEntry(TimeEntry Entry)
        {
            return TimeEntryIdentityMap.UpdateTimeEntry(Entry.EntryID, Entry.Created, Entry.Finished, Entry.Comment.ToString(), Entry.SessionID);
        }

        public static bool DeleteTimeEntry(TimeEntry Entry)
        {
            return TimeEntryIdentityMap.DeleteTimeEntry(Entry.EntryID);
        }
    }
}
