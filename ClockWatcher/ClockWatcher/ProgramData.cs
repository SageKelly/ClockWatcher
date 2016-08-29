using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace ClockWatcher
{
    [Serializable]
    public class ProgramData : INotifyPropertyChanged, ISerializable
    {
        /// <summary>
        /// Holds a list of the filenames of all sessions ever created by this user
        /// </summary>
        public List<string> Sessions;
        /// <summary>
        /// The list of comment entries that exist as option across multiple sessions
        /// </summary>
        public List<string> PersistentCommentEntries;

        public string SESSION_ADDRESS { get; private set; }
        private DateTime start_time;

        public event PropertyChangedEventHandler PropertyChanged;

        public DateTime StartTime
        {
            get
            {
                return start_time;
            }
            private set
            {
                if (start_time != value)
                {
                    start_time = value;
                    OnPropertyChanged("StartTime");
                }
            }
        }


        public ProgramData(Session currrentSession)
        {
            SESSION_ADDRESS = "Sessions/";
            Sessions = new List<string>();
            PersistentCommentEntries = new List<string>();
            start_time = DateTime.Now;
        }

        protected ProgramData(SerializationInfo info, StreamingContext context)
        {
            Sessions = (List<string>)info.GetValue("Sessions", typeof(List<string>));
            PersistentCommentEntries = (List<string>)info.GetValue("PersistentCommentEntries", typeof(List<string>));
            start_time = (DateTime)info.GetValue("start_time", typeof(DateTime));
        }

        public Session OpenSession(string session_name)
        {
            if (File.Exists(session_name))
            {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream(session_name, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
                Session sesh = (Session)formatter.Deserialize(stream);
                stream.Close();
                return sesh;
            }
            else
            {
                //File integrity compromised
                Sessions = (List<string>)Directory.EnumerateFiles(SESSION_ADDRESS);
                return null;
            }
        }

        public Session OpenSession(int index)
        {
            if (File.Exists(Sessions[index]))
            {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream(Sessions[index], FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
                Session sesh = (Session)formatter.Deserialize(stream);
                stream.Close();
                return sesh;
            }
            else
            {
                Sessions = (List<string>)Directory.EnumerateFiles(SESSION_ADDRESS);
                return null;
            }
        }

        public List<Session> OpenSession(IList items)
        {
            List<Session> result = new List<Session>();
            bool complete = true;
            foreach (string s in items)
            {
                string temp = Session.FileName(s);
                if (File.Exists(temp))
                {
                    IFormatter formatter = new BinaryFormatter();
                    Stream stream = new FileStream(temp, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
                    Session sesh = (Session)formatter.Deserialize(stream);
                    stream.Close();
                    result.Add(sesh);
                }
                else
                {
                    complete = false;
                    Sessions = (List<string>)Directory.EnumerateFiles(SESSION_ADDRESS);
                    break;
                }
            }
            if (complete)
                return result;
            return null;
        }

        private void OnPropertyChanged([CallerMemberName] string member_name = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(member_name));
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Sessions", Sessions, typeof(List<string>));
            info.AddValue("PersistentCommentEntries", PersistentCommentEntries, typeof(List<string>));
            info.AddValue("start_time", start_time, typeof(DateTime));
        }

        public bool DeleteSession(IList items)
        {
            foreach (string s in items)
            {
                if (!File.Exists(Session.FileName(s)))
                {
                    Sessions = (List<string>)Directory.EnumerateFiles(SESSION_ADDRESS);
                    return false;
                }
            }
            foreach (string s in items)
            {
                File.Delete(Session.FileName(s));
                Sessions.Remove(Session.FileName(s));
            }
            return true;
        }
    }
}
