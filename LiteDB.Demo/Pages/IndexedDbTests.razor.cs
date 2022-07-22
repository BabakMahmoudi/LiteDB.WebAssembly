using LiteDB.Engine.Disk.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LiteDB.Demo.Pages
{
    public partial class IndexedDbTests
    {
        public override StorageBackends Backend => StorageBackends.IndexedDb;
        public IndexedDbTests()
        {

        }
        
    }
}
