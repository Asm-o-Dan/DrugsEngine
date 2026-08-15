namespace Infrastructure.Dal.Models;

public class DockerDatabaseSettings
{
    public string ConnectionString { get; set; }
    public int CommandTimeout { get; set; }
}