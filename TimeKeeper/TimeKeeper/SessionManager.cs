using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace TimeKeeper
{
    [Serializable]
    public class SessionManager
    {

        private List<Session> sessions;
        private int internal_index;
        /// <summary>
        /// represents the newest session
        /// </summary>
        public Session currentSession;
        /// <summary>
        /// represents the currently viewed session
        /// </summary>
        public Session viewingSession;

        /// <summary>
        /// Instantiates a SessionManager
        /// </summary>
        public SessionManager()
        {
            sessions = new List<Session>();
            internal_index = sessions.Count - 1;
        }

        /// <summary>
        /// Adds a session to the session Manager.
        /// </summary>
        /// <param name="s">The Session to be added</param>
        public void Add(Session s)
        {
            sessions.Add(s);
            internal_index = sessions.Count - 1;
        }

        /// <summary>
        /// Returns the most recently-selected Session. If unaltered the index
        /// will point to the most recently-created session
        /// </summary>
        /// <returns>The most recently-selected Session</returns>
        public Session SelectedSession()
        {
            if (sessions.Count > 0)
                return sessions[internal_index];
            else
                return null;
        }

        /// <summary>
        /// Returns a more recent Session, then wraps around to least recent
        /// </summary>
        /// <returns>A more recent Session, then wraps around the least recent</returns>
        public Session NextSession()
        {
            internal_index = (internal_index + 1) % sessions.Count;
            return SelectedSession();
        }

        /// <summary>
        /// Return a less recent Session, then wraps around to most recent
        /// </summary>
        /// <returns>A less recent Session, then wraps around the most recent</returns>
        public Session PreviousSession()
        {
            internal_index = (internal_index - 1) < 0 ? sessions.Count - 1 : internal_index - 1;
            return SelectedSession();
        }

        /// <summary>
        /// Serializes all Sessions made
        /// </summary>
        /// <param name="filename">The filename of the serialization file</param>
        public void SaveSessions(string filename)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, sessions);
            stream.Close();
        }

        /// <summary>
        /// Deltes the currently-selected session. You cannot delete
        /// the last, or most-recently created, session
        /// </summary>
        public void RemoveSession()
        {
            if (sessions.Count > 1 && internal_index != sessions.Count - 1)
                sessions.RemoveAt(internal_index);
        }

        /// <summary>
        /// Loads all Sessions made
        /// </summary>
        /// <param name="filename">The filename of the serialization file</param>
        public void LoadSessions(string filename)
        {
            IFormatter formatter = new BinaryFormatter();
            if (!File.Exists(filename))
            {
                File.Create(filename);
            }
            Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length > 0)
            {
                sessions = (List<Session>)formatter.Deserialize(stream);
                internal_index = sessions.Count - 1;
            }
            stream.Close();
        }

        /// <summary>
        /// Takes quick inventory of the total time spent so far.
        /// Useful when deleting TimeEntries.
        /// </summary>
        public TimeSpan TallyTime()
        {
            TimeSpan TotalTimeSpan = TimeSpan.Zero;
            if (currentSession.Times.Count != 0)
            {
                foreach (TimeEntry TI in currentSession.Times)
                {
                    if (TI.marked)
                        TotalTimeSpan += TI.timeSpent;
                }
            }
            return TotalTimeSpan;
        }
    }
}
