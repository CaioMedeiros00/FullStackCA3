using DriveNow.Models;

namespace DriveNow.State;

public sealed record ToastMessage(Guid Id, string Message, AlertType Type, DateTime CreatedAt);

/// <summary>Queues toast notifications that are shown by the ToastContainer component.</summary>
public class ToastService
{
    private readonly List<ToastMessage> _toasts = [];
    private static readonly TimeSpan DisplayTime = TimeSpan.FromSeconds(4);

    public event Action? OnChange;

    public IReadOnlyList<ToastMessage> Toasts => _toasts;

    public void ShowSuccess(string message) => Show(message, AlertType.Success);
    public void ShowError(string message) => Show(message, AlertType.Danger);
    public void ShowInfo(string message) => Show(message, AlertType.Info);
    public void ShowWarning(string message) => Show(message, AlertType.Warning);

    public void Show(string message, AlertType type)
    {
        var toast = new ToastMessage(Guid.NewGuid(), message, type, DateTime.Now);
        _toasts.Add(toast);
        OnChange?.Invoke();
        _ = RemoveAfterDelayAsync(toast.Id);
    }

    public void Remove(Guid id)
    {
        if (_toasts.RemoveAll(t => t.Id == id) > 0)
        {
            OnChange?.Invoke();
        }
    }

    private async Task RemoveAfterDelayAsync(Guid id)
    {
        await Task.Delay(DisplayTime);
        Remove(id);
    }
}
