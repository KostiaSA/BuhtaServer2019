using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuhtaServer
{
    public class BuhtaConfig
    {
        public string serverUniqueName;
        public List<Database> databases=new List<Database>();

        public Database GetDatabase(string dbName)
        {
            return databases.Find(db => db.Name.Equals(dbName,StringComparison.InvariantCultureIgnoreCase));
        }
    }

    public class Database
    {
        public string Name;
        public string Dialect;
        public string Note;
        public string ConnectionString;
        public string SqlName;
    }
}
