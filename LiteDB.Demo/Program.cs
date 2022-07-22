using LiteDB.Engine;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using LiteDB.WebAssembly;

namespace LiteDB.Demo
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            builder.Services.AddTransient<ILiteDatabase>(sp => new LiteDatabase(new LocalStorageStream(sp.GetService<IJSRuntime>())));

            builder.Services.AddLiteDb();
            //builder.Services.AddTransient<ILiteDatabase>(sp => new LiteDatabase(new LocalStorageStream2(sp.GetService<IJSRuntime>())));
            //builder.Services.AddTransient<ILiteDatabase>(sp => new LiteDatabase(new IndexdDbStream(sp.GetService<IJSRuntime>(),"mydb")));
            //builder.Services.AddSingleton<ILiteDatabase>(sp => new LiteDatabase(new MemoryStream()));
            

            await builder.Build().RunAsync();
        }
    }
}
