using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Mitter.Animations.Behaviors
{

    public class AddListBoxBehaviour : Behavior<FrameworkElement>
    {
        protected override void OnAttached()
        {
            base.OnAttached();            
            AssociatedObject.Loaded += AssociatedObject_Loaded;
        }

        void AssociatedObject_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            CreateStoryboard().Begin();
        }

        private Storyboard CreateStoryboard()
        {
            var sb = new Storyboard();
            var animation = new DoubleAnimation
            {
                Duration = TimeSpan.FromMilliseconds(1000),
                To = 0,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.Projection).(PlaneProjection.RotationX)"));

            sb.Children.Add(animation);

            animation = new DoubleAnimation
            {
                Duration = TimeSpan.FromMilliseconds(1000),
                To = 1,
                EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTargetProperty(animation, new PropertyPath(UIElement.OpacityProperty));

            sb.Children.Add(animation);
            Storyboard.SetTarget(sb, AssociatedObject);
            return sb;
        }

    }

}
