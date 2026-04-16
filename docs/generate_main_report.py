from __future__ import annotations

import datetime as dt
import struct
import zipfile
from pathlib import Path
from xml.sax.saxutils import escape


ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / "docs"
OUT = DOCS / "รายงานหลัก.docx"

NS = (
    'xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" '
    'xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" '
    'xmlns:wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing" '
    'xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" '
    'xmlns:pic="http://schemas.openxmlformats.org/drawingml/2006/picture"'
)


def text(value: object) -> str:
    return escape(str(value), {'"': "&quot;"})


def run(value: str, bold: bool = False, size: int = 32) -> str:
    value = text(value)
    bold_xml = "<w:b/>" if bold else ""
    return (
        "<w:r><w:rPr>"
        f"{bold_xml}<w:rFonts w:ascii=\"TH Sarabun New\" w:hAnsi=\"TH Sarabun New\" "
        "w:eastAsia=\"TH Sarabun New\" w:cs=\"TH Sarabun New\"/>"
        f"<w:sz w:val=\"{size}\"/><w:szCs w:val=\"{size}\"/>"
        "</w:rPr>"
        f"<w:t xml:space=\"preserve\">{value}</w:t></w:r>"
    )


def p(
    value: str = "",
    *,
    bold: bool = False,
    size: int = 32,
    align: str | None = None,
    before: int = 0,
    after: int = 120,
    indent: int = 0,
) -> str:
    jc = f"<w:jc w:val=\"{align}\"/>" if align else ""
    ind = f"<w:ind w:left=\"{indent}\"/>" if indent else ""
    return (
        "<w:p><w:pPr>"
        f"<w:spacing w:before=\"{before}\" w:after=\"{after}\"/>"
        f"{jc}{ind}</w:pPr>{run(value, bold=bold, size=size)}</w:p>"
    )


def h1(value: str) -> str:
    return p(value, bold=True, size=40, before=280, after=160)


def h2(value: str) -> str:
    return p(value, bold=True, size=36, before=180, after=120)


def bullet(value: str) -> str:
    return p(f"• {value}", indent=360, after=60)


def page_break() -> str:
    return '<w:p><w:r><w:br w:type="page"/></w:r></w:p>'


def png_size(path: Path) -> tuple[int, int]:
    with path.open("rb") as f:
        header = f.read(24)
    if header[:8] != b"\x89PNG\r\n\x1a\n":
        return 1200, 800
    return struct.unpack(">II", header[16:24])


def image_block(rid: str, title: str, path: Path, docpr_id: int) -> str:
    width_px, height_px = png_size(path)
    max_width_emu = 5_900_000
    cx = max_width_emu
    cy = int(max_width_emu * height_px / max(width_px, 1))
    if cy > 7_400_000:
        cy = 7_400_000
        cx = int(7_400_000 * width_px / max(height_px, 1))
    return (
        p(title, bold=True, align="center", before=120)
        + "<w:p><w:pPr><w:jc w:val=\"center\"/></w:pPr><w:r><w:drawing>"
        f"<wp:inline distT=\"0\" distB=\"0\" distL=\"0\" distR=\"0\">"
        f"<wp:extent cx=\"{cx}\" cy=\"{cy}\"/>"
        f"<wp:docPr id=\"{docpr_id}\" name=\"{text(title)}\"/>"
        "<wp:cNvGraphicFramePr><a:graphicFrameLocks noChangeAspect=\"1\"/></wp:cNvGraphicFramePr>"
        "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\">"
        "<pic:pic><pic:nvPicPr>"
        f"<pic:cNvPr id=\"{docpr_id}\" name=\"{text(path.name)}\"/>"
        "<pic:cNvPicPr/>"
        "</pic:nvPicPr><pic:blipFill>"
        f"<a:blip r:embed=\"{rid}\"/>"
        "<a:stretch><a:fillRect/></a:stretch>"
        "</pic:blipFill><pic:spPr>"
        f"<a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"{cx}\" cy=\"{cy}\"/></a:xfrm>"
        "<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom>"
        "</pic:spPr></pic:pic>"
        "</a:graphicData></a:graphic></wp:inline></w:drawing></w:r></w:p>"
    )


