using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ClockWatcher
{
    [Serializable]
    public class TimeEntryData : INotifyPropertyChanged, ISerializable
    {
        private static string defaultComment = "Type Comment Here";
        
        private TimeSpan time_spent;
        private int entry_ID;
        private List<string> sub_comments;
        private string comment;
        private DateTime time_in;
        private DateTime time_out;

        public event PropertyChangedEventHandler PropertyChanged;
        
        #region Properties
        public string Comment
        {
            get
            {
                return comment;
            }
            set
            {
                if (comment != value)
                {
                    comment = value;
                    OnPropertyChanged("Comment");
                }
            }
        }
        public TimeEntry Owner { get; set; }
        /// <summary>
        /// Represents when this TimeEntry was created
        /// </summary>
        public DateTime TimeIn
        {
            get
            {
                return time_in;
            }
            set
            {
                if (time_in != value)
                {
                    time_in = value;
                    OnPropertyChanged("TimeIn");
                }
            }
        }
        /// <summary>
        /// Represents when this TimeEntry was completed
        /// </summary>
        public DateTime TimeOut
        {
            get
            {
                return time_out;
            }
            set
            {
                if (time_out != value)
                {
                    string time = time_out.ToString("hh:mm tt");
                    time_out = value;
                    OnPropertyChanged("TimeOut");
                }
            }
        }
        /// <summary>
        /// Represents how much time was spent on this TimeEntry
        /// </summary>
        public TimeSpan TimeSpent
        {
            get
            {
                return time_spent;
            }
            set
            {
                if (time_spent != value)
                {
                    time_spent = value;
                    OnPropertyChanged("TimeSpent");
                }
            }
        }
        /// <summary>
        /// A per-session uniue ID for this TimeEntry
        /// </summary>
        public int EntryID
        {
            get
            {
                return entry_ID;
            }
            private set
            {
                entry_ID = value;
            }
        }
        /// <summary>
        /// Extra details about the TimeEntry
        /// </summary>
        public List<string> SubComments
        {
            get
            {
                return sub_comments;
            }
            set
            {
                if (sub_comments != value)
                {
                    sub_comments = value;
                    OnPropertyChanged("SubComments");
                }
            }
        }
        #endregion


        public TimeEntryData(TimeEntry owner, int id)
        {
            Owner = owner;
            EntryID = id;
            TimeSpent = TimeSpan.Zero;
            TimeIn = DateTime.Now;
            Comment = defaultComment;
        }

        protected TimeEntryData(SerializationInfo info, StreamingContext context)
        {
            time_spent = (TimeSpan)info.GetValue("timeSpent", typeof(TimeSpan));
            entry_ID = (int)info.GetValue("entry_ID", typeof(int));
            sub_comments = (List<string>)info.GetValue("sub_comments", typeof(List<string>));
            comment = (string)info.GetValue("comment", typeof(string));
            time_in = (DateTime)info.GetValue("time_in", typeof(DateTime));
            time_out = (DateTime)info.GetValue("time_out", typeof(DateTime));
        }


        private void OnPropertyChanged([CallerMemberName] string property_name = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property_name));
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("timeSpent", time_spent, typeof(TimeSpan));
            info.AddValue("entry_ID", entry_ID);
            info.AddValue("sub_comments", sub_comments, typeof(List<string>));
            info.AddValue("comment", comment);
            info.AddValue("time_in", time_in);
            info.AddValue("time_out", time_out);
        }
    }
}
