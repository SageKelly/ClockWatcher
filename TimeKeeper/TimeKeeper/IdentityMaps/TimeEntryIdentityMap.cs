using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeKeeper
{
    public static class TimeEntryIdentityMap
    {
        private static Dictionary<Guid, object[]> LoadedTimeEntries;
        private static Dictionary<Guid, bool> FullyLoadedTimeEntries;

        static TimeEntryIdentityMap()
        {
            LoadedTimeEntries = new Dictionary<Guid, object[]>();
            FullyLoadedTimeEntries = new Dictionary<Guid, bool>();
        }


        public static Guid CreateTimeEntry(DateTimeOffset Created, Guid SessionID)
        {
            Guid result = Gateway.CreateTimeEntry(Created, DateTimeOffset.Now, string.Empty, SessionID);
            LoadedTimeEntries.Add(result, new object[] { result, Created, SessionID });
            FullyLoadedTimeEntries.Add(result, true);
            return result;
        }

        public static List<object[]> LoadSessionTimeEntries(Guid SessionID)
        {
            List<object[]> results = Gateway.FindTimeEntriesForSession(SessionID);

            foreach (object[] timeEntry in results)
            {
                TimeEntry temp = TimeEntryMapper.MapToTimeEntry(timeEntry);
                if (!LoadedTimeEntries.Keys.Contains(temp.EntryID))
                {
                    LoadedTimeEntries.Add(temp.EntryID, timeEntry);
                    FullyLoadedTimeEntries.Add(temp.EntryID, true);
                }
            }

            return results;
        }



        public static object[] LoadTimeEntry(Guid EntryID)
        {
            if (!LoadedTimeEntries.Keys.Contains(EntryID))
            {
                object[] temp = Gateway.FindTimeEntry(EntryID);
                if (temp != null)
                {
                    LoadedTimeEntries.Add(EntryID, temp);
                }

            }
            return LoadedTimeEntries[EntryID];
        }

        public static bool UpdateTimeEntry(Guid EntryID, DateTimeOffset Created, DateTimeOffset Finished, string Comment, Guid SessionID)
        {
            object[] results = Gateway.UpdateTimeEntry(EntryID, Created, Finished, Comment, SessionID)[0];
            if (LoadedTimeEntries.Keys.Contains(EntryID))
            {
                LoadedTimeEntries[EntryID] = results;
                return true;
            }
            return false;
        }

        public static bool DeleteTimeEntry(Guid EntryID)
        {
            Guid Entry = Gateway.DeleteTimeEntry(EntryID);

            LoadedTimeEntries.Remove(EntryID);
            FullyLoadedTimeEntries.Remove(EntryID);

            return Entry != Guid.Empty;
        }

    }
}
