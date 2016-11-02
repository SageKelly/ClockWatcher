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
        public DateTime ProgramStarted,ProgramEnded;
        public List<TimeEntry> Times;

        public Session()
        {
            Times = new List<TimeEntry>();
        }

        public void BeginSession()
        {
            ProgramStarted = DateTime.Now;
        }

        public void EndSession()
        {
            ProgramEnded = DateTime.Now;
        }


    }
}
