using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading;
using System.Windows;
using EqBeatsAPI;
using Microsoft.Phone.BackgroundAudio;
using Newtonsoft.Json.Linq;

namespace EqBeatsPlaybackAgent {
    public class AudioPlayer : AudioPlayerAgent {
        private static volatile bool _classInitialized;

        public static Track[] CurrentPlaylist = new Track[0];
        public static int PlaylistLocation = 0;

        public static void LoadPlaylist() {
            using (var mutex = new Mutex(true, "eqb_playlist")) {
                mutex.WaitOne();
                try {
                    if (!IsolatedStorageFile.GetUserStoreForApplication().FileExists("playlist")) {
                        CurrentPlaylist = new Track[0];
                        PlaylistLocation = 0;
                        return;
                    }
                    using (var fs = IsolatedStorageFile.GetUserStoreForApplication().
                                    OpenFile("playlist", FileMode.Open, FileAccess.Read))
                    using (var reader = new StreamReader(fs)) {
                        var text = reader.ReadToEnd();
                        var data = JObject.Parse(text);
                        CurrentPlaylist = data["playlist"].ToObject<Track[]>();
                        PlaylistLocation = (int)data["position"];
                    }
                } finally {
                    mutex.ReleaseMutex();
                }
            }
        }

        public static void SavePlaylist(Track[] playlist, int position) {
            using (var mutex = new Mutex(true, "eqb_playlist")) {
                mutex.WaitOne();
                try {
                    if (IsolatedStorageFile.GetUserStoreForApplication().FileExists("playlist")) {
                        IsolatedStorageFile.GetUserStoreForApplication().DeleteFile("playlist");
                    }
                    using (var fs = IsolatedStorageFile.GetUserStoreForApplication().
                                    OpenFile("playlist", FileMode.OpenOrCreate, FileAccess.Write))
                    using (var writer = new StreamWriter(fs)) {
                        var data = new JObject();
                        data["playlist"] = JArray.FromObject(playlist);
                        data["position"] = position;
                        writer.Write(data);
                    }
                } finally {
                    mutex.ReleaseMutex();
                }
            }
        }

        /// <remarks>
        /// AudioPlayer instances can share the same process. 
        /// Static fields can be used to share state between AudioPlayer instances
        /// or to communicate with the Audio Streaming agent.
        /// </remarks>
        public AudioPlayer() {
            if (_classInitialized) return;
            _classInitialized = true;

            // Subscribe to the managed exception handler
            Deployment.Current.Dispatcher.BeginInvoke(delegate {
                Application.Current.UnhandledException += AudioPlayer_UnhandledException;
            });
        }

