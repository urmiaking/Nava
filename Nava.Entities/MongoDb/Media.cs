using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Nava.Common.Utilities;
using Nava.Entities.Media;

namespace Nava.Entities.MongoDb
{
    [BsonCollection("Medias")]
    public class Media : Document
    {
        public Media()
        {
            LikedUsers = new List<ObjectId>();
            VisitedUsers = new List<ObjectId>();
        }

        public string Title { get; set; }
        public MediaType Type { get; set; }
        public string FilePath { get; set; }
        [BsonDateTimeOptions]
        public DateTime ReleaseDate { get; set; }
        public string ArtworkPath { get; set; }
        // International Standard Recording Code (ISRC) for musics only
        public string Isrc { get; set; }
        public int TrackNumber { get; set; }
        public string Lyric { get; set; }

        public ObjectId AlbumId { get; set; }
        public List<ObjectId> LikedUsers { get; set; }
        public List<ObjectId> VisitedUsers { get; set; }
    }
}
