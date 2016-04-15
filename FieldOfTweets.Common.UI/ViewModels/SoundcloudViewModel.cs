using System;
using System.ComponentModel;
using System.Threading;
using System.Web;
using System.Windows;
using System.Windows.Media.Imaging;
using FieldOfTweets.Common.PlaylistDetails;
using FieldOfTweets.Common.UI.ImageCaching;
using Newtonsoft.Json;

namespace FieldOfTweets.Common.UI.ViewModels
{

    public class SoundcloudViewModel : INotifyPropertyChanged
    {

        public ApplicationConstants.SoundcloudTypeEnum StreamType { get; set; }

        public long UserId { get; set; }
        public string UserName { get; set; }

        public BitmapImage TypeImageUri
        {
            get
            {
                if (Type == "favoriting")
                    return UiHelper.GetAddMentionPng();

                return UiHelper.GetSoundcloudStreamPng();
            }
        }

        public string Title { get; set; }

        public string StreamingUrl { get; set; }
        public long Id { get; set; }

        public string Description
        {
            get { return HttpUtility.HtmlDecode(_description); }
            set { _description = value; }
        }

        public Visibility DescriptionVisibility
        {
            get { return (string.IsNullOrEmpty(Description.Trim())) ? Visibility.Collapsed : Visibility.Visible; }
        }

        public string WaveformUrl { get; set; }
        public string Genre { get; set; }
        public int Index { get; set; }

        public long AccountId { get; set; }
        public string PermalinkUrl { get; set; }
        public bool IsStreamable { get; set; }


        private Uri _albumArtImageSource;

        [JsonIgnore]
        public Uri AlbumArtImageSource
        {
            get
            {
                if (_albumArtImageSource != null)
                    return _albumArtImageSource;

                if (string.IsNullOrWhiteSpace(AlbumArt))
                    return null;

                ThreadPool.QueueUserWorkItem(delegate(object state)
                {
                    var model = state as SoundcloudViewModel;

                    var urlParts = model.AlbumArt.Split('/');
                    var currentImage = urlParts[urlParts.Length - 1];

                    if (currentImage.Contains("?"))
                        currentImage = currentImage.Substring(0, currentImage.IndexOf('?'));

                    var userId = urlParts[urlParts.Length - 2];

                    if (ImageCacheHelper.IsProfileImageCached(userId, currentImage))
                    {
                        model._albumArtImageSource = ImageCacheHelper.GetUriForCachedImage(userId, currentImage);
                        model.NotifyPropertyChanged("AlbumArtImageSource");
                    }
                    else
                    {
                        ImageCacheHelper.CacheImage(model.AlbumArt, userId, currentImage, () => model.NotifyPropertyChanged("AlbumArtImageSource"));
                    }
                }, this);

                return null;

            }
        }

        private string _albumArt;
        public string AlbumArt
        {
            get
            {
                return _albumArt;
            }
            set
            {
                _albumArt = value;
            }
        }

        public string AlbumArtLarge
        {
            get
            {
                if (_albumArt.Contains("-large"))
                    return _albumArt.Replace("-large", "-t500x500");
                return _albumArt;
            }
        }

        private string _avatarUrl;
        private string _description;

        public string AvatarUrl
        {
            get
            {
                return _avatarUrl;
            }
            set
            {
                _avatarUrl = value;
            }
        }

        public string Type { get; set; }

        public long Duration { get; set; }

        public TimeSpan DurationTimeSpan
        {
            get { return TimeSpan.FromMilliseconds(Duration); }
        }

        public string PlayerTag
        {
            get
            {
                var tag = new SoundcloudNowPlayingDetails()
                                                      {
                                                          Marker = "MEHDOHSC",
                                                          AccountId = AccountId,
                                                          Id = Id
                                                      };

                return JsonConvert.SerializeObject(tag);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            UiHelper.SafeDispatch(() =>
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        public SoundcloudViewModel Clone()
        {
            return this.MemberwiseClone() as SoundcloudViewModel;
        }

    }

}
