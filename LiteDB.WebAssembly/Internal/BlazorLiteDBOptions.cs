using LiteDB.Engine;
using LiteDB.Engine.Disk.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.WebAssembly.Internal
{
    public class BlazorLiteDBOptions : StreamOptions
    {
        public string FileName { get; set; }
        public bool UseChache { get; set; }


        internal StreamOptions GetStreamOptions()
        {
            return this;
        }
        public BlazorLiteDBOptions WithWriteCaches(bool value = true)
        {
            this.UseCache = true;
            return this;
        }
        public BlazorLiteDBOptions WithLocalStorage()
        {
            this.StorageBackend = StorageBackends.LocalStorage;
            return this;
        }
        public BlazorLiteDBOptions WithIndexdDb()
        {
            this.StorageBackend = StorageBackends.IndexedDb;
            return this;
        }

        //public BlazorLiteDBOptions WithReadCache(bool value = true)
        //{
        //    this.UseReadCaches = true;
        //    return this;
        //}


        public BlazorLiteDBOptions Clone()
        {
            var result = new BlazorLiteDBOptions();
            this.GetType()
                .GetProperties()
                .ToList()
                .ForEach(x => x.SetValue(result, x.GetValue(this)));
            return result;
        }

    }
}
