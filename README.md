# MechJeb Waypoint Navigator

**Draw a flight route on a Kerbin map. Let your aircraft fly it automatically.**

- **Flight Plan Editor** — An interactive, offline map for creating routes and previewing turns.
- **Waypoint Navigator** — Follows your route using MechJeb Aircraft Autopilot.

Inspired by [RCTestFlight's waypoint missions](https://youtu.be/vYeYZpBE51I). Uses [Kramax directional guidance](https://forum.kerbalspaceprogram.com/topic/150846-112x-kramax-autopilot-continued-course-guidance-and-auto-land-for-spaceplanes/) and the [Kerbin map from Micro Map by KerbalMissile](https://github.com/KerbalMissile/Micro-Map) (MIT license).

## Download & Install

**1. Download:**

- [**Download MechJeb-Waypoint-Navigator (ZIP)**](https://github.com/siyang03/MechJeb-Waypoint-Navigator/releases/latest/download/MechJeb-Waypoint-Navigator.zip)

**2. Install Waypoint Navigator**

Requires [MechJeb2](https://github.com/MuMech/MechJeb2) and **KSP Harmony 2** (`000_Harmony`, available on CKAN).

Extract the ZIP and copy `MechJebWaypointNavigator` into your KSP `GameData` folder.

On updates, replace the plugin files but **keep your existing CFG flight plans**.

**3. Launch Flight Plan Editor**

Open `GameData/MechJebWaypointNavigator/Launch the Editor.html` in **Chrome or Edge**. No separate installation or internet connection required.

## Quick Start — Plan & Fly

**1. Draw your route**

Open **Flight Plan Editor**, click the map to add waypoints, and drag them to adjust your route.

Save the flight plan as a `.cfg` file **directly in** `GameData/MechJebWaypointNavigator/` (**not inside any subfolder**) so Waypoint Navigator can load it in-game.

**⚠️ Browser compatibility:** Flight Plan Editor only supports **Google Chrome and Microsoft Edge**. **Firefox and Safari are not supported.**

![Flight Plan Editor showing a Kerbin route and estimated turns](Screenshots/Flight_Plan_Editor_Screenshot.png)

**2. Set up MechJeb**

In flight, enable **MechJeb Aircraft Autopilot**.

Configure your desired altitude, speed, **BankAngle**, and **RollLimit** in MechJeb. Higher bank limits allow tighter turns and reduce waypoint overshoot.

The navigator's maximum commanded bank angle is **min(BankAngle, RollLimit)**, using the two values set in MechJeb.

**3. Engage navigation**

Open **Waypoint Navigator → Plans**, refresh the files, and select your flight plan.

Press **NAV on**. The window should display `NAV engaged`.

That's it! Your aircraft will follow the waypoints automatically.

![Waypoint Navigator and MechJeb Aircraft Autopilot in flight](Screenshots/MechJebWaypointNavigator_Screenshot.png)

## Good to Know

- **MechJeb Aircraft Autopilot must remain enabled.** The navigator uses MechJeb for roll and yaw control; altitude and speed remain independently controlled.
- **F8** shows or hides the Waypoint Navigator window.
- The navigator's bank limit follows the lower of MechJeb's BankAngle and RollLimit. Actual aircraft bank may exceed the commanded value.
- Turn predictions are estimates, not guarantees. Very sharp turns at high speeds may overshoot.
- On updates, **preserve your custom CFG files**.

## Development & Credits

`Source/` contains development code and tests; it is not needed for installation. Navigator builds require .NET 8, Python 3, and a local KSP installation. Diagnostic records appear in `KSP.log` during NAV.

See [LICENSE](LICENSE) and [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for licensing and acknowledgments.

MechJeb Waypoint Navigator is an independent add-on for MechJeb2 and is not affiliated with or endorsed by the MechJeb development team.
