#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
DOTNET="${DOTNET:-dotnet}"
SDK_BASE="$("$DOTNET" --list-sdks | tail -1)"
SDK_VERSION="${SDK_BASE%% *}"
SDK_PARENT="${SDK_BASE#*[}"; SDK_PARENT="${SDK_PARENT%]*}"
CSC="$SDK_PARENT/$SDK_VERSION/Roslyn/bincore/csc.dll"
REF_ROOT="$SDK_PARENT/../packs/Microsoft.NETCore.App.Ref"
REF_VERSION="$(ls "$REF_ROOT" | sort -V | tail -1)"
refs=(); for file in "$REF_ROOT/$REF_VERSION/ref/"*/*.dll; do refs+=("-r:$file"); done
mkdir -p build/tests
python3 tools/prepare-kramax-reference.py
"$DOTNET" "$CSC" -nologo -noconfig -nostdlib+ -target:exe "${refs[@]}" -out:build/tests/Tests.dll src/Navigation.cs src/KramaxGeometry.cs src/KramaxVelocityPid.cs src/MechJebAdapter.cs src/MechJebYawBridge.cs tests/BridgeInstallerStub.cs tests/Program.cs build/tests/UpstreamOracle.cs
cat > build/tests/Tests.runtimeconfig.json <<'JSON'
{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.0"},"rollForward":"LatestMajor"}}
JSON
"$DOTNET" build/tests/Tests.dll
if [[ $# -gt 0 ]]; then
 MANAGED="$1/KSP.app/Contents/Resources/Data/Managed"
 [[ -d "$MANAGED" ]] || MANAGED="$1/KSP_x64_Data/Managed"
 "$DOTNET" "$CSC" -nologo -noconfig -nostdlib+ -target:exe "${refs[@]}" "-r:$MANAGED/Assembly-CSharp.dll" "-r:$MANAGED/UnityEngine.CoreModule.dll" -out:build/tests/ConfigTests.dll src/Navigation.cs src/KramaxGeometry.cs src/KramaxVelocityPid.cs src/FlightPlans.cs tests/ConfigIntegration/Program.cs
 cp build/tests/Tests.runtimeconfig.json build/tests/ConfigTests.runtimeconfig.json
 cp "$MANAGED/Assembly-CSharp.dll" "$MANAGED/UnityEngine.CoreModule.dll" build/tests/
 "$DOTNET" build/tests/ConfigTests.dll "${2:-examples/FlightPlans.cfg}"
 HARMONY_DLL="$1/GameData/000_Harmony/0Harmony.dll"
 "$DOTNET" "$CSC" -nologo -noconfig -nostdlib+ -target:exe "${refs[@]}" "-r:$HARMONY_DLL" -out:build/tests/YawPatchTests.dll src/MechJebYawBridge.cs src/HarmonyYawPatch.cs tests/YawPatchTests.cs
 cp "$HARMONY_DLL" build/tests/
 cp build/tests/Tests.runtimeconfig.json build/tests/YawPatchTests.runtimeconfig.json
 "$DOTNET" build/tests/YawPatchTests.dll "$1/GameData/MechJeb2/Plugins/MechJeb2.dll"
fi
