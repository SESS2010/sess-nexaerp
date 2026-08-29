# Generates item_import.sql from the legacy item-master workbook.
# Seeds real UOMs and categories, then inserts items (dedup on item code).
import openpyxl
import uuid

PATH = r"C:\Users\SESS IT\Downloads\sess\testing files\item-master.xlsx"

wb = openpyxl.load_workbook(PATH, read_only=True, data_only=True)
ws = wb["Sheet3"]
rows = [r for r in ws.iter_rows(values_only=True) if any(c not in (None, "") for c in r)]
hdr = [str(h).strip() if h else "" for h in rows[1]]
idx = {h: i for i, h in enumerate(hdr)}

UOM_MAP = {  # normalized code -> (name, dimension, precision)
    "NOS": ("Numbers", "COUNT", 0), "MTR": ("Metre", "LENGTH", 2), "FT": ("Feet", "LENGTH", 2),
    "KGS": ("Kilogram", "MASS", 3), "LTR": ("Litre", "VOLUME", 2), "BOX": ("Box", "COUNT", 0),
    "PKT": ("Packet", "COUNT", 0), "ROLL": ("Roll", "COUNT", 0),
}
UOM_ALIAS = {"NOS": "NOS", "NOS.": "NOS", "NOs": "NOS", "MTRS": "MTR", "MTR": "MTR", "FT": "FT",
             "KGS": "KGS", "LTR": "LTR", "BOX": "BOX", "PKT": "PKT", "ROLL": "ROLL"}
CATEGORIES = {"REFRIDGERATION": ("REFRIGERATION", "Refrigeration"),
              "ELECTRICALS": ("ELECTRICALS", "Electricals"),
              "FABRICATION": ("FABRICATION", "Fabrication")}


def q(v):
    if v is None:
        return "NULL"
    return "'" + str(v).replace("'", "''") + "'"


def cell(r, name):
    v = r[idx[name]] if name in idx else None
    if v is None:
        return None
    s = str(v).strip()
    return s if s else None


items, problems, seen = [], [], set()
seen_barcodes = set()
for n, r in enumerate(rows[2:], start=3):
    code = cell(r, "ITEM CODE")
    name = cell(r, "Material Name")
    if not code or not name:
        if code or name:
            problems.append(f"row {n}: missing code or name -> skipped")
        continue
    code = code.upper()
    if " " in code or code in ("NOT AVAILABLE", "NA", "-", "N/A"):
        problems.append(f"row {n}: unusable item code '{code}' -> skipped (needs a real code)")
        continue
    if code in seen:
        problems.append(f"row {n}: duplicate item code {code} -> skipped (first kept)")
        continue
    seen.add(code)
    uom_raw = (cell(r, "UOM") or "NOS").strip()
    uom = UOM_ALIAS.get(uom_raw, UOM_ALIAS.get(uom_raw.upper(), "NOS"))
    dept = (cell(r, "Department") or "").upper()
    cat = CATEGORIES.get(dept, (None, None))[0]
    hsn = cell(r, "HSN Code")
    if hsn and hsn.endswith(".0"):
        hsn = hsn[:-2]
    gst = cell(r, "GST")
    try:
        gst_pct = round(float(gst) * 100, 2) if gst else 0
        if gst_pct > 100:  # someone typed 18 instead of 0.18
            gst_pct = round(float(gst), 2)
    except ValueError:
        gst_pct = 0
    cost = cell(r, "COST VALUE")
    try:
        cost_val = round(float(cost), 2) if cost else None
    except ValueError:
        cost_val = None
    barcode = cell(r, "BAR CODE")
    if barcode:
        bc = barcode.strip().upper()
        if bc in ("NOT AVAILABLE", "NA", "N/A", "-", "NIL"):
            barcode = None
        elif bc in seen_barcodes:
            problems.append(f"row {n} {code}: duplicate barcode '{barcode}' -> cleared")
            barcode = None
        else:
            seen_barcodes.add(bc)
    items.append(dict(
        id=str(uuid.uuid4()), code=code, name=name[:240], uom=uom, cat=cat,
        make=cell(r, "Make"), part=cell(r, "Model / Part Number"), barcode=barcode,
        hsn=hsn, gst=gst_pct, cost=cost_val, dept=dept.title(),
    ))

print(f"parsed {len(items)} items; {len(problems)} skipped/anomalies")
for p in problems[:8]:
    print(" !", p)
if len(problems) > 8:
    print(f" ... and {len(problems) - 8} more")

lines = ["BEGIN;"]
# Seed real UOMs (skip if code exists)
for code, (name, dim, prec) in UOM_MAP.items():
    lines.append(
        f'INSERT INTO advance.uoms ("Id","Code","Name","MeasurementDimension","QuantityPrecision","IsActive","CreatedAt","CreatedBy","Version") '
        f"SELECT gen_random_uuid(),{q(code)},{q(name)},{q(dim)},{prec},true,now(),'EXCEL_IMPORT',0 "
        f'WHERE NOT EXISTS (SELECT 1 FROM advance.uoms WHERE "Code"={q(code)});')
# Seed real categories
for legacy, (code, name) in CATEGORIES.items():
    lines.append(
        f'INSERT INTO advance.item_categories ("Id","Code","Name","IsActive","CreatedAt","CreatedBy","Version") '
        f"SELECT gen_random_uuid(),{q(code)},{q(name)},true,now(),'EXCEL_IMPORT',0 "
        f'WHERE NOT EXISTS (SELECT 1 FROM advance.item_categories WHERE "Code"={q(code)});')

for v in items:
    cat_expr = f'(SELECT "Id" FROM advance.item_categories WHERE "Code"={q(v["cat"])})' if v["cat"] else "NULL"
    lines.append(
        'INSERT INTO advance.items ("Id","ItemCode","IsItemCodeLocked","Name","DetailedDescription","CategoryId","SubcategoryId",'
        '"MaterialType","ItemType","IsReturnable","Uom","UomId","BaseUomId","ManufacturerMake","Model","PartNumber","HsnSacCode",'
        '"GstPercentage","QcRequired","SerialNumberTracking","BatchTracking","ShelfLifeTracking","Barcode",'
        '"MinimumStock","MaximumStock","ReorderLevel","StandardEstimatedPrice","Status","ApprovalStatus","IsActive","CreatedAt","CreatedBy","Version") VALUES ('
        f'{q(v["id"])},{q(v["code"])},true,{q(v["name"])},{q(v["name"])},{cat_expr},NULL,'
        f'{q(v["dept"])},\'SPARE\',false,{q(v["uom"])},'
        f'(SELECT "Id" FROM advance.uoms WHERE "Code"={q(v["uom"])}),'
        f'(SELECT "Id" FROM advance.uoms WHERE "Code"={q(v["uom"])}),'
        f'{q(v["make"])},NULL,{q(v["part"])},{q(v["hsn"])},'
        f'{v["gst"]},false,false,false,false,{q(v["barcode"])},'
        f'0,0,0,{v["cost"] if v["cost"] is not None else "NULL"},'
        "'Active','Approved',true,now(),'EXCEL_IMPORT',0) "
        'ON CONFLICT ("ItemCode") DO NOTHING;')
lines.append("COMMIT;")
lines.append("SELECT COUNT(*) AS total_items FROM advance.items;")
with open("item_import.sql", "w", encoding="utf-8") as f:
    f.write("\n".join(lines))
print("SQL written with", len(items), "item inserts")
