namespace CampLedger.Services;

public sealed class TripDurationValidationService : ITripDurationValidationService
{
    public bool IsValid(DateTime startDate, DateTime endDate)
    {
        return startDate.Date != endDate.Date;
    }

    public string GetValidationMessage()
    {
        return "This app is not intended for same day trips. Please plan for at least one night.";
    }
}
