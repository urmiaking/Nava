﻿using System;
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
        }
        public string UserName { get; set; }
        public string PasswordHash { get; set; }
        public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();
        public string SecurityStamp { get; set; }
        public string FullName { get; set; }
        public string AvatarPath { get; set; }
        public string Bio { get; set; }
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; }
    }
}
