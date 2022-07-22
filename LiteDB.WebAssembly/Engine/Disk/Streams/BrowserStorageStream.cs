using LiteDB.Engine.Disk.Streams;
using LiteDB.Engine.Disk.Streams.Adapters;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Engine.Disk.Streams
{
    public class BrowserStorageStream : Stream, IAsyncStreamEx
    {
        protected long _position;
        protected long? _length;
        protected string _name;
        private readonly StreamOptions options;
        private IStorageAdapter adapter;

        public long PageSize { get; private set; }
        public string Name { get; private set; }
        protected const string DEFAULT_DB_NAME = "defaultdb";
        protected const long PAGE_SIZE = LiteDB.Constants.PAGE_SIZE;
        protected List<Task> _tasks;
        public bool UseWriteCaches = false;
        protected Dictionary<long, PageData> _cache = new Dictionary<long, PageData>();
        public IJSRuntime Runtime { get; private set; }

        public string DatabaseName => this._name;


        public BrowserStorageStream(IJSRuntime runtime, string name, StreamOptions options)
        {
            this._position = 0;
            this._length = null;
            this._name = string.IsNullOrWhiteSpace(name) ? options.DefaultDbName : name;
            this.options = options;
            this.PageSize = LiteDB.Constants.PAGE_SIZE;
            this.UseWriteCaches = options.UseCache;
            this._tasks = new List<Task>();
            this.Runtime = runtime;
            switch (options.StorageBackend)
            {
                case StorageBackends.LocalStorage:
                    this.adapter = new LocalStorageAdapter(runtime, name);
                    break;
                default:
                    this.adapter = new IndexedDbAdapter(runtime, name);
                    break;
            }
            //this.adapter = new BaseAdapter(runtime, name);

        }
        public long GetPageIndex(long position)
        {
            return position / LiteDB.Constants.PAGE_SIZE;
        }
        public string GetPageKeyByIndex(long index)
        {
            return string.Format(this.options.KeyTemplate, this._name, index);
        }
        public string GetPageKeyByPosition(long position)
        {

            return string.Format(this.options.KeyTemplate, this._name, GetPageIndex(position));
        }
        protected void Enqueue(Task task)
        {
            if (!task.IsCompleted)
            {
                this._tasks = this._tasks ?? new List<Task>();
                task.ContinueWith(x =>
                {
                    _tasks.Remove(x);
                });
            }
        }
        public override void Flush()
        {
            FlushAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await Task.WhenAll(this._tasks.ToArray());
            var pages = this._cache.Values.Where(x => x.IsDirty).ToArray();
            if (pages.Length > 0)
                await this.adapter.DoWriteAsync(pages);
            pages.ToList().ForEach(x => x.IsDirty = false);
            this._cache = new Dictionary<long, PageData>();
            this._tasks = new List<Task>();
        }


        public async Task InitializeAsync()
        {
            await this.adapter.DoInitializeAsync();
            await GetLength();

        }
        private async Task<long> GetLength(bool refersh = false)
        {
            if (!this._length.HasValue || refersh)
            {
                this._length = (await this.adapter.DoGetPageCount()) * PAGE_SIZE;
            }
            return this._length.Value;
        }
        //protected abstract Task<long> DoWriteAsync(PageData[] pages);
        //protected abstract Task<PageData> DoReadAsync(long pageIndex, string pageKey);
        //protected abstract Task<long> DoGetPageCount();
        //protected abstract Task DoInitializeAsync();
        //protected abstract Task DoDeletePages(PageData[] pages);
        public override long Length => this._length ?? 0;
        public override bool CanRead => true;
        public override bool CanWrite => true;
        public override bool CanSeek => true;

        public override long Position { get => _position; set => _position = value; }

        public StreamOptions Options => this.options;

        

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var index = this.GetPageIndex(this.Position);
            var key = this.GetPageKeyByPosition(this.Position);
            PageData page = this.options.UseCache && this._cache.TryGetValue(index, out var _page) && _page != null
                ? _page
                : await this.adapter.DoReadAsync(index, key);
            _position += count;
            if (page == null)
            {
                // read empty (not created) page
                for (var i = offset; i < offset + count; i++)
                {
                    buffer[i] = 0;
                }
                return count;
            }
            Buffer.BlockCopy(page.Bytes(), 0, buffer, offset, count);
            if (this.options.UseCache)
            {
                this._cache[index] = page;
            }
            return page.Bytes().Length;

        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            this.WriteAsync(buffer, offset, count).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        
        protected bool IsValidPosition(long value, bool Throw = true)
        {
            var result = value % this.PageSize == 0;
            if (!result && Throw)
            {
                throw new Exception($"Invalid Postion {Position}");
            }
            return result;

        }
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var content = Convert.ToBase64String(buffer, offset, count, Base64FormattingOptions.None);
            var page = new PageData(GetPageIndex(this.Position), GetPageKeyByPosition(this.Position), Convert.ToBase64String(buffer, offset, count, Base64FormattingOptions.None));
            _position += count;
            if (_position > this.Length)
            {
                _length = _position;
            }
            if (this.options.UseCache)
            {
                page.IsDirty = true;
                this._cache[page.Index] = page;
            }
            else
            {
                await this.adapter.DoWriteAsync(new PageData[] { page });
                page.IsDirty = false;
            }
        }


        public async Task SetLengthAsync(long value)
        {
            IsValidPosition(value);
            var idx1 = this.GetPageIndex(value);
            var idx2 = this.GetPageIndex(this.Length);
            var pages = new List<PageData>();
            for (var i = idx1; i <= idx2; i++)
            {
                pages.Add(new PageData
                {
                    Index = i,
                    PageKey = this.GetPageKeyByIndex(i),
                    Content = ""
                });
            }

            if (pages.Count > 0)
            {
                await this.adapter.DoDeletePages(pages.ToArray());
                if (await this.GetLength(true) != value)
                {

                    // Unexpected error
                    //throw new Exception($"Unexpected: Length: '{_length}' differs from expected length {value} after deleting pages.");
                }
            }
        }
        public override void SetLength(long value)
        {
            this._tasks.Add(this.SetLengthAsync(value));
            return;
        }


        public override long Seek(long offset, SeekOrigin origin)
        {
            var position =
                origin == SeekOrigin.Begin ? offset :
                origin == SeekOrigin.Current ? _position + offset :
                _position - offset;

            _position = position;

            return _position;
        }
        public override async ValueTask DisposeAsync()
        {
            if (this.adapter != null)
            {
                await adapter.DisposeAsync();
            }
        }

        public Task DeleteDatabase()
        {
            return this.adapter.DeleteDatabase();
        }
    }
}
