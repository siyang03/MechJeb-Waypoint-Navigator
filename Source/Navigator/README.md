# MechJeb Waypoint Navigator

KSP 1 aircraft waypoint autopilot using Kramax directional guidance and MechJeb aircraft control. Supports independent multi-aircraft navigation starting with **v1.2.0**.

Requires **KSP 1**, [MechJeb2](https://github.com/MuMech/MechJeb2), and **KSP Harmony 2** (`000_Harmony`).

See the [main README](../../README.md) for screenshots, the flight demonstration, and formation-flight tips.

## Install

Quit KSP, then copy `dist/GameData/MechJebWaypointNavigator` into your KSP `GameData` folder.

If using the ZIP, extract its `GameData/MechJebWaypointNavigator` folder. On updates, replace the DLL and preserve your existing CFG flight plans.

The included `FlightPlans.cfg` is an example.

## Fly

1. Enable **MechJeb Aircraft Autopilot** on a stable airborne aircraft. Disable competing horizontal autopilots.
2. Place CFG files in `GameData/MechJebWaypointNavigator`. Open **Plans**, select a CFG and flight plan. Use **Refresh** to reload files.
3. Click a waypoint to select your starting target.
4. Press **NAV on** to follow the route. Press **NAV off** to disengage.
5. Click another waypoint during NAV to redirect the aircraft immediately.
6. Press **F8** to hide or show the Navigator window.

NAV uses Kramax guidance to command MechJeb's native roll control and yaw stabilization. **Altitude and speed must be configured separately in MechJeb.**

**Bank limits:** NAV follows the lower of MechJeb's **BankAngle** and **RollLimit**, read live during flight. There is no additional fixed 30° limit. Actual aircraft bank may exceed the commanded value.

## Multi-aircraft NAV (v1.2.0)

Multiple aircraft can follow independent flight plans simultaneously.

1. Engage NAV on aircraft A.
2. Switch to aircraft B while A remains within physics range.
3. Select B's flight plan and engage NAV.
4. Switch between aircraft to monitor or adjust each one's navigation.

Each aircraft maintains its own flight plan, waypoint progress, navigation PID, MechJeb controller, and SAS state.

The Navigator window always controls the **currently active aircraft**. NAV off and waypoint selections do not affect other aircraft. The window displays the number of active NAV sessions.

**Limitations:**

- All aircraft must remain **loaded and unpacked** within KSP's active physics range.
- NAV disengages when an aircraft becomes packed or unloaded. Reengagement is manual.
- Each aircraft requires its own enabled MechJeb Aircraft Autopilot and separately configured altitude/speed control.
- Leaving the flight scene disengages all sessions.
- Automatic formation keeping, speed synchronization, collision avoidance, and on-rails background navigation are not supported.

[Physics Range Extender (PRE)](https://github.com/jrodrigv/PhysicsRangeExtender) may help keep aircraft loaded at greater distances, subject to performance limitations.

## Readouts

- **Active:** Current waypoint number and name.
- **WP direction / Current Hdg:** Bearing to the waypoint / current aircraft heading.
- **Target bank / Current bank:** Requested bank / measured aircraft bank.
- **Limit:** Live effective MechJeb bank limit and its source.
- **Course deviation:** Positive means right of the route;
