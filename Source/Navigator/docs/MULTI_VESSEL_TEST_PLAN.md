# v1.2.0 manual KSP flight acceptance

**Actual in-game status: NOT TESTED.** Every box below starts unchecked. Compilation, binary inspection and unit tests are not flight-test passes.

Use a separate disposable copy of KSP 1.12.x and a disposable save. Do not replace the supplied installation's files or saves without permission. Copy the locally built navigator DLL and example/custom CFGs into that test copy only; retain its existing compatible MechJeb and KSP Harmony 2 installation. Never run the original and test copy against the same save. Back up the disposable save before each destructive test.

Record tester/date, KSP version, MechJeb DLL SHA-256, navigator BUILD.json/SHA-256, Harmony version, craft names/GUIDs, routes/starting waypoints, physics configuration, and any other autopilot mods. Use stable aircraft with adequate fuel and aerodynamic stability. Assign safe altitude separation and divergent routes; NAV supplies no collision avoidance or formation keeping. Arrange switching using stock nearby-vessel controls without changing physics loading distances. If simultaneous airborne/unpacked setup is not feasible, mark the test BLOCKED rather than passing it.

For every test retain KSP.log from the disposable copy and record observed results and pass/fail. Filter for `Waypoint Navigator telemetry: vessel=<GUID> name=...`, `Waypoint Navigator stopped <GUID> name=...: <reason>`, `Waypoint Navigator cleanup`, and MechJeb exceptions/focus-reload records. Telemetry appears approximately every five real-time seconds per session. No unrelated session should emit a stop when testing an isolated failure. A missing expected line or exception needs investigation; it is not a pass.

## A. Single aircraft regression

- [ ] PASS
- [ ] FAIL
- [ ] BLOCKED / NOT TESTED

**Setup:** One airborne, loaded/unpacked, MechJeb-equipped aircraft; a route with several legs and a reachable final waypoint.

**Actions:**

1. Enable MechJeb Aircraft Autopilot and configure altitude/speed hold. Record BankAngle and RollLimit; select the CFG, plan and starting waypoint.
2. Engage NAV nearly level; verify initially enabled SAS remains on. Command or reach bank above 5° and verify SAS stays on for 2.98 seconds and turns off at three continuous physics seconds. Interrupt shutdown with bank <=5° and verify a fresh three seconds is required; repeat when engaging already banked. Return actual and effective commanded bank to <=5° for five physics seconds and verify recovery. Test both bank directions, exactly 5°, and a new turn during recovery. Follow several waypoint transitions; compare bearing, heading, bank, cross-track distance and route progress with the craft.
3. Click a different waypoint during NAV. Verify an immediate new intercept with no SAS/autopilot toggle. Test FAF/RW inbound-leg behavior where available.
4. Change each bank limit independently and apply its Set button. Verify target bank respects the lower live limit; observe actual bank separately.
5. Disengage, verify SAS restoration, reengage and complete the route. Repeat with prior Heading Hold and with prior Roll Hold. Hide/show using F8.

**Expected:** Single-aircraft guidance/readouts and horizontal restoration match v1.1.0. Completion releases roll ownership and restores SAS; altitude/speed targets remain intact.

**KSP.log evidence:** One vessel GUID in telemetry; explicit Disengaged and Route complete stop reasons. No cleanup exceptions.

**Tester/date, observations, log path and deviations:** ____________________

## B. Two aircraft simultaneous navigation

- [ ] PASS
- [ ] FAIL
- [ ] BLOCKED / NOT TESTED

**Setup:** Two stable MechJeb aircraft airborne and unpacked within active physics; safely separated altitudes and distinctly different routes A/B.

**Actions:**

1. Configure altitude/speed hold and enable Aircraft Autopilot separately on both craft. Select A route and engage A NAV.
2. Switch A → B. Observe A continuing its turn/route physically; verify the UI displays B selection/readouts and count 1.
3. Select route B and engage B NAV. Verify count 2 and B adaptive SAS state. Hold A near 20° and B near 1°; verify A SAS off and B SAS on.
4. Switch B → A → B repeatedly, including while each is turning. Observe both aircraft following their own different routes for several waypoint transitions.
5. Hide the window with F8 and continue; show it again. Record any stop during focus/settings reload.

**Expected:** Both physically navigate independently while unpacked. Switching alone preserves engagement, route/PID progress and SAS state. UI always belongs to the active aircraft. A focus reload that changes ownership must stop safely but fails uninterrupted-switch acceptance and must be reported.

**KSP.log evidence:** Interleaved telemetry for two GUIDs, separate demands/progress; no switch-triggered stop. Retain MechJeb focus save/reload messages.

**Tester/date, observations, log path and deviations:** ____________________

## C. Independent controls

- [ ] PASS
- [ ] FAIL
- [ ] BLOCKED / NOT TESTED

**Setup:** Both aircraft from B navigating distinct routes.

**Actions:**

1. Disengage A; verify A SAS/horizontal restoration and B still flying NAV with count 1.
2. Reengage A, change its target waypoint and inspect both craft.
3. Change/apply only A BankAngle/RollLimit. Compare B limits and commands before/after.
4. Choose different CFGs, different plans within one CFG, then the same plan with different starting waypoints on A/B. Switch repeatedly.
5. Refresh A CFG while B is navigating; confirm B retains its original plan and guidance. Restore any edited CFG only in the disposable copy.

**Expected:** Only the intended craft changes. Separate route state and selections survive focus switches. Refresh does not replace a running snapshot.

