namespace Famick.HomeManagement.Tools.EfMigration;

public class CliOptions
{
    public string ContextName { get; set; } = "HomeManagementDbContext";
    public string OutputPath { get; set; } = "schema.md";
    public string DocsPath { get; set; } = "docs/entities";
    public bool UseTableNames { get; set; } = true;
    public bool ExcludeAudit { get; set; } = false;
    public bool IncludeOwned { get; set; } = true;
    public bool CollapseManyToMany { get; set; } = true;
    public bool GenerateEntityDocs { get; set; } = true;
    public string[] ExcludeEntities { get; set; } = [];

    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--context" or "-c":
                    if (i + 1 < args.Length) options.ContextName = args[++i];
                    break;
                case "--output" or "-o":
                    if (i + 1 < args.Length) options.OutputPath = args[++i];
                    break;
                case "--docs" or "-d":
                    if (i + 1 < args.Length) options.DocsPath = args[++i];
                    break;
                case "--use-table-names":
                    options.UseTableNames = true;
                    break;
                case "--use-entity-names":
                    options.UseTableNames = false;
                    break;
                case "--exclude-audit":
                    options.ExcludeAudit = true;
                    break;
                case "--exclude-owned":
                    options.IncludeOwned = false;
                    break;
                case "--expand-many-to-many":
                    options.CollapseManyToMany = false;
                    break;
                case "--no-entity-docs":
                    options.GenerateEntityDocs = false;
                    break;
                case "--exclude":
                    if (i + 1 < args.Length) options.ExcludeEntities = args[++i].Split(',');
                    break;
                case "--help" or "-h":
                    PrintHelp();
                    Environment.Exit(0);
                    break;
            }
        }

        return options;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            EF Migration Documentation Generator

            Usage: dotnet run -- [options]

            Options:
              -c, --context <name>       DbContext class name (default: HomeManagementDbContext)
              -o, --output <path>        Output path for Mermaid ER diagram (default: schema.md)
              -d, --docs <path>          Output directory for entity docs (default: docs/entities)
              --use-table-names          Use database table names (default)
              --use-entity-names         Use CLR entity names instead of table names
              --exclude-audit            Exclude audit columns (CreatedAt, UpdatedAt, etc.)
              --exclude-owned            Exclude owned type properties
              --expand-many-to-many      Show junction tables for many-to-many relationships
              --no-entity-docs           Skip generating individual entity documentation
              --exclude <entities>       Comma-separated list of entities to exclude
              -h, --help                 Show this help message
            """);
    }
}
