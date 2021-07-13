using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Nava.Common.Utilities;

namespace Nava.Entities.MongoDb
{
    [BsonCollection("Albums")]
    public class Album : Document
    {
        public Album()
        {
            Artists = new List<ObjectId>();
            Medias = new List<ObjectId>();
        }
        public string Title { get; set; }
        [BsonDateTimeOptions]
        public DateTime ReleaseDate { get; set; }
        public string Genre { get; set; }
        public bool IsComplete { get; set; }
        public bool IsSingle { get; set; }
        public string Copyright { get; set; }
        public string ArtworkPath { get; set; }

        public List<ObjectId> Artists { get; set; }
        public List<ObjectId> Medias { get; set; }
    }
}
