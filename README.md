# Kerbin Waypoint Toolkit
Plan a Kerbin sightseeing route, then let your aircraft follow it. Inspired by **RCTestFlight’s RC-plane waypoint-mission videos**.

## What’s included

- **Waypoint Navigator:** KSP plugin using Kramax directional guidance and MechJeb aircraft control.
- **Flight Plan Editor:** offline editor for drawing routes and exporting compatible CFG files.

## Screenshots

### Flight Plan Editor

Draw and edit a route on the Kerbin map, with an estimated flight path and turn-radius guides.

![Flight Plan Editor showing a Kerbin route and estimated turns](Screenshots/Flight%20Plan%20Editor%20Screenshot.png)

### MechJeb Waypoint Navigator

The Waypoint Navigator window alongside MechJeb Aircraft Autopilot during flight.

![Waypoint Navigator and MechJeb Aircraft Autopilot in flight](Screenshots/MechJebWaypointNavigator%20Screenshot.png)

## Install the navigator

1. Install KSP 1, MechJeb and KSP Harmony 2 (`000_Harmony`).
2. Quit KSP. Copy `GameData/MechJebWaypointNavigator` from this folder into your KSP `GameData` folder.
3. On updates, replace the DLL and preserve your existing CFG files. The included CFG is only an example.

The navigator works only while **MechJeb Aircraft Autopilot is on**. It controls **roll and yaw**; **set altitude in MechJeb**. Set bank limits there too. NAV follows the lower of MechJeb BankAngle and RollLimit; it has no separate 30° cap.

## Make a route and fly it

1. Open `Flight Plan Editor/Launch the Editor.html` in a browser.
2. Add or move waypoints on the map, edit their details and save a CFG. Browsers with direct-file support can save to a selected file; otherwise use the download option.
3. Put the CFG in your KSP `GameData/MechJebWaypointNavigator` folder.
4. In flight, open **Plans**, refresh the files, choose your route and press **NAV on**.

Click a waypoint to change the target. **NAV off** disengages; **F8** hides/shows the window. Positive course deviation means right of the route. Route progress includes skipped legs and is not actual distance flown.

The editor’s orange flight-path overlay is a rough planning estimate. Match its speed and bank setting to your intended flight; it does not reproduce actual MechJeb tracking, yaw, wind or aircraft dynamics.

## Notes

The navigator’s code tests pass; coordinated flight and the physical bank envelope still need in-game validation. Requested-bank limits do not guarantee actual roll limits. Diagnostic records go to `KSP.log` during NAV.

`Source/` is for development, not installation. Navigator builds require .NET 8, Python 3 and an explicit KSP folder. Editor tests use `Source/Editor/tests/run-macos.py` on macOS. The shared `LICENSE` and `THIRD_PARTY_NOTICES.md` describe licensing and credits.
