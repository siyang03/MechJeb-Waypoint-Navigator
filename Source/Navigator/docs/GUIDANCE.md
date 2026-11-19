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

4,657 core/session assertions (including 42 multi-vessel assertions) and 256 yaw-bridge/profile assertions pass, including original-code comparisons for geometry, PID state, sequencing and a 200-position replay. Real KSP CFG tests also pass. These are code-level checks, not a flight test or proof that physical overbank is fixed.

## Yaw bridge

`HarmonyYawPatch` changes only the final `HeadingHoldEnabled` read in the verified classic aircraft controller's `Drive` method. The first read, which selects heading-generated bank versus RollTarget, stays unchanged. When NAV owns Roll Hold, the final gate also accepts that ownership and runs the existing zero-sideslip yaw PID. All original yaw target, error, gain, integral and actuator calculations remain in MechJeb. Kramax guidance is unchanged.

`ComputeYaw` compares surface-path direction with nose direction, not heading rate. A coordinated left or right turn therefore produces zero yaw error. This is sideslip stabilization, not a new turn-coordination/feedforward law or a guarantee of perfect coordination.

Compatibility is checked using SHA-256 of the installed `Drive` and `ComputeYaw` IL bodies, plus a strict two-gate instruction match. Unsupported profiles fail closed. The supplied MechJeb DLL matches; the downloaded dev branch does not contain this classic module. Harmony changes code in memory only. No installed DLL or PID gain is modified.

The 256 bridge tests check the transpiler, unknown-layout rejection, normal-mode preservation, installed IL hashes/gates and zero-sideslip left/right heading changes. Core tests check yaw ownership and immediate override/release. They do not run the actual detour in KSP or test aerodynamic rudder response.

The UI now has NAV on/off and Plans controls only. The redundant Pause/Resume and Guidance panel were removed. The bank row includes the live MechJeb limit; the navigation PID output cap now follows that live limit rather than retaining Kramax’s fixed ±30° cap.


## v1.2.0 session lifecycle

One flight-scene window owns a NavSessionRegistry keyed by Vessel.id, plus per-vessel VesselPlanSelection records. Each NavSession holds the original Vessel reference, body and parts snapshot, a deep plan snapshot, Navigation (including its PID), adapter and SAS suppression obligation. No navigation state is shared. Refreshing a foreground CFG cannot replace a running route. Selections remain local to this flight-scene instance; they are not persisted in saves.

Start validates physics/identity/body/conflicts, rejects duplicate vessel/controller ownership, initializes guidance, acquires native MechJeb roll and suppresses that vessel's SAS transactionally. Update checks validity and overrides; FixedUpdate alone steps guidance using universal time and the physics timestep. Both process defensive session snapshots and isolate exceptions. All operations remain on KSP's main thread. Direct-to resets guidance without reacquiring control. Stop releases yaw/horizontal ownership before SAS restoration, records a per-vessel reason and removes only that entry. All-session cleanup continues through exceptions and is repeatable.

Vessel switching is not a stop condition. Packing, unloading, landing, destruction, scene exit, identity/reference/topology/body changes, MechJeb controller replacement/disable, SAS activation, horizontal takeover and competing autopilots stop the affected session. A changed parts list is treated conservatively as docking/splitting; NAV is never transferred automatically. Stops/telemetry include vessel GUIDs, with vessel names in stop and telemetry records. No physics distances, packing rules, native yaw/pitch/throttle controllers or guidance gains were changed.

### Installed MechJeb compatibility inspection

The supplied installed MechJeb2.dll SHA-256 is `1691fa9cdc18d6f73efe954d19f2dc7bb57e23becb5e30ea66677104d87f9bc6`. Binary IL inspection found:

- CheckControlledVessel registers/removes OnFlyByWire on the associated vessel.
- OnFlyByWire checks deactivation, vessel binding and master core, then calls Drive; it has no active-vessel gate.
- Core Drive updates that vessel's VesselState and invokes enabled modules' Drive methods. Aircraft Autopilot Drive has no active-vessel gate.
- FixedUpdate continues for a nonfocused master. Focus checks govern settings reload: ClearModulesCache and OnLoad run on gaining focus. The local dev source has the same broad per-vessel design but differs from the installed binary.

This establishes an inactive-vessel control path by binary inspection, not proof of physical flight. Settings reloads can change modes or controller references; the navigator detects these and stops safely rather than rebinding. Whether the supplied installation retains all NAV settings through repeated focus changes remains an in-game acceptance test. No inactive-vessel bypass patch was added. Harmony's existing verified IL hashes and gate checks are unchanged; unsupported profiles retain passive yaw fallback.

### Kramax conflict attribution

The supplied Kramax George class has a public Vessel vessel field, assigned by SwitchVessels and used by its control callbacks. For that API, a pilot bound to a different vessel does not conflict. Missing/unknown attribution or mode fields retain conservative global rejection. A pilot with unknown/null vessel and any active mode remains a conflict. Kramax switching can move its pilot onto a NAV aircraft; subsequent checks stop that aircraft's NAV. Kramax's algorithms are untouched.

### Validation boundaries

Baseline: 4,615 assertions. Current: 4,657 core/session assertions, 256 yaw/profile assertions, and real KSP ConfigNode round-trip checks. Tests use the production NavSession and registry with minimal vessels/controllers; they cover independent demands/limits/SAS/yaw, switching lookup, route snapshots, completion, direct-to, failures, cleanup exceptions, identity/topology/controller changes and no automatic resumption. They do not execute Unity physics or the native Harmony detour.

All flight acceptance cases are **NOT TESTED**. See [MULTI_VESSEL_TEST_PLAN.md](MULTI_VESSEL_TEST_PLAN.md). The local distribution ZIP is a build-validation artifact only; no release, tag or publication is authorized.
