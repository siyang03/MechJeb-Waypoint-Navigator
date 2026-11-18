#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
if [[ $# != 1 ]]; then echo 'Usage: DOTNET=/path/to/dotnet ./build.sh /path/to/KSP'; exit 2; fi
KSP_ROOT="$1"
MANAGED="$KSP_ROOT/KSP.app/Contents/Resources/Data/Managed"
if [[ ! -d "$MANAGED" ]]; then MANAGED="$KSP_ROOT/KSP_x64_Data/Managed"; fi
[[ -f "$MANAGED/Assembly-CSharp.dll" ]] || { echo 'KSP managed assemblies not found'; exit 2; }
[[ -f "$KSP_ROOT/GameData/MechJeb2/Plugins/MechJeb2.dll" ]] || { echo 'MechJeb2.dll not found'; exit 2; }
DOTNET="${DOTNET:-dotnet}"
SDK_BASE="$("$DOTNET" --list-sdks | tail -1)"
SDK_VERSION="${SDK_BASE%% *}"
SDK_PARENT="${SDK_BASE#*[}"; SDK_PARENT="${SDK_PARENT%]*}"
CSC="$SDK_PARENT/$SDK_VERSION/Roslyn/bincore/csc.dll"
mkdir -p build dist/GameData/MechJebWaypointNavigator/Plugins
args=(-noconfig -nostdlib+ -target:library -langversion:7.3 -optimize+ -out:build/MechJebWaypointNavigator.dll)
for name in mscorlib System System.Core Assembly-CSharp UnityEngine UnityEngine.CoreModule UnityEngine.IMGUIModule UnityEngine.InputLegacyModule; do
 [[ ! -f "$MANAGED/$name.dll" ]] || args+=("-r:$MANAGED/$name.dll")
done
HARMONY_DLL="$KSP_ROOT/GameData/000_Harmony/0Harmony.dll"
[[ -f "$HARMONY_DLL" ]] || { echo 'Install KSP Harmony 2 (000_Harmony) before building this yaw-bridge version'; exit 2; }
args+=("-r:$HARMONY_DLL")
"$DOTNET" "$CSC" "${args[@]}" src/*.cs
cp build/MechJebWaypointNavigator.dll dist/GameData/MechJebWaypointNavigator/Plugins/
cp examples/FlightPlans.cfg dist/GameData/MechJebWaypointNavigator/
python3 tools/package.py
echo 'Built dist/GameData/MechJebWaypointNavigator. No game files were modified.'
