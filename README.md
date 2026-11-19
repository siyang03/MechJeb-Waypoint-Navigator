# MechJeb Waypoint Navigator

**Draw a flight route on a Kerbin map. Let your aircraft fly it automatically.**

- **Flight Plan Editor** — An interactive, offline map for creating routes and previewing turns.
- **Waypoint Navigator** — Follows your route using MechJeb Aircraft Autopilot.

Inspired by [RCTestFlight's waypoint missions](https://youtu.be/vYeYZpBE51I). Uses [Kramax directional guidance](https://forum.kerbalspaceprogram.com/topic/150846-112x-kramax-autopilot-continued-course-guidance-and-auto-land-for-spaceplanes/) and the [Kerbin map from Micro Map by KerbalMissile](https://github.com/KerbalMissile/Micro-Map) (MIT license).

## Download & Install

**1. Download**

[**Download MechJeb-Waypoint-Navigator (ZIP)**](https://github.com/siyang03/MechJeb-Waypoint-Navigator/releases/latest/download/MechJeb-Waypoint-Navigator.zip)

**2. Install**

Requires **KSP 1**, [MechJeb2](https://github.com/MuMech/MechJeb2), and **KSP Harmony 2** (`000_Harmony`, available on CKAN).

Extract the ZIP and copy `MechJebWaypointNavigator` into your KSP `GameData` folder. On updates, preserve your existing CFG flight plans.

**3. Launch Flight Plan Editor**

Open `GameData/MechJebWaypointNavigator/Launch the Editor.html` in **Chrome or Edge**. No installation or internet connection required.

## Quick Start — Plan & Fly

**1. Draw your route**

Open **Flight Plan Editor**, click the Kerbin map to add waypoints, and drag them to adjust your route.

Save your flight plan as a `.cfg` file directly in `GameData/MechJebWaypointNavigator/` (not inside a subfolder).

**⚠️ Browser compatibility:** Only **Chrome and Edge** are supported. Firefox and Safari are not supported.

![Flight Plan Editor showing a Kerbin route and estimated turns](Screenshots/Flight_Plan_Editor_Screenshot.png)

**2. Set up MechJeb**

In flight, enable **MechJeb Aircraft Autopilot**.

Configure your desired altitude, speed, **BankAngle**, and **RollLimit**. Click **Set** to apply changes to BankAngle (a green Set button means the new value hasn't been applied).

Higher bank limits allow tighter turns and reduce waypoint overshoot. The Navigator's effective bank limit is **min(BankAngle, RollLimit)**.

**3. Engage navigation**

Open **Waypoint Navigator → Plans**, refresh the CFG files, and select your flight plan.

Press **NAV on**. Your aircraft will follow the waypoints automatically!

![Waypoint Navigator and MechJeb Aircraft Autopilot in flight](Screenshots/MechJebWaypointNavigator_Screenshot.png)

## Good to Know

- **Altitude and speed:** NAV controls horizontal navigation only. Waypoint altitudes are informational; set Altitude Hold and Speed Hold separately in MechJeb.
- **Bank limits:** NAV displays the live effective limit and its source: `(MechJeb Roll)`, `(MechJeb Bank)`, or `(MechJeb)` when both limits are equal. Actual aircraft bank may exceed the commanded value.
- **Yaw control:** NAV normally uses MechJeb's yaw stabilization. If incompatible, it falls back to passive aerodynamic yaw stability, which may be less reliable on some aircraft.
- **SAS:** NAV automatically turns SAS off when engaged and back on when disengaged. Manually enabling SAS or MechJeb Heading Hold disengages NAV.
- **Waypoints:** Select another waypoint during NAV to redirect the aircraft without interrupting navigation.
- **Turn predictions:** The editor estimates turn paths using the selected speed and bank angle. These are estimates, not guarantees; high-speed sharp turns may overshoot.
- **F8** hides or shows the Navigator window.

## Development & Credits

`Source/` contains development code, tests, and documentation; it is not needed for installation. Building the Navigator requires .NET 8, Python 3, and a local KSP installation.

Diagnostic records appear in `KSP.log` during NAV.

See [LICENSE](LICENSE) and [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for licensing and acknowledgments.

MechJeb Waypoint Navigator is an independent add-on for MechJeb2 and is not affiliated with or endorsed by the MechJeb development team.
