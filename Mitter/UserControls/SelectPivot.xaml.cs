using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using FieldOfTweets.Common;
using FieldOfTweets.Common.ColumnConfig;
using FieldOfTweets.Common.UI;
using Microsoft.Phone.Controls;
using MyToolkit.Utilities;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

namespace Mitter.UserControls
{
    public partial class SelectPivot : UserControl
    {

        private const double ItemHeight = 90;
        private List<UIElement> Headers { get; set; }
        private Pivot MainPivot { get; set; }

        public SelectPivot(Pivot mainPivot)
        {
            InitializeComponent();

            MainPivot = mainPivot;

            Loaded += new RoutedEventHandler(SelectPivot_Loaded);
        }

        internal void Hide()
        {
            IsShown = false;

            // hide the stack with pivot titles
            ShuffleUpItems(() =>
                               {
                                   canvasContainer.Opacity = 0;
                                   this.Visibility = Visibility.Collapsed;

                                   if (PivotSelectedEvent != null)
                                   {
                                       var e = new SelectPivotSelectedArgs()
                                               {
                                                   SelectedIndex = SelectedIndex
                                               };
                                       PivotSelectedEvent(this, e);
                                   }
                               });

        }

        internal void Show()
        {

            LoadControls();

            ArrangeItems();

            this.Visibility = Visibility.Visible;

            SelectedIndex = CurrentIndex;

            // hide the stack with pivot titles
            canvasContainer.Opacity = 1;

            ShuffleDownItems();

            IsShown = true;

        }

        private void ArrangeItems()
        {
            canvasContainer.Margin = new Thickness(4, 0, 0, 0);
            canvasContainer.Children.Clear();
            canvasContainer.Children.Add(Headers[CurrentIndex]);

            var sh = new SettingsHelper();

            for (int i = 0; i < Headers.Count; i++)
            {
                if (i != CurrentIndex)
                    canvasContainer.Children.Add(Headers[i]);

                if (sh.GetShowPivotHeaderCounts())
                {
                    var count = UiHelper.GetColumnCount(MainPivot, i);
                    UpdateCount(Headers[i], count);
                }
            }
        }

        private void UpdateCount(UIElement control, string newValue)
        {

            var stackPanel = control as StackPanel;

            var textStack = stackPanel.Children[0] as StackPanel;
            if (textStack != null)
            {
                var textBlock = textStack.Children[0] as TextBlock;
                if (textBlock != null)
                {
                    textBlock.Text = newValue;                    
                    textBlock.UpdateLayout();
                    textStack.UpdateLayout();
                    stackPanel.UpdateLayout();
                }
            }
        }

        private DoubleAnimation CreateAnimation(double from, double to, double duration,
                                               PropertyPath targetProperty, DependencyObject target)
        {
            var db = new DoubleAnimation
            {
                To = to,
                EasingFunction = new QuadraticEase(),
                Duration = TimeSpan.FromSeconds(duration)
            };

            if (from > 0)
                db.From = from;

            Storyboard.SetTarget(db, target);
            Storyboard.SetTargetProperty(db, targetProperty);
            return db;
        }


        private void ShuffleDownItems()
        {

            double currentHeight = 0;

            var sb = new Storyboard();

            var fadeAnim = CreateAnimation(0, 0.8, 0.3, new PropertyPath(OpacityProperty), background);

            sb.Children.Add(fadeAnim);

            foreach (var child in canvasContainer.Children)
            {
                var anim = CreateAnimation(0, currentHeight, 0.3, new PropertyPath(Canvas.TopProperty), child);
                currentHeight += ItemHeight;
                sb.Children.Add(anim);

                var childFade = CreateAnimation(0, 1, 0.3, new PropertyPath(OpacityProperty), child);
                sb.Children.Add(childFade);
            }

            canvasContainer.Height = currentHeight;

            sb.Begin();

        }


        private void ShuffleUpItems(Action endAction)
        {

            double currentHeight = 0;

            var sb = new Storyboard();

            var fadeAnim = CreateAnimation(0.8, 0, 0.3, new PropertyPath(OpacityProperty), background);

            sb.Children.Add(fadeAnim);

            foreach (var child in canvasContainer.Children)
            {
                var anim = CreateAnimation(0, 0, 0.3, new PropertyPath(Canvas.TopProperty), child);
                currentHeight += ItemHeight;
                sb.Children.Add(anim);

                var childFade = CreateAnimation(1, 0, 0.3, new PropertyPath(OpacityProperty), child);
                sb.Children.Add(childFade);
            }

            sb.Completed += delegate(object sender, EventArgs args)
            {
                if (endAction != null)
                    endAction();
            };

            sb.Begin();

        }

        void SelectPivot_Loaded(object sender, RoutedEventArgs e)
        {
            //LoadControls();

            if (ControlLoaded)
                return;

            canvasContainer.Opacity = 0;

            this.Visibility = Visibility.Collapsed;

            ControlLoaded = true;

        }

        private void LoadControls()
        {

            canvasContainer.Children.Clear();

            Headers = new List<UIElement>();

            var sh = new SettingsHelper();

            var showPivotHeaderAvatars = sh.GetShowPivotHeaderAvatars();
            var showPivotHeaderCounts = sh.GetShowPivotHeaderCounts();

            for (int i = 0; i < ColumnHelper.ColumnConfig.Count; i++)
            {
                var col = ColumnHelper.ColumnConfig[i];
                var newCol = UiHelper.GetColumnHeader(col.ColumnType, col.DisplayName, col.AccountId, null, outerStackPanel_Tap, null, showPivotHeaderCounts, showPivotHeaderAvatars, i);
                Headers.Add(newCol);
            }

        }

        protected bool ControlLoaded { get; set; }

        void outerStackPanel_Tap(object sender, GestureEventArgs e)
        {
            var panel = sender as StackPanel;
            SelectedIndex = (int)panel.Tag;

            Hide();
        }


        public int CurrentIndex { get; set; }
        public int SelectedIndex { get; set; }

        public bool IsShown { get; private set; }

        public event EventHandler<SelectPivotSelectedArgs> PivotSelectedEvent;

    }

    public class SelectPivotSelectedArgs : EventArgs
    {
        public int SelectedIndex { get; set; }
    }

}
