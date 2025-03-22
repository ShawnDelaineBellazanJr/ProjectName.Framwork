var builder = DistributedApplication.CreateBuilder(args);


var sql = builder.AddSqlServer("sql")
    .WithDataVolume("sql-data");

var db = sql.AddDatabase("ProjectNameDb");


builder.AddProject<Projects.ProjectName_AIServices>("ai-services")
    .WithReference(db);


builder.Build().Run();