def table(headers: list[str], rows: list[list[object]]) -> str:
    def cell(value: object, header: bool = False) -> str:
        fill = "<w:shd w:fill=\"E8F0E8\"/>" if header else ""
        return (
            "<w:tc><w:tcPr><w:tcW w:w=\"0\" w:type=\"auto\"/>"
            f"{fill}</w:tcPr>{p(str(value), bold=header, size=30, after=40)}</w:tc>"
        )

    header_row = "<w:tr>" + "".join(cell(h, True) for h in headers) + "</w:tr>"
    body = "".join("<w:tr>" + "".join(cell(v) for v in row) + "</w:tr>" for row in rows)
    return (
        "<w:tbl><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\"/>"
        "<w:tblBorders>"
        "<w:top w:val=\"single\" w:sz=\"6\"/><w:left w:val=\"single\" w:sz=\"6\"/>"
        "<w:bottom w:val=\"single\" w:sz=\"6\"/><w:right w:val=\"single\" w:sz=\"6\"/>"
        "<w:insideH w:val=\"single\" w:sz=\"4\"/><w:insideV w:val=\"single\" w:sz=\"4\"/>"
        "</w:tblBorders></w:tblPr>"
        f"{header_row}{body}</w:tbl>"
    )


def cover() -> str:
    return (
        p("รายงาน", bold=True, size=54, align="center", before=1200, after=240)
        + p("One Man Vekery เว็บไซต์ร้านเบเกอรี่ออนไลน์", bold=True, size=42, align="center", after=700)
        + p("จัดทำโดย", bold=True, align="center")
        + p("ผู้จัดทำโครงงาน OneManVekery", align="center", after=500)
        + p("เสนอ", bold=True, align="center")
        + p("อาจารย์ผู้สอนรายวิชา", align="center", after=500)
        + p("รายงานนี้เป็นส่วนหนึ่งของรายวิชา", align="center")
        + p("การเชื่อมต่อโปรแกรมส่วนหน้ากับโปรแกรมส่วนหลัง (CSI402)", align="center")
        + p("ภาคเรียนที่ 2 ปีการศึกษา 2568", align="center")
        + p("คณะเทคโนโลยีสารสนเทศ สาขาวิทยาการคอมพิวเตอร์และนวัตกรรมการพัฒนาซอฟต์แวร์", align="center")
        + p("มหาวิทยาลัยศรีปทุม บางเขน", align="center")
        + page_break()
    )


def requirements() -> str:
    rows = [
        ["Guest", "ดูหน้าแรก ร้านสินค้า เกี่ยวกับเรา ติดต่อเรา สมัครสมาชิก และเข้าสู่ระบบ"],
        ["Customer/User", "จัดการโปรไฟล์และที่อยู่ เพิ่มสินค้าเข้าตะกร้า ใช้โปรโมชัน แลกคะแนน สั่งซื้อ และติดตามคำสั่งซื้อ"],
        ["Staff", "เข้าหลังบ้านเพื่อดูงานที่เกี่ยวกับคำสั่งซื้อและข้อมูลที่ได้รับสิทธิ์"],
        ["Admin", "จัดการสินค้า หมวดสินค้า สต๊อก โปรโมชั่น คำสั่งซื้อ และบัญชีผู้ใช้"],
        ["Owner", "ดูภาพรวมธุรกิจ Dashboard ยอดขาย สินค้าขายดี และจัดการบัญชีระดับสูง"],
    ]
    return (
        h1("Requirement Specification")
        + h2("1. ภาพรวมของระบบ")
        + p("One Man Vekery เป็นเว็บแอปพลิเคชันร้านเบเกอรี่ออนไลน์ที่รวมหน้าร้านและระบบหลังบ้านไว้ในระบบเดียว ลูกค้าสามารถเลือกดูสินค้าเบเกอรี่ตามหมวดหมู่ เพิ่มสินค้าเข้าตะกร้า ใช้โปรโมชันหรือคะแนนสะสม และสั่งซื้อผ่านเว็บไซต์ ส่วนผู้ดูแลระบบสามารถจัดการสินค้า สต๊อก คำสั่งซื้อ โค้ดส่วนลด บัญชีผู้ใช้ และดู Dashboard สำหรับติดตามภาพรวมของร้านได้")
        + p("ระบบถูกพัฒนาด้วย ASP.NET Core MVC ใช้ Razor Views เป็นส่วนติดต่อผู้ใช้ ใช้ Entity Framework Core เชื่อมต่อฐานข้อมูล SQL Server และใช้ Session สำหรับเก็บสถานะผู้ใช้ ตะกร้าสินค้า โปรโมชัน และข้อมูลระหว่างขั้นตอนสมัครสมาชิก")
        + h2("2. วัตถุประสงค์ของระบบ")
        + bullet("ให้ลูกค้าซื้อสินค้าเบเกอรี่ผ่านเว็บไซต์ได้สะดวกและครบขั้นตอน")
        + bullet("ลดงาน manual ของร้านในการจัดการสินค้า สต๊อก และคำสั่งซื้อ")
        + bullet("รองรับการใช้โปรโมชัน โค้ดส่วนลด และคะแนนสะสมในขั้นตอน checkout")
        + bullet("แบ่งสิทธิ์ผู้ใช้ระหว่างลูกค้า พนักงาน ผู้ดูแลระบบ และเจ้าของร้านให้ชัดเจน")
        + bullet("เก็บข้อมูลคำสั่งซื้อและลูกค้าเพื่อนำไปใช้วิเคราะห์ยอดขายและวางแผนร้าน")
        + h2("3. บทบาทผู้ใช้และสิทธิ์การใช้งาน")
        + table(["บทบาท", "สิทธิ์หลัก"], rows)
        + h2("4. ขอบเขตของระบบ")
        + bullet("ระบบสมัครสมาชิก เข้าสู่ระบบ โปรไฟล์ และที่อยู่จัดส่ง")
        + bullet("ระบบแสดงสินค้า หมวดสินค้า สินค้าขายดี และสินค้าเข้าใหม่")
        + bullet("ระบบตะกร้าสินค้า คำนวณราคา ค่าส่ง ส่วนลด คะแนน และ checkout")
        + bullet("ระบบสร้างคำสั่งซื้อ ตัดสต๊อก และติดตามสถานะคำสั่งซื้อ")
        + bullet("ระบบจัดการสินค้า หมวดสินค้า รูปภาพ และการเปิด/ปิดการแสดงผลหน้าร้าน")
        + bullet("ระบบจัดการโปรโมชั่น โค้ดส่วนลด และกติกาสิทธิประโยชน์")
        + bullet("ระบบ Dashboard หลังบ้านสำหรับยอดขาย รายการล่าสุด และสินค้าขายดี")
        + page_break()
    )


