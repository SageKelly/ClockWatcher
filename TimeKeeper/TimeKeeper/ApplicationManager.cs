using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeKeeper
{
    public static class ApplicationManager
    {
        public enum States
        {
            Off,
            Watch,
            Edit,
            Combine,
            Mark,
            Comment,
            Save,
            View,
            SaveAndExit,
            Exit
        }

        private static States programState;
        public static States ProgramState
        {
            get
            {
                return programState;
            }
            set
            {
                prevState = ProgramState;
                programState = value;
                Printer.QueuePrintStatus();
                Printer.QueuePrintInstructions();
            }
        }
        private static States prevState;
        public static States PrevState
        {
            get
            {
                return prevState;
            }
        }
        
    }
}
