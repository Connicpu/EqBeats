using System;
using System.Linq;
using System.Windows;
using EqBeatsAPI;

namespace EqBeats {
    public partial class FindArtists {
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