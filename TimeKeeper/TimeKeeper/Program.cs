using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

namespace TimeKeeper
{
    class Program
    {
        #region VARIABLES
        static Thread InputWatcher, DayWatcher;
        static ConsoleKeyInfo CKI;
        static Stopwatch Timer;
        /// <summary>
        /// represents the newest session
        /// </summary>
        static Session currentSession;
        /// <summary>
        /// represents the currently viewed session
        /// </summary>
        static Session viewingSession;
        static SessionManager SM;
        delegate void MethodInvoker();
        static Queue<MethodInvoker> PrintQueue;
        static int listIndex = 0;
        static TimeSpan curTime = TimeSpan.Zero, prevTime = curTime;
        /// <summary>
        /// Represents if the Timer's minutes or hours changed
        /// </summary>
        static bool TimerChanged;
        static bool newDayOccurred = false;
        /// <summary>
        /// Represents the currently selected in the currentSession.Times list.
        /// In this program, it assumes the newest index before it exists.
        /// Therefore, if currentSession.Times has 3 entries, ListIndex, at that point would equal 3.
        /// </summary>
        static int ListIndex
        {
            get
            {
                return listIndex;
            }
            set
            {
                listIndex = value;
                Console.CursorTop = listIndex + TEXT_COUNT;
            }
        }

        const string FILENAME = "Sessions.bin";

        #region CONSTANTS
        /// <summary>
        /// Represents the last line the Console's cursor before
        /// a method manipulated it (0).
        /// </summary>
        static int LastLine = 0;

        /// <summary>
        /// The Left location for the comments column for a TimeInfo (16)
        /// </summary>
        const int COMMENT_LEFT = 16;

        static int COMMENT_PAD = Console.BufferWidth - COMMENT_LEFT - 1;

        /// <summary>
        /// The Top location for the program's status. Also used for Total Time (6)
        /// </summary>
        const int STATUS_TOP = 6;

        /// <summary>
        /// The Left location for the program's status (8).
        /// </summary>
        const int STATUS_LEFT = 8;

        /// <summary>
        /// The padding for the status section of the screen (7)
        /// </summary>
        const int STATUS_PAD = 7;

        /// <summary>
        /// How many lines before TimeEntries are written (8).
        /// </summary>
        const int TEXT_COUNT = 8;

        /// <summary>
        /// The Left location for the Total Time (16)
        /// </summary>
        const int TOTAL_LEFT = 16;

        /// <summary>
        /// The Left locations of the cursor for a TimeEntry mark (0)
        /// </summary>
        const int MARK_LEFT = 0;

        /// <summary>
        /// The Left location of the cursor for a TimeEntry (1)
        /// </summary>
        const int TIME_LEFT = 1;

        /// <summary>
        /// Holds the tab escape sequences for printing (12)
        /// </summary>
        const int TAB_PAD = 12/*two tabs*/;

        /// <summary>
        /// Represents the cursor's top location for the instructions (1)
        /// </summary>
        const int INSTRUCTIONS_TOP = 1;//No left needed

        /// <summary>
        /// Represents how much console padding should happen
        /// on certain printing instructions (39)
        /// </summary>
        const int INSTRUCTIONS_PAD = 39;
        #endregion

        #region Printing Booleans
        private static bool printCombineMark = false;
        private static bool printInstructions = false;
        private static bool printMark = false;
        private static bool printTime = false;
        private static bool printScreen = false;
        private static bool printStatus = false;
        private static bool printAllTimes = false;
        private static bool printTotalTime = false;
        private static bool saveTimes = false;

        private static bool BPrintCombineMark
        {
            get
            {
                return printCombineMark;
            }
            set
            {
                printCombineMark = value;
                if (printCombineMark)
                    PrintQueue.Enqueue(PrintCombineMark);
            }
        }
        private static bool BPrintInstructions
        {
            get
            {
                return printInstructions;
            }
            set
            {
                printInstructions = value;
                if (printInstructions)
                    PrintQueue.Enqueue(PrintInstructions);
            }
        }
        private static bool BPrintMark
        {
            get
            {
                return printMark;
            }
            set
            {
                printMark = value;
                if (printMark)
                    PrintQueue.Enqueue(PrintMark);
            }
        }
        private static bool BPrintTime
        {
            get
            {
                return printTime;
            }
            set
            {
                printTime = value;
                if (printTime)
                {
                    PrintQueue.Enqueue(PrintTime);
                }
            }
        }
        private static bool BPrintScreen
        {
            get
            {
                return printScreen;
            }
            set
            {
                printScreen = value;
                if (printScreen)
                    PrintQueue.Enqueue(PrintScreen);
            }
        }
        private static bool BPrintStatus
        {
            get
            {
                return printStatus;
            }
            set
            {
                printStatus = value;
                if (printStatus)
                    PrintQueue.Enqueue(PrintStatus);
            }
        }
        private static bool BPrintAllTimes
        {
            get
            {
                return printAllTimes;
            }
            set
            {
                printAllTimes = value;
                if (printAllTimes)
                    PrintQueue.Enqueue(PrintAllTimes);
            }
        }
        private static bool BPrintTotalTime
        {
            get
            {
                return printTotalTime;
            }
            set
            {
                printTotalTime = value;
                if (printTotalTime)
                    PrintQueue.Enqueue(PrintTotalTime);
            }
        }
        private static bool BSaveTimes
        {
            get
            {
                return saveTimes;
            }
            set
            {
                saveTimes = value;
                if (saveTimes)
                    PrintQueue.Enqueue(SaveTimes);
            }
        }

