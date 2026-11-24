namespace DriveNow.State;

/// <summary>
/// Shared, per-user application state. Components subscribe to <see cref="OnChange"/>
/// so that, for example, the navigation badge and dashboard refresh as soon as a
/// booking or car is changed on another page.
/// </summary>
public class AppState
{
    public event Action? OnChange;

    /// <summary>Number of bookings in progress right now (shown as a badge in the NavMenu).</summary>
    public int? ActiveBookingCount { get; private set; }

    /// <summary>When cars or bookings were last changed by this user.</summary>
    public DateTime? LastDataChange { get; private set; }

    // Remembered Cars page filters - kept when navigating away and back.
    public string CarSearchTerm { get; set; } = string.Empty;
    public bool ShowAvailableCarsOnly { get; set; }
    public string CarFuelFilter { get; set; } = string.Empty;

    // Remembered Bookings page filters.
    public string BookingSearchTerm { get; set; } = string.Empty;
    public string BookingStatusFilter { get; set; } = string.Empty;

    public void SetActiveBookingCount(int count)
    {
        if (ActiveBookingCount == count)
        {
            return;
        }
        ActiveBookingCount = count;
        NotifyStateChanged();
    }

    /// <summary>Call after any create/update/delete so other components can refresh.</summary>
    public void NotifyDataChanged()
    {
        LastDataChange = DateTime.Now;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
