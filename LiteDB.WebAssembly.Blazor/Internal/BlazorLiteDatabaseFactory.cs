using LiteDB.Engine;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace LiteDB.WebAssembly.Blazor.Internal
{
    class BlazorLiteDatabaseFactory : IBlazorLiteDatabaseFactory
    {
        private readonly BlazorLiteDBOptions options;
        private readonly IServiceProvider serviceProvider;

        public BlazorLiteDatabaseFactory(BlazorLiteDBOptions options, IServiceProvider serviceProvider)
        {
            this.options = options;
            this.serviceProvider = serviceProvider;
        }
        public ILiteDatabase Create(string name, Action<BlazorLiteDBOptions> configure = null)
        {
            var options = this.serviceProvider.GetService<BlazorLiteDBOptions>();
            configure?.Invoke(options);
            name = string.IsNullOrWhiteSpace(name) ? options.FileName : name;
            return new LiteDatabase(new IndexedDbStream(serviceProvider.GetService<IJSRuntime>(), 
                name, 
                options.GetStreamOptions()));
        }
    }
}
