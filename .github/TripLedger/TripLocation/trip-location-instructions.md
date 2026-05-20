## Update current Trip Date: to be Trip Duration:
- Follow main `campledger-instructions.md` for overall project requirements. 
- TripLedgerPage needs to be updated to show Trip Location: under Trip Duration:
- Update TripLedgerViewModel to support this change.
- Update History and HistoryViewModel to support this change. 
## Update functionality requirements
	- User clicks Trip Location pin button
	- A popup using CommunityToolkit Popup shows displaying maps.google.com
	- user adds the location in this popup of google maps.
	- When user clicks Confirm, popup closes, the url that is generated behind the scenes for https://www.google.com/maps/place/{user chosen location} gets copied and saved.
	 - example: I go to Lackawanna State Park PA:
	  - Should get returned when popup closes:
	   -https://www.google.com/maps/place/Lackawanna+State+Park/@41.562691,-75.7067476,17z/data=!3m1!4b1!4m6!3m5!1s0x89c4d60f6cdd35bd:0x9ce14cea6604d73f!8m2!3d41.562691!4d-75.7067476!16s%2Fm%2F026rrjr?entry=ttu&g_ep=EgoyMDI2MDUxMy4wIKXMDSoASAFQAw%3D%3D
	-Do not alter or try to massage this url in anyway, this is what the user chose, use the exact url value.
	-Trip Location pin button text updates to display the Name of the location user chose.
	-WHen user clicks Trip Location pin after setting, it should load the exact url that was saved.
	-Popup Window should always be able to display buttons, it should be 80% of the current height and width of main window.
	-No external search bar. Searching should be strictly within the google maps webview.
## Agent/Model requirements
	-Do not use Claude models of any kind.
	-Using OpenAI models or Copilot for implementation.

## required packages
 - Communitytoolkit.Popupv2 or most modern version