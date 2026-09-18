"""Run: python -m unittest discover -s tools/tests -p test_generate_item_import.py"""
import pathlib
import subprocess
import sys
import tempfile
import unittest
import openpyxl

ROOT = pathlib.Path(__file__).resolve().parents[2]


class ItemImportCategoryTests(unittest.TestCase):
    def generate(self, departments, directory):
        book = openpyxl.Workbook()
        sheet = book.active
        sheet.title = "Sheet3"
        sheet.append(["Legacy item import fixture"])
        sheet.append(["ITEM CODE", "Material Name", "Department", "UOM"])
        for ordinal, department in enumerate(departments):
            sheet.append([f"TEST-{ordinal}", f"Test component {ordinal}", department, "NOS"])
        source = directory / "input.xlsx"
        output = directory / "output.sql"
        book.save(source)
        result = subprocess.run([sys.executable, str(ROOT / "tools/generate-item-import.py"),
                                 str(source), str(output)], capture_output=True, text=True)
        return result, output

    def test_all_known_department_spellings_emit_canonical_stores_categories(self):
        with tempfile.TemporaryDirectory(prefix="item-import-category-") as temp:
            result, output = self.generate(["REFRIDGERATION", "REFRIGERATION", "ELECTRICALS", "FABRICATION"], pathlib.Path(temp))
            self.assertEqual(0, result.returncode, result.stderr)
            sql = output.read_text(encoding="utf-8")
            self.assertEqual(4, sql.count("INSERT INTO advance.items "))
            self.assertEqual(3, sql.count("INSERT INTO advance.item_categories "))
            for code in ["REF", "ELE", "FAB"]:
                self.assertIn(f'WHERE "Code"=\'{code}\'', sql)
            for code in ["REFRIGERATION", "ELECTRICALS", "FABRICATION"]:
                self.assertNotIn(f'WHERE "Code"=\'{code}\'', sql)
            self.assertIn("'NOS','Numbers','COUNT',0,true", sql)

    def test_unknown_department_refuses_before_writing_an_unreceivable_item(self):
        with tempfile.TemporaryDirectory(prefix="item-import-category-") as temp:
            result, output = self.generate(["UNKNOWN"], pathlib.Path(temp))
            self.assertNotEqual(0, result.returncode)
            self.assertIn("unmapped department", result.stderr)
            self.assertFalse(output.exists())


if __name__ == "__main__":
    unittest.main()
