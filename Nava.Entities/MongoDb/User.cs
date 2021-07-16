using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using Nava.Common.Utilities;

namespace Nava.Entities.MongoDb
{
    [BsonCollection("Users")]
    public class User : Document
    {
        public User()
        {
            Roles = new List<string>();
            FollowingArtists = new List<ObjectId>();
            LikedMedias = new List<ObjectId>();
            VisitedMedias = new List<ObjectId>();
        }
        public string UserName { get; set; }
        public string PasswordHash { get; set; }
        public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
        public string FullName { get; set; }
        public string AvatarPath { get; set; }
        public string Bio { get; set; }
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; }
        public List<ObjectId> FollowingArtists { get; set; }
        public List<ObjectId> LikedMedias { get; set; }
        public List<ObjectId> VisitedMedias { get; set; }
    }
}
