// *********************************************************************************************************
// <copyright file="SearchPage.xaml.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary> Mehdoh for Windows Phone </summary>
// *********************************************************************************************************

using System;
using System.Windows;
using System.Windows.Controls;

namespace Mitter.UI.Settings.UserControls
{
    public partial class SettingsButton : UserControl
    {
        #region Constructor

        public SettingsButton()
        {
            InitializeComponent();
            DataContext = this;
        }

        #endregion

        #region Properties

        public Uri ImageSource { get; set; }

        public string Text { get; set; }

        #endregion

        public event EventHandler Click;

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (Click != null)
                Click(this, e);
        }
    }
}