using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace TimeKeeper
{
    [Serializable]
    public class Session
    {
        public Guid SessionID
        {
            get;
            private set;
        }

        public DateTimeOffset Created
        {
            get;
            private set;
        }
        public DateTimeOffset Finished;

        public Guid UserID
        {
            get;
            private set;
        }

        public List<TimeEntry> Times;

        /// <summary>
        /// Represents in index of the last TimeEntry in the Session
        /// </summary>
        public int LastTimeIndex
        {
            get
            {
                return Times.Count - 1;
            }
            private set { }
        }

        /// <summary>
        /// Represents the last TimeEntry in the Session
        /// </summary>
        public TimeEntry LastTimeEntry
        {
            get
            {
                return Times[LastTimeIndex];
            }
            private set { }
        }

        public Session()
        {
            Times = new List<TimeEntry>();
        }

        public Session(Guid SessionID, DateTimeOffset Created) : this()
        {
            this.SessionID = SessionID;
            this.Created = Created;
        }


        public Session(Guid SessionID, DateTimeOffset Created, DateTimeOffset Finished, Guid UserID) : this(SessionID, Created)
        {
            this.Finished = Finished;
            this.UserID = UserID;
        }

        #region Gateway Methods
        public void CreateTimeEntry()
        {
            Times.Add(TimeEntryMapper.CreateTimeEntry(this));
        }

        public void RemoveTimeEntry(TimeEntry Entry)
        {
            TimeEntryMapper.DeleteTimeEntry(Entry);
            Times.Remove(Entry);
        }


        public void Finalize()
        {
            Finished = DateTimeOffset.Now;
            SessionMapper.UpdateSession(this);
        }
        #endregion

    }
}
