using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Threading;

namespace ClockWatcher
{
    public class SessionManager : INotifyPropertyChanged
    {
        public string PD_FILE = "ProgramData.bin";

        public event PropertyChangedEventHandler PropertyChanged;

        public ProgramData Data { get; private set; }
        public delegate void NewDayEventHandler(object sender, EventArgs ea);
        public event NewDayEventHandler NewDayEvent;

        public ObservableCollection<string> SessionNames { get; private set; }
        private List<string> comment_library;
        /// <summary>
        /// Represents the Session opened at the start of the program.
        /// </summary>
        private Session current_session;
        private bool _isWatching;
        private Timer _timer;
        private Stopwatch _clockWatch;
        private TimeSpan filtered_time;
        public TimeSpan FilteredTime
        {
            get
            {
                return filtered_time;
            }
            private set
            {
                if (filtered_time != value)
                {
                    filtered_time = value;
                    OnPropertyChanged("FilteredTime");
                }
            }
        }
        private DateTime current_time;
        #region Properties
        public DateTime CurrentTime
        {
            get
            {
                return DateTime.Now;
            }
            set
            {
                if (current_time != value)
                {
                    current_time = value;
                    OnPropertyChanged("CurrentTime");
                }
            }
        }

        private bool isWatching
        {
            get
            {
                return _isWatching;
            }
            set
            {
                _isWatching = value;
                if (!_isWatching)
                {
                    _clockWatch.Stop();
                }
                else
                {
                    _clockWatch.Start();
                }
            }
        }

        public List<string> CommentLibrary
        {
            get
            {
                return comment_library;
            }
            private set
            {
                if (comment_library != value)
                {
                    comment_library = value;
                    OnPropertyChanged("CommentLibrary");
                }
            }
        }

        public List<Session> OpenSessions { get; private set; }

        public ObservableCollection<UIElement> Entries { get; private set; }

        public Session CurrentSession
        {
            get
            {
                return current_session;
            }
            set
            {
                if (current_session != value)
                {
                    current_session = value;
                    OnPropertyChanged("CurrentSession");
                }
            }
        }

        #endregion


        public SessionManager()
        {
            _clockWatch = new Stopwatch();
            _timer = new Timer(1000);//one second
            _timer.Elapsed += timerElapsed;

            CommentLibrary = new List<string>();
            SessionNames = new ObservableCollection<string>();
            Data = LoadProgramData();
            comment_library.AddRange(Data.PersistentCommentEntries);
            OpenSessions = new List<Session>();

            OpenSessions.Add(new Session());
            CurrentSession = OpenSessions.Last();

            MergeCommentLibraries();
            Entries = new ObservableCollection<UIElement>();
            FilteredTime = TimeSpan.Zero;
            _isWatching = false;
            current_time = new DateTime();
            CurrentTime = DateTime.Now;
            _timer.Start();
        }


        #region Methods
        #region Event Methods
        /// <summary>
        /// Registered to Timer.Elapsed Event
        /// (See constructor)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void timerElapsed(object sender, ElapsedEventArgs e)
        {
            CurrentTime = DateTime.Now;
            if ((CurrentTime.TimeOfDay.Hours == 0 &&
                CurrentTime.TimeOfDay.Minutes == 0 &&
                CurrentTime.TimeOfDay.Seconds == 0) &&
                NewDayEvent != null)
            {
                NewDayEvent(this, new EventArgs());
            }
            if (isWatching)
            {
                if (CurrentSession != null)
                {
                    //update the timespent variable of the current timeEntry
                    if (CurrentSession.currentTEData != null)
                    {
                        CurrentSession.currentTEData.TimeSpent = _clockWatch.Elapsed;
                        calculateTotalTime();

                        //CalculateFilteredTimeSpent();
                    }
                }
            }
        }
        /// <summary>
        /// Registered to TimeEntry.newCommentEvent Event
        /// (see addNewTimeEntry())
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public commentEntry newCommentEntry(string comment)
        {
            //If you made it this far, then none of them matched
            commentEntry newEntry = current_session.addComment(comment);
            if (newEntry != null)
            {
                CommentLibrary.Add(comment);
            }
            return newEntry;
        }
        /// <summary>
        /// Registered to TimeEntry.delete Event
        /// (see addNewTimeEntry())
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void deleteTimeEntry(TimeEntry t)
        {
            CurrentSession.deleteTimeEntry(t);
            Entries.Remove(t);
            if (CurrentSession.TimeEntries.Count > 0 &&
                t.Data.EntryID > CurrentSession.TimeEntries.Last().EntryID)
            {
                isWatching = false;
                _clockWatch.Reset();
            }
            calculateTotalTime();
        }
        /// <summary>
        /// Registered commentEntry.delete Event
        /// (see newCommentEntry())
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void deleteComment(commentEntry ce)
        {
            current_session.deleteComment(ce);
            comment_library.Remove(ce.comment);
        }

