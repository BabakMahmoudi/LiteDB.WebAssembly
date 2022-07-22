using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Engine.Services
{
    public interface IDatabaseServices
    {
        Task DeleteDatabaseAsync();
        Task RegisterCollectionAsync<T>(ILiteCollection<T> collection);
        Task CopyCollectionTo(ILiteDatabase destination);

    }
    class CollectionData
    {
        public string Id { get; set; }
        public string Data { get; set; }
    }
    enum SysDataTypes
    {
        Collection
    }
    class DatabaseServices : IDatabaseServices
    {
        private readonly LiteDatabase database;

        public DatabaseServices(LiteDatabase database)
        {
            this.database = database;
        }

        public Task DeleteDatabaseAsync()
        {
            throw new NotImplementedException();
        }
        private ILiteCollection<CollectionData> GetCollections()
        {
            return this.database.GetCollection<CollectionData>("$cols");

        }
        public async Task RegisterCollectionAsync<T>(ILiteCollection<T> collection)
        {
            await this.GetCollections()
                .UpsertAsync(new CollectionData { Id = collection.Name, Data = "" });
        }

        public Task CopyCollectionTo(ILiteDatabase destination)
        {
            throw new NotImplementedException();
        }
    }
}
