using LiteDB.WebAssembly.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.WebAssembly
{
    public interface IBlazorLiteDatabaseFactory
    {
        ILiteDatabase Create(string name,Action<BlazorLiteDBOptions> configure = null);

    }
}
