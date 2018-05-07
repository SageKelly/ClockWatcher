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
                SessionPrinter.Invoke(new Printer.PrintTaskHandler(Printer.QueuePrintTask), new object[] { Printer.QueuePrintStatusPrintTask() });
                SessionPrinter.Invoke(new Printer.PrintTaskHandler(Printer.QueuePrintTask), new object[] { Printer.QueuePrintInstructionsPrintTask() });
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

        public static bool IsCommenting;

        public static Printer SessionPrinter;
    }
}