        #endregion

        static TimeSpan TotalTimeSpan;

        static ConsoleColor CombineOnColor;
        static ConsoleColor CombineOffColor;
        static ConsoleColor MarkOnColor;
        static ConsoleColor MarkOffColor;

        static string MarkChar, CombineChar;

        enum States
        {
            Off,
            Watch,
            Edit,
            Combine,
            Mark,
            Comment,
            Save,
            View,
            Exit
        }
        private static States programState;
        private static States ProgramState
        {
            get
            {
                return programState;
            }
            set
            {
                programState = value;
                BPrintStatus = true;
                BPrintInstructions = true;
            }
        }
        private static States PrevState;

        /// <summary>
        /// Represents the day this program was initialized
        /// </summary>
        private static DateTime StartingDate;

        /// <summary>
        /// Represents the previous cycle's time
        /// </summary>
        private static DateTime Previous = DateTime.Now;
        /// <summary>
        /// Represents the current cycle's time
        /// </summary>
        private static DateTime Now = DateTime.Now;

        /// <summary>
        /// Represents the first-most possible index for current TimeEntry combining
        /// </summary>
        static int CombineIndexFirst;

        /// <summary>
        /// Represents the last-most possible index for current TimeEntry combining
        /// </summary>
        static int CombineIndexLast;
        #endregion

        static void Main(string[] args)
        {
            Console.Title = "Time Keeper";
            ///Start the threads
            InputWatcher = new Thread(ReadKeys);
            DayWatcher = new Thread(ClockAndPrintWatcher);

            //Setup printing queue
            PrintQueue = new Queue<MethodInvoker>();

            ///Set special colors
            CombineOnColor = ConsoleColor.Cyan;
            CombineOffColor = ConsoleColor.DarkGray;
            MarkOnColor = ConsoleColor.Green;
            MarkOffColor = ConsoleColor.Red;

            ///Set speical marking characters
            MarkChar = "*";
            CombineChar = "+";

            CKI = new ConsoleKeyInfo();

            SM = new SessionManager();
            SM.LoadSessions(FILENAME);

            SM.Add(new Session());
            currentSession = SM.selectedSession();

            ListIndex = currentSession.Times.Count - 1;
            ResetCombineIndices();

            ///Record the day this program started
            currentSession.BeginSession();

            Timer = new Stopwatch();
            TotalTimeSpan = new TimeSpan();

            BPrintScreen = true;
            ProgramState = States.Off;
            DayWatcher.Start();
            InputWatcher.Start();

        }

        /// <summary>
        /// Adds a new TimeInfo instance to the list of TimeInfo objects.
        /// </summary>
        public static void AddNewTime()
        {
            if (currentSession.Times.Count > 0)
            {
                Timer.Stop();
                currentSession.Times[currentSession.Times.Count - 1].ended = DateTime.Now;
                currentSession.Times[currentSession.Times.Count - 1].timeSpent = Timer.Elapsed;
                currentSession.Times.Add(new TimeEntry(DateTime.Now));
                Timer.Restart();
            }
            else
            {
                currentSession.Times.Add(new TimeEntry(DateTime.Now));
                Timer.Start();
            }
            BPrintTotalTime = true;
        }

