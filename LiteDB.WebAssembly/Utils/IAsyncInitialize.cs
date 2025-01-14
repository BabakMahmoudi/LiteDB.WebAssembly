﻿using LiteDB.Engine.Disk.Streams;
using Microsoft.JSInterop;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using static LiteDB.Constants;

namespace LiteDB
{
    public interface IAsyncInitialize
    {
        Task InitializeAsync();
        //Task SetLengthAsync(long value);
    }
    public interface IAsyncStreamEx : IAsyncInitialize
    {
        //Task InitializeAsync();
        Task SetLengthAsync(long value);
        Task DeleteDatabase();
        StreamOptions Options { get; }
        IJSRuntime Runtime { get; }
        string DatabaseName { get; }
    }
}