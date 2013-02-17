using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using EqBeatsAPI;
using System.Windows.Controls;
using Microsoft.Phone.Shell;

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

            StartDownload();

            ResourceManager.SearchUsers(Search.Text.Trim(),
                users => Dispatcher.BeginInvoke(() => {
                    ArtistList.ItemsSource = users.Take(25);
                    DownloadDone(users.Length > 0);
                }),
                exception => Dispatcher.BeginInvoke(() => MessageBox.Show("Error loading songs!")));
        }

        private void ArtistClick(object sender, RoutedEventArgs e) {
            var artist = (User)((FrameworkElement)sender).DataContext;
            NavigationService.Navigate(new Uri("/ArtistView.xaml?id=" + artist.Id, UriKind.Relative));
        }

        public void StartDownload() {
            ArtistList.Visibility = Visibility.Collapsed;
            NoResults.Visibility = Visibility.Collapsed;
            SystemTray.SetProgressIndicator(this, new ProgressIndicator { IsVisible = true, IsIndeterminate = true });
        }

        public void DownloadDone(bool anyItems) {
            SystemTray.SetProgressIndicator(this, null);
            if (!anyItems) {
                ArtistList.Visibility = Visibility.Collapsed;
                NoResults.Visibility = Visibility.Visible;
            } else {
                ArtistList.Visibility = Visibility.Visible;
                NoResults.Visibility = Visibility.Collapsed;
            }
        }
    }
}