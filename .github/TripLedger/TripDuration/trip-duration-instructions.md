## Update current Trip Date: to be Trip Duration:
- Follow main `campledger-instructions.md` for overall project requirements. 
- TripLedgerPage needs to be updated from Trip Date: to Trip Duration:
- Update TripLedgerViewModel to support this change.
- Update History and HistoryViewModel to support this change. 
## Update functionality requirements
- Two DatePickers 
	- First DatePicker is StartDate
	- Second DatePicker is EndDate
- StartDate can't be the same as the EndDate
	- If they are prompt user that this app is not intended for same day trips. Please plan for at least one night.
- EndDate cannot be less than StartDate.
	- Disable any date previous to StartDate.