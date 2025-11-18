namespace webapi.Configurations
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = null!;
        public string DatabaseName { get; set; } = null!;
        public string UsersCollection { get; set; } = null!;
    }
}
