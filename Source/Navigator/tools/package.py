#!/usr/bin/env python3
"""One install tree in dist; one matching archive in releases. Never bundle game DLLs."""
from pathlib import Path
from datetime import datetime, timezone
import hashlib, json, shutil, zipfile
r=Path(__file__).resolve().parents[1]
d=r/'dist'; game=d/'GameData/MechJebWaypointNavigator'; release=r/'releases'
release.mkdir(exist_ok=True)
# Do not silently leave an older extracted mod beside the current build.
extra=[p.name for p in d.iterdir() if (p.is_dir() and p.name!='GameData') or p.suffix.lower()=='.zip']
if extra:raise SystemExit('Move extracted packages/ZIPs out of dist before packaging: '+', '.join(extra))
licenses=game/'Licenses';licenses.mkdir(parents=True,exist_ok=True)
for name in ['LICENSE','LICENSE.MIT-original','THIRD_PARTY_NOTICES.md']:shutil.copy2(r/name,licenses/name)
shutil.copy2(r/'third_party/Kramax/LICENSE.txt',licenses/'Kramax-LICENSE.txt')
dll=game/'Plugins/MechJebWaypointNavigator.dll'
assert dll.read_bytes()==(r/'build/MechJebWaypointNavigator.dll').read_bytes()
manifest={'built_at_utc':datetime.now(timezone.utc).isoformat(timespec='seconds'),'dll':'GameData/MechJebWaypointNavigator/Plugins/MechJebWaypointNavigator.dll','sha256':hashlib.sha256(dll.read_bytes()).hexdigest(),'source_sha256':{str(p.relative_to(r)):hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted((r/'src').glob('*.cs'))}}
(d/'BUILD.json').write_text(json.dumps(manifest,indent=2)+'\n')
(d/'INSTALL.txt').write_text('CURRENT INSTALL FILES\n\nCopy GameData/MechJebWaypointNavigator into your KSP GameData folder.\nQuit KSP before replacing the plugin. Preserve your existing CFG files.\n\nThis is the only installable folder in dist.\nThe matching ZIP is ../releases/MechJebWaypointNavigator.zip.\nBUILD.json records this DLL checksum and its source checksums.\n')
archive=release/'MechJebWaypointNavigator.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as z:
 for p in sorted(game.rglob('*')):
  if p.is_file() and p.name!='.DS_Store':z.write(p,p.relative_to(d))
 for name in ['README.md','LICENSE','LICENSE.MIT-original','THIRD_PARTY_NOTICES.md']:z.write(r/name,name)
 for name in ['BUILD.json','INSTALL.txt']:z.write(d/name,name)
 z.write(r/'docs/GUIDANCE.md','docs/GUIDANCE.md')
 for folder in ['src','third_party','tools','tests','examples','docs']:
  for p in sorted((r/folder).rglob('*')):
   if p.is_file() and not any(x in p.relative_to(r).parts for x in ['bin','obj','__pycache__']) and p.suffix!='.pyc' and p.name!='.DS_Store':z.write(p,Path('Source')/p.relative_to(r))
 for name in ['build.sh','test.sh','README.md','LICENSE','LICENSE.MIT-original','THIRD_PARTY_NOTICES.md']:z.write(r/name,Path('Source')/name)
with zipfile.ZipFile(archive) as z:
 assert z.testzip() is None
 assert z.read(manifest['dll'])==dll.read_bytes()
 assert not any(p.endswith(('Assembly-CSharp.dll','MechJeb2.dll','0Harmony.dll')) for p in z.namelist())
print('Verified current dist/GameData and matching releases/MechJebWaypointNavigator.zip')
