namespace CampLedger.Services;

public interface IToastNotificationService
{
    Task ShowAsync(string message);
}
