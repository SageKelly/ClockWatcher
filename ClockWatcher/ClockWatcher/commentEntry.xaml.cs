using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
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
    /// Interaction logic for commentEntry.xaml
    /// </summary>
    public partial class commentEntry : UserControl
    {

        public static readonly DependencyProperty commentProperty =
            DependencyProperty.Register("comment", typeof(string),
            typeof(commentEntry), new PropertyMetadata(""));


        public static readonly DependencyProperty isFilteringProperty =
            DependencyProperty.Register("isFiltering", typeof(bool),
            typeof(commentEntry), new PropertyMetadata(false));

        public static readonly RoutedEvent deleteEvent = EventManager.RegisterRoutedEvent("delete",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(commentEntry));

        public delegate void checkedEventHandler(object sender, RoutedEventArgs rea);
        public event checkedEventHandler checkedEvent;

        private TransformGroup animatedTransform;
        private ScaleTransform animatedScale;

        private DoubleAnimation scaleAnim;
        private bool _isChecked;
        private bool view_only;

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

        public string comment
        {
            get
            {
                return (string)GetValue(commentEntry.commentProperty);
            }
            set
            {
                SetValue(commentEntry.commentProperty, value);
            }
        }
        public bool isChecked
        {
            get
            {
                return _isChecked;
            }
            set
            {
                _isChecked = value;
                if (checkedEvent != null)
                    checkedEvent(this, new RoutedEventArgs());
            }
        }

        public bool isFiltering
        {
            get
            {
                return (bool)GetValue(isFilteringProperty);
            }
            set
            {
                SetValue(isFilteringProperty, value);
            }
        }

        public event RoutedEventHandler delete
        {
            add
            {
                AddHandler(deleteEvent, value);
            }
            remove
            {
                RemoveHandler(deleteEvent, value);
            }
        }

        public commentEntry()
        {
            this.InitializeComponent();
            scaleAnim = new DoubleAnimation();
            animatedTransform = new TransformGroup();
            animatedScale = new ScaleTransform();
            animatedTransform.Children.Add(animatedScale);
            this.RenderTransform = animatedTransform;
            scaleAnim.Completed += animationScale_Completed;
            ViewOnly = false;
        }
        public commentEntry(string comment)
            : this()
        {
            this.comment = comment;
        }

        #region Methods
        #region Local Methods
        private void deleteAnimation()
        {
            scaleAnim.To = 0;
            scaleAnim.Duration = new Duration(TimeSpan.Parse("0:0:.25"));
            animatedScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            animatedScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            this.BeginAnimation(FrameworkElement.HeightProperty, scaleAnim);
        }
        #endregion



        #region Event Methods
        void animationScale_Completed(object sender, EventArgs e)
        {
            RoutedEventArgs newDeleteEvent = new RoutedEventArgs(deleteEvent, this);
            RaiseEvent(newDeleteEvent);
        }
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            deleteAnimation();
        }
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            isChecked = (bool)commentCheck.IsChecked;
        }
        #endregion
        #endregion
    }
}