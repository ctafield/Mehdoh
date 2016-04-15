// *********************************************************************************************************
// <copyright file="LocalisedStrings.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary>Mehdoh for Windows Phone</summary>
// *********************************************************************************************************

namespace FieldOfTweets.Common.UI.Resources
{
    public class LocalisedStrings
    {
        private static readonly ApplicationResources localisedResources = new ApplicationResources();
        
        #region Properties

        public string LockAndTileCaption
        {
            get
            {
#if WP7
                return "tile";
#elif WP8
                return "lock &amp; tile";
#endif
            }
        }

        public ApplicationResources LocalisedResources
        {
            get { return localisedResources; }
        }

        public string ApplicationName
        {
            get { return ApplicationConstants.ApplicationName; }
        }

        public string WelcomeToMehdoh
        {
            get { return string.Format("welcome to {0}!", ApplicationName.ToLower()); }
        }

        #endregion
    }
}