def flowcharts() -> str:
    return (
        h1("1. ผังงาน (Flowchart) One Man Vekery")
        + h2("1.1 Flowchart ภาพรวมระบบ")
        + p("เมื่อผู้ใช้เข้าสู่เว็บไซต์ ระบบจะเริ่มจากหน้า Home ซึ่งแสดง hero, หมวดสินค้า, สินค้าขายดี และสินค้าเข้าใหม่ หากยังไม่ได้เข้าสู่ระบบ ผู้ใช้ยังสามารถดูสินค้าและข้อมูลร้านได้ แต่เมื่อจะ checkout หรือจัดการข้อมูลส่วนตัว ระบบจะพาไปหน้า Login ก่อน")
        + p("หลังเข้าสู่ระบบ ระบบอ่าน role จาก session แล้วแยกเส้นทางใช้งาน หากเป็น user จะกลับไปฝั่งหน้าร้านเพื่อซื้อสินค้าและติดตามคำสั่งซื้อ หากเป็น staff, admin หรือ owner จะเข้าสู่ Admin Workspace ที่ควบคุมด้วย AdminPortalAuth และ OnActionExecuting guard")
        + h2("1.2 Flowchart กระบวนการสมัครสมาชิกและเข้าสู่ระบบ")
        + p("การสมัครสมาชิกแบ่งเป็น 2 ขั้นตอน เริ่มจากกรอกข้อมูลบัญชี จากนั้นระบบเก็บ pending registration ไว้ใน session แล้วให้กรอกที่อยู่เริ่มต้น เมื่อข้อมูลครบจึงสร้าง User และ UserAddress ภายใน transaction เดียวกัน เพื่อไม่ให้มีบัญชีที่สมัครค้างโดยไม่มีที่อยู่")
        + p("การเข้าสู่ระบบเริ่มจากตรวจ ModelState, ตรวจ email/password, ตรวจสถานะบัญชี แล้วบันทึก session keys ได้แก่ account id, name, role key และ role label จากนั้น redirect ตามสิทธิ์ผู้ใช้")
        + h2("1.3 Flowchart กระบวนการฝั่ง Customer")
        + p("ลูกค้าเริ่มจากเลือกสินค้าในหน้า Shop หรือหน้า Home ระบบอ่านข้อมูล Product และ Category แล้วแสดงการ์ดสินค้า เมื่อลูกค้ากดเพิ่มสินค้า ระบบตรวจ stock และบันทึก cart ลง session หากสินค้าเดิมมีอยู่แล้วจะรวมจำนวนแทนการสร้างรายการซ้ำ")
        + p("เมื่อ checkout ระบบตรวจ login, อ่านตะกร้า, เติมข้อมูลที่อยู่เริ่มต้น, คำนวณ subtotal, delivery fee, promo discount, shipping discount และ point discount จากนั้นตรวจ stock อีกครั้งก่อนสร้าง Order, OrderItems, OrderPromotions และปรับ stock สินค้าลง")
        + h2("1.4 Flowchart กระบวนการฝั่งระบบหลังบ้าน")
        + p("ระบบหลังบ้านเริ่มจาก AdminController ตรวจ role ทุก request ผู้ใช้ที่ผ่านสิทธิ์จะเข้า Dashboard, Orders, Items, Products, Codes, Accounts, Staff หรือ Profile ได้ตามขอบเขตที่ระบบกำหนด หน้าหลังบ้านใช้ ViewModel เฉพาะหน้าและมี JavaScript ช่วยจัดการ modal, combobox, order line และ AJAX บางส่วน")
        + p("ตัวอย่าง flow สำคัญคือการสร้าง order จากหลังบ้าน ระบบ validate ลูกค้า สินค้า และ stock ก่อนเปิด transaction เพื่อสร้าง order ลด stock และบันทึกข้อมูลทั้งหมดพร้อมกัน ส่วนการจัดการสินค้าแยกระหว่าง Items สำหรับ stock/category/image และ Products สำหรับ publish/hide หน้าร้าน")
    )


