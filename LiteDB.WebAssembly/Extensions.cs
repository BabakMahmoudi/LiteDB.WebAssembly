﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB
{
    public static class Extensions
    {
        public static async Task<T[]> ReadAll<T>(this IAsyncEnumerable<T> items)
        {
            var result = new List<T>();
            await foreach(var item in items)
            {
                result.Add(item);
            }
            return result.ToArray();
        }
    }
}