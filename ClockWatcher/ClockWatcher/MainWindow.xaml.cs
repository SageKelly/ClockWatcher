using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls.Primitives;
using System.Xml.Serialization;
using System.IO;
using System.Windows.Media.Animation;

namespace ClockWatcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Represents, during selection mode, which TimeEntry is currently selected.
        /// </summary>
        private TimeEntryData currentSelectedEntry;

        private int selectionIndex;
        public string defaultComment { get; private set; }

        private bool _isSelecting;
        private Binding binding;
        private TextBox _currentTextbox;

        public SessionManager SM { get; private set; }

        /// <summary>
        /// Represents a session that was started earlier today but ended.
        /// </summary>
        private string oldSession;
        private bool isDeleting;
        private bool isContinuing;

        public MainWindow()
        {
            SM = new SessionManager();
            SM.NewDayEvent += SplitSession;
            InitializeComponent();
            LoadStacks();
            AddSessionStamp();
            //Deserialize();
            defaultComment = "Add New Comment";
            commentAddingBox.Text = defaultComment;
            _isSelecting = false;
            DataContext = SM;
            binding = new Binding();

            binding.Source = SM.CommentLibrary;
            B_OpenSession.Visibility = Visibility.Hidden;
            isDeleting = isContinuing = false;
            CheckForRecentSession();
            //entryAdder_Click(null, null);
        }

        private void SplitSession(object sender, EventArgs ea)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                bool prevEntry = SM.SplitSession();
                if (!prevEntry)
                {
                    TextBlock temp = (TextBlock)SM.Entries.Last(x => x.GetType() == typeof(TextBlock));
                    SM.Entries.Remove(temp);
                    timeStack.Children.Remove(temp);
                }
                AddSessionStamp();
                if (prevEntry)
                {
                    TimeEntry t = (TimeEntry)SM.Entries.LastOrDefault(x => x.GetType() == typeof(TimeEntry));
                    string s = t == null ? "New Day" : t.Data.Comment;
                    entryAdder_Click(null, null);

                    TimeEntry temp = (TimeEntry)SM.Entries.Last(x => x.GetType() == typeof(TimeEntry));
                    temp.Data.Comment = s;
                }
                this.Focus();
            }));
        }


        #region Methods
        #region Event Methods
        #region commentAddBox
        /// <summary>
        /// Registered to MainWindow.commentAddingButton.Click Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void commentAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (commentAddingBox.Text != defaultComment && commentAddingBox.Text != "")
            {
                addCommentEntry(commentAddingBox.Text);

            }
        }
        /// <summary>
        /// Registered to MainWindow.commentAddingBox.GotFocus Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void commentAddingBox_GotFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox).Text = "";
        }
        /// <summary>
        /// Registered to MainWindow.commentAddingBox.LostFocus Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void commentAddingBox_LostFocus(object sender, RoutedEventArgs e)
        {
            commentAddingBox.Text = defaultComment;
        }
        /// <summary>
        /// Registered to SessionManager.commentEntryDeletedEvent Event
        /// </summary>
        /// <param name="deletionIndex"></param>
        private void commentEntry_delete(object sender, RoutedEventArgs rea)
        {
            commentEntry sentinel = (commentEntry)sender;
            commentStack.Children.Remove(sentinel);
            SM.deleteComment(sentinel);
        }
        #endregion

        /// <summary>
        /// Registered to TimeEntry.textChangedEvent Event
        /// (see entryAdder_Click())
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void filterIntelList(object sender, TextChangedEventArgs e)
        {
            _currentTextbox = sender as TextBox;

            intelListBox.SetBinding(ListBox.ItemsSourceProperty, binding);

            ICollectionView view = CollectionViewSource.GetDefaultView(SM.CommentLibrary);
            view.Filter =
                //null;
            (o) =>
            {
                //filter out all entries that neither start with nor contain the sentinel's comment
                if (_currentTextbox.Text == string.Empty)
                {
                    return (o as string) != string.Empty;
                }
                else
                {
                    return (o as string).Contains(_currentTextbox.Text);
                }
            };
            /*
            */
            if (_currentTextbox.IsFocused)
            {
                intelPopup.Placement = PlacementMode.Left;
                intelPopup.PlacementTarget = _currentTextbox;
                if (view.IsEmpty)
                {
                    intelPopup.IsOpen = false;
                }
                else intelPopup.IsOpen = true;
            }
        }

        /// <summary>
        /// Registered to MainWindow.entryadderButton.Click Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void entryAdder_Click(object sender, RoutedEventArgs e)
        {
            //Subscribe to the assorted events
            TimeEntry newTE = SM.addNewTimeEntry();
            //Subscribe to the assorted events
            RegisterToTimeEntry(newTE);
            timeStack.Children.Add(newTE);
        }

        void deleteTimeEntry(object sender)
        {
            TimeEntry sentinel = (TimeEntry)sender;
            SM.deleteTimeEntry(sentinel);
            timeStack.Children.Remove(sentinel);
        }
        /// <summary>
        /// Registered to MainWindow.intelPopup.intelListBox.SelectionChanged Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void intelListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox temp = sender as ListBox;
            if (temp.SelectedItem != null)
                _currentTextbox.Text = (string)temp.SelectedItem;

            temp.SelectedItem = null;
            intelPopup.IsOpen = false;
        }

        /// <summary>
        /// Registered to commentEntry.checkedEvent Event:
        /// triggers when the isChecked status of any commentEntry changes
        /// (see newCommentEntry())
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="rea"></param>
        public void filterTimeEntries(object sender, RoutedEventArgs rea)
        {
            /*
             * IsMarkedForView is used internally to check to see if something
             * should be messed with by the checking of another commentEntry.
             * However, isCollapsed should have the highest priority of
             * accuracy, for even if something is not marked for view, if none
             * of the commentEntries are checked, all TimeEntries should still
             * be visible.
             */
            commentEntry sentinel = sender as commentEntry;
            bool isOneChecked = sentinel.isChecked;
            //first check to see if there are other comments checked
            if (!sentinel.isChecked)
            {
                foreach (commentEntry ce in commentStack.Children)
                {
                    if (ce != sentinel && ce.isChecked)
                    {
                        isOneChecked = true;
                        break;
                    }
                }
            }
            foreach (UIElement uie in timeStack.Children)
            {
                if (uie.GetType() == typeof(TimeEntry))
                {
                    TimeEntry temp = (TimeEntry)uie;
                    if (temp.Data.Comment == sentinel.comment)
                    {
                        temp.isMarkedForView = sentinel.isChecked;
                    }
                }
            }
            if (isOneChecked)
            {
                //Do basic filtering

                foreach (UIElement uie in timeStack.Children)
                {
                    if (uie.GetType() == typeof(TimeEntry))
                    {
                        TimeEntry temp = (TimeEntry)uie;
                        /*
                         * If the comments don't match and this entry wasn't
                         * tagged by another checked commentEntry to stay visibile...
                        */
                        if (temp.Data.Comment != sentinel.comment && !temp.isMarkedForView)
                        {
                            //...then close it.
                            temp.isCollapsed = true;
                        }
                        else
                        {
                            //if the comments match...
                            if (temp.Data.Comment == sentinel.comment)
                            {
                                /*
                                 * have the TimeEntry collapse or expand if necessary
                                */
                                temp.isCollapsed = !sentinel.isChecked;
                            }
                        }
                    }
                }
            }
            else
            {
                //set all timeEntries' collapsed variables to false
                foreach (UIElement uie in timeStack.Children)
                {
                    if (uie.GetType() == typeof(TimeEntry))
                    {
                        TimeEntry temp = (TimeEntry)uie;
                        temp.isCollapsed = false;
                        temp.isMarkedForView = false;
                    }
                }
            }
            SM.CalculateFilteredTimeSpent();
        }

        /// <summary>
        /// Registered to MainWindow.KeysDown Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="kea"></param>
        private void KeysDown(object sender, KeyEventArgs kea)
        {
            switch (kea.Key)
            {
                case Key.Enter:
                    if (commentAddingBox.IsFocused)
                    {
                        //adds a comment Entry to the comment library
                        if (commentAddingBox.Text != defaultComment && commentAddingBox.Text != "")
                            SM.newCommentEntry(commentAddingBox.Text);
                        this.Focus();
                    }
                    if (_currentTextbox != null && !_currentTextbox.IsFocused)
                    {
                        intelPopup.IsOpen = false;
                    }
                    if (_isSelecting)
                    {

                    }
                    break;
                case Key.Escape:
                    this.Close();
                    break;
                case Key.Space:
                    if (!_isSelecting)
                    {
                        entryAdder_Click(null, null);
                    }
                    break;
                /*
            case Key.E:
                    
                if (!_isSelecting && currentSession.timeEntries.Count != 0)
                {
                    selectionIndex = 0;
                    currentSelectedEntry = currentSession.timeEntries[selectionIndex];
                    currentSelectedEntry.isSelected = true;
                    _isSelecting = true;
                }
                else if (_isSelecting)
                {
                    currentSelectedEntry.isSelected = false;
                    _isSelecting = false;
                }
                break;
                */
                case Key.Up:
                    if (_isSelecting)
                    {
                        if (selectionIndex == 0)
                            selectionIndex = SM.CurrentSession.TimeEntries.Count - 1;
                        else
                            selectionIndex--;
                        currentSelectedEntry.Owner.IsSelected = false;
                        currentSelectedEntry = SM.CurrentSession.TimeEntries[selectionIndex];
                        currentSelectedEntry.Owner.IsSelected = true;
                        //currentSelectedEntry.Focus();//this works for forcing scroll
                    }
                    break;
                case Key.Down:
                    if (_isSelecting)
                    {
                        selectionIndex = (selectionIndex + 1) % SM.CurrentSession.TimeEntries.Count;
                        currentSelectedEntry.Owner.IsSelected = false;
                        currentSelectedEntry = SM.CurrentSession.TimeEntries[selectionIndex];
                        currentSelectedEntry.Owner.IsSelected = true;
                    }
                    break;
            }
        }

        /// <summary>
        /// Registered to the Window.Closing event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveSession(object sender, CancelEventArgs e)
        {

            SM.SaveSession();
        }

        /// <summary>
        /// Registered to the B_ViewSession.Click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfirmSelection(object sender, RoutedEventArgs e)
        {
            uncheckView();
            //Then load new Session
            SM.LoadSession(OldSessions.SelectedItems);

            LoadStacks();
            B_OpenSession.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Registered to B_OpenSession.Click event
        /// Loads list of old sessions and presents them onscreen.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowOldSessions(object sender, RoutedEventArgs e)
        {
            SM.LoadSessions();
            /*
            if (SM.CurrentSession != SM.OpenSession)
            {
                B_OpenSession.Visibility = Visibility.Visible;
            }
            else
            {
                B_OpenSession.Visibility = Visibility.Hidden;
            }
            */
        }

        /// <summary>
        /// Registered to the B_DeleteSession.Click Event
        /// Deletes selected session
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteSelectedSession(object sender, RoutedEventArgs e)
        {
            /*
             * 1. Delete the .bin file
             * 2. Remove the name of the .bin file from the ProgramData's list.
             * 3. Remove the name of the UI-friendly name from the SessionManager's list
             */
            SM.DeleteSession(OldSessions.SelectedItems);
        }

        private void CancelView(object sender, RoutedEventArgs e)
        {
            uncheckView();
        }

        private void RealoadOpenSession(object sender, RoutedEventArgs e)
        {
            /*
             * 1. Load old session
             * 2. Add TimeEntries and CommentEntries back to the UI
             * 3. Rig the TimeEntries and CommentEntries with their
             * control-based and UI-based events
             * 4. Turn watching back on.
             */
            SM.LoadOpenSession();
            LoadStacks(false);
            uncheckView();
            B_OpenSession.Visibility = Visibility.Hidden;
        }
        #endregion

        private void AddSessionStamp()
        {
            timeStack.Children.Add(SM.AddSessionStamp());
        }

        private void addCommentEntry(string comment)
        {
            commentEntry newEntry = SM.newCommentEntry(comment);
            if (newEntry != null)
            {
                commentStack.Children.Add(newEntry);
                RegisterToComment(newEntry);
            }
        }

        private void RegisterToComment(commentEntry comment)
        {
            comment.checkedEvent += filterTimeEntries;
            comment.delete += commentEntry_delete;
        }

        private void RegisterToTimeEntry(TimeEntry te)
        {
            te.deleteEvent += deleteTimeEntry;
            te.newCommentEvent += addCommentEntry;
            te.textChangedEvent += filterIntelList;
        }

        private void uncheckView()
        {
            TB_ViewSessions.IsChecked = false;
        }

        private void LoadStacks(bool view_only = true)
        {
            //Clear UI of TimeEntries
            timeStack.Children.Clear();
            //Add enough TimeEntries for new Session
            foreach (UIElement uie in SM.Entries)
            {
                timeStack.Children.Add(uie);
                if (uie.GetType() == typeof(TimeEntry))
                {
                    TimeEntry temp = (TimeEntry)uie;
                    temp.ViewOnly = view_only;
                    RegisterToTimeEntry(temp);
                }
            }

            //Remove comment entries
            commentStack.Children.Clear();
            foreach (string s in SM.CommentLibrary)
            {
                commentEntry newEntry = new commentEntry(s);
                newEntry.ViewOnly = view_only;
                RegisterToComment(newEntry);
                commentStack.Children.Add(newEntry);
            }
        }
        /// <summary>
        /// Checks to see if a Session for today already exists
        /// </summary>
        public void CheckForRecentSession()
        {
            oldSession = SM.CheckForRecentSession();
            if (oldSession != null)
            {
                isContinuing = true;
                //Open Popup in the middle of screen
                dialogBlock.Text =
                    "A session for today already exists.\n" +
                    "Would you like to open that one?";
                dialogPopup.IsOpen = true;
                dialogPopup.PlacementTarget = entryAdderButton;
                dialogPopup.Placement = PlacementMode.Bottom;
            }
        }

        void B_No_Click(object sender, RoutedEventArgs e)
        {
            if (isDeleting)
            {
                isDeleting = false;
            }
            else
            {
                isContinuing = false;
            }
            dialogPopup.IsOpen = false;
        }

        void B_OK_Click(object sender, RoutedEventArgs e)
        {
            if (isDeleting)
            {
                isDeleting = false;
            }
            else
            {
                SM.LoadSession(oldSession);
                LoadStacks(false);
                isContinuing = false;
            }
            dialogPopup.IsOpen = false;
        }
        #endregion
    }
}