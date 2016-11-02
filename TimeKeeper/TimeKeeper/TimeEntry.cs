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
        /// Denotes how much time was spent
        /// </summary>
        public TimeSpan timeSpent;

        /// <summary>
        /// The comment for the time spent
        /// </summary>
        public StringBuilder comment;

        /// <summary>
        /// Represents the date this TimeEntry was made
        /// </summary>
        public DateTime started;

        /// <summary>
        /// Represents the date at which this thi TimeEntry was ended
        /// </summary>
        public DateTime ended;

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
            comment = new StringBuilder(20);
            combine = false;
            marked = true;
        }

        /// <summary>
        /// Creates a TimeEntry object
        /// </summary>
        /// <param name="timeSpent">How much time was spent during time span</param>
        public TimeEntry(DateTime startingTime)
            : this()
        {
            started = startingTime;
        }
    }
}