        /// <summary>
        /// Combines all TimeEntries marked for combining and keeps the first-most's comment
        /// </summary>
        public static void CombineMarked()
        {
            TimeEntry mainEntry;
            int CombineIndex;
            if (CombineIndexFirst == 0 && currentSession.Times[CombineIndexFirst].combine)
            {
                mainEntry = currentSession.Times[CombineIndexFirst];
            }
            else
            {
                mainEntry = currentSession.Times[CombineIndexFirst + 1];
            }

            if (CombineIndexLast == currentSession.Times.Count - 1 && currentSession.Times[CombineIndexLast].combine)
            {
                CombineIndex = CombineIndexLast;
            }
            else
            {
                CombineIndex = CombineIndexLast - 1;
            }
            while (currentSession.Times[CombineIndex].combine && CombineIndex != currentSession.Times.IndexOf(mainEntry))
            {
                mainEntry.timeSpent += currentSession.Times[CombineIndex].timeSpent;
                currentSession.Times.RemoveAt(CombineIndex);
                CombineIndex--;
            }

        }

        /// <summary>
        /// Deccrements both ListIndex and Console.CursorTop
        /// </summary>
        public static void DecListAndTop()
        {
            ListIndex--;
        }

        /// <summary>
        /// Deletes the TimeEntry instance at ListIndex
        /// </summary>
        private static void DeleteTime()
        {
            if (ListIndex < currentSession.Times.Count && currentSession.Times.Count != 0)
            {
                //Clear the last line off the buffer
                int temp = ListIndex;
                SetToBottom();
                DecListAndTop();
                Console.Write("".PadRight(Console.BufferWidth - 1));
                ListIndex = temp;
                Console.CursorLeft = 0;
                Timer.Stop();
                currentSession.Times.RemoveAt(ListIndex);
                Timer.Restart();

                if (ListIndex == currentSession.Times.Count && ListIndex > 0)//If you deleted the last TimeEntry
                    DecListAndTop();
            }
        }

        /// <summary>
        /// Find the first-most and last-most possible TimeEntries can be current combined.
        /// </summary>
        public static void FindCombineEdges()
        {
            if (ListIndex != 0 && !currentSession.Times[ListIndex - 1].combine)
            {
                CombineIndexFirst = ListIndex - 1;
            }
            else
            {
                while (!currentSession.Times[CombineIndexFirst].combine)
                {
                    CombineIndexFirst++;
                }
            }
            if (ListIndex < currentSession.Times.Count - 1 && !currentSession.Times[ListIndex + 1].combine)
            {
                CombineIndexLast = ListIndex + 1;
            }
            else
            {
                while (!currentSession.Times[CombineIndexLast].combine)
                {
                    CombineIndexLast--;
                }
            }
        }

        /// <summary>
        /// Increments both ListIndex and Console.CursorTop
        /// </summary>
        public static void IncListAndTop()
        {
            ListIndex++;
        }

