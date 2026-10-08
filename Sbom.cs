#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace sbom;

public record Sbom
{
    public string bomFormat { get; set; }
    public string specVersion { get; set; }
    public string serialNumber { get; set; }
    public int version { get; set; }
    public Components[] components { get; set; }
}

public class Components
{
    public string type { get; set; }
    public string name { get; set; }
    public string version { get; set; }
    public string cpe { get; set; }
    public string purl { get; set; }
    public Swid swid { get; set; }
    public string group { get; set; }
    public LicenseChoice[] licenses { get; set; }
}

public class Swid
{
    public string tagId { get; set; }
    public string name { get; set; }
    public string version { get; set; }
    public Text text { get; set; }
}

public class Text
{
    public string contentType { get; set; }
    public string encoding { get; set; }
    public string content { get; set; }
}

public class LicenseChoice
{
    public License? license { get; set; }
    public string? expression { get; set; }
}

public class License
{
    public string? id { get; set; }
    public string? name { get; set; }
}