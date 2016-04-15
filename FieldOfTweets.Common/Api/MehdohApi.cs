// *********************************************************************************************************
// <copyright file="SearchPage.xaml.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary> Mehdoh for Windows Phone </summary>
// *********************************************************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Hammock;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace FieldOfTweets.Common.Api
{
    public abstract class MehdohApi
    {
        #region Properties

        public bool HasError { get; set; }

        public string ErrorMessage { get; set; }

        #endregion

        #region Members

        protected T DeserialiseResponse<T>(string responseContent)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(responseContent))
                return default(T);

            try
            {
                var result = JsonConvert.DeserializeObject<T>(responseContent, new JsonSerializerSettings
                {
                    Error = delegate(object sender, ErrorEventArgs args)
                    {
                        if (Debugger.IsAttached)
                            Debugger.Break();

                        errors.Add(args.ErrorContext.Path + " : " + args.ErrorContext.Error.Message);
                        args.ErrorContext.Handled = true;
                    }
                });

                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return default(T);
            }
            
        }

        protected T DeserialiseResponse<T>(RestResponse response)
        {
            return DeserialiseResponse<T>(response.Content);
        }

        #endregion
    }
}