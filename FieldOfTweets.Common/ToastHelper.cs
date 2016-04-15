using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace FieldOfTweets.Common
{
    public class ToastHelper
    {

        // Note: All toast templates available in the Toast Template Catalog (http://msdn.microsoft.com/en-us/library/windows/apps/hh761494.aspx)
        // are treated as a ToastText02 template on Windows Phone.
        // That template defines a maximum of 2 text elements. The first text element is treated as header text and is always bold.
        // Images will never be downloaded when any of the other templates containing image elements are used, because Windows Phone will
        // not display the image. The app icon (Square 150 x 150) is displayed to the left of the toast text and is also show in the action center.
        public static ToastNotification CreateTextOnlyToast(string toastHeading, string toastBody)
        {
            // Using the ToastText02 toast template.
            var toastTemplate = ToastTemplateType.ToastText02;

            // Retrieve the content part of the toast so we can change the text.
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);

            //Find the text component of the content
            XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");

            // Set the text on the toast. 
            // The first line of text in the ToastText02 template is treated as header text, and will be bold.
            toastTextElements[0].AppendChild(toastXml.CreateTextNode(toastHeading));
            toastTextElements[1].AppendChild(toastXml.CreateTextNode(toastBody));

            // Set the duration on the toast
            IXmlNode toastNode = toastXml.SelectSingleNode("/toast");
            ((XmlElement)toastNode).SetAttribute("duration", "long");

            // Create the actual toast object using this toast specification.
            var toast = new ToastNotification(toastXml);

            return toast;
        }

    }
}
