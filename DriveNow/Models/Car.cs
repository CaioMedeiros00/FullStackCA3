using System.ComponentModel.DataAnnotations;

namespace DriveNow.Models;

/// <summary>A rental vehicle. Data annotations enforce the Car business rules on the client.</summary>
public class Car
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Registration number is required.")]
    [StringLength(20, MinimumLength = 5, ErrorMessage = "Registration number must be between 5 and 20 characters.")]
    [Display(Name = "Registration Number")]
    public string RegistrationNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Make is required.")]
    [StringLength(50, ErrorMessage = "Make cannot exceed 50 characters.")]
    public string Make { get; set; } = string.Empty;

    [Required(ErrorMessage = "Model is required.")]
    [StringLength(50, ErrorMessage = "Model cannot exceed 50 characters.")]
    public string Model { get; set; } = string.Empty;

    [Range(1990, 2035, ErrorMessage = "Year must be between 1990 and 2035.")]
    public int Year { get; set; } = DateTime.Today.Year;

    [Range(typeof(decimal), "1", "1000", ErrorMessage = "Daily rate must be between €1 and €1000.")]
    [Display(Name = "Daily Rate")]
    public decimal DailyRate { get; set; } = 50;

    public bool Available { get; set; } = true;

    [Required(ErrorMessage = "Fuel type is required.")]
    [StringLength(20, ErrorMessage = "Fuel type cannot exceed 20 characters.")]
    [Display(Name = "Fuel Type")]
    public string FuelType { get; set; } = string.Empty;

    public string DisplayName => $"{Make} {Model} ({RegistrationNumber})";

    /// <summary>Creates a copy so edits in a form don't change the list until saved.</summary>
    public Car Clone() => (Car)MemberwiseClone();
}
