## bug fixes
- do not modify any code outside of direct bug fixes
- Please fix all bugs listed in `bugs`
- use copilot skill `C:\Users\roija\.copilot\skills\maui-mvvm-development`
- use copilot skill `C:\Users\roija\.copilot\skills\test-driven-development-maui`

## bugs
- When dragging items between Wants Needs and Has, the Border background color is set to BackgroundColor="{DynamicResource SurfaceColor}". It changes when the item hovers over the list to #D8CCFF as it should, but when the item leaves the area of the drag gesture, it does not go back to the #FFF9F0 color it should be. 