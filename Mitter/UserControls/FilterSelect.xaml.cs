using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lumia.Imaging;
using Lumia.Imaging.Adjustments;
using Lumia.Imaging.Artistic;

namespace Mitter.UserControls
{

    public partial class FilterSelect : UserControl
    {

        private const int OriginalWidth = 150;
        private const int OriginalHeight = 150;

        private List<IFilter> Filters { get; set; }

        public static readonly DependencyProperty SelectedFilterProperty =
            DependencyProperty.Register("SelectedFilter", typeof(IFilter), typeof(FilterSelect), new PropertyMetadata(default(IFilter)));

        public IFilter SelectedFilter
        {
            get
            {
                if (Filters == null)
                    return null;

                return (IFilter)GetValue(SelectedFilterProperty) ?? Filters[0];
            }
            private set
            {
                SetValue(SelectedFilterProperty, value);
            }
        }

        public FilterSelect()
        {
            InitializeComponent();
        }

        public void ClearImages()
        {
            Filters = null;
            stackImages.Children.Clear();
        }

        public async Task PrepImages(Stream chosenPhoto)
        {
            Filters = new List<IFilter>
                      {
                          new AutoEnhanceFilter(true, true),
                          new LomoFilter(0.7, 0.5, LomoVignetting.High, LomoStyle.Neutral),
                          new LomoFilter(0.2, 0.5, LomoVignetting.Low, LomoStyle.Yellow),
                          new LomoFilter(0.5, 0.5, LomoVignetting.Medium, LomoStyle.Red),
                          new AntiqueFilter(),
                          new SepiaFilter(),
                          new GrayscaleFilter(),
                          new MagicPenFilter(),
                          new CartoonFilter(true),
                          new NegativeFilter(),
                          new SketchFilter(SketchMode.Color),
                          new SketchFilter(SketchMode.Gray)
                      };
            
            for (var i = 0; i < Filters.Count; i++)
            {
                await AddImage(i, chosenPhoto, Filters[i]);
            }

        }

        private async Task AddImage(int index, Stream chosenPhoto, IFilter filter)
        {

            var image = new Image()
                        {
                            Width = OriginalWidth, 
                            Height = OriginalHeight, 
                            Stretch = Stretch.UniformToFill
                        };

            var button = new Button
                         {
                             BorderBrush = (index == 0) ? Resources["PhoneAccentBrush"] as SolidColorBrush : new SolidColorBrush(Colors.Transparent),
                             BorderThickness = new Thickness(3),
                             Margin = new Thickness(0, 0, 20, 0),
                             Tag = filter, 
                             Padding = new Thickness(0),
                             Content = image,
                             VerticalContentAlignment = VerticalAlignment.Stretch,
                             HorizontalContentAlignment = HorizontalAlignment.Stretch
                         };

            button.Click += delegate(object sender, RoutedEventArgs args)
                            {
                                var clickedButton = sender as Button;
                                SelectedFilter = clickedButton.Tag as IFilter;

                                foreach (Button thisButton in stackImages.Children.OfType<Button>())
                                {
                                    thisButton.BorderBrush = new SolidColorBrush(Colors.Transparent);
                                }

                                clickedButton.BorderBrush = Resources["PhoneAccentBrush"] as SolidColorBrush;
                            };

            // Rewind stream to start.                     
            chosenPhoto.Position = 0;

            var imageStream = new StreamImageSource(chosenPhoto);

            var cartoonEffect = new FilterEffect(imageStream)
                                {
                                    Filters = new[] {filter}
                                };

            var imageBitmap = new WriteableBitmap(OriginalWidth, OriginalHeight);

            // Render the image to a WriteableBitmap.
            var renderer = new WriteableBitmapRenderer(cartoonEffect, imageBitmap);
            imageBitmap = await renderer.RenderAsync();

            // Set the rendered image as source for the cartoon image control.
            image.Source = imageBitmap;

            stackImages.Children.Add(button);
        }

    }

}

