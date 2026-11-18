# Third-party credits

This plugin adapts horizontal navigation code from [Kramax AutoPilot](https://github.com/linuxgurugamer/KramaxAutoPilot), including work by its original author, contributors and maintainers. Kramax derives from [Pilot Assistant by Crzyrndm](https://github.com/Crzyrndm/Pilot-Assistant) and credits the [Movable Type great-circle formulas](https://www.movable-type.co.uk/scripts/latlong.html).

Kramax's supplied root license states **CC BY-NC-SA 4.0**. It is preserved in `third_party/Kramax/LICENSE.txt`; installed copies include `Licenses/Kramax-LICENSE.txt`. See [the license](https://creativecommons.org/licenses/by-nc-sa/4.0/) and the distribution's `LICENSE`.

The original `George.cs` header also credits Thunder Aerospace Corporation's KSP examples, **Copyright 2013 Taranis Elsu**, and carries an [Apache 2.0 notice](https://www.apache.org/licenses/LICENSE-2.0). Original headers and non-affiliation notices are retained in the source snapshots.

Our changes separate horizontal guidance from aircraft control, inject radius/time inputs, add a coincident-leg guard, and send bank demand to MechJeb. Vertical, throttle, landing, gear and Kramax actuator controllers are excluded. **GUIDANCE.md** maps the adapted blocks.

`LICENSE.MIT-original` preserves the notice for independently written code. It does not relicense Kramax-derived code under MIT.

The yaw bridge uses the separately installed KSP Harmony 2 library. Harmony and MechJeb binaries are not bundled or modified on disk.
