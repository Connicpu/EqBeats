using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using EQBeatsAPI;
using EQBeatsPlaybackAgent;
using Microsoft.Phone.BackgroundAudio;

namespace EQBeats {
    public partial class NowPlaying {
        readonly DispatcherTimer positionTimer = new DispatcherTimer();

        public NowPlaying() {
            InitializeComponent();
            AudioPlayer.LoadPlaylist();
            BackgroundAudioPlayer.Instance.PlayStateChanged += InstanceOnPlayStateChanged;
            if (AudioPlayer.CurrentPlaylist != null && AudioPlayer.CurrentPlaylist.Length > 0) {
                var song = AudioPlayer.CurrentPlaylist[AudioPlayer.PlaylistLocation];
                DataContext = song;
            }
            positionTimer.Interval = new TimeSpan(0, 0, 0, 1);
            positionTimer.Tick += (sender, args) => {
                var track = BackgroundAudioPlayer.Instance.Track;
                if (track == null) {
                    if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Stopped ||
                        BackgroundAudioPlayer.Instance.PlayerState == PlayState.Shutdown) {
                        NavigationService.GoBack();
                    } else {
                        SongProgress.IsIndeterminate = true;
                    }
                    return;
                }
                var duration = track.Duration;
                var position = BackgroundAudioPlayer.Instance.Position;
                var percentage = 100d * position.Ticks / duration.Ticks;
                if (double.IsNaN(percentage) || double.IsInfinity(percentage)) {
                    percentage = 0;
                    SongProgress.IsIndeterminate = true;
                } else {
                    SongProgress.IsIndeterminate = false;
                }
                SongProgress.Value = percentage;
                PositionLabel.Text = position.ToString(@"mm\:ss");
                RemainingLabel.Text = (duration - position).ToString(@"mm\:ss");
                if (AudioPlayer.CurrentPlaylist != null && AudioPlayer.PlaylistLocation < AudioPlayer.CurrentPlaylist.Length - 1) {
                    UpNextGrid.Visibility = Visibility.Visible;
                    UpNextGrid.DataContext = AudioPlayer.CurrentPlaylist[AudioPlayer.PlaylistLocation + 1];
                } else {
                    UpNextGrid.Visibility = Visibility.Collapsed;
                }
            };
            positionTimer.Start();
            InstanceOnPlayStateChanged(null, null);
        }

        private void InstanceOnPlayStateChanged(object sender, EventArgs eventArgs) {
            var playState = BackgroundAudioPlayer.Instance.PlayerState;
            switch (playState) {
                case PlayState.TrackEnded:
                    SongProgress.IsIndeterminate = true;
                    break;
                case PlayState.TrackReady:
                    break;
                case PlayState.Shutdown:
                    NavigationService.GoBack();
                    break;
                case PlayState.Unknown:
                    break;
                case PlayState.Stopped:
                    PlayPauseImage.Source = new BitmapImage(new Uri("/Icons/play.png", UriKind.Relative));
                    break;
                case PlayState.Paused:
                    PlayPauseImage.Source = new BitmapImage(new Uri("/Icons/play.png", UriKind.Relative));
                    break;
                case PlayState.Playing:
                    AudioPlayer.LoadPlaylist();
                    var song = AudioPlayer.CurrentPlaylist[AudioPlayer.PlaylistLocation];
                    DataContext = song;
                    PlayPauseImage.Source = new BitmapImage(new Uri("/Icons/pause.png", UriKind.Relative));
                    break;
                case PlayState.BufferingStarted:
                    break;
                case PlayState.BufferingStopped:
                    break;
                case PlayState.Rewinding:
                    break;
                case PlayState.FastForwarding:
                    break;
            }
        }

        private void PlayPauseClick(object sender, RoutedEventArgs e) {
            if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing) {
                BackgroundAudioPlayer.Instance.Pause();
            } else {
                BackgroundAudioPlayer.Instance.Play();
            }
        }

        private void NextClick(object sender, RoutedEventArgs e) {
            BackgroundAudioPlayer.Instance.SkipNext();
        }

        private void PreviousClick(object sender, RoutedEventArgs e) {
            BackgroundAudioPlayer.Instance.SkipPrevious();
        }

        private void ArtistClick(object sender, RoutedEventArgs e) {
            var song = (Track)((FrameworkElement)sender).DataContext;
            NavigationService.Navigate(new Uri("/ArtistView.xaml?id=" + song.Artist.Id, UriKind.Relative));
        }
    }
}