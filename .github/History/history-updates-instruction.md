## Update current Trip Date: to be Trip Duration:
- Follow main `campledger-instructions.md` for overall project requirements. 
- `TripHistoryPage.xaml` needs to be updated.
- Include all relational files like viewmodels/models/views. 


## Fixes
- user clicks edit it should not show "Location: [set location]" twice. It should only show it once.
- `Change Location` button and `Clear` button should be HorizontalStackLayout and the same size.
- `Change Location` button and `Clear` button should appear under the current location button.
- If no location is set, then just show `Add Location`. 
- If clicks `Confirm` or `Cancel` for location, it should not close the Edit box of that `TripHistroyViewModel`. It should remain in edit mode until the user clicks `Save Trip` or `Cancel Edit`.
- `Notes` should be editable when user clicks `Edit`.
	- Do not include the original `Save` and `Cancel` buttons for the notes. 
	-`Notes` edits should be managed by clicking `Save Trip` or `Cancel Edit`.
- `Packed` and `Unpacked` should be editable when user clicks `Edit`.

## Requirements
- Follow current project insturctions and repository instructions.
- see UI Framework and Style from `campledger-instructions.md`.
- Do not remove or modify existing code. 