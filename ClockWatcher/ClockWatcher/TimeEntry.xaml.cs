using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClockWatcher
{
    [Serializable]
    /// <summary>
    /// Interaction logic for timeEntry.xaml
    /// </summary>
    public partial class TimeEntry : UserControl, INotifyPropertyChanged
    {
        private static string defaultComment = "Type Comment Here";

        #region Dependency Properties
        public static readonly DependencyProperty alarmExpandedProperty =
            DependencyProperty.Register("alarmExpanded", typeof(bool),
            typeof(TimeEntry), new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnAlarmExpandedChanged)));
        public static readonly DependencyProperty controlHeightProperty = DependencyProperty.Register("controlHeight", typeof(double),
            typeof(TimeEntry), new FrameworkPropertyMetadata(0.0));
        public static readonly DependencyProperty controlWidthProperty = DependencyProperty.Register("controlWidth", typeof(double),
            typeof(TimeEntry), new FrameworkPropertyMetadata(0.0));
        public static readonly DependencyProperty detailsExpandedProperty =
            DependencyProperty.Register("detailsExpanded", typeof(bool), typeof(TimeEntry),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnDetailsExpanded)));
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(TimeEntry), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty IsCollapsedProperty = DependencyProperty.Register("isCollapsed", typeof(bool), typeof(TimeEntry), new FrameworkPropertyMetadata(false));
        #endregion
        public TimeEntryData Data { get; private set; }
        #region Animation fields
        private TransformGroup animatedTransform;
        private ScaleTransform animatedScale;
        private DoubleAnimation deleteDoubleAnimation;
        #endregion

        #region Event-based fields
        public delegate void deleteEventHandler(object sender);
        public delegate void textChangedEventHandler(object sender, TextChangedEventArgs tcea);
        public delegate void enterPressEventHandler(object sender, KeyEventArgs kea);
        public delegate void newCommentEventHandler(string comment);

        public event deleteEventHandler deleteEvent;
        public event enterPressEventHandler enterPressEvent;
        public event textChangedEventHandler textChangedEvent;
        public event newCommentEventHandler newCommentEvent;
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        /// <summary>
        /// This will be used along with isCollapsed to ensure
        /// this entry was marked as collapsed/uncollapsed by
        /// a commentEntry being checked.
        /// </summary>
        private bool _isMarkedForView;
        private bool view_only;

        #region Properties
        #region Dependency Properties
        public bool alarmExpanded
        {
            get
            {
                return (bool)GetValue(alarmExpandedProperty);
            }
            set
            {
                SetValue(alarmExpandedProperty, value);
            }
        }
        public double controlHeight
        {
            get
            {
                return (double)GetValue(controlHeightProperty);
            }
            set
            {
                SetValue(controlHeightProperty, value);
            }
        }
        public double controlWidth
        {
            get
            {
                return (double)GetValue(controlWidthProperty);
            }
            set
            {
                SetValue(controlWidthProperty, value);
            }
        }
        public bool detailsExpanded
        {
            get
            {
                return (bool)GetValue(detailsExpandedProperty);
            }
            set
            {
                SetValue(detailsExpandedProperty, value);
            }
        }
        public bool isCollapsed
        {
            get
            {
                return (bool)GetValue(IsCollapsedProperty);
            }
            set
            {
                if (isCollapsed != value)
                {
                    SetValue(IsCollapsedProperty, value);
                    if (isCollapsed)
                    {
                        VisualStateManager.GoToState(this, "Collapsed", true);
                    }
                    else
                    {
                        VisualStateManager.GoToState(this, "Visible", true);
                    }
                }
            }
        }
        public bool IsSelected
        {
            get
            {
                return (bool)GetValue(IsSelectedProperty);
            }
            set
            {
                SetValue(IsSelectedProperty, value);
            }
        }
        #endregion
        public bool ViewOnly
        {
            get
            {
                return view_only;
            }
            set
            {
                if (view_only != value)
                {
                    view_only = value;
                    if (view_only)
                    {
                        deleteButton.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        deleteButton.Visibility = Visibility.Visible;
                    }
                }
            }
        }
        public bool isMarkedForView
        {
            get
            {
                return _isMarkedForView;
            }
            set
            {
                _isMarkedForView = value;
            }
        }
        public bool isFinalized
        {
            get;
            private set;
        }
        #endregion

        static TimeEntry() { }

        public TimeEntry(int id)
        {
            setUpRenderTransform();
            setUpAnimationVariables();
            Data = new TimeEntryData(this, id);
            DataContext = Data;
            this.InitializeComponent();
            deleteDoubleAnimation.Completed += deleteDoubleAnimation_Completed;
            ViewOnly = false;
            isFinalized = false;
        }

        public TimeEntry(TimeEntryData ted)
        {
            setUpRenderTransform();
            setUpAnimationVariables();
            Data = ted;
            Data.Owner = this;
            DataContext = Data;
            this.InitializeComponent();
            deleteDoubleAnimation.Completed += deleteDoubleAnimation_Completed;
            isFinalized = false;
        }

        #region Methods
        #region Event Methods
        #region commentBox
        private void commentBox_GotFocus(object sender, RoutedEventArgs e)
        {
            SolidColorBrush foregroundBrush = new SolidColorBrush((Color)Resources["blackCommentColor"]);
            commentBox.Foreground = foregroundBrush;
            if (commentBox.Text == defaultComment)
                commentBox.Text = "";
            else
            {
                commentBox.SelectAll();
            }
        }
        private void commentBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SolidColorBrush foregroundBrush;
            if (commentBox.Text == "" || commentBox.Text == defaultComment)
            {
                foregroundBrush = new SolidColorBrush((Color)Resources["grayCommentColor"]);
            }
            else
            {
                foregroundBrush = new SolidColorBrush((Color)Resources["blackCommentColor"]);
            }
            commentBox.Foreground = foregroundBrush;
        }
        private void commentBox_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            Data.Comment = (sender as TextBox).Text;
            if (Data.Comment != defaultComment || Data.Comment != "")
            {
                commentBox.Foreground = new SolidColorBrush((Color)Resources["blackCommentColor"]);
            }
        }
        private void commentBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Data.Comment = (sender as TextBox).Text;
            if (textChangedEvent != null)
            {
                textChangedEvent(sender, e);
            }
        }
        private void commentConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (newCommentEvent != null)
                newCommentEvent(Data.Comment);
        }
        #endregion
        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            deleteAnimation();
        }
        private void deleteDoubleAnimation_Completed(object sender, EventArgs e)
        {
            if (deleteEvent != null)
                deleteEvent(this);
        }
        private void detailsButton_Click(object sender, RoutedEventArgs e)
        {
            detailsExpanded = !detailsExpanded;
        }
        private static void OnAlarmExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeEntry sentinel = d as TimeEntry;

            if (sentinel.alarmExpanded)
            {
                sentinel.detailsExpanded = false;
            }
        }
        private static void OnDetailsExpanded(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeEntry sentinel = d as TimeEntry;

            if (sentinel.detailsExpanded)
            {
                sentinel.alarmExpanded = false;
            }
        }
        private void UserControl_KeyDown(object sender, KeyEventArgs kea)
        {
            if (kea.Key == Key.Enter)
            {
                if (commentBox.IsFocused)
                {
                    if (commentBox.Text != defaultComment && commentBox.Text != "")
                    {
                        this.Focus();
                        if (newCommentEvent != null)
                            newCommentEvent(commentBox.Text);
                    }
                }
            }
        }
        #endregion

        #region Class Methods
        /// <summary>
        /// Clears the comment's context
        /// </summary>
        public void ClearComment()
        {
            Data.Comment = string.Empty;
        }
        /// <summary>
        /// Determines whether or not the TimeEntry's comment contains the default text
        /// </summary>
        /// <returns>returns true if it does, else it returns false</returns>
        public bool commentDefault()
        {
            if (Data.Comment == defaultComment)
                return true;
            return false;
        }
        private void deleteAnimation()
        {
            animatedScale.BeginAnimation(ScaleTransform.ScaleXProperty, deleteDoubleAnimation);
            animatedScale.BeginAnimation(ScaleTransform.ScaleYProperty, deleteDoubleAnimation);
            this.BeginAnimation(FrameworkElement.HeightProperty, deleteDoubleAnimation);
        }
        /// <summary>
        /// Adds the timeOut data for the TimeEntry
        /// </summary>
        public void finalize()
        {
            Data.TimeOut = DateTime.Now;
            isFinalized = true;
        }
        private void setUpAnimationVariables()
        {
            deleteDoubleAnimation = new DoubleAnimation();
            deleteDoubleAnimation.To = 0;
            deleteDoubleAnimation.Duration = new Duration(TimeSpan.Parse("0:0:.25"));
        }
        private void setUpRenderTransform()
        {
            animatedTransform = new TransformGroup();
            animatedScale = new ScaleTransform();
            animatedTransform.Children.Add(animatedScale);

            this.RenderTransform = animatedTransform;
        }

        private void OnPropertyChanged([CallerMemberName]string property_name = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property_name));
            }
        }
        #endregion

        /// <summary>
        /// Registered to the UserControl.Loaded Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FocusOnComment(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus(commentBox);
        }

        #endregion



    }
}