def dfd() -> str:
    return (
        h1("2. แผนภาพการไหลของข้อมูล (Data Flow Diagram)")
        + h2("2.1 ระบบ Login/Register")
        + p("Guest ส่งข้อมูล email/password หรือข้อมูลสมัครสมาชิกเข้าสู่ AccountController ระบบตรวจข้อมูลกับตาราง users และ roles ถ้าสำเร็จจะบันทึก session เพื่อใช้ระบุตัวตนในการเรียกหน้าอื่น ๆ ส่วนการสมัครสมาชิกจะสร้างข้อมูล users และ user_addresses พร้อมกัน")
        + h2("2.2 ระบบ Catalog และ Cart")
        + p("Customer ค้นหาหรือเลือกดูสินค้า ระบบอ่าน products และ categories เพื่อแสดงรายการสินค้า เมื่อลูกค้าเพิ่มสินค้าลงตะกร้า ระบบบันทึก cart state ลง session และใช้ข้อมูล products เพื่อตรวจ stock, ราคา และสถานะ active")
        + h2("2.3 ระบบ Checkout และ Order")
        + p("Checkout รับข้อมูลตะกร้า ที่อยู่ วิธีชำระเงิน โค้ดส่วนลด และคะแนนที่ต้องการใช้ จากนั้นเชื่อมข้อมูล promotions, promo_codes, loyalty_wallets และ products เพื่อคำนวณยอดสุทธิ เมื่อยืนยันแล้วจะบันทึก orders, order_items, order_promotions และ loyalty_points_ledger")
        + h2("2.4 ระบบ Admin: Dashboard, Orders, Items, Codes, Accounts")
        + p("หลังบ้านอ่านข้อมูลจาก orders, order_items, products, categories, promo_codes, promotions และ users เพื่อแสดง Dashboard และรายการจัดการ เมื่อมีการเพิ่มหรือแก้ไขข้อมูล ระบบจะ validate ก่อนบันทึกกลับฐานข้อมูลผ่าน Entity Framework Core")
    )


