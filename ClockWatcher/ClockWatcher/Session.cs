using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ClockWatcher
{
    [Serializable]
    public class Session : INotifyPropertyChanged, ISerializable
    {
        private TimeSpan total_time;
        private string name;
        private List<TimeEntryData> time_entries;
        private List<string> comment_library;
        private DateTime creation_date;

        public event PropertyChangedEventHandler PropertyChanged;

        #region Properties
        public List<string> CommentLibrary
        {
            get
            {
                return comment_library;
            }
            set
            {
                if (comment_library != value)
                {
                    comment_library = value;
                    OnPropertyChanged("CommentLibrary");
                }
            }
        }

        public DateTime creationDate
        {
            get
            { return creation_date; }
            private set
            { creation_date = value; }
        }

        public TimeEntryData currentTEData { get; private set; }
        public List<TimeEntryData> TimeEntries
        {
            get
            { return time_entries; }
            private set
            { time_entries = value; }
        }
        public TimeSpan TotalTime
        {
            get
            {
                return total_time;
            }
            set
            {
                if (total_time != value)
                {
                    OnPropertyChanged("TotalTime");
                    total_time = value;
                }
            }
        }

        public string Name
        {
            get
            {
                return FriendlyName(name);
            }
            private set
            {
                name = value;
            }
        }
        #endregion

        public Session()
        {
            TimeEntries = new List<TimeEntryData>();
            comment_library = new List<string>();
            creationDate = DateTime.Now;
            Name = FileName(creationDate.ToString());
        }

        protected Session(SerializationInfo info, StreamingContext context)
        {
            total_time = (TimeSpan)info.GetValue("total_time", typeof(TimeSpan));
            name = (string)info.GetValue("name", typeof(string));
            time_entries = (List<TimeEntryData>)info.GetValue("time_entries", typeof(List<TimeEntryData>));
            creation_date = (DateTime)info.GetValue("creation_date", typeof(DateTime));
            comment_library = (List<string>)info.GetValue("comment_library", typeof(List<string>));
        }

        public commentEntry addComment(string text)
        {
            foreach (string s in comment_library)
            {
                if (s == text)
                    return null;
            }
            commentEntry result = new commentEntry(text);
            comment_library.Add(text);
            return result;
        }

        public void deleteComment(commentEntry ce)
        {
            /*
             * Remove the comment from all TimeEntries whose
             * comment matches this one.
            */
            foreach (TimeEntryData te in TimeEntries)
            {
                if (ce.comment == te.Comment)
                {
                    te.Comment = "";
                }
            }
            comment_library.Remove(ce.comment);
        }

        /// <summary>
        /// Makes a new TimeEntry and sets the currentTEData
        /// </summary>
        /// <param name="timeIn"></param>
        /// <returns>The newly-created TimeEntry</returns>
        public TimeEntry addEntry(DateTime timeIn)
        {
            TimeEntry result = new TimeEntry(TimeEntries.Count);
            TimeEntries.Add(result.Data);
            currentTEData = result.Data;
            currentTEData.TimeIn = timeIn;
            return result;
        }

        public void deleteTimeEntry(TimeEntry t)
        {
            TimeEntries.Remove(t.Data);
            if (TimeEntries.Count > 0)
                currentTEData = TimeEntries.Last();
            else
                currentTEData = null;
        }

        public void Save()
        {
            if (!time_entries.Last().Owner.isFinalized)
            {
                time_entries.Last().Owner.finalize();
            }
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream("Sessions/" + name, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, this);
            stream.Close();
        }

        private void OnPropertyChanged([CallerMemberName] string member_name = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(member_name));
            }
        }

        public static string FriendlyName(string text)
        {
            string[] data = text.Split(' ');
            //3 parts: date time AM/PM
            string date = data[0].Replace('_', '/');
            string time = data[1].Replace('_', ':');
            string AMPM = data[2].Substring(0, 2);
            return date + " " + time + " " + AMPM;
        }

        public static string FileName(string text)
        {
            text += ".bin";
            text = text.Replace('/', '_');
            text = text.Replace(':', '_');
            return text;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("total_time", total_time, typeof(TimeSpan));
            info.AddValue("name", name);
            info.AddValue("time_entries", time_entries, typeof(List<TimeEntryData>));
            info.AddValue("creation_date", creation_date);
            info.AddValue("comment_library", comment_library, typeof(List<string>));
        }
    }
}
