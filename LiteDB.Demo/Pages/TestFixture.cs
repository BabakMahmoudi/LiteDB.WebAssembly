using LiteDB.Demo.Tests;
using LiteDB.WebAssembly;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Demo.Pages
{
    public class TestFixture : ComponentBase, ITestFixture, IAsyncDisposable
    {

        [Inject]
        public IBlazorLiteDatabaseFactory factory { get; protected set; }
        private ILiteDatabase db;
        protected StringBuilder _log = new StringBuilder();

        public virtual Engine.Disk.Streams.StorageBackends Backend { get; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }
        public void Log(string fmt, params object[] args)
        {
            _log.AppendFormat(fmt, args);
            _log.AppendLine();
            this.StateHasChanged();
        }
        public void ClearLog()
        {
            _log = new StringBuilder();
        }
        public virtual async Task<ILiteDatabase> GetDb(bool refersh = true, bool open = true, bool cache = false)
        {
            if (this.db == null || refersh)
            {
                if (this.db != null)
                    await this.db.DisposeAsync();
                this.db = factory.Create("demo", cfg =>
                {
                    cfg.UseCache = cache;
                    cfg.StorageBackend = Backend;

                });
            }
            if (!this.db.IsOpen && open)
            {
                await this.db.OpenAsync();
            }
            return this.db;
        }

        public async Task OpenDatabase()
        {

            var db = await this.GetDb(true);
            Log("Database successfully opened");
        }
        void Assert(bool check, string comment)
        {
            if (!check)
                throw new Exception(comment);
        }
        public async Task Insert()
        {
            var db = await this.GetDb();
            var collection = db.GetCollection<PersonData>();
            var count_before_add = await collection.LongCountAsync();
            Log($"Database successfully opened. Backend: '{this.Backend}' Count: '{count_before_add}'.");
            var name = $"Random {Guid.NewGuid()}";
            var id = await collection.InsertAsync(new PersonData { Name = name });
            var count_after_add = await collection.LongCountAsync();
            Log($"Person successfully added. Id:{id}, Count: '{count_after_add}'");
            var retrived = await collection.FindByIdAsync(id);
            Assert(retrived != null, "Retreived should not be null.");
            Assert(count_before_add + 1 == count_after_add, $"Count after add '{count_after_add}' should be '{count_before_add}'+1");
            Log($"Person successfully retreived: {retrived}");
            await db.DisposeAsync();
            this.db = null;
            db = await this.GetDb();
            if (!db.IsOpen)
                await db.OpenAsync();
            collection = db.GetCollection<PersonData>();
            retrived = await collection.FindByIdAsync(id);
            Log($"Database reopened. Persons Count:'{ await collection.LongCountAsync()}'");
            Assert(retrived != null, "Retreived should not be null.");
            Assert(retrived.Name == name, $"Retreived person name should be {name}");
            Log($"Person successfully retreived after reopenning : {retrived}");

            /// We should be able to find the inserted
            /// record

            var items = (await collection.FindAsync(x => x.Name == name).ReadAll()).ToArray();

            Assert(items.Length == 1, "We should be able to exactly find one item");

            var count_before_delete = await collection.LongCountAsync();
            await collection.DeleteManyAsync(x => x.Name == name);
            items = await collection.FindAsync(x => x.Name == name).ReadAll();
            Assert(items.Length == 0, "Items should not be found after delete.");
            Log($"Item successfully deleted. Count: '{await collection.LongCountAsync()}'");

            /// Delete all
            /// 
            await collection.DeleteAllAsync();
            Assert((await collection.LongCountAsync()) == 0, "Count should be 0 after delete all");
            Log($"All items successfully deleted. Count: '{await collection.LongCountAsync()}'");
            await this.GetDb(true, false);
            Log($"Test terminated successfully.\r\n ==============================");
        }

        public async Task Bulk()
        {
            var db = await this.GetDb(cache: false);
            var colection = db.GetCollection<LargeData>();
            Log($"Collection Opened. Count: {await colection.LongCountAsync()}");
            await colection.EnsureIndexAsync("index1", x => x.Name);
            await colection.EnsureIndexAsync("index2", x => x.Age);
            var count = 2000;
            var _rnd = new Random();
            Log($"Inserting {count} items...");
            var docs = Enumerable.Range(1, count).Select(i => new LargeData
            {
                Name = "Bulk " + i,
                Age = _rnd.Next(10, 90)
            });
            var dt = Stopwatch.StartNew();
            await colection.InsertAsync(docs);
            Log($"{count} items inserted in {dt.ElapsedMilliseconds} milliseconds. Collection Count:{await colection.LongCountAsync()}");
            dt = Stopwatch.StartNew();
            var items = await colection.Query().OrderBy(x => x.Age).ToArrayAsync();
            Log($" '{items.Length}' Queried with orderby x.Age in {dt.ElapsedMilliseconds} milliseconds. First.Age: '{items[0].Age}'. Last.Age: '{items[items.Length - 1].Age}'");
            dt = Stopwatch.StartNew();
            items = await colection.FindAsync(Query.All("Age", Query.Ascending)).ReadAll();
            Log($" '{items.Length}' Queried with orderby x.Age in {dt.ElapsedMilliseconds} milliseconds. First.Age: '{items[0].Age}'. Last.Age: '{items[items.Length - 1].Age}'");
            for (var i = 0; i < items.Length; i++)
            {
                if (i > 0)
                {
                    var diff = items[i].Age - items[i - 1].Age;
                    Assert(diff >= 0, "");
                }
            }
            //dt = Stopwatch.StartNew();
            //await colection.DeleteAllAsync();
            //Log($"All items deleted in {dt.ElapsedMilliseconds} milliseconds. Count: {await colection.LongCountAsync()}");
            db = await this.GetDb(true);
            Log($"Test terminated successfully.\r\n ==============================");

        }
        public async Task Index()
        {
            var db = await this.GetDb();
            var colection = db.GetCollection<IndexData>();
            var count = 2000;
            var _rnd = new Random();
            var docs = Enumerable.Range(1, count).Select(i => new IndexData
            {
                Name = "Bulk " + i,
                Age = _rnd.Next(10, 90)
            });
            await colection.InsertAsync(docs);

            var dt = Stopwatch.StartNew();
            var items = await colection.FindAsync(x => x.Age == 20).ReadAll();
            Log($"{items.Length} found for Age==20 without index in {dt.ElapsedMilliseconds} milliseconds.");
            await colection.EnsureIndexAsync("index", x => x.Age);
            dt = Stopwatch.StartNew();
            items = await colection.FindAsync(x => x.Age == 20).ReadAll();
            Log($"{items.Length} found for Age==20 with index in {dt.ElapsedMilliseconds} milliseconds.");

            await colection.DropIndexAsync("index");
            db = await this.GetDb(true);
            Log($"Test terminated successfully.\r\n ==============================");
        }

        public async Task LargeData()
        {
            var db = await this.GetDb(cache: true);
            var collection = db.GetCollection<LargeData>();

            var count = 100;
            var _rnd = new Random();
            var docs = Enumerable.Range(1, count).Select(i => new LargeData
            {
                Name = "Bulk " + i,
                Age = _rnd.Next(10, 90),
                Data = new string('x', 50000)
            });
            var dt = Stopwatch.StartNew();
            await collection.InsertAsync(docs);
            Log($"Inserted {count} large documents in {dt.ElapsedMilliseconds} milliseconds");
            dt = Stopwatch.StartNew();
            var allitems = await collection.FindAllAsync().ReadAll();
            Log($"Retrieved {allitems.Length} items in {dt.ElapsedMilliseconds} milliseconds");
            Log($"Test terminated successfully.\r\n ==============================");
            //db = await this.GetDb(true, useCaceh:true);
            //collection = db.GetCollection<LargeData>();
            //dt = Stopwatch.StartNew();
            //await collection.InsertAsync(docs);
            //Log($"Inserted {count} large documents in {dt.ElapsedMilliseconds} milliseconds");
            //dt = Stopwatch.StartNew();
            //allitems = await collection.FindAllAsync().ReadAll();
            //Log($"Retrieved {allitems.Length} items in {dt.ElapsedMilliseconds} milliseconds");
            //db = await this.GetDb(true, useCaceh: true);





        }

        public async Task Checkpoint()
        {
            var db = await this.GetDb();
            await db.CheckpointAsync();

        }
        public async ValueTask DisposeAsync()
        {
            if (this.db != null)
            {
                await this.db.DisposeAsync();
            }
        }

        public async Task CopyDatabase()
        {
            var db = await this.GetDb(cache: false);
            var collection = db.GetCollection<LargeData>();
            var personsCollection = db.GetCollection<PersonData>();
            var count = 10;
            var _rnd = new Random();
            var docs = Enumerable.Range(1, count).Select(i => new LargeData
            {
                Name = "Bulk " + i,
                Age = _rnd.Next(10, 90),
                Data = new string('x', 50)
            });

            var persons = Enumerable.Range(1, count).Select(i => new PersonData
            {
                Name = "Bulk " + i,
            });

            var dt = Stopwatch.StartNew();
            await collection.InsertAsync(docs);
            await personsCollection.InsertAsync(persons);
            var db2 = factory.Create("temp", cfg =>
            {
                cfg.StorageBackend = Engine.Disk.Streams.StorageBackends.LocalStorage;
            });
            await db2.OpenAsync();
            await db.CopyCollections(db2, collection, personsCollection);
            var items = await db2.GetCollection<LargeData>().FindAllAsync().ReadAll();
            var persons_in_destination = await db2.GetCollection<PersonData>().FindAllAsync().ReadAll();
        }

        public async Task DeleteDatabase()
        {
            Log("Deleting database");
            var dt = Stopwatch.StartNew();
            var db = await this.GetDb();
            this.db = null;
            await db.DeleteDatabase();
            Log($"Database successfully deleted in {dt.ElapsedMilliseconds} milliseconds.");

        }
        public async Task RepairDatabase()
        {
            var db = await this.GetDb(true);
            var largeData = db.GetCollection<LargeData>();
            var persons = db.GetCollection<PersonData>();
            var count = 10;
            var _rnd = new Random();
            Log("Inserting data.");
            await persons.DeleteAllAsync();
            await largeData.DeleteAllAsync();
            var docs = Enumerable.Range(1, count).Select(i => new LargeData
            {
                Name = "Bulk " + i,
                Age = _rnd.Next(10, 90),
                Data = new string('x', 50)
            });

            var personsItems = Enumerable.Range(1, count).Select(i => new PersonData
            {
                Name = "Bulk " + i,
            });

            await largeData.InsertAsync(docs);
            await persons.InsertAsync(personsItems);

            var persons_before_repair = await db.GetCollection<PersonData>().FindAllAsync().ReadAll();

            Log("Repairing database ...");
            var dt = Stopwatch.StartNew();
            await db.RepairDatabase(persons,largeData);
            Log($"Database repaired in {dt.ElapsedMilliseconds} milliseconds");

            db = await this.GetDb(true);

            var dara_after_repair = await db.GetCollection<LargeData>().FindAllAsync().ReadAll();
            var persons_after_repair = await db.GetCollection<PersonData>().FindAllAsync().ReadAll();
            Assert(dara_after_repair.Length == docs.Count(),"Data count should be same after repair");
            Assert(persons_after_repair.Length == personsItems.Count(), "Data count should be same after repair");
            Log($"Database verified");




        }
    
        public async Task Stats()
        {
            var db = await this.GetDb(true);
            
            Log($"Database Stats: Persons Count:{await db.GetCollection<PersonData>().LongCountAsync()} " +
                $" Data Count:{await db.GetCollection<LargeData>().LongCountAsync()}");
        }
        public async Task Clear()
        {
            var db = await this.GetDb(true);
            Log("Clearing Database");
            await db.GetCollection<PersonData>().DeleteAllAsync();
            await db.GetCollection<LargeData>().DeleteAllAsync();
            Log($"Database Stats: Persons Count:{await db.GetCollection<PersonData>().LongCountAsync()} " +
                $" Data Count:{await db.GetCollection<LargeData>().LongCountAsync()}");
        }


    }
}
