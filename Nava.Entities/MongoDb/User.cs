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
        public string Name { get; set; }
    }
}
