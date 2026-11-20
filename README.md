# MechJeb Waypoint Navigator

**Draw a flight route on a Kerbin map. Let your aircraft fly it automatically.**

- **Flight Plan Editor** — An interactive, offline map for creating routes and previewing turns.
- **Waypoint Navigator** — Follows your route using MechJeb Aircraft Autopilot.
- **Multi-aircraft NAV (v1.2.0)** — Fly multiple aircraft along independent flight plans simultaneously, as long as they remain within KSP's active physics range.

### Video Demonstration — Multi-Aircraft NAV (v1.2.0)

[![MechJeb Waypoint Navigator — Multi-Aircraft Flight Demonstration](https://img.youtube.com/vi/7WIXcDXlVmw/hqdefault.jpg)](https://youtu.be/7WIXcDXlVmw)

[**Watch the demonstration on YouTube (6:27)**](https://youtu.be/7WIXcDXlVmw)

Two aircraft independently following waypoint routes, with close formation flying achieved through manual speed adjustments.

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
<!-- ![Flight Plan Editor showing a Kerbin route and estimated turns](https://raw.githubusercontent.com/siyang03/MechJeb-Waypoint-Navigator/main/Screenshots/Flight_Plan_Editor_Screenshot.png) -->

**2. Set up MechJeb**

In flight, enable **MechJeb Aircraft Autopilot**.

Configure your desired altitude, speed, **BankAngle**, and **RollLimit**. Click **Set** to apply changes to BankAngle (a green Set button means the new value hasn't been applied).

Higher bank limits allow tighter turns and reduce waypoint overshoot. The Navigator's effective bank limit is **min(BankAngle, RollLimit)**.

**3. Engage navigation**

Open **Waypoint Navigator → Plans**, refresh the CFG files, and select your flight plan.

Press **NAV on**. Your aircraft will follow the waypoints automatically!

![Waypoint Navigator and MechJeb Aircraft Autopilot in flight](Screenshots/MechJebWaypointNavigator_Screenshot.png)
<!-- ![Waypoint Navigator and MechJeb Aircraft Autopilot in flight](https://raw.githubusercontent.com/siyang03/MechJeb-Waypoint-Navigator/main/Screenshots/MechJebWaypointNavigator_Screenshot.png) -->

## Good to Know

- **Altitude and speed:** NAV controls horizontal navigation only. Waypoint altitudes are informational; set Altitude Hold and Speed Hold separately in MechJeb.
- **Bank limits:** NAV displays the live effective limit and its source: `(MechJeb Roll)`, `(MechJeb Bank)`, or `(MechJeb)` when both limits are equal. Actual aircraft bank may exceed the commanded value.
- **Yaw control:** NAV normally uses MechJeb's yaw stabilization. If incompatible, it falls back to passive aerodynamic yaw stability, which may be less reliable on some aircraft.
- **SAS:** NAV automatically turns SAS off when engaged and back on when disengaged. Manually enabling SAS or MechJeb Heading Hold disengages NAV.
- **Waypoints:** Select another waypoint during NAV to redirect the aircraft without interrupting navigation.
- **Turn predictions:** The editor estimates turn paths using the selected speed and bank angle. These are estimates, not guarantees; high-speed sharp turns may overshoot.
- **F8** hides or shows the Navigator window.


## Multi-Aircraft Navigation & Formation Flight (v1.2.0)

**Fly multiple aircraft simultaneously, each following its own flight plan!**

![Two aircraft flying in formation](Screenshots/Formation_Flight_Screenshot.png)
<!-- ![Two aircraft flying in formation](https://raw.githubusercontent.com/siyang03/MechJeb-Waypoint-Navigator/main/Screenshots/Formation_Flight_Screenshot.png) -->

### Quick Start

1. **Aircraft A:** Enable MechJeb Aircraft Autopilot, load a flight plan, and engage NAV.
2. **Aircraft B:** Switch to B (keeping A within physics range), configure MechJeb, and engage NAV with its own flight plan.
3. **That's it!** Both aircraft navigate independently. Switch between them to monitor or adjust their routes.

Each aircraft remembers its own flight plan, waypoint progress, and NAV state. The Navigator window controls only the currently selected aircraft.

### Formation Flying Tips

For close formation flight, **using the same route works better than separate parallel routes.**

1. Load the **same flight plan** on both aircraft.
2. Increase the trailing aircraft's target speed to close the gap.
3. Adjust speeds manually to maintain separation.

### Important Limitations

- **Physics range:** Both aircraft must remain loaded and unpacked. [Physics Range Extender (PRE)](https://github.com/jrodrigv/PhysicsRangeExtender) may help extend the range.
- **No automatic formation keeping:** NAV does not synchronize speeds, maintain spacing, or prevent collisions.
- **NAV disengagement:** Packed or unloaded aircraft lose NAV. Leaving the flight scene disengages all NAV sessions. Reengage manually when necessary.
- **Independent autopilots:** Each aircraft needs its own MechJeb Aircraft Autopilot configured.

**Tested:** Simultaneous two-aircraft navigation and close formation flying, including manual speed adjustments. Larger formations have not yet been flight-tested.

[Watch the multi-aircraft demonstration (6:27)](https://youtu.be/7WIXcDXlVmw) | [Full flight timelapse (11 min)](https://youtu.be/j17pTe91xRg)

## Development & Credits

`Source/` contains development code, tests, and documentation; it is not needed for installation. Building the Navigator requires .NET 8, Python 3, and a local KSP installation.

Diagnostic records appear in `KSP.log` during NAV.

See [LICENSE](LICENSE) and [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for licensing and acknowledgments.
<!-- See [LICENSE](https://github.com/siyang03/MechJeb-Waypoint-Navigator/blob/main/LICENSE) and [THIRD_PARTY_NOTICES.md](https://github.com/siyang03/MechJeb-Waypoint-Navigator/blob/main/THIRD_PARTY_NOTICES.md) for licensing and acknowledgments. -->

MechJeb Waypoint Navigator is an independent add-on for MechJeb2 and is not affiliated with or endorsed by the MechJeb development team.

## Appendix: Full Flight Recording

[**Watch the full flight timelapse on YouTube (11 min, 3× speed)**](https://youtu.be/j17pTe91xRg)

An accelerated recording of the original 34-minute test flight, preserved for reference. The [shorter demonstration](https://youtu.be/7WIXcDXlVmw) highlights the key moments.

---

**AI Acknowledgment:** This project was developed with the assistance of [ChatGPT by OpenAI](https://chatgpt.com), including software design, implementation, debugging, testing, and documentation.
