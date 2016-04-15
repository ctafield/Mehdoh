// *********************************************************************************************************
// <copyright file="ThumbnailCacheConvertor.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary>Mehdoh for Windows Phone</summary>
// *********************************************************************************************************

using System;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using FieldOfTweets.Common.UI.ImageCaching;

namespace Mitter.Convertors
{
    public class ThumbnailCacheConvertor : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return value;

            if (value is BitmapImage)
                return value;

            var urlParts = value.ToString().Split('/');
            // http://www.twitter.com/pictures/something/123123123/myface.jpg
            // -> 123123123.jpg

            var currentImage = urlParts[urlParts.Length - 1];
            var userId = urlParts[urlParts.Length - 2];

            if (ImageCacheHelper.IsProfileImageCached(userId, currentImage))
            {
                return ImageCacheHelper.GetCachedImage(userId, currentImage);
            }

            // Cache it for next time, but for now get it
            ImageCacheHelper.CacheImage(value.ToString(), userId, currentImage, null);

            // get it
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

        #endregion
    }
}