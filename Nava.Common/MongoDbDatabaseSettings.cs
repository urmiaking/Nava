using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nava.Common
{
    public class MongoDbDatabaseSettings : IMongoDbDatabaseSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }

    public interface IMongoDbDatabaseSettings
    {
        string ConnectionString { get; set; }
        string DatabaseName { get; set; }
    }
}
