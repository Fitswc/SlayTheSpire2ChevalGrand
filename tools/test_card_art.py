"""All write tests are isolated under tools, never in the project's art folder."""
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from PIL import Image

import card_art as app


class CropTests(unittest.TestCase):
    def test_aspect_and_bounds_for_portrait_landscape_and_edge_drags(self):
        for source in [(2400, 800), (600, 1800), (850, 646)]:
            for target in [(850, 646), (1000, 760), (500, 500)]:
                for center in [(-10, -10), (.5, .5), (10, 10)]:
                    for zoom in [1, 3, 8]:
                        with self.subTest(source=source, target=target, center=center, zoom=zoom):
                            x1, y1, x2, y2 = app.crop_box(source, target, center, zoom)
                            self.assertGreaterEqual(x1, 0)
                            self.assertGreaterEqual(y1, 0)
                            self.assertLessEqual(x2, source[0] + 1e-8)
                            self.assertLessEqual(y2, source[1] + 1e-8)
                            self.assertAlmostEqual((x2-x1)/(y2-y1), target[0]/target[1])

    def test_actual_pixels_follow_crop_position(self):
        image = Image.new("RGBA", (200, 100), "red")
        image.paste("blue", (100, 0, 200, 100))
        left = app.render_crop(image, (50, 50), (0, .5), 1)
        right = app.render_crop(image, (50, 50), (1, .5), 1)
        self.assertEqual(left.size, (50, 50))
        self.assertEqual(left.getpixel((25, 25)), (255, 0, 0, 255))
        self.assertEqual(right.getpixel((25, 25)), (0, 0, 255, 255))

    def test_transparency_survives(self):
        image = app.render_crop(Image.new("RGBA", (300, 300), (20, 40, 60, 0)),
                                (850, 646), (.5, .5), 2)
        self.assertEqual(image.getpixel((400, 300))[3], 0)


class SaveTests(unittest.TestCase):
    def test_png_and_explicit_overwrite_backup(self):
        with tempfile.TemporaryDirectory(dir=app.TOOLS) as folder:
            folder = Path(folder)
            target = folder / "images" / "CGSStrike.png"
            backup_dir = folder / "backups"
            original = Image.new("RGBA", (850, 646), "red")
            replacement = Image.new("RGBA", (1000, 760), "blue")
            app.save_png(original, target, backup_dir=backup_dir)
            before = target.read_bytes()
            with self.assertRaises(FileExistsError):
                app.save_png(replacement, target, backup_dir=backup_dir)
            self.assertEqual(target.read_bytes(), before)
            backup = app.save_png(replacement, target, overwrite=True, backup_dir=backup_dir)
            self.assertEqual(backup.read_bytes(), before)
            with Image.open(target) as saved:
                self.assertEqual(saved.format, "PNG")
                self.assertEqual(saved.size, (1000, 760))
                self.assertEqual(saved.getpixel((0, 0)), (0, 0, 255, 255))
            self.assertEqual(list(target.parent.glob("*.tmp")), [])

    def test_backup_failure_preserves_old_art_and_cleans_staged_file(self):
        with tempfile.TemporaryDirectory(dir=app.TOOLS) as folder:
            folder = Path(folder)
            target = folder / "CGSStrike.png"
            app.save_png(Image.new("RGBA", (50, 50), "red"), target)
            before = target.read_bytes()
            with patch.object(app.shutil, "copy2", side_effect=OSError("cannot back up")):
                with self.assertRaises(OSError):
                    app.save_png(Image.new("RGBA", (50, 50), "blue"), target,
                                 overwrite=True, backup_dir=folder / "backups")
            self.assertEqual(target.read_bytes(), before)
            self.assertEqual(list(folder.glob("*.tmp")), [])


class ProjectTests(unittest.TestCase):
    def test_real_card_mapping_and_unconfigured_card(self):
        cards = {c.name: c for c in app.discover_cards()}
        self.assertEqual(cards["CGSStrike"].title, "打击")
        self.assertTrue(cards["CGSStrike"].configured)
        self.assertEqual(app.preferred_size(cards["CGSStrike"]), (1000, 760))
        self.assertEqual(cards["CGSForwardAStep"].title, "小小的勇气")
        self.assertFalse(cards["CGSEmptyShell"].configured)
        self.assertNotIn("CGSSwitchStance", cards)
        self.assertEqual(len(cards), len(app.discover_cards()))
        for card in cards.values():
            self.assertEqual(card.path.parent, app.ART)
            self.assertEqual(card.path.name, card.name + ".png")


if __name__ == "__main__":
    unittest.main()
