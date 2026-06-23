namespace CampLedger.Services;

public interface ITripDurationValidationService
{
    bool IsValid(DateTime startDate, DateTime endDate);

    string GetValidationMessage();
}
