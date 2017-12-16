using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TimeKeeper
{
    public static class Printer
    {
        private static Queue<PrintTask> PrinterQueue;
        private static SessionManager SeshMan;

        #region CONSTANTS
        /// <summary>
        /// Represents the last line the Console's cursor before
        /// a method manipulated it (0).
        /// </summary>
        public static int LastLine = 0;

        private static Widget commentPoint = new Widget(16, 0, Console.BufferWidth - 16 - 1);

        /// <summary>
        /// The Left location for the comments column for a TimeEntry (16)
        /// </summary>
        public static Widget COMMENT_POINT
        {
            get
            {
                return commentPoint;
            }
        }

        private static Widget status = new Widget(6, 8, 7);

        /// <summary>
        /// Represents the location of the status section of the screen.
        /// </summary>
        public static Widget STATUS_POINT
        {
            get
            {
                return status;
            }
        }

        private static Widget totalPoint = new Widget(16, 8);

        /// <summary>
        /// Represents the location of the Total Time on the screen.
        /// </summary>
        public static Widget TOTAL_POINT
        {
            get
            {
                return totalPoint;
            }
        }

        private static Widget instructionsPoint = new Widget(0, 1, 39);

        /// <summary>
        /// Represents the locations of the Instructions section of the screen
        /// </summary>
        public static Widget INSTRUCTIONS_POINT
        {
            get
            {
                return instructionsPoint;
            }
        }

        /// <summary>
        /// How many lines before TimeEntries are written (8).
        /// </summary>
        public const int TEXT_COUNT = 8;

        /// <summary>
        /// The Left locations of the cursor for a TimeEntry mark (0)
        /// </summary>
        public const int MARK_LEFT = 0;

        /// <summary>
        /// The Left location of the cursor for a TimeEntry (1)
        /// </summary>
        public const int TIME_LEFT = 1;

        /// <summary>
        /// Holds the tab escape sequences for printing (12)
        /// </summary>
        public const int TAB_PAD = 12/*two tabs*/;
        #endregion

        private static ConsoleColor CombineOnColor;
        private static ConsoleColor CombineOffColor;
        private static ConsoleColor MarkOnColor;
        private static ConsoleColor MarkOffColor;

        private static string MarkChar, CombineChar;



        #region Public Methods
        public static void SetupPrinter(SessionManager SM)
        {
            SeshMan = SM;

            //Setup printing queue
            PrinterQueue = new Queue<PrintTask>();

            ///Set special colors
            CombineOnColor = ConsoleColor.Cyan;
            CombineOffColor = ConsoleColor.DarkGray;
            MarkOnColor = ConsoleColor.Green;
            MarkOffColor = ConsoleColor.Red;

            ///Set speical marking characters
            MarkChar = "*";
            CombineChar = "+";
        }

        public static void SetCursorToTimeEntry(int Index)
        {
            Console.SetCursorPosition(TIME_LEFT, Index + TEXT_COUNT);
        }

        /// <summary>
        /// Prompts User with a question of choice.
        /// </summary>
        /// <param name="Message">The with which to prompt the user</param>
        /// <param name="InputChecker">The method being used to validate input</param>
        /// <returns>Returns valid input information</returns>
        private static void PromptUser(string Message, Func<ConsoleKey, bool> InputChecker, Action<ConsoleKey> FinalMethod)
        {
            bool ValidInput = true;
            ConsoleKeyInfo CKI;
            do
            {
                Console.CursorTop = LastLine + 1;
                Console.WriteLine(Message.PadRight(Console.BufferWidth - 1));
                if (!ValidInput)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("\nInvalid Input: try again.".PadRight(Console.BufferWidth - 1));
                    Console.ForegroundColor = ConsoleColor.White;

                    Console.CursorTop = LastLine + 2;
                }

                CKI = Console.ReadKey(true);
                //to clean the screen
                Console.CursorTop = LastLine + 1;
                if (ValidInput)
                {
                    Console.WriteLine("".PadRight(Console.BufferWidth));
                }
                else
                {
                    Console.WriteLine("".PadRight(Console.BufferWidth));
                    Console.WriteLine("".PadRight(Console.BufferWidth));
                }

                ValidInput = InputChecker(CKI.Key);

            } while (!ValidInput);
            FinalMethod(CKI.Key);
        }

        /// <summary>
        /// Queues the interactive printing process
        /// </summary>
        /// <param name="Message">The message to relay to the user</param>
        /// <param name="InputChecker">The method to check the user's response</param>
        public static void QueuePromptUser(string Message, Func<ConsoleKey, bool> InputChecker, Action<ConsoleKey> FinalMethod)
        {
            MethodInfo method = typeof(Printer).GetType().GetMethod("PromptUser", BindingFlags.NonPublic);
            PrintTask pTask = new PrintTask(method, new object[] { Message, InputChecker, FinalMethod });
            PrinterQueue.Enqueue(pTask);
        }

        /// <summary>
        /// Checks printing booleans set to true and does
        /// those tasks
        /// </summary>
        public static void WatchForPrintTasks()
        {
            if (PrinterQueue.Count != 0)
            {
                int left = Console.CursorLeft;
                int top = Console.CursorTop;
                PrintTask task = PrinterQueue.Dequeue();
                task.Invoke();

                switch (ApplicationManager.ProgramState)
                {
                    case ApplicationManager.States.Comment:
                        //SetCursorToTimeEntry(SeshMan.CurrentSession.Times.Count - 1);
                        break;
                    default:
                        Console.SetCursorPosition(left, top);
                        break;
                }
            }
        }

        /// <summary>
        /// Queues the full set of times to be printed.
        /// </summary>
        /// <returns>The PrintTask object used for registering to the task's completion</returns>
        public static PrintTask QueuePrintAllTimes()
        {
            MethodInfo method = typeof(Printer).GetMethod("PrintAllTimes", BindingFlags.NonPublic);
            PrintTask pTask = new PrintTask(method, new object[] { SeshMan });
            PrinterQueue.Enqueue(pTask);
            return pTask;
        }

        /// <summary>
        /// Queues the combine screen to be printed.
        /// </summary>
        /// <returns>The PrintTask object used for registering to the task's completion</returns>
        public static PrintTask QueuePrintCombineMark(int Index)
        {
            MethodInfo method = typeof(Printer).GetType().GetMethod("PrintCombineMark", BindingFlags.NonPublic);
            PrintTask pTask = new PrintTask(method, new object[] { SeshMan, Index });
            PrinterQueue.Enqueue(pTask);
            return pTask;
        }

        public static PrintTask QueuePrintInstructions()
        {
            MethodInfo method = typeof(Printer).GetType().GetMethod("PrintInstructions", BindingFlags.NonPublic);
            PrintTask pTask = new PrintTask(method, new object[] { SeshMan });
            PrinterQueue.Enqueue(pTask);
            return pTask;
        }

        /// <summary>
        /// Queues the Print Mark to be printed.
        /// </summary>
        /// <returns>The PrintTask object used for registering to the task's completion</returns>
        public static PrintTask QueuePrintMark(int Index)
        {
            MethodInfo method = typeof(Printer).GetType().GetMethod("PrintMark", BindingFlags.NonPublic);
            PrintTask pTask = new PrintTask(method, new object[] { SeshMan, Index });
            PrinterQueue.Enqueue(pTask);
            return pTask;
        }

        /// <summary>
        /// Queues the main screen to be printed.
        /// </summary>
        /// <returns>The PrintTask object used for registering to the task's completion</returns>
        public static PrintTask QueuePrintScreen()
        {
            MethodInfo method = typeof(Printer).GetType().GetMethod("PrintScreen", BindingFlags.NonPublic);
            PrintTask pTask = new PrintTask(method, new object[] { SeshMan });
            PrinterQueue.Enqueue(pTask);
            return pTask;
        }

        /// <summary>
        /// Queues the status to be printed.
        /// </summary>
        /// <returns>The PrintTask object used for registering to the task's completion</returns>
        public static PrintTask QueuePrintStatus()
        {
            MethodInfo method = typeof(Printer).GetType().GetMethod("PrintStatus", BindingFlags.NonPublic);
            PrintTask pTask = new PrintTask(method, new object[] { SeshMan });
            PrinterQueue.Enqueue(pTask);
            return pTask;
        }

        /// <summary>
        /// Queues the current time to be printed.
        /// </summary>
        /// <returns>The PrintTask object used for registering to the task's completion</returns>
        public static PrintTask QueuePrintTimeEntry(int Index)
        {
            MethodInfo method = typeof(Printer).GetType().GetMethod("PrintTime", BindingFlags.NonPublic);
            PrintTask pTask = new PrintTask(method, new object[] { SeshMan, Index });
            PrinterQueue.Enqueue(pTask);
            return pTask;
        }

        /// <summary>
        /// Queues the sum of the times to be printed.
        /// </summary>
        /// <returns>The PrintTask object used for registering to the task's completion</returns>
        public static PrintTask QueuePrintTotalTime()
        {
            MethodInfo method = typeof(Printer).GetType().GetMethod("PrintTotalTime", BindingFlags.NonPublic);
            PrintTask pTask = new PrintTask(method, new object[] { SeshMan });
            PrinterQueue.Enqueue(pTask);
            return pTask;
        }
        #endregion

        #region Private
        /// <summary>
        /// Prints all Time Entries
        /// </summary>
        private static void PrintAllTimes(SessionManager SM)
        {
            Console.SetCursorPosition(0, 7);
            Console.WriteLine("Time Spent\tComment");

            //Print all the times and their associated marker
            foreach (TimeEntry TI in SM.CurrentSession.Times)
            {
                if (ApplicationManager.ProgramState == ApplicationManager.States.Combine)
                {
                    Console.ForegroundColor = TI.combine ? CombineOnColor : CombineOffColor;
                    Console.Write(CombineChar);
                }
                else
                {
                    Console.ForegroundColor = TI.marked ? MarkOnColor : MarkOffColor;
                    Console.Write(MarkChar);
                }
                Console.ForegroundColor = ConsoleColor.White;
                string temp = string.Empty;
                if (TI.comment.Length != 0)
                    temp = TI.comment.ToString().Remove(TI.comment.Length - 1);
#if DEBUG
                Console.WriteLine("{0}:{1}{2}",
                    TI.timeSpent.Minutes.ToString().PadLeft(2, '0'), TI.timeSpent.Seconds.ToString().PadRight(TAB_PAD), temp.PadRight(COMMENT_POINT.Pad));
#else
                Console.WriteLine("{0}:{1}{2}",
                    TI.timeSpent.Hours.ToString().PadLeft(2, '0'), TI.timeSpent.Minutes.ToString().PadRight(TAB_PAD),
                temp.PadRight(COMMENT_PAD));
#endif
            }
        }

        /// <summary>
        /// Uses ListIndex to print the Combine mark for the TimeEntry
        /// </summary>
        private static void PrintCombineMark(SessionManager SM, int Index)
        {
            Console.CursorLeft = MARK_LEFT;
            Console.ForegroundColor = SM.CurrentSession.Times[Index].combine ? CombineOnColor : CombineOffColor;
            Console.Write(CombineChar);
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Prints case-related instructions
        /// </summary>
        private static void PrintInstructions(SessionManager SM)
        {
            string l1p1 = "", l1p2 = "", l2p1 = "", l2p2 = "", l3p1 = "", l3p2 = "";
            Console.SetCursorPosition(INSTRUCTIONS_POINT.Left, INSTRUCTIONS_POINT.Top);
            Console.WriteLine("INSTRUCTIONS:");
            switch (ApplicationManager.ProgramState)
            {
                //If necessary apply '\t' only to part 1
                //Space, Escape, Delete, E, R, S, P, M, C, Up/Down, Enter
                #region Instruction comments
                /*
                 * C: C to enter Combine Mode
                 * C: C to mark the end or range, then the start
                 * C: C to mark a combine a range of contiguous entries
                 * Delete: Delete to delete an entry
                 * E: E to enter Edit Mode
                 * E: E to exit Edit Mode
                 * Enter: Enter to Create/Edit Comment
                 * Enter: Enter to Confirm Changes
                 * Enter: Enter to confirm Combine range
                 * Enter: Enter to leave Mark Mode
                 * Escape: Escape to Save/Exit
                 * Escape: Escape to leave Combine Mode without combining
                 * M: M to enter Mark Mode
                 * M: M to mark/unmark an entry to count toward total time
                 * P: P to pause current timer
                 * P: P to start/resume current timer
                 * R: R to refresh
                 * S: S to save
                 * Space: Space to add and start new timer
                 * Up/Down Arrows: Up/Down Arrows to move
                 */
                #endregion
                case ApplicationManager.States.Combine:
                    l1p1 = "Up/Down Arrows to move,";
                    l2p1 = "C to mark the range: end then start";
                    l2p2 = "Enter to confirm Combine range,";
                    l3p1 = "Escape to leave without combining";
                    break;
                case ApplicationManager.States.Comment:
                    l1p1 = "Enter to Confirm Changes";
                    l2p1 = "";
                    l3p1 = "";
                    break;
                case ApplicationManager.States.Edit:
                    l1p1 = "C to enter Combine Mode (nope),";
                    l1p2 = "Delete to delete an entry";
                    l2p1 = "E to exit Edit Mode,";
                    l2p2 = "Enter to Create/Edit Comment,";
                    l3p1 = "M to enter Mark Mode,";
                    l3p2 = "Up/Down Arrows to move, S to Save";
                    break;
                case ApplicationManager.States.Mark:
                    l1p1 = "Up/Down Arrows to move,";
                    l1p2 = "M to mark/unmark an entry";
                    l2p1 = "to count toward total time,";
                    l2p2 = "R to refresh,";
                    l3p1 = "Enter to leave Mark Mode";
                    break;
                case ApplicationManager.States.Off:
                    l1p1 = "P to Pause/Unpause,";
                    l1p2 = "R to refresh,";
                    l2p1 = "E to enter Edit Mode,";
                    l2p2 = "S to save,";
                    l3p1 = "Escape to Save/Exit";
                    l3p2 = "V to View Sessions";
                    break;
                case ApplicationManager.States.Save:
                    l1p1 = "Follow the";
                    l2p1 = "on-screen";
                    l3p1 = "instructions";
                    break;
                case ApplicationManager.States.View:
                    l1p1 = "Left/Right Arrows to view Sessions";
                    l1p2 = "Up/Down Arrows to scroll current session";
                    l2p1 = "Esc to return to previous screen";
                    break;
                case ApplicationManager.States.Watch:
                    l1p1 = "Space to Add a record time,";
                    l1p2 = "P to Pause/Unpause,";
                    l2p1 = "R to refresh,";
                    l2p2 = "E to enter Edit Mode,";
                    l3p1 = "S to save,";
                    l3p2 = "Escape to Save/Exit";
                    break;
            }
            /*
             * To explain the seemingly random numbers in the PadRight methods:
             * 51 is the longest possible line so far
             * 14 is the extra spaces in the \t that aren't counted...for some reason
             */
            Console.WriteLine(
                l1p1.PadRight(INSTRUCTIONS_POINT.Pad) + l1p2.PadRight(INSTRUCTIONS_POINT.Pad) + "\n" +
                l2p1.PadRight(INSTRUCTIONS_POINT.Pad) + l2p2.PadRight(INSTRUCTIONS_POINT.Pad) + "\n" +
                l3p1.PadRight(INSTRUCTIONS_POINT.Pad) + l3p2.PadRight(INSTRUCTIONS_POINT.Pad) + "\n" +
                string.Empty.PadRight(36, '-').PadRight(Console.BufferWidth));
        }

        /// <summary>
        /// Uses ListIndex to print a TimeEntry's mark pip.
        /// </summary>
        private static void PrintMark(SessionManager SM, int Index)
        {
            Console.CursorLeft = MARK_LEFT;
            Console.ForegroundColor = SM.CurrentSession.Times[Index].marked ? MarkOnColor : MarkOffColor;
            Console.Write(MarkChar);
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Clears and prints the entire screen
        /// </summary>
        private static void PrintScreen(SessionManager SM)
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("TIME KEEPER");
            Console.ForegroundColor = ConsoleColor.White;
            QueuePrintInstructions();
            QueuePrintStatus();
            QueuePrintTotalTime();
            QueuePrintAllTimes();
            SetCursorToTimeEntry(SeshMan.CurrentSession.Times.Count - 1);
        }

        /// <summary>
        /// Updates the program status section of the screen
        /// </summary>
        private static void PrintStatus(SessionManager SM)
        {
            Console.SetCursorPosition(STATUS_POINT.Left, STATUS_POINT.Top);
            Console.Write("Status: ");
            switch (ApplicationManager.ProgramState)
            {
                case ApplicationManager.States.Combine:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case ApplicationManager.States.Watch:
                case ApplicationManager.States.Mark:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case ApplicationManager.States.Edit:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case ApplicationManager.States.Comment:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case ApplicationManager.States.Save:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
                case ApplicationManager.States.View:
                case ApplicationManager.States.Off:
                case ApplicationManager.States.Exit:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }
            //Just enough to clear the longest state name
            Console.Write(ApplicationManager.ProgramState.ToString().PadRight(7));
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Uses ListIndex to print a TimeEntry to the screen
        /// </summary>
        private static void PrintTimeEntry(SessionManager SM, int Index)
        {
            PrintMark(SM, Index);
            Console.CursorLeft = TIME_LEFT;
            string temp = "";
            if (SM.CurrentSession.Times[Index].comment.Length != 0)
                temp = SM.CurrentSession.Times[Index].comment.ToString().Remove(SM.CurrentSession.Times[Index].comment.Length - 1);
#if DEBUG
            Console.WriteLine("{0}:{1}{2}",
                SM.CurrentSession.Times[Index].timeSpent.Minutes.ToString().PadLeft(2, '0'),
                SM.CurrentSession.Times[Index].timeSpent.Seconds.ToString().PadRight(TAB_PAD),
                temp.PadRight(COMMENT_POINT.Pad));
#else
            Console.WriteLine("{0}:{1}{2}",
                SM.CurrentSession.Times[Index].timeSpent.Hours.ToString().PadLeft(2, '0'),
                SM.CurrentSession.Times[Index].timeSpent.Minutes.ToString().PadRight(TAB_PAD),
                temp.PadRight(COMMENT_POINT.Pad));
#endif
        }

        /// <summary>
        /// Updates the Total Time section of the screen
        /// </summary>
        private static void PrintTotalTime(SessionManager SM)
        {
            TimeSpan total = SM.TallyTime();
            Console.CursorTop = TOTAL_POINT.Top;
            Console.CursorLeft = TOTAL_POINT.Left;

            //Console.Write("Total Time: {0}:{1} ", TotalTimeSpan.Hours, TotalTimeSpan.Minutes);
            Console.Write("Total Time: {0}".PadRight(INSTRUCTIONS_POINT.Pad), total.Duration());
        }
        #endregion


    }
}
