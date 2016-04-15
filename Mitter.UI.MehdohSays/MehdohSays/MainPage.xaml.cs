using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Phone.Controls;

namespace Mitter.UI.MehdohSays.MehdohSays
{

    public partial class MainPage : PhoneApplicationPage
    {

        private const string FileSettingsPath = "settings.txt";

        const double HighlightThickness = 7;

        private ThemeEnum CurrentTheme;

        public int CurrentScore { get; set; }

        public List<int> CurrentSequence { get; set; }
        private int CurrentIndex { get; set; }
        
        private bool CurrentlyPlayingSequence { get; set; }

        private Settings AppSettings { get; set; }

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            CurrentTheme = GetCurrentTheme();
            GetSettings();
            ShowHighScore();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            buttonStart.IsEnabled = false;

            CurrentScore = 0;
            CurrentSequence = new List<int>();
            ResetAllBoxes();

            ThreadPool.QueueUserWorkItem(StartGame);

        }

        private void StartGame(object state)
        {
            GameLoop();
        }

        private void GameLoop()
        {
            var random = new Random(DateTime.Now.Millisecond);

            var lastNumber = - 1;
            
            if (CurrentSequence.Count > 0)
            {
                lastNumber = CurrentSequence[CurrentSequence.Count - 1];
            }

            int nextNumber = lastNumber;

            while (nextNumber == lastNumber)
            {
                nextNumber= random.Next(1, 5);
            }

            CurrentSequence.Add(nextNumber);
            PlaySequence();
        }

        private void PlaySequence()
        {

            CurrentlyPlayingSequence = true;

            foreach (var item in CurrentSequence)
            {
                int currentItem = item;

                Dispatcher.BeginInvoke(() =>
                                           {


                                               Debug.WriteLine(currentItem);

                                               ResetAllBoxes();

                                               switch (currentItem)
                                               {
                                                   case 1:
                                                       box1.BorderBrush = BorderBrush();
                                                       box1.BorderThickness = new Thickness(HighlightThickness);
                                                       box1.Height = 215;
                                                       box1.Width = 215;
                                                       break;

                                                   case 2:

                                                       box2.BorderBrush = BorderBrush();
                                                       box2.BorderThickness = new Thickness(HighlightThickness);
                                                       box2.Height = 215;
                                                       box2.Width = 215;
                                                       break;

                                                   case 3:
                                                       box3.BorderBrush = BorderBrush();
                                                       box3.BorderThickness = new Thickness(HighlightThickness);
                                                       box3.Height = 215;
                                                       box3.Width = 215;
                                                       break;

                                                   case 4:
                                                       box4.BorderBrush = BorderBrush();
                                                       box4.BorderThickness = new Thickness(HighlightThickness);
                                                       box4.Height = 215;
                                                       box4.Width = 215;
                                                       break;
                                               }
                                           });

                Thread.Sleep(500);

            }


            Dispatcher.BeginInvoke(() =>
                                       {
                                           ResetAllBoxes();
                                           FadeOutText("GO!", sb_Completed);
                                       });

        }

        private Brush BorderBrush()
        {
            if (CurrentTheme == ThemeEnum.Light)
            {
                return new SolidColorBrush(Colors.Black);    
            }

            return new SolidColorBrush(Colors.White);
        }

        private void FadeOutText(string newText, EventHandler finishMethod)
        {
            // Prompt the user
            var sb = new Storyboard();
            var da = new DoubleAnimation
                         {
                             BeginTime = TimeSpan.FromSeconds(0),
                             From = 1,
                             To = 0,
                             Duration = new Duration(TimeSpan.FromSeconds(0.4))
                         };

            Storyboard.SetTargetProperty(da, new PropertyPath(OpacityProperty));
            Storyboard.SetTarget(sb, txtGo);

            sb.Duration = TimeSpan.FromSeconds(0.4);
            sb.Children.Add(da);

            txtGo.Visibility = Visibility.Visible;
            txtGo.Text = newText;
            txtGo.Opacity = 1;

            if (finishMethod != null) 
                sb.Completed += finishMethod;

            sb.Begin();
        }

