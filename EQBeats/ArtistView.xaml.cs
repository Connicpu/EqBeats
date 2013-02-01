﻿using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using EQBeats.Models;
using EQBeatsAPI;
using EQBeatsPlaybackAgent;
using Microsoft.Phone.BackgroundAudio;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace EQBeats {
    public partial class ArtistView {
        private int _id;

        public ArtistView() {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) {
            var id = int.Parse(NavigationContext.QueryString["id"]);
            _id = id;
            ResourceManager.GetUserById(
                id, user => Dispatcher.BeginInvoke(() => DataContext = user),
                exception => Dispatcher.BeginInvoke(() => MessageBox.Show("Couldn't load artist! :<")));
            UpdatePinnedButton();
        }

        private void UpdatePinnedButton() {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (!settings.Contains("pinned_favs")) return;
            var pinned = (PinnedArtists)settings["pinned_favs"];
            var button = (ApplicationBarIconButton)ApplicationBar.Buttons[1];
            if (pinned.Ids.Contains(_id)) {
                button.IconUri = new Uri("/Icons/minus.png", UriKind.Relative);
                button.Text = "remove fav";
            } else {
                button.IconUri = new Uri("/Icons/favs.addto.png", UriKind.Relative);
                button.Text = "add to favs";
            }
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

        private void PlaylistButtonClicked(object sender, RoutedEventArgs e) {
            var button = (Button)sender;
            var playlist = (Playlist)button.DataContext;
            NavigationService.Navigate(new Uri("/PlaylistView.xaml?id=" + playlist.Id, UriKind.Relative));
        }

        private void NowPlayingClicked(object sender, EventArgs e) {

        }

        private void AddFavClick(object sender, EventArgs e) {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            var artist = DataContext as User;
            if (artist == null) {
                return;
            }
            if (settings.Contains("pinned_favs")) {
                var pinned = (PinnedArtists)settings["pinned_favs"];
                if (!pinned.Ids.Contains(artist.Id)) pinned.Ids.Add(artist.Id);
                else pinned.Ids.Remove(artist.Id);
                settings["pinned_favs"] = pinned;
            } else {
                var pinned = new PinnedArtists();
                pinned.Ids.Add(artist.Id);
                settings["pinned_favs"] = pinned;
            }
            settings.Save();
            UpdatePinnedButton();
        }
    }
}