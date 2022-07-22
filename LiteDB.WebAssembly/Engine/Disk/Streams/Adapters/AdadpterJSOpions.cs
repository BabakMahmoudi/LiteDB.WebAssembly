using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Engine.Disk.Streams.Adapters
{
    class AdadpterJSOpions
    {
        public string IndexKey => ToCamel(nameof(PageData.Index));
        public string ContentKey => ToCamel(nameof(PageData.Content));
        public object CallBackReference { get; set; }
        public string Backend { get; set; }
        private static string ToCamel(string str)
        {
            if (!string.IsNullOrWhiteSpace(str) && str.Length > 0)
            {
                return Char.ToLowerInvariant(str[0]) + str.Substring(1);
            }
            return str;
        }
    }
}
