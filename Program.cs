using System.CommandLine;
using System.CommandLine.Help;

namespace sbom;

internal class Program
{
    private const string DbFile = "sbom-repository.db";

    private static int Main(string[] args)
    {
        RootCommand rootCommand = GetRootCommand();
        ParseResult parseResult = rootCommand.Parse(args);
        return parseResult.Invoke();
    }

    private static RootCommand GetRootCommand()
    {
        Option<bool> initDbOption = new("init-db");

        RootCommand rootCommand =
            new("SBOM CLI tool") { initDbOption, GetIngestCommand(), GetQueryCommand() };

        rootCommand.SetAction(parseResult =>
        {
            if (parseResult.GetValue(initDbOption))
            {
                File.Delete(DbFile);
                return 0;
            }

            // No command given
            new HelpAction().Invoke(parseResult);
            return 1;
        });
        return rootCommand;
    }

    private static Command GetIngestCommand()
    {
        Argument<FileInfo> fileArgument =
            new Argument<FileInfo>("sbom-file") { Description = "The path to the CycloneDX v1.7 JSON file to ingest." }
                .AcceptExistingOnly();

        Command ingestCommand = new("ingest", "Ingest an SBOM file into the database.") { fileArgument };
        ingestCommand.SetAction(parseResult =>
        {
            SbomStore store = new(DbFile);
            store.Ingest(parseResult.GetValue(fileArgument)!);
            return 0;
        });
        return ingestCommand;
    }

    private static Command GetQueryCommand()
    {
        Option<string> componentOption = new("--component")
        {
            Description = "The name of the component to search for."
        };
        Option<string> versionOption = new("--version")
        {
            Description = "The specific version of the component (only used with --component)."
        };
        Option<string> licenseOption = new("--license") { Description = "The license identifier to search for." };

        Command queryCommand = new("query", "Search for components in the SBOM.")
        {
            componentOption, versionOption, licenseOption
        };
        queryCommand.Validators.Add(result =>
        {
            bool hasComponent = result.GetValue(componentOption) != null;
            bool hasLicense = result.GetValue(licenseOption) != null;
            bool hasVersion = result.GetValue(versionOption) != null;

            switch (hasComponent)
            {
                case true when hasLicense:
                    result.AddError("You cannot specify both --component and --license at the same time.");
                    break;
                case false when !hasLicense:
                    result.AddError("You must specify either --component or --license.");
                    break;
                default:
                    {
                        if (hasVersion && !hasComponent)
                        {
                            result.AddError("The --version option can only be used when --component is specified.");
                        }

                        break;
                    }
            }
        });
        queryCommand.SetAction(parseResult =>
        {
            string? component = parseResult.GetValue(componentOption);
            string? version = parseResult.GetValue(versionOption);
            string? license = parseResult.GetValue(licenseOption);

            if (component != null)
            {
                QueryComponent(component, version);
            }
            else if (license != null)
            {
                QueryLicense(license);
            }

            return 0;
        });
        return queryCommand;
    }

    private static void QueryComponent(string component, string? version)
    {
        SbomStore store = new(DbFile);
        PrintMatchedPackages(store.QueryComponent(component, version));
    }

    private static void QueryLicense(string license)
    {
        SbomStore store = new(DbFile);
        PrintMatchedPackages(store.QueryLicense(license));
    }

    private static void PrintMatchedPackages(IEnumerable<string> matchedPackages)
    {
        Console.WriteLine("Matched packages: " + string.Join(", ", matchedPackages));
    }
}