        /// <summary>
        /// Checks to see if any TimeEntries are marked for combining
        /// </summary>
        /// <returns>returns true if there are, else false</returns>
        private static bool NoCombineMarks()
        {
            #region Logic behind method
            /*
             * Since the CombineIndexFirst and ""Last variables are going
             * to keep ListIndex within their bounds, and grow/shrink each
             * time the TimeEntry at ListIndex is toggled on/off, that means
             * that those variables, if there's only one marked, will be
             * directly next to, or equalling, ListIndex. Therefore, you only
             * need to check, at most, three indices.
             */
            #endregion
            if (!currentSession.Times[ListIndex].combine)
            {
                //If not at 0, and above location not combine
                if (ListIndex - 1 > -1 && !currentSession.Times[ListIndex - 1].combine)
                {
                    //If not at end and location below not combine
                    if (ListIndex + 1 < currentSession.Times.Count - 1 && !currentSession.Times[ListIndex + 1].combine)
                    {
                        return true;
                    }
                }
                //If you made it here, then there was only one entry
                else if (currentSession.Times.Count == 1)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Prints all Time Entries
        /// </summary>
        public static void PrintAllTimes()
        {
            int left = Console.CursorLeft;
            int top = Console.CursorTop;
            Console.SetCursorPosition(0, 7);
            Console.WriteLine("Time Spent\tComment");

            //Print all the times and their associated marker
            foreach (TimeEntry TI in currentSession.Times)
            {
                if (programState == States.Combine)
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
                    TI.timeSpent.Minutes.ToString().PadLeft(2, '0'), TI.timeSpent.Seconds.ToString().PadRight(TAB_PAD), temp.PadRight(COMMENT_PAD));
#else
                Console.WriteLine("{0}:{1}{2}",
                    TI.timeSpent.Hours.ToString().PadLeft(2, '0'), TI.timeSpent.Minutes.ToString().PadRight(TAB_PAD),
                temp.PadRight(COMMENT_PAD));
#endif
            }
            Console.SetCursorPosition(left, top);
        }

        /// <summary>
        /// Uses ListIndex to print the Combine mark for the TimeEntry
        /// </summary>
        public static void PrintCombineMark()
        {
            int left = Console.CursorLeft;
            int top = Console.CursorTop;
            Console.CursorLeft = MARK_LEFT;
            Console.ForegroundColor = currentSession.Times[ListIndex].combine ? CombineOnColor : CombineOffColor;
            Console.Write(CombineChar);
            Console.ForegroundColor = ConsoleColor.White;
            Console.SetCursorPosition(left, top);
        }

        /// <summary>
        /// Prints case-related instructions
        /// </summary>
        private static void PrintInstructions()
        {
            int left = Console.CursorLeft;
            int top = Console.CursorTop;
            string l1p1 = "", l1p2 = "", l2p1 = "", l2p2 = "", l3p1 = "", l3p2 = "";
            Console.SetCursorPosition(0, INSTRUCTIONS_TOP);
            Console.WriteLine("INSTRUCTIONS:");
            switch (ProgramState)
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
                case States.Combine:
                    l1p1 = "Up/Down Arrows to move,";
                    l2p1 = "C to mark the range: end then start";
                    l2p2 = "Enter to confirm Combine range,";
                    l3p1 = "Escape to leave without combining";
                    break;
                case States.Comment:
                    l1p1 = "Enter to Confirm Changes";
                    l2p1 = "";
                    l3p1 = "";
                    break;
                case States.Edit:
                    l1p1 = "C to enter Combine Mode (nope),";
                    l1p2 = "Delete to delete an entry";
                    l2p1 = "E to exit Edit Mode,";
                    l2p2 = "Enter to Create/Edit Comment,";
                    l3p1 = "M to enter Mark Mode,";
                    l3p2 = "Up/Down Arrows to move, S to Save";
                    break;
                case States.Mark:
                    l1p1 = "Up/Down Arrows to move,";
                    l1p2 = "M to mark/unmark an entry";
                    l2p1 = "to count toward total time,";
                    l2p2 = "R to refresh,";
                    l3p1 = "Enter to leave Mark Mode";
                    break;
                case States.Off:
                    l1p1 = "P to Pause/Unpause,";
                    l1p2 = "R to refresh,";
                    l2p1 = "E to enter Edit Mode,";
                    l2p2 = "S to save,";
                    l3p1 = "Escape to Save/Exit";
                    l3p2 = "V to View Sessions";
                    break;
                case States.Save:
                    l1p1 = "Follow the";
                    l2p1 = "on-screen";
                    l3p1 = "instructions";
                    break;
                case States.View:
                    l1p1 = "Left/Right Arrows to view Sessions";
                    l1p2 = "Up/Down Arrows to scroll current session";
                    l2p1 = "Esc to return to previous screen";
                    break;
                case States.Watch:
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
                l1p1.PadRight(INSTRUCTIONS_PAD) + l1p2.PadRight(INSTRUCTIONS_PAD) + "\n" +
                l2p1.PadRight(INSTRUCTIONS_PAD) + l2p2.PadRight(INSTRUCTIONS_PAD) + "\n" +
                l3p1.PadRight(INSTRUCTIONS_PAD) + l3p2.PadRight(INSTRUCTIONS_PAD) + "\n" +
                "------------------------------------".PadRight(Console.BufferWidth));
            Console.SetCursorPosition(left, top);
        }

        /// <summary>
        /// Uses ListIndex to print a TimeEntry's mark pip.
        /// </summary>
        public static void PrintMark()
        {
            int left = Console.CursorLeft;
            int top = Console.CursorTop;
            Console.CursorLeft = MARK_LEFT;
            Console.ForegroundColor = currentSession.Times[ListIndex].marked ? MarkOnColor : MarkOffColor;
            Console.Write(MarkChar);
            Console.ForegroundColor = ConsoleColor.White;
            Console.SetCursorPosition(left, top);
        }

        /// <summary>
        /// Clears and prints the entire screen
        /// </summary>
        public static void PrintScreen()
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("TIME KEEPER");
            Console.ForegroundColor = ConsoleColor.White;
            BPrintInstructions = true;
            BPrintStatus = true;
            BPrintTotalTime = true;
            BPrintAllTimes = true;
            SetToBottom();
        }

