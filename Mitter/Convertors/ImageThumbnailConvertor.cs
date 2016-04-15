// *********************************************************************************************************
// <copyright file="ImageThumbnailConvertor.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary>Mehdoh for Windows Phone</summary>
// *********************************************************************************************************

using System;
using System.Globalization;
using System.Windows.Data;

namespace Mitter.Convertors
{
    public class ImageThumbnailConvertor : IValueConverter
    {
        #region Implementation of IValueConverter

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}