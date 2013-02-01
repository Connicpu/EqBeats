using System;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EqBeats.Models;
using EqBeatsAPI;
using EqBeatsPlaybackAgent;
using Microsoft.Phone.BackgroundAudio;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace EqBeats {
    public partial class MainPage {
        private bool justLaunched = true;

        // Constructor
        public MainPage() {
            InitializeComponent();
            ResourceManager.FeaturedBeats(
                tracks => Dispatcher.BeginInvoke(() => {
                    FeaturedSongList.ItemsSource = tracks.Length > 25 ? tracks.Take(25).ToArray() : tracks;
                }),
                exception => Dispatcher.BeginInvoke(() => MessageBox.Show("Error loading songs!")));
            ResourceManager.LatestBeats(
                tracks => Dispatcher.BeginInvoke(() => {
                    LatestSongList.ItemsSource = tracks.Length > 25 ? tracks.Take(25).ToArray() : tracks;
                }),
                exception => { });
            ResourceManager.RandomBeats(
                tracks => Dispatcher.BeginInvoke(() => {
                    RandomSongList.ItemsSource = tracks.Length > 25 ? tracks.Take(25).ToArray() : tracks;
                }),
                exception => { });

            BackgroundAudioPlayer.Instance.PlayStateChanged += PlayStateChanged;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains("pinned_favs")) {
                var faves = (PinnedArtists)settings["pinned_favs"];
                FavoriteArtists.ItemsSource = new ObservableCollection<User>();
                foreach (var fav in faves.Ids) {
                    ResourceManager.GetUserById(
                        fav, user => Dispatcher.BeginInvoke(
                            () => ((ObservableCollection<User>)FavoriteArtists.ItemsSource).Add(user)),
                        exception => { });
                }
            }

            if (justLaunched) {
                if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing) {
                    NavigationService.Navigate(new Uri("/NowPlaying.xaml", UriKind.Relative));
                }
            }
            justLaunched = false;
        }

        private void PlayButtonClicked(object sender, RoutedEventArgs e) {
            var button = (Button)sender;
            var song = (Track)button.DataContext;
            var playlist = (Track[])GetParentControl(button).ItemsSource;
            var index = Array.IndexOf(playlist, song);

            AudioPlayer.SavePlaylist(playlist, index);

            BackgroundAudioPlayer.Instance.Track = song.ToAudioTrack("Update");
            BackgroundAudioPlayer.Instance.Play();
            NavigationService.Navigate(new Uri("/NowPlaying.xaml", UriKind.Relative));
        }

        private static ItemsControl GetParentControl(DependencyObject button) {
            var parent = button;
            while (!(parent is ItemsControl)) {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return (ItemsControl)parent;
        }

        private void SearchClicked(object sender, RoutedEventArgs e) {
            if (String.IsNullOrWhiteSpace(Search.Text)) {
                MessageBox.Show("Please enter a search");
                return;
            }
            SearchedSongList.ItemsSource = new Track[0];
            ResourceManager.SearchTracks(Search.Text.Trim(),
                tracks => Dispatcher.BeginInvoke(() => {
                    SearchedSongList.ItemsSource = tracks.Length > 25 ? tracks.Take(25).ToArray() : tracks;
                }),
                exception => Dispatcher.BeginInvoke(() => MessageBox.Show("Error loading songs!")));
            Search.Text = "";
        }

        private void PanoramaSelectionChanged(object sender, SelectionChangedEventArgs e) {
            switch (((PanoramaItem)Panorama.SelectedItem).Header.ToString()) {
                case "Favorites":
                case "Search":
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
                    break;
                default:
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = true;
                    break;
            }
        }

        private void RefreshClicked(object sender, EventArgs e) {
            switch (((PanoramaItem)Panorama.SelectedItem).Header.ToString()) {
                case "Latest":
                    LatestSongList.ItemsSource = new Track[0];
                    ResourceManager.LatestBeats(
                        tracks => Dispatcher.BeginInvoke(() => {
                            LatestSongList.ItemsSource = tracks.Length > 25 ? tracks.Take(25).ToArray() : tracks;
                        }),
                        exception => { });
                    break;
                case "Featured":
                    FeaturedSongList.ItemsSource = new Track[0];
                    ResourceManager.FeaturedBeats(
                        tracks => Dispatcher.BeginInvoke(() => {
                            FeaturedSongList.ItemsSource = tracks.Length > 25 ? tracks.Take(25).ToArray() : tracks;
                        }),
                        exception => { });
                    break;
                case "Random":
                    RandomSongList.ItemsSource = new Track[0];
                    ResourceManager.RandomBeats(
                        tracks => Dispatcher.BeginInvoke(() => {
                            RandomSongList.ItemsSource = tracks.Length > 25 ? tracks.Take(25).ToArray() : tracks;
                        }),
                        exception => { });
                    break;
            }
        }

        private void NowPlayingClicked(object sender, EventArgs e) {
            switch (BackgroundAudioPlayer.Instance.PlayerState) {
                case PlayState.Shutdown:
                case PlayState.Unknown:
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = false;
                    return;
                default:
                    NavigationService.Navigate(new Uri("/NowPlaying.xaml", UriKind.Relative));
                    return;
            }
        }

        private void PlayStateChanged(object sender, EventArgs eventArgs) {
            var playState = BackgroundAudioPlayer.Instance.PlayerState;
            switch (playState) {
                case PlayState.Shutdown:
                case PlayState.Unknown:
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = false;
                    break;
                default:
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = true;
                    break;
            }
        }

        ~MainPage() {
            BackgroundAudioPlayer.Instance.PlayStateChanged -= PlayStateChanged;
        }

        private void ArtistsClick(object sender, EventArgs e) {
            NavigationService.Navigate(new Uri("/FindArtists.xaml", UriKind.Relative));
        }

        private void ArtistItemClicked(object sender, RoutedEventArgs e) {
            var artist = ((FrameworkElement) sender).DataContext as User;
            if (artist == null) return;
            NavigationService.Navigate(new Uri("/ArtistView.xaml?id=" + artist.Id, UriKind.Relative));
        }
    }
}