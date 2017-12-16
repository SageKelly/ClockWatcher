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

        static SessionManager SM;
        static int listIndex = 0;
        static TimeSpan curTime = TimeSpan.Zero, prevTime = curTime;
        /// <summary>
        /// Represents if the Timer's minutes or hours changed
        /// </summary>
        static bool TimerChanged;
        static bool newDayOccurred = false;
        /// <summary>
        /// Represents the currently selected in the SM.currentSession.Times list.
        /// In this program, it assumes the newest index before it exists.
        /// Therefore, if SM.currentSession.Times has 3 entries, ListIndex, at that point would equal 3.
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
                Printer.SetCursorToTimeEntry(value);
            }
        }

        const string FILENAME = "Sessions.bin";

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

            CKI = new ConsoleKeyInfo();

            SM = new SessionManager();
            SM.LoadSessions(FILENAME);

            SM.Add(new Session());
            SM.CurrentSession = SM.SelectedSession();

            ListIndex = SM.CurrentSession.Times.Count - 1;
            ResetCombineIndices();

            ///Record the day this program started
            SM.CurrentSession.BeginSession();

            Timer = new Stopwatch();

            Printer.SetupPrinter(SM);
            Printer.QueuePrintScreen();
            ApplicationManager.ProgramState = ApplicationManager.States.Off;
            DayWatcher.Start();
            InputWatcher.Start();

        }

        /// <summary>
        /// Sets the ListIndex to the last TimeEntry in the Session.
        /// </summary>
        public static void SetToLast()
        {
            ListIndex = SM.CurrentSession.Times.Count - 1;
        }

        /// <summary>
        /// Adds a new TimeInfo instance to the list of TimeInfo objects.
        /// </summary>
        public static void AddNewTime()
        {
            if (SM.CurrentSession.Times.Count > 0)
            {
                Timer.Stop();
                SM.CurrentSession.Times[SM.CurrentSession.Times.Count - 1].ended = DateTime.Now;
                SM.CurrentSession.Times[SM.CurrentSession.Times.Count - 1].timeSpent = Timer.Elapsed;
                SM.CurrentSession.Times.Add(new TimeEntry(DateTime.Now));
                Timer.Restart();
            }
            else
            {
                SM.CurrentSession.Times.Add(new TimeEntry(DateTime.Now));
                Timer.Start();
            }
            Printer.QueuePrintTotalTime();
        }

        /// <summary>
        /// Combines all TimeEntries marked for combining and keeps the first-most's comment
        /// </summary>
        public static void CombineMarked()
        {
            TimeEntry mainEntry;
            int CombineIndex;
            if (CombineIndexFirst == 0 && SM.CurrentSession.Times[CombineIndexFirst].combine)
            {
                mainEntry = SM.CurrentSession.Times[CombineIndexFirst];
            }
            else
            {
                mainEntry = SM.CurrentSession.Times[CombineIndexFirst + 1];
            }

            if (CombineIndexLast == SM.CurrentSession.Times.Count - 1 && SM.CurrentSession.Times[CombineIndexLast].combine)
            {
                CombineIndex = CombineIndexLast;
            }
            else
            {
                CombineIndex = CombineIndexLast - 1;
            }
            while (SM.CurrentSession.Times[CombineIndex].combine && CombineIndex != SM.CurrentSession.Times.IndexOf(mainEntry))
            {
                mainEntry.timeSpent += SM.CurrentSession.Times[CombineIndex].timeSpent;
                SM.CurrentSession.Times.RemoveAt(CombineIndex);
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
            if (ListIndex < SM.CurrentSession.Times.Count && SM.CurrentSession.Times.Count != 0)
            {
                //Clear the last line off the buffer
                int temp = ListIndex;
                SetToLast();
                DecListAndTop();
                Console.Write("".PadRight(Console.BufferWidth - 1));
                ListIndex = temp;
                Console.CursorLeft = 0;
                Timer.Stop();
                SM.CurrentSession.Times.RemoveAt(ListIndex);
                Timer.Restart();

                if (ListIndex == SM.CurrentSession.Times.Count && ListIndex > 0)//If you deleted the last TimeEntry
                    DecListAndTop();
            }
        }

        /// <summary>
        /// Find the first-most and last-most possible TimeEntries can be current combined.
        /// </summary>
        public static void FindCombineEdges()
        {
            if (ListIndex != 0 && !SM.CurrentSession.Times[ListIndex - 1].combine)
            {
                CombineIndexFirst = ListIndex - 1;
            }
            else
            {
                while (!SM.CurrentSession.Times[CombineIndexFirst].combine)
                {
                    CombineIndexFirst++;
                }
            }
            if (ListIndex < SM.CurrentSession.Times.Count - 1 && !SM.CurrentSession.Times[ListIndex + 1].combine)
            {
                CombineIndexLast = ListIndex + 1;
            }
            else
            {
                while (!SM.CurrentSession.Times[CombineIndexLast].combine)
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
            if (!SM.CurrentSession.Times[ListIndex].combine)
            {
                //If not at 0, and above location not combine
                if (ListIndex - 1 > -1 && !SM.CurrentSession.Times[ListIndex - 1].combine)
                {
                    //If not at end and location below not combine
                    if (ListIndex + 1 < SM.CurrentSession.Times.Count - 1 && !SM.CurrentSession.Times[ListIndex + 1].combine)
                    {
                        return true;
                    }
                }
                //If you made it here, then there was only one entry
                else if (SM.CurrentSession.Times.Count == 1)
                {
                    return true;
                }
            }
            return false;
        }



        /// <summary>
        /// Reads particular key inputs for UI controls
        /// </summary>
        public static void ReadKeys()
        {
            while (ApplicationManager.ProgramState != ApplicationManager.States.Exit)
            {
                switch (ApplicationManager.ProgramState)
                {
                    #region CKI Program States
                    case ApplicationManager.States.Off:
                    case ApplicationManager.States.Watch:
                    case ApplicationManager.States.Edit:
                    case ApplicationManager.States.Combine:
                    case ApplicationManager.States.View:
                    case ApplicationManager.States.Mark:
                        CKI = Console.ReadKey(true);
                        break;
                    case ApplicationManager.States.Comment:
                        CKI = Console.ReadKey();
                        switch (CKI.Key)
                        {
                            case ConsoleKey.Backspace:
                                if (SM.CurrentSession.Times[ListIndex].comment.Length > 0)
                                {
                                    Console.Write(" ");
                                    Console.CursorLeft--;
                                    SM.CurrentSession.Times[ListIndex].comment.Remove(SM.CurrentSession.Times[ListIndex].comment.Length - 1, 1);
                                }
                                break;
                            default:
                                SM.CurrentSession.Times[ListIndex].comment.Append(CKI.KeyChar);
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
                        switch (ApplicationManager.ProgramState)
                        {
                            case ApplicationManager.States.View:
                                SM.RemoveSession();
                                break;
                        }
                        break;
                    #endregion
                    #region Enter
                    case ConsoleKey.Enter:
                        switch (ApplicationManager.ProgramState)
                        {
                            case ApplicationManager.States.Combine:
                                //TODO: Get combine marking working
                                CombineMarked();
                                ResetCombineIndices();
                                Printer.QueuePrintScreen();
                                ApplicationManager.ProgramState = ApplicationManager.PrevState;
                                break;
                            case ApplicationManager.States.Comment:
                                //finished writing/editing
                                ApplicationManager.ProgramState = ApplicationManager.States.Edit;
                                break;
                            case ApplicationManager.States.Edit:
                                if (ListIndex < SM.CurrentSession.Times.Count && SM.CurrentSession.Times.Count != 0)
                                {
                                    Console.CursorLeft = Printer.COMMENT_POINT.Left;
                                    //Remove the comment from the screen buffer
                                    string temp = string.Empty;
                                    if (SM.CurrentSession.Times[ListIndex].comment.Length != 0)
                                        temp = SM.CurrentSession.Times[ListIndex].comment.ToString().Remove(SM.CurrentSession.Times[ListIndex].comment.Length - 1);
                                    Console.Write(" ".PadRight(temp.Length));
                                    Console.CursorLeft = Printer.COMMENT_POINT.Left;
                                    SM.CurrentSession.Times[ListIndex].comment.Clear();

                                    //begin editing
                                    ApplicationManager.ProgramState = ApplicationManager.States.Comment;
                                }
                                break;
                            case ApplicationManager.States.Mark:
                                ApplicationManager.ProgramState = ApplicationManager.States.Edit;
                                break;
                            case ApplicationManager.States.Save:
                                ApplicationManager.ProgramState = ApplicationManager.States.Exit;
                                break;
                        }
                        break;
                    #endregion
                    #region Spacebar
                    case ConsoleKey.Spacebar:
                        switch (ApplicationManager.ProgramState)
                        {
                            case ApplicationManager.States.Watch:
                                AddNewTime();
                                ListIndex++;
                                Printer.QueuePrintTimeEntry(ListIndex);
                                Printer.QueuePrintTotalTime();
                                break;
                        }
                        break;
                    #endregion
                    #region Escape
                    case ConsoleKey.Escape://Close program
                        switch (ApplicationManager.ProgramState)
                        {
                            case ApplicationManager.States.Off:
                            case ApplicationManager.States.Watch:
                                //write out times/comments to file
                                if (SM.CurrentSession.Times.Count > 0)
                                {
                                    ApplicationManager.ProgramState = ApplicationManager.States.SaveAndExit;
                                    SetToLast();
                                    SaveTimes();
                                }
                                else
                                {
                                    ApplicationManager.ProgramState = ApplicationManager.States.Exit;
                                }
                                break;
                            case ApplicationManager.States.Combine:
                                ApplicationManager.ProgramState = ApplicationManager.States.Edit;
                                break;
                            case ApplicationManager.States.View:
                                ApplicationManager.ProgramState = ApplicationManager.States.Off;
                                break;
                        }
                        break;
                    #endregion
                    #region C: Combine SM.currentSession.Times
                    /*
                    case ConsoleKey.C:
                        switch (ApplicationManager.ProgramState)
                        {
                            case ApplicationManager.States.Edit:
                                if (SM.currentSession.Times.Count > 0)
                                {
                                    ApplicationManager.ProgramState = ApplicationManager.States.Combine;
                                    ResetCombineIndices();
                                    PrintScreen();
                                    SetToBottom();
                                }
                                break;
                            case ApplicationManager.States.Combine:
                                if (ListIndex < SM.currentSession.Times.Count && SM.currentSession.Times.Count != 0)
                                {
                                    if (SM.currentSession.Times[ListIndex].Combine ||
                                        (!SM.currentSession.Times[CombineIndexFirst].Combine || !SM.currentSession.Times[CombineIndexLast].Combine))
                                    {
                                        int Last = SM.currentSession.Times.Count - 1;
                                        SM.currentSession.Times[ListIndex].Combine = !SM.currentSession.Times[ListIndex].Combine;
                                        if (SM.currentSession.Times[ListIndex].Combine)
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
                                            if (!SM.currentSession.Times[CombineIndexFirst].Combine)
                                            {
                                                CombineIndexFirst = SM.currentSession.Times.Count - 1;
                                            }
                                            else if (!SM.currentSession.Times[CombineIndexLast].Combine)
                                            {
                                                CombineIndexLast = SM.currentSession.Times.Count - 1;
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
                        switch (ApplicationManager.ProgramState)
                        {
                            case ApplicationManager.States.Off:
                                ApplicationManager.ProgramState = ApplicationManager.States.Edit;
                                break;
                            case ApplicationManager.States.Watch:
                                ApplicationManager.ProgramState = ApplicationManager.States.Edit;
                                break;
                            case ApplicationManager.States.Edit:
                                ApplicationManager.ProgramState = ApplicationManager.PrevState;
                                SetToLast();
                                break;
                        }
                        break;
                    #endregion
                    #region M: Marking
                    case ConsoleKey.M:
                        switch (ApplicationManager.ProgramState)
                        {
                            case ApplicationManager.States.Edit:
                                ApplicationManager.ProgramState = ApplicationManager.States.Mark;
                                break;
                            case ApplicationManager.States.Mark:
                                if (ListIndex < SM.CurrentSession.Times.Count && SM.CurrentSession.Times.Count != 0)
                                {
                                    SM.CurrentSession.Times[ListIndex].marked = !SM.CurrentSession.Times[ListIndex].marked;
                                    Printer.QueuePrintMark(ListIndex);
                                    Console.CursorLeft = Printer.MARK_LEFT;
                                    Printer.QueuePrintTotalTime();
                                }
                                break;
                        }
                        break;
                    #endregion
                    #region P: Pause/Unpause
                    case ConsoleKey.P:
                        switch (ApplicationManager.ProgramState)
                        {
                            case ApplicationManager.States.Off:
                                if (SM.CurrentSession.Times.Count != 0)
                                    Timer.Start();
                                ApplicationManager.ProgramState = ApplicationManager.States.Watch;
                                break;
                            case ApplicationManager.States.Watch:
                                if (SM.CurrentSession.Times.Count != 0)
                                    Timer.Stop();
                                ApplicationManager.ProgramState = ApplicationManager.States.Off;
                                break;
                        }
                        break;
                    #endregion
                    #region R: Refresh
                    case ConsoleKey.R:
                        switch (ApplicationManager.ProgramState)
                        {
                            case ApplicationManager.States.Off:
                            case ApplicationManager.States.Watch:
                            case ApplicationManager.States.Mark:
                            case ApplicationManager.States.Edit:
                                int index = ListIndex;
                                Printer.QueuePrintScreen();
                                ListIndex = index;
                                break;
                        }
                        break;
                    #endregion
                    #region S: Save
                    case ConsoleKey.S:
                        switch (ApplicationManager.ProgramState)
                        {
                            case ApplicationManager.States.Off:
                            case ApplicationManager.States.Watch:
                                //write out times/comments to file
                                if (SM.CurrentSession.Times.Count > 0)
                                {
                                    ApplicationManager.ProgramState = ApplicationManager.States.Save;
                                    SaveTimes();
                                    ApplicationManager.ProgramState = ApplicationManager.PrevState;
                                }
                                break;
                        }
                        break;
                    #endregion
                    #region V: View Sessions
                    case ConsoleKey.V:
                        switch (ApplicationManager.ProgramState)
                        {
                            case ApplicationManager.States.Off:
                                ApplicationManager.ProgramState = ApplicationManager.States.View;
                                SM.CurrentSession = SM.SelectedSession();
                                Printer.QueuePrintAllTimes();
                                break;
                        }
                        break;
                    #endregion
                    #region Arrows
                    #region Up/Down
                    case ConsoleKey.UpArrow:
                        switch (ApplicationManager.ProgramState)
                        {
                            case ApplicationManager.States.Combine:
                            /*
                            if (ListIndex > CombineIndexFirst)
                            {
                                DecListAndTop();
                            }
                            break;
                            */
                            case ApplicationManager.States.Mark:
                            case ApplicationManager.States.Edit:
                                if (ListIndex > 0)
                                    DecListAndTop();
                                break;
                            case ApplicationManager.States.View:
                                break;
                        }
                        break;
                    case ConsoleKey.DownArrow:
                        switch (ApplicationManager.ProgramState)
                        {
                            case ApplicationManager.States.Combine:
                            /*
                            if (ListIndex < CombineIndexLast)
                            {
                                IncListAndTop();
                            }
                            break;
                            */
                            case ApplicationManager.States.Mark:
                            case ApplicationManager.States.Edit:
                                if (ListIndex < SM.CurrentSession.Times.Count - 1)
                                    IncListAndTop();
                                break;
                            case ApplicationManager.States.View:
                                break;
                        }
                        break;
                    #endregion
                    #region Left/Right
                    case ConsoleKey.LeftArrow:
                        switch (ApplicationManager.ProgramState)
                        {
                            case ApplicationManager.States.Combine:
                            case ApplicationManager.States.Edit:
                                //Set Console.CursorLeft to specific values for the console
                                break;
                            case ApplicationManager.States.View:
                                SM.CurrentSession = SM.PreviousSession();
                                Printer.QueuePrintAllTimes();
                                break;
                        }
                        break;
                    case ConsoleKey.RightArrow:
                        switch (ApplicationManager.ProgramState)
                        {
                            case ApplicationManager.States.Combine:
                            case ApplicationManager.States.Edit:
                                //Set Console.CursorLeft to specific values for the console
                                break;
                            case ApplicationManager.States.View:
                                SM.CurrentSession = SM.NextSession();
                                Printer.QueuePrintAllTimes();
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
            CombineIndexLast = SM.CurrentSession.Times.Count - 1;
        }

        /// <summary>
        /// Stops the Timer and adds new TimeEntry
        /// </summary>
        private static void SaveTimes()
        {
            //Stop the timer
            Timer.Stop();

            //Do you want to save your latest time?
            Printer.QueuePromptUser("Do you want to save your latest time? (y/n)", CheckForValidSaveInput, new Action<ConsoleKey>(delegate (ConsoleKey CK)
            {
                switch (CK)
                {
                    case ConsoleKey.Y:
                        SM.CurrentSession.Times[SM.CurrentSession.Times.Count - 1].ended = DateTime.Now;
                        SM.CurrentSession.Times[SM.CurrentSession.Times.Count - 1].timeSpent = Timer.Elapsed;
                        ListIndex++;
                        break;
                    case ConsoleKey.N:
                        SM.CurrentSession.Times.RemoveAt(SM.CurrentSession.Times.Count - 1);
                        break;
                    default:
                        break;
                }
            }));


            Printer.QueuePrintScreen().PrintCompleteEvent += new PrintTask.PrintCompleteEventHandler(delegate ()
            {
                //Do you want save the time sheet?
                Printer.QueuePromptUser("Do you want to save? (y/n)", CheckForValidSaveInput, new Action<ConsoleKey>(delegate (ConsoleKey CK)
                {
                    switch (CK)
                    {
                        case ConsoleKey.Y:
                            SM.Add(SM.CurrentSession);
                            SM.SaveSessions(FILENAME);
                            break;
                        default:
                            break;
                    }

                    if (ApplicationManager.PrevState == ApplicationManager.States.SaveAndExit)
                        ApplicationManager.ProgramState = ApplicationManager.States.Exit;
                }));
            });
        }

        public static bool CheckForValidSaveInput(ConsoleKey CK)
        {
            bool ValidInput = true;
            switch (CKI.Key)
            {
                case ConsoleKey.Y:
                case ConsoleKey.N:
                    break;
                default:
                    ValidInput = false;
                    break;
            }
            return ValidInput;
        }



        /// <summary>
        /// Watches the day and the Timer StopWatch
        /// </summary>
        public static void ClockAndPrintWatcher()
        {
            while (ApplicationManager.ProgramState != ApplicationManager.States.Exit)
            {
                WatchForNewDay();
                WatchTimer();
                Printer.WatchForPrintTasks();
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
                Printer.QueuePrintTotalTime();
                int length = SM.CurrentSession.Times.Count - 1;//Last TimeEntry
                if (length > 0)
                {
                    //Set a char array equal to the length of the second-to-last time entry's comment +
                    //the length of "new Day!" + 1 for a space
                    char[] temp = new char[SM.CurrentSession.Times[length - 1].comment.Length + newDay.Length + 1];
                    //Copy New Day! to the beginning registers of temp
                    newDay.CopyTo(0, temp, 0, newDay.Length);
                    //The register after "New Day!" should be a space: ' '
                    temp[newDay.Length] = ' ';
                    //Then, copy the full length of the second-to-last index
                    SM.CurrentSession.Times[length - 1].comment.CopyTo(0, temp, newDay.Length + 1, SM.CurrentSession.Times[length - 1].comment.Length);
                    //Finally, add the entire comment to the newest time entry
                    SM.CurrentSession.Times[length].comment.Append(temp);
                }
            }
            if (newDayOccurred && ApplicationManager.ProgramState == ApplicationManager.States.Watch)
            {
                Printer.QueuePrintTimeEntry(ListIndex);
                newDayOccurred = false;
                SetToLast();
            }
            Previous = Now;
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
            switch (ApplicationManager.ProgramState)
            {
                case ApplicationManager.States.Watch:
                    if (TimerChanged)
                    {
                        listIndex = SM.CurrentSession.Times.Count - 1;
                        SM.CurrentSession.Times[SM.CurrentSession.Times.Count - 1].timeSpent = Timer.Elapsed;
                        SetToLast();
                        Printer.QueuePrintTimeEntry(listIndex);
                        if (SM.CurrentSession.Times[SM.CurrentSession.Times.Count - 1].marked)
                            Printer.QueuePrintTotalTime();
                        TimerChanged = false;
                    }
                    break;
            }
            prevTime = curTime;
        }
    }
}
