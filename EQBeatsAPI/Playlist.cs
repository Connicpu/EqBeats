using System;

namespace EQBeatsAPI {
    public class Playlist {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public User Author { get; set; }
        public Track[] Tracks { get; set; }
        public Uri Link { get; set; }
    }
}
