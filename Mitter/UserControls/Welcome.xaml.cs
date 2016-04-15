// *********************************************************************************************************
// <copyright file="Welcome.xaml.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary>Mehdoh for Windows Phone</summary>
// *********************************************************************************************************

using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FieldOfTweets.Common;
using FieldOfTweets.Common.UI.Classes;
using Mitter.Classes;

namespace Mitter.UserControls
{
    public partial class Welcome : UserControl
    {
        #region Constructor

        public Welcome()
        {
            InitializeComponent();
            Loaded += Welcome_Loaded;
        }

        #endregion

        public class WelcomeEventArgs : RoutedEventArgs
        {
            public ApplicationConstants.AccountTypeEnum SelectedAccountType { get; set; }
        }

        private void Welcome_Loaded(object sender, RoutedEventArgs e)
        {
            //var height = (800/100)*App.Current.Host.Content.ScaleFactor;

            double height = 800;

            if (ResolutionHelper.CurrentResolution == Resolutions.HD720p)
            {
                height = 880;
            }

            LayoutRoot.Height = height;
            LayoutRootOuter.Height = height; 
            imageSky.Height = height;
        }

        public event EventHandler<WelcomeEventArgs> LinkClickEvent;

        private void lnkAccountTwitter_Click(object sender, RoutedEventArgs e)
        {
            App.AttempedLink = true;
            var args = new WelcomeEventArgs()
                                        {
                                            SelectedAccountType = ApplicationConstants.AccountTypeEnum.Twitter
                                        };
            LinkClickEvent(this, args);
        }

        private static DoubleAnimation CreateAnimation(double from, double to, double duration,
                                                       PropertyPath targetProperty, DependencyObject target, TimeSpan? beginTime)
        {
            var db = new DoubleAnimation
            {
                To = to,
                From = from,
                EasingFunction = new SineEase(),
                Duration = TimeSpan.FromSeconds(duration),
                BeginTime = beginTime
            };
            Storyboard.SetTarget(db, target);
            Storyboard.SetTargetProperty(db, targetProperty);
            return db;
        }

        public void AnimateBoxesOn()
        {

            var timer = new DispatcherTimer();
            timer.Tick += delegate(object sender, EventArgs args)
                              {

                                  var senderTimer = sender as DispatcherTimer;
                                  senderTimer.Stop();

                                  var sb = new Storyboard();

                                  sb.Children.Add(CreateAnimation(0, 1, 0.4, new PropertyPath(OpacityProperty), btn1, TimeSpan.FromSeconds(0)));
                                  sb.Children.Add(CreateAnimation(100, 0, 0.4, new PropertyPath(TranslateTransform.XProperty), btn1.RenderTransform, TimeSpan.FromSeconds(0)));

                                  sb.Children.Add(CreateAnimation(0, 1, 0.2, new PropertyPath(OpacityProperty), imageSky, TimeSpan.FromSeconds(0)));
                                  sb.Children.Add(CreateAnimation(-350, -650, 10, new PropertyPath(TranslateTransform.XProperty), imageSky.RenderTransform, TimeSpan.FromSeconds(0)));

                                  sb.Duration = new Duration(TimeSpan.FromSeconds(10));
                                  sb.Begin();

                              };
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer.Start();

        }

    }
}