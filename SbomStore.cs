using LiteDB;

namespace sbom;

public class SbomStore(string databasePath)
{
    private readonly string databasePath = databasePath;

    static SbomStore()
    {
        // LiteDB maps any property named "id" to the document key "_id", which drops the license id
        BsonMapper.Global.Entity<License>().Field(x => x.id, "id");
    }

    public void Ingest(FileInfo file)
    {
        Console.WriteLine($"Ingesting file: {file.FullName}");

        try
        {
            string jsonContent = File.ReadAllText(file.FullName);
            BsonValue bsonDocument = JsonSerializer.Deserialize(jsonContent);
            Sbom sbom = BsonMapper.Global.ToObject<Sbom>(bsonDocument.AsDocument);

            using LiteDatabase db = new(databasePath);
            ILiteCollection<Sbom> sbomCollection = db.GetCollection<Sbom>("sboms");
            sbomCollection.Insert(sbom);

            Console.WriteLine($"Total records in DB: {sbomCollection.Count()}");
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to ingest {file.FullName}", e);
        }
    }

    public IEnumerable<string> QueryComponent(string name, string? version = null)
    {
        using LiteDatabase db = new(databasePath);
        ILiteCollection<Sbom> sbomCollection = db.GetCollection<Sbom>("sboms");

        // @. refers to the current array element inside a filter
        string where = version == null
            ? $"components[*].name ANY = {Literal(name)}"
            : $"COUNT(components[@.name = {Literal(name)} AND @.version = {Literal(version)}]) > 0";

        return sbomCollection.Query()
            .Where(where)
            .Select(s => s.serialNumber)
            .ToList();
    }

    public IEnumerable<string> QueryLicense(string license)
    {
        using LiteDatabase db = new(databasePath);
        ILiteCollection<Sbom> sbomCollection = db.GetCollection<Sbom>("sboms");

        // CycloneDX records a license as an SPDX license.id (e.g. "MIT"), a free-text license.name for licenses
        // without an SPDX id (never both), or an SPDX expression (e.g. "Apache-2.0 OR MIT", matched exactly).
        // Searching name alone would miss the common SPDX-identified case.
        string value = Literal(license);
        return sbomCollection.Query()
            .Where($"components[*].licenses[*].license.id ANY = {value}" +
                   $" OR components[*].licenses[*].license.name ANY = {value}" +
                   $" OR components[*].licenses[*].expression ANY = {value}")
            .Select(s => s.serialNumber)
            .ToList();
    }

    // LiteDB caches parsed expressions by their text, including the first call's @0/@1 parameter values,
    // so values are embedded as escaped JSON literals instead of passed as parameters.
    private static string Literal(string value) => JsonSerializer.Serialize(new BsonValue(value));
}