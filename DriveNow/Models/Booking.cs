using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DriveNow.Validation;

namespace DriveNow.Models;

/// <summary>
/// A customer booking for a car.
/// The brief names the dates StartDate/EndDate, while the live egolance.com API uses
/// pickupDate/returnDate (and adds locationId). The alias properties below let the same
/// model read either format and send both, so it works with either API version.
/// </summary>
public class Booking
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please select a car.")]
    [Display(Name = "Car")]
    public int CarId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please select a pickup location.")]
    [Display(Name = "Pickup Location")]
    public int LocationId { get; set; }

    [Required(ErrorMessage = "Customer name is required.")]
    [StringLength(100, ErrorMessage = "Customer name cannot exceed 100 characters.")]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = string.Empty;

    [NotInPast(ErrorMessage = "Start date cannot be in the past.")]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [DateGreaterThan(nameof(StartDate), ErrorMessage = "End date must be after the start date.")]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(1);

    /// <summary>JSON alias for <see cref="StartDate"/> used by the egolance.com API.</summary>
    [JsonPropertyName("pickupDate")]
    public DateTime PickupDate
    {
        get => StartDate;
        set => StartDate = value;
    }

    /// <summary>JSON alias for <see cref="EndDate"/> used by the egolance.com API.</summary>
    [JsonPropertyName("returnDate")]
    public DateTime ReturnDate
    {
        get => EndDate;
        set => EndDate = value;
    }
}