        /// Code to execute on Unhandled Exceptions
        private void AudioPlayer_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e) {
            if (System.Diagnostics.Debugger.IsAttached) {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        /// <summary>
        /// Called when the playstate changes, except for the Error state (see OnError)
        /// </summary>
        /// <param name="player">The BackgroundAudioPlayer</param>
        /// <param name="track">The track playing at the time the playstate changed</param>
        /// <param name="playState">The new playstate of the player</param>
        /// <remarks>
        /// Play State changes cannot be cancelled. They are raised even if the application
        /// caused the state change itself, assuming the application has opted-in to the callback.
        /// 
        /// Notable playstate events: 
        /// (a) TrackEnded: invoked when the player has no current track. The agent can set the next track.
        /// (b) TrackReady: an audio track has been set and it is now ready for playack.
        /// 
        /// Call NotifyComplete() only once, after the agent request has been completed, including async callbacks.
        /// </remarks>
        protected override void OnPlayStateChanged(BackgroundAudioPlayer player, AudioTrack track, PlayState playState) {
            player.Volume = 1;
            switch (playState) {
                case PlayState.TrackEnded:
                    try {
                        player.Track = GetNextTrack();
                    } catch (LastTrackException ex) {
                        player.Track = ex.Track;
                        player.Stop();
                    }
                    break;
                case PlayState.TrackReady:
                    LoadPlaylist();
                    player.Play();
                    break;
                case PlayState.Shutdown:
                    break;
                case PlayState.Unknown:
                    break;
                case PlayState.Stopped:
                    break;
                case PlayState.Paused:
                    break;
                case PlayState.Playing:
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

            NotifyComplete();
        }


        /// <summary>
        /// Called when the user requests an action using application/system provided UI
        /// </summary>
        /// <param name="player">The BackgroundAudioPlayer</param>
        /// <param name="track">The track playing at the time of the user action</param>
        /// <param name="action">The action the user has requested</param>
        /// <param name="param">The data associated with the requested action.
        /// In the current version this parameter is only for use with the Seek action,
        /// to indicate the requested position of an audio track</param>
        /// <remarks>
        /// User actions do not automatically make any changes in system state; the agent is responsible
        /// for carrying out the user actions if they are supported.
        /// 
        /// Call NotifyComplete() only once, after the agent request has been completed, including async callbacks.
        /// </remarks>
        protected override void OnUserAction(BackgroundAudioPlayer player, AudioTrack track, UserAction action, object param) {
            switch (action) {
                case UserAction.Play:
                    if (player.PlayerState != PlayState.Playing) {
                        player.Play();
                    }
                    break;
                case UserAction.Stop:
                    player.Stop();
                    break;
                case UserAction.Pause:
                    player.Pause();
                    break;
                case UserAction.FastForward:
                    player.FastForward();
                    break;
                case UserAction.Rewind:
                    player.Rewind();
                    break;
                case UserAction.Seek:
                    player.Position = (TimeSpan)param;
                    break;
                case UserAction.SkipNext:
                    try {
                        player.Track = GetNextTrack();
                    } catch (LastTrackException ex) {
                        player.Track = ex.Track;
                        player.Stop();
                    }
                    break;
                case UserAction.SkipPrevious:
                    if (player.Position > TimeSpan.FromSeconds(7)) {
                        player.Position = new TimeSpan(0);
                    } else {
                        AudioTrack previousTrack = GetPreviousTrack();
                        if (previousTrack != null) {
                            player.Track = previousTrack;
                        }
                    }
                    break;
            }

            NotifyComplete();
        }


        /// <summary>
        /// Implements the logic to get the next AudioTrack instance.
        /// In a playlist, the source can be from a file, a web request, etc.
        /// </summary>
        /// <remarks>
        /// The AudioTrack URI determines the source, which can be:
        /// (a) Isolated-storage file (Relative URI, represents path in the isolated storage)
        /// (b) HTTP URL (absolute URI)
        /// (c) MediaStreamSource (null)
        /// </remarks>
        /// <returns>an instance of AudioTrack, or null if the playback is completed</returns>
        private static AudioTrack GetNextTrack() {
            AudioTrack track = null;

            if (CurrentPlaylist == null || CurrentPlaylist.Length < 1) {
                CurrentPlaylist = new Track[0];
                LoadPlaylist();
            }

            if (++PlaylistLocation < CurrentPlaylist.Length) {
                track = (AudioTrack)CurrentPlaylist[PlaylistLocation];
                SavePlaylist(CurrentPlaylist, PlaylistLocation);
            } else if (CurrentPlaylist.Length > 0) {
                PlaylistLocation = 0;
                track = (AudioTrack)CurrentPlaylist[PlaylistLocation];
                SavePlaylist(CurrentPlaylist, PlaylistLocation);
                throw new LastTrackException(track);
            }

            return track;
        }


        /// <summary>
        /// Implements the logic to get the previous AudioTrack instance.
        /// </summary>
        /// <remarks>
        /// The AudioTrack URI determines the source, which can be:
        /// (a) Isolated-storage file (Relative URI, represents path in the isolated storage)
        /// (b) HTTP URL (absolute URI)
        /// (c) MediaStreamSource (null)
        /// </remarks>
        /// <returns>an instance of AudioTrack, or null if previous track is not allowed</returns>
        private static AudioTrack GetPreviousTrack() {
            AudioTrack track = null;

            if (CurrentPlaylist == null || CurrentPlaylist.Length < 1) {
                CurrentPlaylist = new Track[0];
                LoadPlaylist();
            }

            if (--PlaylistLocation >= 0) {
                track = (AudioTrack)CurrentPlaylist[PlaylistLocation];
                SavePlaylist(CurrentPlaylist, PlaylistLocation);
            } else if (CurrentPlaylist.Length > 0) {
                PlaylistLocation = CurrentPlaylist.Length - 1;
                track = (AudioTrack)CurrentPlaylist[PlaylistLocation];
                SavePlaylist(CurrentPlaylist, PlaylistLocation);
            }

            return track;
        }

        /// <summary>
        /// Called whenever there is an error with playback, such as an AudioTrack not downloading correctly
        /// </summary>
        /// <param name="player">The BackgroundAudioPlayer</param>
        /// <param name="track">The track that had the error</param>
        /// <param name="error">The error that occured</param>
        /// <param name="isFatal">If true, playback cannot continue and playback of the track will stop</param>
        /// <remarks>
        /// This method is not guaranteed to be called in all cases. For example, if the background agent 
        /// itself has an unhandled exception, it won't get called back to handle its own errors.
        /// </remarks>
        protected override void OnError(BackgroundAudioPlayer player, AudioTrack track, Exception error, bool isFatal) {
            if (isFatal) {
                Abort();
            } else {
                NotifyComplete();
            }

        }

        /// <summary>
        /// Called when the agent request is getting cancelled
        /// </summary>
        /// <remarks>
        /// Once the request is Cancelled, the agent gets 5 seconds to finish its work,
        /// by calling NotifyComplete()/Abort().
        /// </remarks>
        protected override void OnCancel() {

        }

        public class LastTrackException : Exception {
            public LastTrackException(AudioTrack track) {
                Track = track;
            }

            public AudioTrack Track;
        }
    }
}
