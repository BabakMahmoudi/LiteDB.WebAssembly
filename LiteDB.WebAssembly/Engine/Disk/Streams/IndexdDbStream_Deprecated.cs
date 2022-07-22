using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public class InitModel
    {
        public string Name { get; set; }
        public long Length { get; set; }
    }
    public class IndexdDbStream_Deprecated : Stream, IAsyncInitialize
    {
        private readonly IJSRuntime runtime;
        private readonly string dbName;
        private DotNetObjectReference<IndexdDbStream_Deprecated> objRef;
        private IJSObjectReference _module;
        private long _position = 0;
        private long _length = 0;

        public IndexdDbStream_Deprecated(IJSRuntime runtime, string dbName)
        {
            this.runtime = runtime;
            this.dbName = dbName;
        }
        public async Task<IJSObjectReference> GetModule()
        {
            if (this._module == null)
            {
                this.objRef = DotNetObjectReference.Create(this);
                this._module = await
                    (await runtime.InvokeAsync<IJSObjectReference>("import", "/litedb-indexeddb-2.js"))
                    .InvokeAsync<IJSObjectReference>("createInstance", this.dbName, this.objRef);
            }
            return this._module;
        }
        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => this._length;

        public override long Position { get => this._position; set => this._position = value; }

        public override void Flush()
        {
            //throw new NotImplementedException();
        }

        public async Task InitializeAsync()
        {
            var m = await this.GetModule();
            var res = await m.InvokeAsync<InitModel>("initialize");
            this._length = res.Length * PAGE_SIZE;




        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.ReadAsync(buffer, offset, count).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var module = await this.GetModule();
            var idx = (offset / PAGE_SIZE);
            var content = await module.InvokeAsync<string>("read", idx, count);
            if (string.IsNullOrEmpty(content))
            {

                // read empty (not created) page
                for (var i = offset; i < offset + count; i++)
                {
                    buffer[i] = 0;
                }

                return 0;
            }
            var data = Convert.FromBase64String(content);
            System.Buffer.BlockCopy(data, 0, buffer, offset, count);
            return data.Length;


        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            var count = value / PAGE_SIZE;
            
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.WriteAsync(buffer, offset, count).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var m = await this.GetModule();
            var of = (offset / PAGE_SIZE);
            var content = Convert.ToBase64String(buffer, offset, count, Base64FormattingOptions.None);
            var r = await m.InvokeAsync<int>("write", content, of, count);
            _position += count;

            if (_position > this.Length)
            {
                _length = _position;
            }

        }
    }
}