        /// <summary>
        /// Updates the program status section of the screen
        /// </summary>
        public static void PrintStatus()
        {
            //Cursor: 2,8   
            int left = Console.CursorLeft;
            int top = Console.CursorTop;
            Console.SetCursorPosition(0, STATUS_TOP);
            Console.Write("Status: ");
            switch (ProgramState)
            {
                case States.Combine:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case States.Watch:
                case States.Mark:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case States.Edit:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case States.Comment:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case States.Save:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
                case States.View:
                case States.Off:
                case States.Exit:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }
            //Just enough to clear the longest state name
            Console.Write(ProgramState.ToString().PadRight(7));
            Console.ForegroundColor = ConsoleColor.White;

            Console.SetCursorPosition(left, top);
        }

        /// <summary>
        /// Uses ListIndex to print a TimeEntry to the screen
        /// </summary>
        public static void PrintTime()
        {
            int left = Console.CursorLeft;
            int top = Console.CursorTop;
            PrintMark();
            Console.CursorLeft = TIME_LEFT;
            string temp = "";
            if (currentSession.Times[ListIndex].comment.Length != 0)
                temp = currentSession.Times[listIndex].comment.ToString().Remove(currentSession.Times[ListIndex].comment.Length - 1);
#if DEBUG
            Console.WriteLine("{0}:{1}{2}",
                currentSession.Times[ListIndex].timeSpent.Minutes.ToString().PadLeft(2, '0'),
                currentSession.Times[ListIndex].timeSpent.Seconds.ToString().PadRight(TAB_PAD),
                temp.PadRight(COMMENT_PAD));
#else
            Console.WriteLine("{0}:{1}{2}",
                currentSession.Times[ListIndex].timeSpent.Hours.ToString().PadLeft(2, '0'),
                currentSession.Times[ListIndex].timeSpent.Minutes.ToString().PadRight(TAB_PAD),
                temp.PadRight(COMMENT_PAD));
#endif
            Console.SetCursorPosition(left, top);
        }

        /// <summary>
        /// Updates the Total Time section of the screen
        /// </summary>
        public static void PrintTotalTime()
        {
            TallyTime();
            //Cursor: 2,16
            int left = Console.CursorLeft;
            int top = Console.CursorTop;
            Console.CursorTop = STATUS_TOP;
            Console.CursorLeft = TOTAL_LEFT;

            //Console.Write("Total Time: {0}:{1} ", TotalTimeSpan.Hours, TotalTimeSpan.Minutes);
            Console.Write("Total Time: {0}".PadRight(INSTRUCTIONS_PAD), TotalTimeSpan.Duration());

            Console.SetCursorPosition(left, top);
        }

