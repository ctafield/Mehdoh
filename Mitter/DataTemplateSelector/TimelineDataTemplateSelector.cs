using System.Windows;
using FieldOfTweets.Common.UI.ViewModels;

namespace Mitter.DataTemplateSelector
{

    public class TimelineDataTemplateSelector : DataTemplateSelector
    {

        public DataTemplate Regular { get; set; }

        public DataTemplate RegularMedia { get; set; }

        public DataTemplate Retweet { get; set; }

        public DataTemplate RetweetMedia { get; set; }

        public DataTemplate Gap { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {

            var timeline = item as TimelineViewModel;

            if (timeline != null)
            {
                if (timeline.IsRetweet)
                    return Retweet;

                if (!timeline.IsGap)
                    return Regular;

                return Gap;
            }

            return base.SelectTemplate(item, container);

        }

    }


}