        void sb_Completed(object sender, EventArgs e)
        {
            CurrentlyPlayingSequence = false;
            CurrentIndex = 0;
            txtGo.Visibility = Visibility.Collapsed;
        }

        private void ResetAllBoxes()
        {
            box1.BorderThickness = new Thickness(0);
            box2.BorderThickness = new Thickness(0);
            box3.BorderThickness = new Thickness(0);
            box4.BorderThickness = new Thickness(0);

            box1.Width = 210;
            box1.Height = 210;
            box2.Width = 210;
            box2.Height = 210;
            box3.Width = 210;
            box3.Height = 210;
            box4.Width = 210;
            box4.Height = 210;
        }

        private void box1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (CurrentlyPlayingSequence || CurrentSequence == null) return;


            if (CurrentSequence[CurrentIndex] == 1)
            {
                GoodMove();
            }
            else
            {
                // bad
                ThreadPool.QueueUserWorkItem(EndGame);
            }

        }

        private void box2_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (CurrentlyPlayingSequence || CurrentSequence == null) return;

            if (CurrentSequence[CurrentIndex] == 2)
            {
                GoodMove();
            }
            else
            {
                // bad
                ThreadPool.QueueUserWorkItem(EndGame);
            }

        }

        private void box3_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (CurrentlyPlayingSequence || CurrentSequence == null) return;

            if (CurrentSequence[CurrentIndex] == 3)
            {
                GoodMove();
            }
            else
            {
                // bad
                ThreadPool.QueueUserWorkItem(EndGame);
            }

        }

        private void box4_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (CurrentlyPlayingSequence || CurrentSequence == null) return;

            if (CurrentSequence[CurrentIndex] == 4)
            {
                GoodMove();
            }
            else
            {
                // bad
                ThreadPool.QueueUserWorkItem(EndGame);
            }

        }

        private void GoodMove()
        {
            // Is it at the end?
            if (++CurrentIndex == CurrentSequence.Count)
            {
                txtScore.Text = string.Format("Current Score: {0}", CurrentSequence.Count);

                // Go to the next number
                ThreadPool.QueueUserWorkItem(StartGame);
            }
        }

        private void EndGame(object state)
        {
            Dispatcher.BeginInvoke(() => FadeOutText("END!", End_AnimationDone));
        }

        private void End_AnimationDone(object sender, EventArgs e)
        {
            buttonStart.IsEnabled = true;

            if (CurrentSequence.Count > AppSettings.HighScore)
            {
                AppSettings.HighScore = CurrentSequence.Count - 1;
                SaveSettings();
            }

            txtGo.Visibility = Visibility.Collapsed;

            ShowHighScore();

        }

        private void ShowHighScore()
        {
            txtScore.Text = string.Format("High Score: {0}", AppSettings.HighScore);
        }

        private void GetSettings()
        {
            using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
            {

                if (myStore.FileExists(FileSettingsPath))
                {

                    using (var isoFileStream = new IsolatedStorageFileStream(FileSettingsPath, FileMode.Open, myStore))
                    {
                        var serializer = new DataContractSerializer(typeof(Settings));
                        AppSettings = (Settings)serializer.ReadObject(isoFileStream);
                    }
                }
                else
                {
                    AppSettings = new Settings()
                    {
                        HighScore = 0
                    };
                }
            }

        }

        private void SaveSettings()
        {

            using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var isoFileStream = new IsolatedStorageFileStream(FileSettingsPath, FileMode.Create, myStore))
                {
                    var serializer = new DataContractSerializer(typeof(Settings));
                    serializer.WriteObject(isoFileStream, AppSettings);
                }
            }
        }


        public static ThemeEnum GetCurrentTheme()
        {
            var bgc = Application.Current.Resources["PhoneBackgroundColor"].ToString();
            if (bgc == "#FF000000")
                return ThemeEnum.Dark;

            return ThemeEnum.Light;
        }



    }
}