using System;
using System.Runtime.Serialization;
using Microsoft.Phone.BackgroundAudio;

namespace EqBeatsAPI {
    [DataContract]
    public class Track {

        [DataMember]
        public int Id;
        [DataMember]
        public string Title { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public User Artist { get; set; }
        [DataMember]
        public Uri Link { get; set; }
        [DataMember]
        public TrackDownload Download { get; set; }

        #region Operators
        public AudioTrack ToAudioTrack(string tag = null) {
            return new AudioTrack(
                Download.Mp3, //Source
                Title, //Title
                Artist.Name, //Artist
                null, //Album
                Download.Art,
                tag,
                EnabledPlayerControls.All); //Album art
        }

        public static explicit operator AudioTrack(Track eqbTrack) {
            return new AudioTrack(
                eqbTrack.Download.Mp3, //Source
                eqbTrack.Title, //Title
                eqbTrack.Artist.Name, //Artist
                null, //Album
                eqbTrack.Download.Art); //Album art
        }
        #endregion
    }

    [DataContract]
    public class TrackDownload {
        [DataMember]
        public Uri Art { get; set; }
        [DataMember]
        public Uri Opus { get; set; }
        [DataMember]
        public Uri Vorbis { get; set; }
        [DataMember]
        public Uri Aac { get; set; }
        [DataMember]
        public Uri Mp3 { get; set; }
        [DataMember]
        public Uri Original { get; set; }
    }
}
