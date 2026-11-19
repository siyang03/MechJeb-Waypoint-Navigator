# MechJeb Waypoint Navigator

Follows Kramax flight plans using Kramax directional guidance and MechJeb aircraft control. KSP 1, MechJeb and KSP Harmony 2 (`000_Harmony`) are required.

## Install

Quit KSP, then copy **`dist/GameData/MechJebWaypointNavigator`** into your KSP `GameData` folder. If using the ZIP, copy its `GameData/MechJebWaypointNavigator` folder. On updates, replace the DLL and keep your existing CFG files. The included `FlightPlans.cfg` is an example.

## Fly

1. Enable MechJeb Aircraft Autopilot on a stable airborne aircraft. Turn off other horizontal autopilots.
2. Place CFG files in `GameData/MechJebWaypointNavigator`. Open **Plans**, choose a file and plan. **Refresh** reloads files.
3. Click a waypoint to select it. With NAV running, clicking immediately changes the target.
4. Press **NAV on** to follow the route. **NAV off** disengages. Turning NAV on again starts a fresh intercept to the selected waypoint.
5. Press **F8** to hide or show the window. Set bank limits in MechJeb.

This mod works only while **MechJeb Aircraft Autopilot is on**. It controls **roll and yaw** to follow the route. **Set the altitude in MechJeb.**

> NAV follows the lower of MechJeb’s **BankAngle** and **RollLimit**, read live during flight. There is no additional navigator cap at 30°. Higher limits permit stronger bank commands; actual aircraft response is still controlled by MechJeb.

## Readout

- **Active:** waypoint number and name.
- **WP direction / Current Hdg:** bearing to the waypoint / aircraft heading.
- **Target bank / Current bank:** requested bank / measured aircraft bank. **Limit** shows MechJeb’s effective bank/roll limit; the navigation PID uses that same live limit.
- **Course deviation:** positive means right of the route; negative means left.
- **Route progress:** progress through the stored plan, including skipped legs—not actual distance flown.

Code tests pass; the rewritten guidance has not been flight-tested. Actual roll can exceed the requested bank. Diagnostic records appear in `KSP.log` every five seconds during NAV.

## Build

Requires .NET 8 (or compatible newer SDK), Python 3 and KSP with MechJeb and Harmony 2 installed:

```sh
./build.sh "/path/to/KSP"
./test.sh
./test.sh "/path/to/KSP"  # includes real KSP configuration tests
```

Set `DOTNET=/path/to/dotnet` if needed. Build output is in `dist/GameData`. The matching ZIP is `releases/MechJebWaypointNavigator.zip` and includes source. `dist/BUILD.json` records the current DLL checksum. Do not distribute `build/`, which may contain game dependencies.

See [docs/GUIDANCE.md](docs/GUIDANCE.md) for the source mapping and **THIRD_PARTY_NOTICES.md** for credits. License files describe the applicable terms.

## Folder layout

| Folder | Contents |
|---|---|
| `src/`, `tests/`, `tools/` | Source code, tests and build helpers |
| `dist/GameData/` | **Current installable plugin—copy from here** |
| `releases/` | Matching ZIP; extract outside `dist` |
| `docs/` | Guidance notes; old development records in `docs/history/` |
| `build/` | Temporary compiler/test output |
| `examples/`, `third_party/` | Example plan and attributed Kramax reference code |

Your supplied `KSP_osx`, `Kramax`, `MechJeb`, and suggestions folders remain development/reference material. They are not release packages.

## Multi-aircraft NAV

Each aircraft remembers its CFG, plan and starting waypoint. Engage NAV on A, switch to B, select B's route and engage NAV again. Switching changes the window's controls and readouts; NAV off and waypoint buttons affect only the active aircraft. The compact count shows all running NAV sessions. Each aircraft requires its own enabled MechJeb Aircraft Autopilot and independent altitude/speed setup.

All aircraft must remain loaded and unpacked inside KSP's active physics simulation. NAV automatically disengages on packing/unloading and requires manual reengagement after returning. Physics distance varies with KSP configuration and circumstances. Scene exit cleans up every session; scene reload starts with NAV off. There is no on-rails navigation, collision avoidance or automatic formation keeping.

Automated checks and compilation do not establish simultaneous physical flight. Multi-aircraft flight is **NOT TESTED in KSP**. Use the [manual flight checklist](docs/MULTI_VESSEL_TEST_PLAN.md) in a disposable installation/save.
