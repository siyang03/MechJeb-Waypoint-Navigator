# Kerbin Waypoint Toolkit
1. Plan a Kerbin sightseeing route with **Flight Plan Editor**, then
2. let your aircraft follow it with **MechJeb Waypoint Navigator**.

Inspired by [RCTestFlight’s waypoint-mission videos](https://youtu.be/vYeYZpBE51I). Based on [Kramax directional guidance](https://forum.kerbalspaceprogram.com/topic/150846-112x-kramax-autopilot-continued-course-guidance-and-auto-land-for-spaceplanes/).

## Install

1. **Waypoint Navigator** mod:
   1. Install MechJeb and KSP Harmony 2 (`000_Harmony`, search it on CKAN).
       - Chances are you have already installed them.
   2. Download `GameData/MechJebWaypointNavigator`, and copy folder `MechJebWaypointNavigator` into your KSP `GameData` folder.
       - On updates, replace the `GameData/MechJebWaypointNavigator` folder but **preserve your existing CFG files**.
2. Standalone **Flight Plan Editor** tool:
   1. Download folder `Flight Plan Editor`, place it anywhere you like, click `Launch the Editor.html` in the folder to run.


## User instruction: make a route and fly it

1. **Pre flight**, plan the routes:
   1. Open `Flight Plan Editor/Launch the Editor.html` in a browser.
   2. Add or move waypoints on the map, edit their details and save a CFG.
       - Browsers with direct-file support (Chrome / Edge) can save to a selected file; otherwise (Firefox) use the create and download option.
   3. Put the CFG in your KSP `GameData/MechJebWaypointNavigator` folder.
       - Or, as recommended, simply modify `GameData/MechJebWaypointNavigator/FlightPlans.cfg` directly on your Chrome browser

![Flight Plan Editor showing a Kerbin route and estimated turns](Screenshots/Flight%20Plan%20Editor%20Screenshot.png)

2. **In flight**:
   1. Open **Plans**, refresh the files, choose your route.
   2. **MechJeb Aircraft Autopilot**: turn on MechJeb Aircraft Autopilot.
      - In MechJeb, set the BankAngle and RollLimit, Altitude and Speed (to prevent Overshoot). 
   3. **Waypoint Navigator**: press NAV on, and it should read `NAV engaged`.

The navigator works only while **MechJeb Aircraft Autopilot is on**. It controls **roll and yaw**.

![Waypoint Navigator and MechJeb Aircraft Autopilot in flight](Screenshots/MechJebWaypointNavigator%20Screenshot.png)

## Notes

The navigator’s code tests pass; coordinated flight and the physical bank envelope still need in-game validation. Requested-bank limits do not guarantee actual roll limits. Diagnostic records go to `KSP.log` during NAV.

`Source/` is for development, not installation. Navigator builds require .NET 8, Python 3 and an explicit KSP folder. Editor tests use `Source/Editor/tests/run-macos.py` on macOS. The shared `LICENSE` and `THIRD_PARTY_NOTICES.md` describe licensing and credits.