        #endregion

        #region Class Methods
        public TimeEntry addNewTimeEntry()
        {
            if (CurrentSession != null && CurrentSession.currentTEData != null)
            {
                isWatching = false;
                CurrentSession.currentTEData.Owner.finalize();
            }
            TimeEntry result = CurrentSession.addEntry(DateTime.Now);
            Entries.Add(result);
            _clockWatch.Reset();
            isWatching = true;
            CalculateFilteredTimeSpent();

            return result;
        }

        public void calculateTotalTime()
        {
            CurrentSession.TotalTime = TimeSpan.Zero;
            foreach (TimeEntryData te in CurrentSession.TimeEntries)
            {
                CurrentSession.TotalTime += te.TimeSpent;
            }
        }
        private void updateTimeSpent()
        {
            if (CurrentSession != null)
            {
                //update the timespent variable of the current timeEntry
                if (CurrentSession.currentTEData != null)
                {
                    CurrentSession.currentTEData.TimeSpent = _clockWatch.Elapsed;
                    calculateTotalTime();
                }
            }
        }
        /// <summary>
        /// Calculates the total time spent on the filtered time entries
        /// </summary>
        public void CalculateFilteredTimeSpent()
        {
            TimeSpan result = TimeSpan.Zero;
            foreach (UIElement uie in Entries)
            {
                if (uie.GetType() == typeof(TimeEntry))
                {
                    TimeEntry temp = (TimeEntry)uie;
                    if (!temp.isCollapsed)
                    {
                        result += temp.Data.TimeSpent;
                    }
                }
            }
            FilteredTime = result;
        }

        public void SaveSession()
        {
            IFormatter formatter = new BinaryFormatter();

            foreach (Session sesh in OpenSessions)
            {
                if (sesh.TimeEntries.Count != 0)
                {
                    if (!sesh.TimeEntries.Last().Owner.isFinalized)
                    {
                        sesh.TimeEntries.Last().Owner.finalize();
                    }
                    Stream stream = new FileStream(Data.SESSION_ADDRESS + sesh.Name, FileMode.Create, FileAccess.Write, FileShare.None);
                    formatter.Serialize(stream, sesh);
                    stream.Close();
                    sesh.Save();
                    Data.Sessions.Add(Session.FileName(sesh.Name));
                }
                else
                {
                    Data.Sessions.Remove(sesh.Name);
                }
            }
            SaveProgramData();
        }

        public void SaveProgramData()
        {
            //Serialize the ProgramData
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(PD_FILE, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, Data);
            stream.Close();
        }

