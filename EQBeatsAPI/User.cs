using System;

namespace EQBeatsAPI {
    public class User {
        public int Id { get; set; }
        public string Name { get; set; }
        public Uri Avatar { get; set; }
        public string Description { get; set; }
        public Track[] Tracks { get; set; }
        public Playlist[] Playlists { get; set; }
        public Uri Link { get; set; }
    }
}
