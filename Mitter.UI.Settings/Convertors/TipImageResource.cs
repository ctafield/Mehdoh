// *********************************************************************************************************
// <copyright file="TipImageResource.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary>Mehdoh for Windows Phone</summary>
// *********************************************************************************************************

using System.Windows.Media;
using FieldOfTweets.Common.UI;

namespace Mitter.UI.Settings.Convertors
{
    public class TipImageResource
    {
        #region Properties

        public ImageSource TipImage
        {
            get { return UiHelper.GetTipImage(); }
        }

        #endregion
    }
}