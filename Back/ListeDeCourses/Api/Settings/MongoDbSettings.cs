using System.ComponentModel.DataAnnotations;

namespace ListeDeCourses.Api.Settings;

public class MongoDbSettings
{
    [Required, MinLength(1)]
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    [Required, MinLength(1)]
    public string DatabaseName { get; set; } = "liste-de-courses";
}