**KSP.log evidence:** A-only stops/target changes; B telemetry remains continuous with its own limits and waypoint progress.

**Tester/date, observations, log path and deviations:** ____________________

## D. Physics range

- [ ] PASS
- [ ] FAIL
- [ ] BLOCKED / NOT TESTED

**Setup:** Two engaged aircraft; no physics/loading-distance modifications.

**Actions:**

1. Allow one craft to move beyond the circumstances-dependent active physics range while observing loaded/unpacked state using available stock/debug information.
2. Confirm its NAV terminates when packed or unloaded, with no further control commands.
3. Keep the other craft loaded/unpacked and verify continued NAV.
4. Return the distant craft to active physics. Verify NAV remains off until explicitly reengaged; check SAS/horizontal/yaw cleanup.

**Expected:** Only the invalid session stops. There is no on-rails flight simulation and no automatic resume. Do not assume a fixed distance.

**KSP.log evidence:** Affected GUID stops with vessel identity/flight-state reason; remaining GUID continues telemetry; no resumed telemetry until manual engagement.

**Tester/date, observations, log path and deviations:** ____________________

## E. Manual override

- [ ] PASS
- [ ] FAIL
- [ ] BLOCKED / NOT TESTED

**Setup:** Both aircraft engaged; record pre-NAV horizontal modes.

**Actions:**

1. While A expects SAS off, enable it manually; while A expects SAS on, disable it manually. Verify each mismatch stops only A and preserves its manual state. A toggle that returns to the expected boolean between checks cannot be detected.
2. Reengage A and activate MechJeb Heading Hold on A. Verify takeover is preserved and B continues.
3. Repeat with manual RollTarget/mode changes and another incompatible MechJeb attitude mode.
4. If Kramax is installed, activate a Kramax mode on A and then on an unrelated vessel. Verify attributable conflicts stop the competing craft; unknown APIs must retain conservative protection.

**Expected:** SAS override releases owned horizontal holds before restoration. Manual Heading Hold/targets are not overwritten. B remains independent unless an unattributable conservative conflict is detected.

**KSP.log evidence:** A-only external SAS override / horizontal mode changed / conflict reasons. No unrelated stop with a reliably attributable Kramax API.

**Tester/date, observations, log path and deviations:** ____________________

## F. Scene transition

- [ ] PASS
- [ ] FAIL
- [ ] BLOCKED / NOT TESTED

**Setup:** Both aircraft engaged in the disposable save.

**Actions:**

1. Leave flight via a supported stock scene transition; retain the log.
2. Reload the save/flight scene. Inspect both craft and the navigator count.
3. Reengage manually and repeat the transition to exercise repeated disable/destroy cleanup.

**Expected:** Every session cleans up; count returns to zero. Reload does not reengage NAV or preserve stale yaw/controller ownership. Prior independent MechJeb modes may persist according to MechJeb itself.

**KSP.log evidence:** All GUIDs have Navigator disabled / Left flight stop records, no repeated-cleanup exception and no NAV telemetry until manual reengagement.

**Tester/date, observations, log path and deviations:** ____________________

## G. Failure recovery

- [ ] PASS
- [ ] FAIL
- [ ] BLOCKED / NOT TESTED

**Setup:** Both aircraft engaged, safely separated.

**Actions:**

1. Disable Aircraft Autopilot or MechJeb on A. Confirm isolated stop and restoration.
2. Reenable it and manually reengage NAV. Confirm B remained unaffected.
3. Where practical in the disposable save, exercise docking/splitting or destruction of one test vessel safely. Never expect NAV transfer to the resulting vessel.

**Expected:** Failures stop only the affected session. Controller replacement/topology/identity changes terminate conservatively; recovery always requires manual engagement.

**KSP.log evidence:** Affected GUID has disabled / controller changed / vessel topology or flight-state reason; B continues and there are no stale command writes.

**Tester/date, observations, log path and deviations:** ____________________

## H. Three-aircraft stress test

- [ ] PASS
- [ ] FAIL
- [ ] BLOCKED / NOT TESTED

**Setup:** If feasible, three stable airborne/unpacked aircraft with safely separated flight paths and three distinct routes.

**Actions:**

1. Enable/configure each MechJeb and engage three separate NAV sessions. Verify count 3.
2. Cycle focus A → B → C repeatedly for multiple route legs; hide/show UI and inspect each selection, readout and physical trajectory.
3. Change a waypoint/limit on one craft, override a second, and keep the third navigating.
4. Reengage as needed, then leave flight and inspect all cleanup records.

**Expected:** Every loaded/unpacked session remains independent; no cross-vessel writes or unexplained terminations. All sessions clean up on shutdown. Record BLOCKED if setup cannot keep three craft unpacked.

**KSP.log evidence:** Three GUIDs with distinct telemetry; isolated stops and clean final shutdown for all remaining GUIDs.

**Tester/date, observations, log path and deviations:** ____________________


## Adaptive SAS acceptance additions

- [ ] Stock SAS supplies stabilization; NAV reports no active Harmony yaw bridge (dependency retained). Actual flight behavior remains unverified.
- [ ] At <=5° in both actual and commanded bank, initially OFF SAS recovers after five continuous physics seconds. A turn at 4.9 seconds resets the full delay. Small corrections preserve enabled SAS.
- [ ] Switching vessels preserves independent countdowns. Direct-to resets only its own countdown and cannot hide an existing large RollTarget behind a zero UI command.
- [ ] Normal stops with SAS either ON or OFF restore prior owned horizontal control; manual SAS overrides preserve manual state. Altitude/speed holds and bank limits remain unchanged.