def data_dictionary() -> str:
    def dd_table(title: str, description: str, rows: list[list[str]]) -> str:
        return h2(title) + p(description) + table(["Field", "Type", "Key", "Description"], rows)

    body = (
        h1("3. Data Dictionary")
        + p("ฐานข้อมูลหลักของระบบ One Man Vekery ถูก map ผ่าน Entity Framework Core ใน OneManVekeryDBContext โดยใช้ SQL Server เป็นฐานข้อมูลหลัก ตารางสำคัญมีดังนี้")
    )
    body += dd_table(
        "ตาราง roles",
        "ใช้สำหรับจัดเก็บข้อมูลสิทธิ์การใช้งานของผู้ใช้ในระบบ โดยระบบแบ่งสิทธิ์ออกเป็นหลายระดับ เช่น User, Staff, Admin และ Owner เพื่อใช้ควบคุมการเข้าถึงเมนูและฟังก์ชันต่าง ๆ ภายในระบบ",
        [["Id", "Int", "PK", "รหัส role"], ["role_key", "String", "Unique", "ค่าระบุสิทธิ์"], ["role_name", "String", "", "ชื่อสิทธิ์ที่แสดงผล"], ["created_at", "Datetime", "", "วันที่สร้างข้อมูล"]],
    )
    body += dd_table(
        "ตาราง users",
        "ใช้สำหรับจัดเก็บข้อมูลบัญชีผู้ใช้งานทั้งหมดในระบบ ทั้งลูกค้าทั่วไป พนักงาน ผู้ดูแลระบบ และเจ้าของร้าน ข้อมูลในตารางนี้ถูกใช้สำหรับการเข้าสู่ระบบ การแสดงข้อมูลโปรไฟล์ การตรวจสอบสิทธิ์ตาม Role และการเชื่อมโยงกับข้อมูลคำสั่งซื้อของลูกค้า",
        [["Id", "Int", "PK", "รหัสผู้ใช้"], ["full_name", "String", "", "ชื่อ-นามสกุล"], ["email", "String", "Unique", "อีเมลสำหรับเข้าสู่ระบบ"], ["password_hash", "String", "", "รหัสผ่านที่จัดเก็บในระบบ"], ["phone", "String", "", "เบอร์โทรศัพท์"], ["role_id", "Int", "FK", "อ้างอิงสิทธิ์จากตาราง roles"], ["status", "String", "", "สถานะบัญชี เช่น Active, Closed"], ["notes", "String", "", "หมายเหตุของบัญชี"], ["created_at", "Datetime", "", "วันที่สมัครหรือสร้างบัญชี"], ["last_active_at", "Datetime", "", "วันที่ใช้งานล่าสุด"]],
    )
    body += dd_table(
        "ตาราง user_addresses",
        "ใช้สำหรับจัดเก็บข้อมูลที่อยู่จัดส่งของผู้ใช้แต่ละคน ผู้ใช้หนึ่งคนสามารถมีที่อยู่ได้มากกว่าหนึ่งรายการ เช่น ที่อยู่บ้านหรือที่ทำงาน โดยระบบสามารถกำหนดที่อยู่หลักเพื่อนำไปใช้ในการกรอกข้อมูลจัดส่งตอนสั่งซื้อสินค้าได้",
        [["Id", "Int", "PK", "รหัสที่อยู่"], ["user_id", "Int", "FK", "อ้างอิงผู้ใช้จาก users"], ["recipient_name", "String", "", "ชื่อผู้รับ"], ["phone", "String", "", "เบอร์โทรผู้รับ"], ["address_line", "String", "", "รายละเอียดที่อยู่"], ["postal_code", "String", "", "รหัสไปรษณีย์"], ["label", "String", "", "ชื่อที่อยู่ เช่น บ้าน, ที่ทำงาน"], ["is_default", "Bool", "", "กำหนดว่าเป็นที่อยู่หลักหรือไม่"], ["created_at", "Datetime", "", "วันที่สร้างข้อมูล"], ["updated_at", "Datetime", "", "วันที่แก้ไขล่าสุด"]],
    )
    body += dd_table(
        "ตาราง categories",
        "ใช้สำหรับจัดเก็บหมวดหมู่ของสินค้าเบเกอรี่ เช่น เค้ก ขนมปัง คุกกี้ หรือเครื่องดื่ม เพื่อช่วยให้การจัดกลุ่มสินค้าและการค้นหาสินค้าทำได้ง่ายขึ้น รวมถึงช่วยให้ผู้ดูแลระบบสามารถจัดการสินค้าตามประเภทได้อย่างเป็นระบบ",
        [["Id", "Int", "PK", "รหัสหมวดหมู่"], ["name", "String", "Unique", "ชื่อหมวดหมู่สินค้า"]],
    )
    body += dd_table(
        "ตาราง products",
        "ใช้สำหรับจัดเก็บข้อมูลสินค้าเบเกอรี่ทั้งหมดในร้าน เช่น ชื่อสินค้า รายละเอียด ราคา จำนวนคงเหลือ รูปภาพ และสถานะการเปิดขาย ข้อมูลในตารางนี้ถูกใช้ทั้งในหน้ารายการสินค้า ตะกร้าสินค้า การสั่งซื้อ และหน้าจัดการสินค้าของผู้ดูแลระบบ",
        [["Id", "Int", "PK", "รหัสสินค้า"], ["category_id", "Int", "FK", "อ้างอิงหมวดหมู่จาก categories"], ["sku", "String", "Unique", "รหัสสินค้า"], ["name", "String", "", "ชื่อสินค้า"], ["description", "String", "", "รายละเอียดสินค้า"], ["price", "Decimal", "", "ราคาสินค้า"], ["stock_qty", "Int", "", "จำนวนสินค้าคงเหลือ"], ["image_url", "String", "", "ที่อยู่รูปภาพสินค้า"], ["is_active", "Bool", "", "สถานะเปิด/ปิดการขาย"], ["created_at", "Datetime", "", "วันที่เพิ่มสินค้า"]],
    )
    body += dd_table(
        "ตาราง contact_messages",
        "ใช้สำหรับจัดเก็บข้อความติดต่อจากลูกค้าหรือผู้ใช้งานเว็บไซต์ เช่น ชื่อผู้ติดต่อ อีเมล เบอร์โทร หัวข้อ และรายละเอียดข้อความ ข้อมูลนี้ถูกใช้ในส่วนหลังบ้านเพื่อให้ Staff หรือผู้ดูแลระบบสามารถตรวจสอบและติดตามการติดต่อจากลูกค้าได้",
        [["Id", "Int", "PK", "รหัสข้อความ"], ["name", "String", "", "ชื่อผู้ติดต่อ"], ["email", "String", "", "อีเมลผู้ติดต่อ"], ["phone", "String", "", "เบอร์โทร"], ["subject", "String", "", "หัวข้อข้อความ"], ["message", "String", "", "รายละเอียดข้อความ"], ["status", "String", "", "สถานะการจัดการข้อความ"], ["created_at", "Datetime", "", "วันที่ส่งข้อความ"]],
    )
    body += dd_table(
        "ตาราง orders",
        "ใช้สำหรับจัดเก็บข้อมูลคำสั่งซื้อหลักของลูกค้า เช่น เลขที่คำสั่งซื้อ ข้อมูลผู้สั่งซื้อ ที่อยู่จัดส่ง วิธีชำระเงิน สถานะการชำระเงิน สถานะคำสั่งซื้อ ยอดรวม ส่วนลด และวันที่สั่งซื้อ ตารางนี้เป็นข้อมูลหลักที่ใช้ติดตามสถานะออเดอร์และใช้ในส่วนหลังบ้านสำหรับจัดการคำสั่งซื้อ",
        [["Id", "Int", "PK", "รหัสคำสั่งซื้อ"], ["order_no", "String", "Unique", "เลขที่คำสั่งซื้อ"], ["user_id", "Int", "FK", "อ้างอิงผู้สั่งซื้อจาก users"], ["customer_name", "String", "", "ชื่อลูกค้า"], ["phone", "String", "", "เบอร์โทรลูกค้า"], ["address", "String", "", "ที่อยู่จัดส่ง"], ["payment_method", "String", "", "วิธีชำระเงิน"], ["payment_status", "String", "", "สถานะการชำระเงิน"], ["order_status", "String", "", "สถานะคำสั่งซื้อ"], ["subtotal", "Decimal", "", "ยอดรวมก่อนส่วนลดและค่าส่ง"], ["delivery_fee", "Decimal", "", "ค่าส่ง"], ["discount_amount", "Decimal", "", "ส่วนลดสินค้า"], ["shipping_discount_amount", "Decimal", "", "ส่วนลดค่าส่ง"], ["total_amount", "Decimal", "", "ยอดรวมสุทธิ"], ["promo_code_id", "Int", "FK", "อ้างอิงโค้ดส่วนลดจาก promo_codes"], ["discount_code", "String", "", "โค้ดส่วนลดที่ใช้"], ["points_earned", "Int", "", "แต้มที่ได้รับ"], ["points_redeemed", "Int", "", "แต้มที่ใช้แลก"], ["note", "String", "", "หมายเหตุ"], ["created_at", "Datetime", "", "วันที่สร้างคำสั่งซื้อ"]],
    )
    body += dd_table(
        "ตาราง order_items",
        "ใช้สำหรับจัดเก็บรายละเอียดสินค้าที่อยู่ในคำสั่งซื้อแต่ละรายการ โดยหนึ่งคำสั่งซื้อสามารถมีสินค้าได้หลายรายการ ตารางนี้จะเก็บชื่อสินค้า ราคา จำนวน และราคารวมของแต่ละรายการ เพื่อใช้แสดงรายละเอียดคำสั่งซื้อและคำนวณยอดรวมของออเดอร์",
        [["Id", "Int", "PK", "รหัสรายการสินค้าในคำสั่งซื้อ"], ["order_id", "Int", "FK", "อ้างอิงคำสั่งซื้อจาก orders"], ["product_id", "Int", "FK", "อ้างอิงสินค้าจาก products"], ["product_name", "String", "", "ชื่อสินค้า ณ ตอนสั่งซื้อ"], ["price", "Decimal", "", "ราคาสินค้าต่อชิ้น"], ["qty", "Int", "", "จำนวนสินค้า"], ["line_total", "Decimal", "", "ราคารวมของรายการนั้น"]],
    )
    body += dd_table(
        "ตาราง promo_codes",
        "ใช้สำหรับจัดเก็บข้อมูลโค้ดส่วนลดหรือโปรโมชันที่ลูกค้าสามารถนำไปใช้ในขั้นตอนการสั่งซื้อได้ เช่น ประเภทส่วนลด มูลค่าส่วนลด ยอดขั้นต่ำ วันเริ่มต้น วันหมดอายุ และจำนวนครั้งที่สามารถใช้ได้ ตารางนี้ช่วยให้ระบบสามารถตรวจสอบและคำนวณส่วนลดได้อย่างถูกต้อง",
        [["Id", "Int", "PK", "รหัสโค้ดส่วนลด"], ["code", "String", "Unique", "รหัสโค้ด เช่น WELCOME10"], ["title", "String", "", "ชื่อโปรโมชัน"], ["description", "String", "", "รายละเอียดโปรโมชัน"], ["discount_type", "String", "", "ประเภทส่วนลด เช่น percent, amount"], ["discount_value", "Decimal", "", "มูลค่าส่วนลด"], ["min_order_amount", "Decimal", "", "ยอดขั้นต่ำที่ใช้โค้ดได้"], ["max_discount_amount", "Decimal", "", "ส่วนลดสูงสุด"], ["usage_limit", "Int", "", "จำนวนครั้งที่ใช้ได้ทั้งหมด"], ["used_count", "Int", "", "จำนวนครั้งที่ถูกใช้แล้ว"], ["starts_at", "Datetime", "", "วันที่เริ่มใช้"], ["expires_at", "Datetime", "", "วันหมดอายุ"], ["status", "String", "", "สถานะโค้ด เช่น Active, Disabled"], ["created_at", "Datetime", "", "วันที่สร้างโค้ด"]],
    )
    body += h2("ตารางเพิ่มเติมของระบบ Promotion และ Loyalty")
    body += table(
        ["ตาราง", "คอลัมน์สำคัญ", "หน้าที่"],
        [
            ["promotions", "promotion_key, title, campaign_type, rule_type, benefit_type, target_scope, starts_at, expires_at, status", "เก็บกติกาโปรโมชันแบบ auto apply และแบบใช้ร่วมกับโค้ด"],
            ["promotion_targets", "promotion_id, target_type, product_id, category_id", "ระบุสินค้า/หมวดสินค้าที่โปรโมชันมีผล"],
            ["order_promotions", "order_id, promotion_id, promo_code_id, benefit_type, discount_amount, shipping_discount_amount", "บันทึกโปรโมชันที่ถูกใช้จริงในคำสั่งซื้อ"],
            ["loyalty_wallets", "user_id, current_points, lifetime_earned, lifetime_redeemed, updated_at", "เก็บคะแนนสะสมปัจจุบันของลูกค้า"],
            ["loyalty_points_ledger", "user_id, order_id, entry_type, points_delta, balance_after", "เก็บประวัติการเพิ่มและใช้คะแนน"],
        ],
    )
    return body + page_break()


