using LiteDB;
using LiteDB.WebAssembly.Blazor;
using LiteDB.WebAssembly.Blazor.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class LiteDbBlazorServiceCollectionExtentions
    {
        public static IServiceCollection AddLiteDb(this IServiceCollection services, Action<BlazorLiteDBOptions> configure = null)
        {
            var options = new BlazorLiteDBOptions();
            configure?.Invoke(options);
            return services
                .AddSingleton(options)
                .AddSingleton<IBlazorLiteDatabaseFactory, BlazorLiteDatabaseFactory>()
                .AddScoped<ILiteDatabase>(sp => sp.GetService<IBlazorLiteDatabaseFactory>().Create(null));

        }
    }
}
