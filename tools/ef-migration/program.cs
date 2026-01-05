using Famick.HomeManagement.Infrastructure.Data;
using Famick.HomeManagement.Tools.EfMigration;
using Microsoft.EntityFrameworkCore;

var options = CliOptions.Parse(args);

Console.WriteLine("EF Migration Documentation Generator");
Console.WriteLine("=====================================");
Console.WriteLine();

// Create DbContext with SQLite in-memory provider to build the model
// We only need the model metadata, not an actual database connection
var dbOptions = new DbContextOptionsBuilder<HomeManagementDbContext>()
    .UseSqlite("Data Source=:memory:")
    .Options;

using var ctx = new HomeManagementDbContext(dbOptions);

var generator = new MermaidGenerator(options);

// Generate ER diagram
Console.WriteLine($"Generating ER diagram: {options.OutputPath}");
var output = generator.Generate(ctx);
File.WriteAllText(options.OutputPath, output);
Console.WriteLine("  Done.");
Console.WriteLine();

// Generate entity documentation
if (options.GenerateEntityDocs)
{
    Console.WriteLine($"Generating entity documentation in: {options.DocsPath}");
    generator.GenerateEntityDocs(ctx);
    Console.WriteLine("  Done.");
}

Console.WriteLine();
Console.WriteLine("Generation complete!");
