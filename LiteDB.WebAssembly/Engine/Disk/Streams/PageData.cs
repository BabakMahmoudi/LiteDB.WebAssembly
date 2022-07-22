using System;

namespace LiteDB.Engine.Disk.Streams
{
    public class PageData
    {
        public long Index { get; set; }
        public string PageKey { get; set; }

        public string Content { get; set; }

        public DateTime LastAccessed { get; set; }
        public bool IsDirty { get; set; }

        public byte[] Bytes() => Convert.FromBase64String(this.Content??"");

        public PageData()
        {

        }
        public PageData(long index, string key, string content)
        {
            this.Index = index;
            this.PageKey = key;
            this.Content = content ?? "";
        }
        public PageData(long index, string key, byte[] buffer, int offset, int count) :
            this(index, key, Convert.ToBase64String(buffer, offset, count, Base64FormattingOptions.None))
        {
        }

    }
}