        /// <summary>
        /// Reads particular key inputs for UI controls
        /// </summary>
        public static void ReadKeys()
        {
            while (ProgramState != States.Exit)
            {
                switch (ProgramState)
                {
                    #region CKI Program States
                    case States.Off:
                    case States.Watch:
                    case States.Edit:
                    case States.Combine:
                    case States.View:
                    case States.Mark:
                        CKI = Console.ReadKey(true);
                        break;
                    case States.Comment:
                        CKI = Console.ReadKey();
                        switch (CKI.Key)
                        {
                            case ConsoleKey.Backspace:
                                if (currentSession.Times[ListIndex].comment.Length > 0)
                                {
                                    Console.Write(" ");
                                    Console.CursorLeft--;
                                    currentSession.Times[ListIndex].comment.Remove(currentSession.Times[ListIndex].comment.Length - 1, 1);
                                }
                                break;
                            default:
                                currentSession.Times[ListIndex].comment.Append(CKI.KeyChar);
                                break;
                        }
                        break;
                    #endregion
                }

                switch (CKI.Key)
                {
                    case ConsoleKey.Backspace:

                        break;
                    #region Delete
                    case ConsoleKey.Delete://Delete a TimeInfo
                        switch (ProgramState)
                            {
                                case States.View:
                                    SM.RemoveSession();
                                    break;
                            }
                        break;
                    #endregion
                    #region Enter
                    case ConsoleKey.Enter:
                        switch (ProgramState)
                        {
                            case States.Combine:
                                //TODO: Get combine marking working
                                CombineMarked();
                                ResetCombineIndices();
                                BPrintScreen = true;
                                ProgramState = PrevState;
                                break;
                            case States.Comment:
                                //finished writing/editing
                                ProgramState = States.Edit;
                                break;
                            case States.Edit:
                                if (ListIndex < currentSession.Times.Count && currentSession.Times.Count != 0)
                                {
                                    Console.CursorLeft = COMMENT_LEFT;
                                    //Remove the comment from the screen buffer
                                    string temp = string.Empty;
                                    if (currentSession.Times[ListIndex].comment.Length != 0)
                                        temp = currentSession.Times[ListIndex].comment.ToString().Remove(currentSession.Times[ListIndex].comment.Length - 1);
                                    Console.Write(" ".PadRight(temp.Length));
                                    Console.CursorLeft = COMMENT_LEFT;
                                    currentSession.Times[ListIndex].comment.Clear();

                                    //begin editing
                                    ProgramState = States.Comment;
                                }
                                break;
                            case States.Mark:
                                ProgramState = States.Edit;
                                break;
                            case States.Save:
                                ProgramState = States.Exit;
                                break;
                        }
                        break;
                    #endregion
                    #region Spacebar
                    case ConsoleKey.Spacebar:
                        switch (ProgramState)
                        {
                            case States.Watch:
                                AddNewTime();
                                ListIndex++;
                                BPrintTime = true;
                                BPrintTotalTime = true;
                                break;
                        }
                        break;
                    #endregion
                    #region Escape
                    case ConsoleKey.Escape://Close program
                        switch (ProgramState)
                        {
                            case States.Off:
                            case States.Watch:
                                //write out times/comments to file
                                if (currentSession.Times.Count > 0)
                                {
                                    ProgramState = States.Save;
                                    SetToBottom();
                                    LastLine = Console.CursorTop;
                                    BSaveTimes = true;
                                    PrevState = States.Exit;
                                }
                                else
                                {
                                    ProgramState = States.Exit;
                                }
                                break;
                            case States.Combine:
                                ProgramState = States.Edit;
                                break;
                            case States.View:
                                ProgramState = States.Off;                                
                                break;
                        }
                        break;
                    #endregion
                    #region C: Combine currentSession.Times
                    /*
                    case ConsoleKey.C:
                        switch (ProgramState)
                        {
                            case States.Edit:
                                if (currentSession.Times.Count > 0)
                                {
                                    ProgramState = States.Combine;
                                    ResetCombineIndices();
                                    PrintScreen();
                                    SetToBottom();
                                }
                                break;
                            case States.Combine:
                                if (ListIndex < currentSession.Times.Count && currentSession.Times.Count != 0)
                                {
                                    if (currentSession.Times[ListIndex].Combine ||
                                        (!currentSession.Times[CombineIndexFirst].Combine || !currentSession.Times[CombineIndexLast].Combine))
                                    {
                                        int Last = currentSession.Times.Count - 1;
                                        currentSession.Times[ListIndex].Combine = !currentSession.Times[ListIndex].Combine;
                                        if (currentSession.Times[ListIndex].Combine)
                                        {
                                            if (CombineIndexLast ==Last)
                                            {
                                                CombineIndexLast = ListIndex;
                                            }
                                            else if (CombineIndexFirst == Last)
                                            {
                                                CombineIndexFirst = ListIndex;
                                            }

                                        }
                                        else
                                        {
                                            if (!currentSession.Times[CombineIndexFirst].Combine)
                                            {
                                                CombineIndexFirst = currentSession.Times.Count - 1;
                                            }
                                            else if (!currentSession.Times[CombineIndexLast].Combine)
                                            {
                                                CombineIndexLast = currentSession.Times.Count - 1;
                                            }
                                        }

                                        //FindCombineEdges();
                                        PrintCombineMark();
                                        Console.CursorLeft = MARK_LEFT;
                                    }
                                }
                                break;
                        }
                        break;
                        */
                    #endregion
                    #region E: Edit
                    case ConsoleKey.E://Pause/edit 
                        switch (ProgramState)
                        {
                            case States.Off:
                                ProgramState = States.Edit;
                                PrevState = States.Off;
                                break;
                            case States.Watch:
                                ProgramState = States.Edit;
                                PrevState = States.Watch;
                                break;
                            case States.Edit:
                                ProgramState = PrevState;
                                SetToBottom();
                                break;
                        }
                        break;
                    #endregion
                    #region M: Marking
                    case ConsoleKey.M:
                        switch (ProgramState)
                        {
                            case States.Edit:
                                ProgramState = States.Mark;
                                break;
                            case States.Mark:
                                if (ListIndex < currentSession.Times.Count && currentSession.Times.Count != 0)
                                {
                                    currentSession.Times[ListIndex].marked = !currentSession.Times[ListIndex].marked;
                                    BPrintMark = true;
                                    Console.CursorLeft = MARK_LEFT;
                                    TallyTime();
                                    BPrintTotalTime = true;
                                }
                                break;
                        }
                        break;
                    #endregion
                    #region P: Pause/Unpause
                    case ConsoleKey.P:
                        switch (ProgramState)
                        {
                            case States.Off:
                                if (currentSession.Times.Count != 0)
                                    Timer.Start();
                                ProgramState = States.Watch;
                                break;
                            case States.Watch:
                                if (currentSession.Times.Count != 0)
                                    Timer.Stop();
                                ProgramState = States.Off;
                                break;
                        }
                        break;
                    #endregion
                    #region R: Refresh
                    case ConsoleKey.R:
                        switch (ProgramState)
                        {
                            case States.Off:
                            case States.Watch:
                            case States.Mark:
                            case States.Edit:
                                int left = Console.CursorLeft;
                                LastLine = Console.CursorTop;
                                int index = ListIndex;
                                BPrintScreen = true;
                                Console.SetCursorPosition(left, LastLine);
                                ListIndex = index;
                                break;
                        }
                        break;
                    #endregion
                    #region S: Save
                    case ConsoleKey.S:
                        switch (ProgramState)
                        {
                            case States.Off:
                            case States.Watch:
                                //write out times/comments to file
                                if (currentSession.Times.Count > 0)
                                {
                                    PrevState = ProgramState;
                                    ProgramState = States.Save;
                                    SetToBottom();
                                    LastLine = Console.CursorTop;
                                    BSaveTimes = true;
                                    SetToBottom();
                                    ProgramState = PrevState;
                                }
                                break;
                        }
                        break;
                    #endregion
                    #region V: View Sessions
                    case ConsoleKey.V:
                        switch (programState)
                        {
                            case States.Off:
                                PrevState = ProgramState;
                                ProgramState = States.View;
                                currentSession = SM.selectedSession();
                                BPrintAllTimes = true;
                                break;
                        }
                        break;
                    #endregion
                    #region Arrows
                    #region Up/Down
                    case ConsoleKey.UpArrow:
                        switch (ProgramState)
                        {
                            case States.Combine:
                            /*
                            if (ListIndex > CombineIndexFirst)
                            {
                                DecListAndTop();
                            }
                            break;
                            */
                            case States.Mark:
                            case States.Edit:
                                if (ListIndex > 0)
                                    DecListAndTop();
                                break;
                            case States.View:
                                break;
                        }
                        break;
                    case ConsoleKey.DownArrow:
                        switch (ProgramState)
                        {
                            case States.Combine:
                            /*
                            if (ListIndex < CombineIndexLast)
                            {
                                IncListAndTop();
                            }
                            break;
                            */
                            case States.Mark:
                            case States.Edit:
                                if (ListIndex < currentSession.Times.Count - 1)
                                    IncListAndTop();
                                break;
                            case States.View:
                                break;
                        }
                        break;
                    #endregion
                    #region Left/Right
                    case ConsoleKey.LeftArrow:
                        switch (ProgramState)
                        {
                            case States.Combine:
                            case States.Edit:
                                //Set Console.CursorLeft to specific values for the console
                                break;
                            case States.View:
                                currentSession = SM.PreviousSession();
                                BPrintAllTimes = true;
                                break;
                        }
                        break;
                    case ConsoleKey.RightArrow:
                        switch (ProgramState)
                        {
                            case States.Combine:
                            case States.Edit:
                                //Set Console.CursorLeft to specific values for the console
                                break;
                            case States.View:
                                currentSession = SM.NextSession();
                                BPrintAllTimes = true;
                                break;
                        }
                        break;
                    #endregion
                    #endregion
                }
            }
        }