        public void Deserialize()
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(PD_FILE, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
            Data = (ProgramData)formatter.Deserialize(stream);
            stream.Close();
        }
        private void OnPropertyChanged([CallerMemberName] string member_name = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(member_name));
            }
        }

        public ProgramData LoadProgramData()
        {
            if (!File.Exists(PD_FILE))
            {
                return new ProgramData(CurrentSession);
            }
            else
            {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream(PD_FILE, FileMode.Open, FileAccess.Read, FileShare.Read);
                ProgramData Data = (ProgramData)formatter.Deserialize(stream);
                stream.Close();
                return Data;
            }
        }

        public void LoadOpenSession()
        {
            CurrentSession = OpenSessions.Last();
            Entries.Clear();
            foreach (TimeEntryData ted in CurrentSession.TimeEntries)
            {
                Entries.Add(new TimeEntry(ted));
            }
            comment_library.Clear();
            comment_library.AddRange(Data.PersistentCommentEntries);
            MergeCommentLibraries();
        }

        public void LoadSessions()
        {
            if (SessionNames.Count == 0)
            {
                foreach (string s in Data.Sessions)
                {
                    SessionNames.Add(Session.FriendlyName(s));
                }
            }
        }

        /// <summary>
        /// Loads Session from Session View menu
        /// </summary>
        /// <param name="index"></param>
        public void LoadSession(int index)
        {
            isWatching = false;
            Session prevSession = CurrentSession;
            CurrentSession = Data.OpenSession(index);
            if (CurrentSession != null)
            {
                ReloadEntries();
                comment_library.Clear();
                comment_library.AddRange(Data.PersistentCommentEntries);
                MergeCommentLibraries();
            }
            else
            {
                CurrentSession = prevSession;
            }
        }

        /// <summary>
        /// Loads Session from Session View menu
        /// </summary>
        /// <param name="index"></param>
        public void LoadSession(string session_name)
        {
            isWatching = false;
            Session prevSession = CurrentSession;
            CurrentSession = Data.OpenSession(session_name);
            if (CurrentSession != null)
            {
                ReloadEntries();
                comment_library.Clear();
                comment_library.AddRange(Data.PersistentCommentEntries);
                MergeCommentLibraries();
            }
            else
            {
                CurrentSession = prevSession;
            }
        }

        /// <summary>
        /// Loads Session from Session View menu
        /// </summary>
        /// <param name="index"></param>
        public void LoadSession(IList items)
        {
            isWatching = false;
            List<Session> loaded_sessions = Data.OpenSession(items);
            if (loaded_sessions == null) { }
            else if (loaded_sessions.Count > 0)
            {
                //This is arbitrary: just so CurrentSession has a value
                CurrentSession = loaded_sessions[0];
                ReloadEntries(loaded_sessions);
                comment_library.Clear();
                comment_library.AddRange(Data.PersistentCommentEntries);
                MergeCommentLibraries(loaded_sessions);
            }
        }

        /// <summary>
        /// Merges the Session's comment_library with the program's
        /// and makes sure all entries are unique
        /// </summary>
        public void MergeCommentLibraries()
        {
            foreach (string s in CurrentSession.CommentLibrary)
            {
                if (!comment_library.Contains(s))
                {
                    comment_library.Add(s);
                }
            }
            comment_library.RemoveAll(new Predicate<string>(x => x == null));
        }

        /// <summary>
        /// Merges the Session's comment_library with the program's
        /// and makes sure all entries are unique
        /// </summary>
        public void MergeCommentLibraries(List<Session> sessions)
        {
            foreach (Session sesh in sessions)
            {
                foreach (string s in sesh.CommentLibrary)
                {
                    if (!comment_library.Contains(s))
                    {
                        comment_library.Add(s);
                    }
                }
            }
            comment_library.RemoveAll(new Predicate<string>(x => x == null));
        }

        /// <summary>
        /// Reloads Entries list with new entries and reassigns time entry data
        /// </summary>
        public void ReloadEntries()
        {
            //Remove current TimeEntries
            Entries.Clear();
            AddSessionStamp(CurrentSession.Name);
            //Add enough for new session
            foreach (TimeEntryData ted in CurrentSession.TimeEntries)
            {
                Entries.Add(new TimeEntry(ted));
            }
        }

        /// <summary>
        /// Reloads Entries list with new entries and reassigns time entry data
        /// </summary>
        public void ReloadEntries(List<Session> items)
        {
            //Remove current TimeEntries
            Entries.Clear();
            //Add enough for new session
            foreach (Session sesh in items)
            {
                AddSessionStamp(sesh.Name);
                foreach (TimeEntryData ted in sesh.TimeEntries)
                {
                    Entries.Add(new TimeEntry(ted));
                }
            }
        }
        #endregion
        #endregion

        public void DeleteSession(IList items)
        {
            Data.DeleteSession(items);
            for (int i = items.Count - 1; i >= 0; i--)
            {
                string s = (string)items[i];
                SessionNames.Remove(s);
            }
        }

        public bool SplitSession()
        {
            bool PrevEntry = true;
            if (current_session.TimeEntries.Count == 0)
            {
                OpenSessions.Remove(CurrentSession);
                PrevEntry = false;
            }
            else if (CurrentSession.currentTEData != null)
            {
                CurrentSession.currentTEData.Owner.finalize();
            }
            OpenSessions.Add(new Session());
            CurrentSession = OpenSessions.Last();

            if (PrevEntry)
            {
                /*
                 * Take the comment from the last TimeEntry, if any,
                 * and add it to the list of comments in the new Session
                */
                TimeEntry t = (TimeEntry)Entries.LastOrDefault(
                    x => x.GetType() == typeof(TimeEntry));

                string s = t == null ? "New Day" : t.Data.Comment;
                current_session.addComment(s == null ? "New Day" : s);
            }
            return PrevEntry;
        }

        public TextBlock AddSessionStamp()
        {
            TextBlock timeStamp = new TextBlock();
            timeStamp.Text = "-----------" + CurrentSession.Name + "-----------";
            Entries.Add(timeStamp);
            return timeStamp;
        }

        private void AddSessionStamp(string session_name)
        {
            TextBlock timeStamp = new TextBlock();
            timeStamp.Text = "-----------" + session_name + "-----------";
            Entries.Add(timeStamp);
        }

        public string CheckForRecentSession()
        {
            string filename = DateTime.Today.Date.ToString().Replace('/', '_').Split(' ')[0];
            for (int i = Data.Sessions.Count - 1; i >= 0; i--)
            {
                string s = Data.Sessions[i];
                string[] data = s.Split(' ');
                string date = data[0];
                if (filename == date)
                    return Data.Sessions[i];
            }
            return null;
        }
    }
}
