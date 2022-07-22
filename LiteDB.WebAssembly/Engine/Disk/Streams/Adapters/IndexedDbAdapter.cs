﻿using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Engine.Disk.Streams.Adapters
{
    class IndexedDbAdapter:IStorageAdapter
    {
        private readonly IJSRuntime runtime;
        private DotNetObjectReference<IndexedDbAdapter> objRef;
        private IJSObjectReference _module;
        private IJSObjectReference _import;
        private string _name;
        public IndexedDbAdapter(IJSRuntime runtime, string dbName) 
        {
            this.runtime = runtime;
            this._name = dbName;
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

        public virtual async Task DoDeletePages(PageData[] pages)
        {
            var ff = pages.ToArray();
            await (await this.GetModule())
                .InvokeVoidAsync("delete", ff, "lll");
        }

        public virtual async Task<long> DoGetPageCount()
        {
            return await (await this.GetModule())
                .InvokeAsync<long>("getCount");

        }

        public virtual async Task DoInitializeAsync()
        {
            var m = await this.GetModule();
            var res = await m.InvokeAsync<InitModel>("initialize");
        }

        public virtual async Task<PageData> DoReadAsync(long page, string key)
        {
            var res = await (await this.GetModule())
                .InvokeAsync<PageData>("read", page, key);

            return res;

        }
        public virtual async Task<long> DoWriteAsync(PageData[] pages)
        {
            await (await this.GetModule())
                .InvokeVoidAsync("write", pages, "ll");
            return 0;
        }
        public virtual async ValueTask DisposeAsync()
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
