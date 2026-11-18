# Guidance: Kramax to MechJeb

The directional backend ports Kramax's horizontal Course mode. The earlier custom capture, acceptance, turn anticipation and bank-governor algorithms were removed.

## Source mapping

References are to the supplied Kramax `Source/George.cs`, unless noted.

| Our code | Kramax source |
|---|---|
| `KramaxGeometry.cs` | `CourseStatus` and `CourseUtils`, lines 295–469 |
| `Navigation.UpdateData` | `FlightPlan.UpdateData`, lines 592–643: position-derived lateral/along-track velocity |
| `Navigation.ShouldSequence` | `CheckForSequence`, lines 668–738: waypoint passage and early turns |
| `Navigation.CourseDemand` | `vesselController`, lines 1982–2053: desired lateral velocity and bank demand |
| `KramaxVelocityPid.cs` | `Source/PID/PID_Controller.cs`: navigation velocity PID |
| `Navigation.Start` and sequencing | `Activate`, `DirectToWaypoint`, `SequenceWaypoint` and `hdgModeChanged` |

Original snapshots and checksums are in `../third_party/Kramax`. Tests compile original excerpts separately and compare their results with the port.

## Rules

- Desired lateral velocity: cross-track error divided by 10 seconds, directed toward the route and capped at half surface speed. Kramax reverses the demand when traveling backward along the leg.
- Navigation PID defaults: P 4, I 0.8, D 2; bank output uses the live MechJeb effective limit (the lower of BankAngle and RollLimit); integral contribution ±1.
- Passage: along-track leg length minus 10 m, after the leg has been valid. There is no acceptance circle.
- Early turns: fixed 3°/s estimate. FAF/RW/Stop flags suppress early turns.
- Direct-to: uses current position, except FAF/RW targets retain their inbound leg when available.

## MechJeb interface and differences

Kramax produces a bank demand. We pass it to MechJeb's native **RollTarget**, with Roll Hold on and Heading Hold off. MechJeb's bank limits apply unchanged. No Kramax aircraft roll/yaw controller is included. MechJeb's existing yaw PID now runs through the scoped yaw bridge. Full aircraft motion is therefore not identical to Kramax.

Coincident legs under 1 mm are skipped to avoid division by zero. Final completion releases NAV. Reengagement resets the guidance state and starts a fresh intercept. Previously enabled Heading Hold resumes on the current heading to avoid a stale-target turn. Vertical, throttle, landing and gear control are excluded.

## Validation

4,553 core assertions and 256 yaw-bridge/profile assertions pass, including original-code comparisons for geometry, PID state, sequencing and a 200-position replay. Real KSP CFG tests also pass. These are code-level checks, not a flight test or proof that physical overbank is fixed.

## Yaw bridge

`HarmonyYawPatch` changes only the final `HeadingHoldEnabled` read in the verified classic aircraft controller's `Drive` method. The first read, which selects heading-generated bank versus RollTarget, stays unchanged. When NAV owns Roll Hold, the final gate also accepts that ownership and runs the existing zero-sideslip yaw PID. All original yaw target, error, gain, integral and actuator calculations remain in MechJeb. Kramax guidance is unchanged.

`ComputeYaw` compares surface-path direction with nose direction, not heading rate. A coordinated left or right turn therefore produces zero yaw error. This is sideslip stabilization, not a new turn-coordination/feedforward law or a guarantee of perfect coordination.

Compatibility is checked using SHA-256 of the installed `Drive` and `ComputeYaw` IL bodies, plus a strict two-gate instruction match. Unsupported profiles fail closed. The supplied MechJeb DLL matches; the downloaded dev branch does not contain this classic module. Harmony changes code in memory only. No installed DLL or PID gain is modified.

The 256 bridge tests check the transpiler, unknown-layout rejection, normal-mode preservation, installed IL hashes/gates and zero-sideslip left/right heading changes. Core tests check yaw ownership and immediate override/release. They do not run the actual detour in KSP or test aerodynamic rudder response.

The UI now has NAV on/off and Plans controls only. The redundant Pause/Resume and Guidance panel were removed. The bank row includes the live MechJeb limit; the navigation PID output cap now follows that live limit rather than retaining Kramax’s fixed ±30° cap.
