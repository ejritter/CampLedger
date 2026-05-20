## bug fixes
- do not modify any code outside of direct bug fixes

## bugs
- `TripLedger.xaml` page has a visual bug. when adding a location it makes the button very large. it should not adjust the size of the button.
	-	Text="{Binding CurrentTripLocation.LocationName, StringFormat='Location: {0}'}"