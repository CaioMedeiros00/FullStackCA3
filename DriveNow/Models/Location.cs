using System.Text.Json.Serialization;

namespace DriveNow.Models;

/// <summary>
/// A rental office (read-only).
/// The brief describes Name/Address/Phone; the live egolance.com API returns
/// branchName/city/county. Both formats are supported.
/// </summary>
public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;

    /// <summary>JSON alias for <see cref="Name"/> used by the egolance.com API.</summary>
    [JsonPropertyName("branchName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BranchName
    {
        get => null;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                Name = value;
            }
        }
    }

    /// <summary>Full address if the API supplies one, otherwise "City, Co. County".</summary>
    [JsonIgnore]
    public string DisplayAddress
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Address))
            {
                return Address;
            }
            var county = string.IsNullOrWhiteSpace(County) ? null : $"Co. {County}";
            var parts = new[] { City, county }.Where(p => !string.IsNullOrWhiteSpace(p));
            return string.Join(", ", parts);
        }
    }
}
