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
using EqBeatsAPI;
using Microsoft.Phone.Controls;

namespace EqBeats {
    public partial class FindArtists : PhoneApplicationPage {
        public FindArtists() {
            InitializeComponent();
        }

        private void SearchClick(object sender, RoutedEventArgs e) {
            if (string.IsNullOrWhiteSpace(Search.Text)) {
                MessageBox.Show("Please enter a search");
                return;
            }

            ResourceManager.SearchUsers(Search.Text.Trim(),
                users => Dispatcher.BeginInvoke(() => ArtistList.ItemsSource = users.Length > 25 ? users.Take(25) : users),
                exception => Dispatcher.BeginInvoke(() => MessageBox.Show("Error loading songs!")));
        }

        private void ArtistClick(object sender, RoutedEventArgs e) {
            var artist = (User)((FrameworkElement) sender).DataContext;
            NavigationService.Navigate(new Uri("/ArtistView.xaml?id=" + artist.Id, UriKind.Relative));
        }
    }
}