def ui_and_tech() -> str:
    return (
        h1("4. รายละเอียดหน้าจอและการทำงาน")
        + h2("4.1 Frontend Storefront")
        + bullet("Home แสดง hero, สินค้าเข้าใหม่, หมวดสินค้า และสินค้าขายดี 8 รายการ")
        + bullet("Shop แสดง catalog พร้อม search/filter จาก data-product-card")
        + bullet("Cart แสดงรายการสินค้าใน session และยอดรวมก่อน checkout")
        + bullet("OrderStatus และ MyOrders ใช้แสดงผลคำสั่งซื้อและสถานะของลูกค้า")
        + bullet("About และ Contact ใช้แสดงข้อมูลร้านและรับข้อความจากลูกค้า")
        + h2("4.2 Account และ Profile")
        + bullet("Login ตรวจบัญชีและ redirect ตาม role")
        + bullet("Register แบ่งเป็นข้อมูลบัญชีและที่อยู่เริ่มต้น")
        + bullet("Profile ให้ลูกค้าแก้ไขข้อมูลส่วนตัว ที่อยู่ และดูคะแนนสะสม")
        + h2("4.3 Admin Workspace")
        + bullet("Dashboard สรุป metrics, revenue trend, fulfillment summary, top products และ latest orders")
        + bullet("Orders จัดการรายการคำสั่งซื้อ เพิ่ม order จากหลังบ้าน และปรับสถานะ order/payment")
        + bullet("Items จัดการสินค้า หมวดสินค้า รูปภาพ และ stock")
        + bullet("Products ควบคุมการ publish/hide สินค้าบนหน้าร้าน")
        + bullet("Codes จัดการ promo code และสถานะของโค้ด")
        + bullet("Accounts และ Staff ใช้จัดการบัญชีและรายชื่อพนักงานตามสิทธิ์")
        + h2("5. เทคโนโลยีที่ใช้")
        + table(
            ["ส่วน", "เทคโนโลยี"],
            [
                ["Backend", "ASP.NET Core MVC, C# partial controllers"],
                ["Frontend", "Razor Views, HTML, CSS, JavaScript"],
                ["Database", "SQL Server ผ่าน Entity Framework Core"],
                ["State", "ASP.NET Core Session และ Distributed Memory Cache"],
                ["Architecture", "Controller + ViewModel + Razor View + DbContext"],
            ],
        )
        + h2("6. สรุป")
        + p("One Man Vekery เป็นระบบร้านเบเกอรี่ออนไลน์ที่ครอบคลุมตั้งแต่หน้าร้านจนถึงหลังบ้าน จุดเด่นของระบบคือการเชื่อม flow ซื้อสินค้ากับข้อมูลจริงในฐานข้อมูล การแยก controller เป็น partial files เพื่อให้อ่านและดูแลรักษาง่าย การใช้ ViewModel สำหรับส่งข้อมูลไปยัง Razor views และการควบคุมสิทธิ์ผู้ใช้ตาม role เพื่อให้ระบบใช้งานได้เป็นระเบียบและรองรับการขยายในอนาคต")
    )


