using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeKeeper
{
    [Serializable]
    /// <summary>
    /// Holds a timespan for a working shift
    /// </summary>
    public class TimeEntry
    {
        /// <summary>
        /// The server-based ID for the TimeEntry
        /// </summary>
        public Guid EntryID
        {
            get;
            private set;
        }
        /// <summary>
        /// Denotes how much time was spent
        /// </summary>
        public TimeSpan timeSpent;

        /// <summary>
        /// The comment for the time spent
        /// </summary>
        public StringBuilder Comment;

        /// <summary>
        /// Represents the date this TimeEntry was made
        /// </summary>
        public DateTimeOffset Created;

        /// <summary>
        /// Represents the date at which this thi TimeEntry was ended
        /// </summary>
        public DateTimeOffset Finished;

        public Guid SessionID
        {
            get; set;
        }


        /// <summary>
        /// Determines whether or not this Time entry is count toward the Total Time
        /// </summary>
        public bool marked;

        /// <summary>
        /// Marker for if it must be combined with another TimeEntry
        /// </summary>
        public bool combine;

        /// <summary>
        /// Create a TimeEntry object
        /// </summary>
        private TimeEntry()
        {
            timeSpent = TimeSpan.Zero;
            Comment = new StringBuilder(20);
            combine = false;
            marked = true;
        }

        /// <summary>
        /// Creates a TimeEntry object
        /// </summary>
        /// <param name="timeSpent">How much time was spent during time span</param>
        public TimeEntry(DateTimeOffset startingTime)
            : this()
        {
            Created = startingTime;
        }

        public TimeEntry(Guid EntryID, DateTimeOffset Created, Guid SessionID)
        {
            this.EntryID = EntryID;
            this.Created = Created;
            this.Finished = DateTimeOffset.Now;
            Comment = new StringBuilder();
            this.SessionID = SessionID;
        }

        public TimeEntry(Guid EntryID, DateTimeOffset Created, DateTimeOffset Finished, string Comment, Guid SessionID)
        {
            this.EntryID = EntryID;
            this.Created = Created;
            this.Finished = Finished;
            this.Comment.Clear();
            this.Comment.Append(Comment);
            this.SessionID = SessionID;
        }

        public void Finalize(TimeSpan Elapsed)
        {
            Finished = DateTimeOffset.Now;
            timeSpent = Elapsed;
            TimeEntryMapper.UpdateTimeEntry(this);
        }
    }
}
