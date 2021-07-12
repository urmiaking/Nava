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
    [BsonCollection("Artists")]
    public class Artist : Document
    {
        public Artist()
        {
            Followers = new List<ObjectId>();
            Albums = new List<ObjectId>();
        }
        public string FullName { get; set; }
        public string ArtisticName { get; set; }
        [BsonDateTimeOptions]
        public DateTime BirthDate { get; set; }
        public string AvatarPath { get; set; }
        public string Bio { get; set; }
        public List<ObjectId> Followers { get; set; }
        public List<ObjectId> Albums { get; set; }
    }
}