def diagram_sections(image_rels: list[tuple[str, Path]]) -> str:
    if not image_rels:
        return ""
    labels = {
        "DFD LV0.png": "DFD Level 0: ภาพรวมระบบ One Man Vekery",
        "DFD LV1.png": "DFD Level 1: รายละเอียดการไหลของข้อมูลในระบบ",
        "Login Register User.png": "Flowchart: กระบวนการ Login / Register User",
        "Add Order User.png": "Flowchart: กระบวนการสั่งซื้อของผู้ใช้",
        "Create Item.png": "Flowchart: กระบวนการเพิ่มสินค้า",
        "UpdateHideAndPubliuc.png": "Flowchart: กระบวนการ Update / Hide / Publish สินค้า",
        "AdminAndStaff_Manage_Order.png": "Flowchart: Admin และ Staff จัดการคำสั่งซื้อ",
        "Admin Manage Account.png": "Flowchart: Admin จัดการบัญชีผู้ใช้",
        "Staff Manage Account.png": "Flowchart: Staff จัดการบัญชี",
    }
    body = h1("7. ภาคผนวก: แผนภาพประกอบ")
    for index, (rid, path) in enumerate(image_rels, start=1):
        body += image_block(rid, labels.get(path.name, path.stem), path, 100 + index)
    return body


