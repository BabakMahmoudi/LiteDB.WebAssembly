using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Engine.Disk.Streams.Adapters
{
    public class LocalStorageAdapter:BaseAdapter
    {
        public LocalStorageAdapter(IJSRuntime runtime, string dbName) :  base(runtime, dbName, StorageBackends.LocalStorage)
        {

        }

    }
}
