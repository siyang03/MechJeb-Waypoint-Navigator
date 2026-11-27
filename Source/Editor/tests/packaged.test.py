"""Offline packaging checks: python3 Source/Editor/tests/packaged.test.py."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
DEV = ROOT / 'Source/Editor'
PKG = ROOT / 'GameData/MechJebWaypointNavigator/Flight Plan Editor'

class PackagingTests(unittest.TestCase):
    def test_data_is_identical(self):
        self.assertEqual((DEV/'src/kerbinside.js').read_bytes(), (PKG/'src/kerbinside.js').read_bytes())

    def test_scripts_exist_and_load_data_before_app(self):
        for folder, name in [(DEV, 'Launch the Editor.html'), (PKG, 'main.html')]:
            html = (folder/name).read_text()
            scripts = re.findall(r'<script[^>]+src="([^"]+)"', html)
            for script in scripts:
                self.assertTrue((folder/script).is_file(), script)
                self.assertFalse('://' in script)
            self.assertLess(scripts.index('src/kerbinside.js'), scripts.index('src/app.js'))

    def test_optional_section_follows_stats_and_defaults_off(self):
        for folder, name in [(DEV, 'Launch the Editor.html'), (PKG, 'main.html')]:
            html = (folder/name).read_text()
            section = html.index('<section class="additional-map-layers"')
            self.assertGreater(section, html.index('<div class="stats">'))
            self.assertLess(section, html.index('<aside>'))
            self.assertIn('id="toggleKerbinSide" aria-pressed="false"', html)
            app = (folder/'src/app.js').read_text()
            self.assertNotIn('locationsPanel', html)
            self.assertNotIn('locationList', html)
            self.assertNotIn('hitLocation', app)
            self.assertNotIn('selectLocation', app)
            self.assertIn('let showKerbinSide=false;', app)
            self.assertIn('  drawKerbinSideAirfields();', app)
            self.assertNotIn('KerbinSide', (folder/'src/locations.js').read_text())

    def test_packaged_waypoint_visibility_remains(self):
        self.assertIn('id="showWaypoints" type="checkbox" checked', (PKG/'main.html').read_text())
        app = (PKG/'src/app.js').read_text()
        self.assertIn("$('showWaypoints').checked?waypoints():[]", app)
        self.assertIn("$('showWaypoints').checked?waypoints().length-1:-1", app)

if __name__ == '__main__':
    unittest.main()
