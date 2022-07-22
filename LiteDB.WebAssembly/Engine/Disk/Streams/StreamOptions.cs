namespace LiteDB.Engine.Disk.Streams
{
    public class StreamOptions
    {
        public bool UseCache { get; set; }
        public string KeyTemplate { get; private set; } = "page_{0}_{1:000000}";
        public string DefaultDbName { get; private set; } = "litedb";
        public StorageBackends StorageBackend { get; set; }
        public static StreamOptions Default = new StreamOptions();
    }
}
