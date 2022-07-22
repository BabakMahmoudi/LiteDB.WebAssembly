using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Engine
{
    public class IndexedDbStream : AbstractStream
    {
        private readonly IJSRuntime runtime;
        private DotNetObjectReference<IndexedDbStream> objRef;
        private IJSObjectReference _module;
        private IJSObjectReference _import;

        public IndexedDbStream(IJSRuntime runtime, string dbName, StreamOptions options) : base(dbName, options)
        {
            this.runtime = runtime;


        }
        public async Task<IJSObjectReference> GetModule()
        {
            var n = this.GetType().Assembly.GetName().Name;// + ".Blazor";
            var js = $"/_content/{n}/litedb.js";
            if (this._module == null)
            {
                this.objRef = DotNetObjectReference.Create(this);
                this._import = await runtime.InvokeAsync<IJSObjectReference>("import", js);
                this._module = await this._import.InvokeAsync<IJSObjectReference>("createInstance", this._name, this.objRef);
            }
            return this._module;
        }

        protected override async Task DoDeletePages(PageData[] pages)
        {
            var ff = pages.ToArray();
            await (await this.GetModule())
                .InvokeVoidAsync("delete", ff,"lll");
        }

        protected override async Task<long> DoGetPageCount()
        {
            return await (await this.GetModule())
                .InvokeAsync<long>("getCount");

        }

        protected override async Task DoInitializeAsync()
        {
            var m = await this.GetModule();
            var res = await m.InvokeAsync<InitModel>("initialize");
            this._length = res.Length * PAGE_SIZE;
        }

        protected override async Task<PageData> DoReadAsync(long page, string key)
        {
            var res = await (await this.GetModule())
                .InvokeAsync<PageData>("read", page, key);

            return res;

        }
        protected override async Task<long> DoWriteAsync(PageData[] pages)
        {
            await (await this.GetModule())
                .InvokeVoidAsync("write", pages, "ll");
            return 0;
        }
        public override async ValueTask DisposeAsync()
        {
            if (this._module != null)
                await this._module.DisposeAsync();
            if (this._import != null)
                await this._import.DisposeAsync();
            this._module = null;
            this._import = null;
        }
    }
}
