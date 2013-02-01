using System;
using System.Collections.Generic;
using System.Windows;

namespace EqBeatsAPI {
    public static class ResourceManager {
        public const string APIBase = "https://eqbeats.org";
        public const string APIQualifier = "/json";

        #region Cache
        internal static readonly Dictionary<int, Track> TrackCache = new Dictionary<int, Track>();
        internal static readonly Dictionary<int, ResourceAcquirer<Track>> RunningTrackAcquirers 
            = new Dictionary<int, ResourceAcquirer<Track>>();

        internal static readonly Dictionary<int, Playlist> PlaylistCache = new Dictionary<int, Playlist>();
        internal static readonly Dictionary<int, ResourceAcquirer<Playlist>> RunningPlaylistAcquirers
            = new Dictionary<int, ResourceAcquirer<Playlist>>();

        internal static readonly Dictionary<int, User> UserCache = new Dictionary<int, User>();
        internal static readonly Dictionary<int, ResourceAcquirer<User>> RunningUserAcquirers
            = new Dictionary<int, ResourceAcquirer<User>>();
        #endregion

        #region Standard Resource Acquiring
        public static void GetTrackById(int id, ResourceAcquirer<Track>.AcquisitionCompleteHandler complete,
                                                ResourceAcquirer<Track>.AcquisitionFailedHandler failed) {
            Aquire(id, "/tracks", TrackCache, RunningTrackAcquirers, complete, failed);
        }

        public static void GetUserById(int id, ResourceAcquirer<User>.AcquisitionCompleteHandler complete,
                                                ResourceAcquirer<User>.AcquisitionFailedHandler failed) {
            Aquire(id, "/user", UserCache, RunningUserAcquirers, complete, failed);
        }

        public static void GetPlaylistById(int id, ResourceAcquirer<Playlist>.AcquisitionCompleteHandler complete,
                                                ResourceAcquirer<Playlist>.AcquisitionFailedHandler failed) {
            Aquire(id, "/playlist", PlaylistCache, RunningPlaylistAcquirers, complete, failed);
        }

        private static void Aquire<T>(int id, string apiRoot, Dictionary<int, T> cache,
                                      Dictionary<int, ResourceAcquirer<T>> acquirers,
                                      ResourceAcquirer<T>.AcquisitionCompleteHandler complete,
                                      ResourceAcquirer<T>.AcquisitionFailedHandler failed) {
            lock (cache) {
                if (cache.ContainsKey(id)) {
                    Deployment.Current.Dispatcher.BeginInvoke(() => complete(cache[id]));
                } else if (acquirers.ContainsKey(id)) {
                    var acquirer = acquirers[id];
                    acquirer.Complete += complete;
                    acquirer.Failure += failed;
                } else {
                    var acquirer = new ResourceAcquirer<T>(id, new Uri(APIBase + apiRoot + "/" + id + APIQualifier), 
                                                           cache, acquirers);
                    acquirer.Complete += complete;
                    acquirer.Failure += failed;
                    acquirer.Begin();
                    acquirers[id] = acquirer;
                }
            }
        }
        #endregion

        #region Endpoints
        public static void SearchUsers(string user, EndpointLoader<User[]>.EndpointLoadCompletedHandler complete, 
                                                    EndpointLoader<User[]>.EndpointLoadFailedHandler failed) {
            var loader = new EndpointLoader<User[]>(new Uri(APIBase + "/users/search/json?q=" + Uri.EscapeDataString(user)));
            loader.Complete += complete;
            loader.Failure += failed;
            loader.Begin();
        }
        public static void SearchTracks(string track, EndpointLoader<Track[]>.EndpointLoadCompletedHandler complete,
                                                      EndpointLoader<Track[]>.EndpointLoadFailedHandler failed) {
            var loader = new EndpointLoader<Track[]>(new Uri(APIBase + "/tracks/search/json?q=" + Uri.EscapeDataString(track)));
            loader.Complete += complete;
            loader.Failure += failed;
            loader.Begin();
        }
        public static void LatestBeats(EndpointLoader<Track[]>.EndpointLoadCompletedHandler complete,
                                       EndpointLoader<Track[]>.EndpointLoadFailedHandler failed) {
            var loader = new EndpointLoader<Track[]>(new Uri(APIBase + "/tracks/latest" + APIQualifier));
            loader.Complete += complete;
            loader.Failure += failed;
            loader.Begin();
        }
        public static void FeaturedBeats(EndpointLoader<Track[]>.EndpointLoadCompletedHandler complete,
                                         EndpointLoader<Track[]>.EndpointLoadFailedHandler failed) {
            var loader = new EndpointLoader<Track[]>(new Uri(APIBase + "/tracks/featured" + APIQualifier));
            loader.Complete += complete;
            loader.Failure += failed;
            loader.Begin();
        }
        public static void RandomBeats(EndpointLoader<Track[]>.EndpointLoadCompletedHandler complete,
                                       EndpointLoader<Track[]>.EndpointLoadFailedHandler failed) {
            var loader = new EndpointLoader<Track[]>(new Uri(APIBase + "/tracks/random" + APIQualifier));
            loader.Complete += complete;
            loader.Failure += failed;
            loader.Begin();
        }
        #endregion
    }
}