def document_xml(image_rels: list[tuple[str, Path]]) -> str:
    body = (
        cover()
        + requirements()
        + flowcharts()
        + page_break()
        + dfd()
        + page_break()
        + data_dictionary()
        + ui_and_tech()
        + page_break()
        + diagram_sections(image_rels)
    )
    sect = (
        "<w:sectPr><w:pgSz w:w=\"11906\" w:h=\"16838\"/>"
        "<w:pgMar w:top=\"1134\" w:right=\"1134\" w:bottom=\"1134\" w:left=\"1134\" "
        "w:header=\"708\" w:footer=\"708\" w:gutter=\"0\"/></w:sectPr>"
    )
    return f'<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:document {NS}><w:body>{body}{sect}</w:body></w:document>'


def styles_xml() -> str:
    return (
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">'
        '<w:docDefaults><w:rPrDefault><w:rPr>'
        '<w:rFonts w:ascii="TH Sarabun New" w:hAnsi="TH Sarabun New" w:eastAsia="TH Sarabun New" w:cs="TH Sarabun New"/>'
        '<w:sz w:val="32"/><w:szCs w:val="32"/>'
        '</w:rPr></w:rPrDefault></w:docDefaults>'
        '</w:styles>'
    )


def relationships_xml(image_rels: list[tuple[str, Path]]) -> str:
    rels = [
        '<Relationship Id="rIdStyles" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>'
    ]
    for index, (rid, path) in enumerate(image_rels, start=1):
        rels.append(
            f'<Relationship Id="{rid}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="media/report-image{index}.png"/>'
        )
    return (
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'
        + "".join(rels)
        + "</Relationships>"
    )


def content_types_xml(image_rels: list[tuple[str, Path]]) -> str:
    return (
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">'
        '<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>'
        '<Default Extension="xml" ContentType="application/xml"/>'
        '<Default Extension="png" ContentType="image/png"/>'
        '<Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>'
        '<Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>'
        '</Types>'
    )


def package_rels_xml() -> str:
    return (
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'
        '<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>'
        '</Relationships>'
    )


def main() -> None:
    desired = [
        "DFD LV0.png",
        "DFD LV1.png",
        "Login Register User.png",
        "Add Order User.png",
        "Create Item.png",
        "UpdateHideAndPubliuc.png",
        "AdminAndStaff_Manage_Order.png",
        "Admin Manage Account.png",
        "Staff Manage Account.png",
    ]
    image_rels: list[tuple[str, Path]] = []
    for name in desired:
        path = DOCS / "diagrams" / name
        if path.exists():
            image_rels.append((f"rIdImage{len(image_rels) + 1}", path))

    with zipfile.ZipFile(OUT, "w", compression=zipfile.ZIP_DEFLATED) as docx:
        docx.writestr("[Content_Types].xml", content_types_xml(image_rels))
        docx.writestr("_rels/.rels", package_rels_xml())
        docx.writestr("word/document.xml", document_xml(image_rels))
        docx.writestr("word/styles.xml", styles_xml())
        docx.writestr("word/_rels/document.xml.rels", relationships_xml(image_rels))
        for index, (_, path) in enumerate(image_rels, start=1):
            docx.write(path, f"word/media/report-image{index}.png")

    print(f"created {OUT.relative_to(ROOT)} at {dt.datetime.now():%Y-%m-%d %H:%M:%S}")


if __name__ == "__main__":
    main()
