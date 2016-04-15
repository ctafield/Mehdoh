using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Mitter.UserControls
{
    public partial class SpecialCharacters : UserControl
    {

        public SpecialCharacters()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(SpecialCharacters_Loaded);

        }

        public string SelectedCharacter { get; set; }
        public event EventHandler CharacterSelected;

        void SpecialCharacters_Loaded(object sender, RoutedEventArgs e)
        {
            CreateCharacters();
            Focus();
        }

        private void CreateCharacters()
        {

            // these are a few emjoy characters
            // they seem to use two characters and go wrong when its posted.
            // save it for another day....
            // 😁😁💤😣

            var characters = new List<string>()
                                          {
                                              "\u00A1", "\u00BF", "\u00A9", "\u00AE", 
                                              "\u201C", "\u201D", "\u2018", "\u2019",
                                              "\u00BC", "\u00BD", "\u00BE", "\u260A",
                                              "\u2669", "\u266A", "\u266B", "\u266C", 
                                              "\u2666", "\u2665", "\u2663", "\u2660",
                                              "\u00D7", "\u00F7", "\u00B2", "\u00B3"
                                              
                                          };

            wrap.Children.Clear();

            foreach (var character in characters)
            {
                wrap.Children.Add(CreateBox(character));
            }

        }

        private FrameworkElement CreateBox(string character)
        {
            var button = new Button
                             {
                                 Margin = new Thickness(10),
                                 Padding = new Thickness(5),
                                 BorderThickness = new Thickness(2),
                                 Content = character,
                                 FontSize = 36,
                                 Width = 90,
                                 Height = 90
                             };
            button.Click += new RoutedEventHandler(button_Click);
            return button;
        }

        void button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            SelectedCharacter = (string)button.Content;

            if (CharacterSelected != null)
                CharacterSelected(this, new EventArgs());
        }

    }
}
