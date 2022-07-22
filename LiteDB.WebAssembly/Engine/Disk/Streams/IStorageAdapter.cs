using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Engine.Disk.Streams
{
    public interface IStorageAdapter:IAsyncDisposable
    {
        Task<long> DoWriteAsync(PageData[] pages);
        Task<PageData> DoReadAsync(long pageIndex, string pageKey);
        Task<long> DoGetPageCount();
        Task DoInitializeAsync();
        Task DoDeletePages(PageData[] pages);

    }
}