        /// <summary>
        /// Sets the outer bounds of the the combine variables
        /// to each end of the list of TimeEntries
        /// </summary>
        private static void ResetCombineIndices()
        {
            CombineIndexFirst = 0;
            CombineIndexLast = currentSession.Times.Count - 1;
        }

        /// <summary>
        /// Stops the Timer and adds new TimeEntry
        /// </summary>
        private static void SaveTimes()
        {
            //Stop the timer
            Timer.Stop();
            bool ValidInput = true;

            #region Do you want to save your latest time?
            do
            {
                Console.CursorTop = LastLine + 1;
                Console.WriteLine("Do you want to save your latest time? (y/n)".PadRight(Console.BufferWidth - 1));
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

                switch (CKI.Key)
                {
                    case ConsoleKey.Y:
                        currentSession.Times[currentSession.Times.Count - 1].ended = DateTime.Now;
                        currentSession.Times[currentSession.Times.Count - 1].timeSpent = Timer.Elapsed;
                        ListIndex++;
                        ValidInput = true;
                        break;
                    case ConsoleKey.N:
                        currentSession.Times.RemoveAt(currentSession.Times.Count - 1);
                        ValidInput = true;
                        break;
                    default:
                        ValidInput = false;
                        break;
                }
            } while (!ValidInput);
            #endregion
            PrintScreen();
            #region Do you want save the time sheet?
            do
            {
                Console.CursorTop = LastLine + 1;
                Console.WriteLine("Do you want to save? (y/n)".PadRight(Console.BufferWidth - 1));
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


                switch (CKI.Key)
                {
                    case ConsoleKey.Y:
                        ValidInput = true;
                        SM.Add(currentSession);
                        SM.SaveSessions(FILENAME);
                        break;
                    case ConsoleKey.N:
                        ValidInput = true;
                        if (PrevState == States.Exit)
                            ProgramState = States.Exit;
                        return;
                    default:
                        ValidInput = false;
                        break;
                }
            } while (!ValidInput);
            #endregion
            if (PrevState == States.Exit)
                ProgramState = States.Exit;
        }

