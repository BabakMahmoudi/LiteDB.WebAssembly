using LiteDB.Engine.Disk.Streams;
using LiteDB.Engine.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.WebAssembly
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
        public static void TEST()
        {

        }
        public static IDatabaseServices GetDatabaseServices(this ILiteDatabase database)
        {
            if (database is LiteDatabase db)
                return new DatabaseServices(db);
            throw new Exception("Unexpected. Failed to cast ILiteDatabase to LiteDatabase");
        }
        internal static async Task _CopyCollection<T>(this ILiteDatabase source, ILiteDatabase destination, ILiteCollection<T> collection)
        {
            var dest = destination.GetCollection<T>(collection.Name);
            var count = 0;
            await foreach (var item in collection.FindAllAsync())
            {
                await dest.InsertAsync(item);
                count++;
            }

            if (count == 0)
            {

            }
        }
        public static Task CopyCollection<T>(this ILiteDatabase source, ILiteDatabase destination, ILiteCollection<T> collection)
        {
            return _CopyCollection<T>(source, destination, collection);
            
        }
        private static void HH(Action action)
        {

        }
        private static Task __CopyCollection(this ILiteDatabase source, ILiteDatabase destination, object collection)
        {
            if (!collection.GetType().IsGenericType || 
                collection.GetType().GetGenericTypeDefinition() != typeof(LiteCollection<>))
            {
                throw new Exception("Invalid Collection");
            }
            return (Task) typeof(Extensions)
                .GetMethod(nameof(Extensions._CopyCollection), System.Reflection.BindingFlags.Static |System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                .MakeGenericMethod(collection.GetType().GenericTypeArguments)
                .Invoke(null, new object[] { source, destination, collection });
        }
        public async static Task CopyCollections(this ILiteDatabase source, ILiteDatabase destination, params object[] collections)
        {
            foreach(var col in collections)
            {
                await __CopyCollection(source, destination, col);
            }

        }
        internal static IAsyncStreamEx GetStream(this ILiteDatabase source)
        {
            var _engine = source.GetType().GetField("_engine", BindingFlags.NonPublic | BindingFlags.Instance)
               .GetValue(source);
            return (IAsyncStreamEx)_engine.GetType().GetField("_stream", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(_engine);

        }
        public async static Task DeleteDatabase(this ILiteDatabase source)
        {
            await source.DisposeAsync();
            await source.GetStream().DeleteDatabase();
        }

        private static ILiteCollection<T> GetCollectionEx<T>(ILiteDatabase db, ILiteCollection<T> source)
        {
            return db.GetCollection<T>(source.Name);
        }
        
        public async static Task RepairDatabase(this ILiteDatabase source, params object[] collections)
        {
            var stream = source.GetStream();
            var sourceName = source.GetStream().DatabaseName;
            var name = $"temp_{new Random().Next(1, 2000)}";
            var tempDb = new LiteDatabase(new BrowserStorageStream(stream.Runtime, name, stream.Options));
            await tempDb.OpenAsync();
            await CopyCollections(source, tempDb, collections);
            await source.DeleteDatabase();
            await tempDb.DisposeAsync();
            tempDb = new LiteDatabase(new BrowserStorageStream(stream.Runtime, name, stream.Options));
            await tempDb.OpenAsync();
            var temp_collections = collections
                .Select(x =>
                {
                    return typeof(Extensions)
                    .GetMethod(nameof(GetCollectionEx), BindingFlags.Static | BindingFlags.NonPublic)
                    .MakeGenericMethod(x.GetType().GetGenericArguments())
                    .Invoke(null, new object[] { tempDb, x });
                })
                .ToArray();

            var final = new LiteDatabase(new BrowserStorageStream(stream.Runtime, sourceName, stream.Options));
            await final.OpenAsync();
            await CopyCollections( tempDb, final, temp_collections);
            await tempDb.DeleteDatabase();
            await final.DisposeAsync();
            
           
        }



    }
}
