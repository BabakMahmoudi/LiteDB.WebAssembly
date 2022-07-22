using LiteDB.WebAssembly.Blazor.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.WebAssembly.Blazor
{
    public interface IBlazorLiteDatabaseFactory
    {
        ILiteDatabase Create(string name,Action<BlazorLiteDBOptions> configure = null);

    }
}