        /// <summary>
        /// Sets the Cursor's Top location to the last line in the console.
        /// </summary>
        public static void SetToBottom()
        {
            ListIndex = currentSession.Times.Count - 1;
        }

        /// <summary>
        /// Takes quick inventory of the total time spent so far.
        /// Useful when deleting TimeEntries.
        /// </summary>
        public static void TallyTime()
        {
            if (currentSession.Times.Count != 0)
            {
                TotalTimeSpan = TimeSpan.Zero;
                foreach (TimeEntry TI in currentSession.Times)
                {
                    if (TI.marked)
                        TotalTimeSpan += TI.timeSpent;
                }
            }
        }

        /// <summary>
        /// Watches the day and the Timer StopWatch
        /// </summary>
        public static void ClockAndPrintWatcher()
        {
            while (ProgramState != States.Exit)
            {
                WatchForNewDay();
                WatchTimer();
                WatchForPrintTasks();
            }
        }

        /// <summary>
        /// Keeps track of the change of day, and adds a new TimeEntry when it happens
        /// </summary>
        public static void WatchForNewDay()
        {
            string newDay = "New Day!";
            Now = DateTime.Now;
#if DEBUG
            if (Now.Minute != Previous.Minute)
#else
            if (Now.Day != Previous.Day)
#endif
            {
                newDayOccurred = true;
                AddNewTime();
                PrintTotalTime();
                int length = currentSession.Times.Count - 1;//Last TimeEntry
                if (length > 0)
                {
                    //Set a char array equal to the length of the second-to-last time entry's comment +
                    //the length of "new Day!" + 1 for a space
                    char[] temp = new char[currentSession.Times[length - 1].comment.Length + newDay.Length + 1];
                    //Copy New Day! to the beginning registers of temp
                    newDay.CopyTo(0, temp, 0, newDay.Length);
                    //The register after "New Day!" should be a space: ' '
                    temp[newDay.Length] = ' ';
                    //Then, copy the full length of the second-to-last index
                    currentSession.Times[length - 1].comment.CopyTo(0, temp, newDay.Length + 1, currentSession.Times[length - 1].comment.Length);
                    //Finally, add the entire comment to the newest time entry
                    currentSession.Times[length].comment.Append(temp);
                }
            }
            if (newDayOccurred && ProgramState == States.Watch)
            {
                BPrintTime = true;
                newDayOccurred = false;
                SetToBottom();
            }
            Previous = Now;
        }

        /// <summary>
        /// Checks printing booleans set to true and does
        /// those tasks
        /// </summary>
        public static void WatchForPrintTasks()
        {
            if (PrintQueue.Count != 0)
            {
                MethodInvoker temp = PrintQueue.Dequeue();
                temp.Invoke();
                switch (programState)
                {
                    case States.Comment:
                        break;
                    default:
                        SetToBottom();
                        break;
                }
            }
        }

        /// <summary>
        /// Checks for time changes on the Timer StopWatch,
        /// and prints the time accordingly
        /// </summary>
        public static void WatchTimer()
        {
            curTime = Timer.Elapsed;
#if DEBUG
            if (prevTime.Seconds != curTime.Seconds)
#else
            if (prevTime.Minutes != curTime.Minutes)
#endif
                TimerChanged = true;
            switch (programState)
            {
                case States.Watch:
                    if (TimerChanged)
                    {
                        listIndex = currentSession.Times.Count - 1;
                        currentSession.Times[currentSession.Times.Count - 1].timeSpent = Timer.Elapsed;
                        SetToBottom();
                        BPrintTime = true;
                        if (currentSession.Times[currentSession.Times.Count - 1].marked)
                            BPrintTotalTime = true;
                        TimerChanged = false;
                    }
                    break;
            }
            prevTime = curTime;
        }
    }
}
