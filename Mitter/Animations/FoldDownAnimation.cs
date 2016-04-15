using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Mitter.Animations
{
    public static class FoldDownAnimation
    {

        /// <summary>
        /// Animates each element in order, creating a 'peel' effect. The supplied action
        /// is invoked when the animation ends.
        /// </summary>
        public static void FoldUp(this IEnumerable<FrameworkElement> elements, Action endAction)
        {
            var elementList = elements.ToList();
            var lastElement = elementList.Last();

            // iterate over all the elements, animating each of them
            double delay = 0;

            foreach (var element in elementList)
            {
                var sb = GetFoldAnimation(element, delay, false);

                // add a Completed event handler to the last element
                if (element.Equals(lastElement))
                {
                    sb.Completed += (s, e) => endAction();
                }

                sb.Begin();
                delay += 0;
            }
        }


        /// <summary>
        /// Animates each element in order, creating a 'peel' effect. The supplied action
        /// is invoked when the animation ends.
        /// </summary>
        public static void FoldDown(this IEnumerable<FrameworkElement> elements, Action endAction)
        {
            var elementList = elements.ToList();
            var lastElement = elementList.Last();

            // iterate over all the elements, animating each of them
            double delay = 0;

            foreach (var element in elementList)
            {
                var sb = GetFoldAnimation(element, delay, true);

                // add a Completed event handler to the last element
                if (element.Equals(lastElement))
                {
                    sb.Completed += (s, e) => endAction();
                }

                sb.Begin();
                delay += 0;
            }
        }


        /// <summary>
        /// Creates a PlaneProjection and associates it with the given element, returning
        /// a Storyboard which will animate the PlaneProjection to 'peel' the item
        /// from the screen.
        /// </summary>
        private static Storyboard GetFoldAnimation(FrameworkElement element, double delay, bool isFoldDown)
        {
            var projection = new PlaneProjection()
            {
                CenterOfRotationX = -0.1
            };
            element.Projection = projection;

            // animate the projection
            var sb = new Storyboard
            {
                BeginTime = TimeSpan.FromMilliseconds(delay)
            };
            
            if (isFoldDown)
                sb.Children.Add(CreateAnimation(0, 90 , 0.2, "RotationX", projection));
            else // FoldUp
            {
                sb.Children.Add(CreateAnimation(-90, 0, 0.3, "RotationX", projection));
            }

            return sb;
        }


        private static DoubleAnimation CreateAnimation(double from, double to, double duration,
          string targetProperty, DependencyObject target)
        {
            var db = new DoubleAnimation
            {
                To = to,
                From = from,                
                Duration = TimeSpan.FromSeconds(duration)
            };
            Storyboard.SetTarget(db, target);
            Storyboard.SetTargetProperty(db, new PropertyPath(targetProperty));
            return db;
        }

        /// <summary>
        /// Creates a TranslateTransform and associates it with the given element, returning
        /// a Storyboard which will animate the TranslateTransform with a SineEase function
        /// </summary>
        private static Storyboard GetSlideAnimation(FrameworkElement element, bool fromRight)
        {
            double from = fromRight ? 80 : -80;

            double delay = (PeelAnimation.GetAnimationLevel(element)) * 0.1 + 0.1;

            var trans = new TranslateTransform() { X = from };
            element.RenderTransform = trans;

            var sb = new Storyboard
            {
                BeginTime = TimeSpan.FromSeconds(delay)
            };
            sb.Children.Add(CreateAnimation(from, 0, 0.2, "X", trans));
            return sb;
        }


    }
}
