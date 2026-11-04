"""Render the generated standalone HTML with deterministic, embedded-font A4 output."""
from pathlib import Path
from html.parser import HTMLParser
from html import escape
from io import BytesIO
import base64
import calendar
import hashlib
import json
import os
import re
import sys

from reportlab import Version as REPORTLAB_VERSION
from reportlab.lib.colors import HexColor
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.utils import ImageReader
from reportlab.lib.enums import TA_LEFT
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfgen.canvas import Canvas
from reportlab.platypus import SimpleDocTemplate, Paragraph, PageBreak
from pypdf import PdfReader

DIRECTORY = Path(__file__).resolve().parent
STEM = "regulamin-korzystania-z-gf-dla-pracownikow-pl"
MM = 72 / 25.4


def font_pair():
    supplied = os.environ.get("GF_PDF_FONT_DIR")
    candidates = [Path(supplied)] if supplied else [
        Path(os.environ.get("WINDIR", "C:/Windows")) / "Fonts",
        Path("/usr/share/fonts/truetype/dejavu"),
        Path("/usr/share/fonts/dejavu-sans-fonts"),
    ]
    for directory in candidates:
        for regular, bold in [("arial.ttf", "arialbd.ttf"), ("DejaVuSans.ttf", "DejaVuSans-Bold.ttf")]:
            if (directory / regular).exists() and (directory / bold).exists():
                return directory / regular, directory / bold
    raise RuntimeError("Set GF_PDF_FONT_DIR to licensed Arial or DejaVu Sans TTF fonts; no font binaries are distributed.")


class DocumentParser(HTMLParser):
    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.pages = []
        self.in_article = False
        self.current = None
        self.logo = None
        self.number_span = False
        self.links = []

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if tag == "article":
            self.in_article = True
            self.pages.append([])
        if tag == "img" and self.logo is None and attrs.get("src", "").startswith("data:image/png;base64,"):
            self.logo = base64.b64decode(attrs["src"].split(",", 1)[1])
        if not self.in_article:
            return
        if tag in {"h1", "h2", "p"}:
            self.current = {"tag": tag, "class": attrs.get("class", ""), "markup": ""}
        elif self.current:
            if tag == "strong":
                self.current["markup"] += "<b>"
            elif tag == "a":
                # A production origin cannot be inferred. Print known routes until one is confirmed.
                route = attrs.get("data-route")
                href = attrs["href"]
                self.links.append((href, route))
                if href.startswith("https://"):
                    self.current["markup"] += f'<a href="{escape(href, quote=True)}" color="#60519a">'
            elif tag == "span" and attrs.get("class") == "number":
                self.number_span = True

    def handle_endtag(self, tag):
        if tag == "article":
            self.in_article = False
        if not self.current:
            return
        if tag == "strong":
            self.current["markup"] += "</b>"
        elif tag == "a":
            href, route = self.links.pop()
            if href.startswith("https://"):
                self.current["markup"] += "</a>"
            elif route:
                self.current["markup"] += f" ({escape(route)})"
        elif tag == "span" and self.number_span:
            self.current["markup"] += " "
            self.number_span = False
        elif tag == self.current["tag"]:
            self.pages[-1].append(self.current)
            self.current = None

    def handle_data(self, value):
        if self.current:
            self.current["markup"] += escape(value)


def build():
    html = (DIRECTORY / f"{STEM}.html").read_text(encoding="utf-8")
    source = (DIRECTORY / f"{STEM}.md").read_text(encoding="utf-8")
    title = re.search(r"^# (.+)$", source, re.M)[1]
    version = re.search(r"^Wersja: (.+)$", source, re.M)[1]
    status = re.search(r"^Status: (.+)$", source, re.M)[1]
    parser = DocumentParser()
    parser.feed(html)
    if len(parser.pages) != 6 or not parser.logo:
        raise ValueError("Expected six complete page groups and the existing GF logo.")
    regular, bold = font_pair()
    pdfmetrics.registerFont(TTFont("GFBody", str(regular)))
    pdfmetrics.registerFont(TTFont("GFBold", str(bold)))
    pdfmetrics.registerFontFamily("GFBody", normal="GFBody", bold="GFBold", italic="GFBody", boldItalic="GFBold")
    for name in ("GFBody", "GFBold"):
        missing = [char for char in "ąćęłńóśźżĄĆĘŁŃÓŚŹŻ§—" if ord(char) not in pdfmetrics.getFont(name).face.charToGlyph]
        if missing:
            raise ValueError(f"Missing Polish glyphs in {name}: {missing}")
    ink = HexColor("#181a20")
    muted = HexColor("#646775")
    accent = HexColor("#60519a")
    styles = {
        "p": ParagraphStyle("Body", fontName="GFBody", fontSize=10.35, leading=13.4,
                            textColor=ink, alignment=TA_LEFT, spaceAfter=4.3,
                            allowWidows=0, allowOrphans=0, splitLongWords=False),
        "h1": ParagraphStyle("Title", fontName="GFBold", fontSize=21, leading=24,
                             textColor=ink, spaceBefore=5, spaceAfter=10, keepWithNext=True),
        "h2": ParagraphStyle("Section", fontName="GFBold", fontSize=12.4, leading=16,
                             textColor=ink, spaceBefore=10, spaceAfter=6, keepWithNext=True),
        "meta": ParagraphStyle("Metadata", fontName="GFBody", fontSize=8, leading=11.5,
                               textColor=muted, spaceAfter=2, keepWithNext=True),
        "review-note": ParagraphStyle("Review", fontName="GFBody", fontSize=8.1, leading=11,
                                      textColor=muted, spaceBefore=8, spaceAfter=8,
                                      borderColor=accent, borderWidth=0.4, borderPadding=7),
    }
    styles["clause"] = ParagraphStyle("Clause", parent=styles["p"], leftIndent=13, firstLineIndent=-13)
    logo = ImageReader(BytesIO(parser.logo))
    output = BytesIO()
    doc = SimpleDocTemplate(output, pagesize=A4, leftMargin=21 * MM, rightMargin=21 * MM,
                            topMargin=24 * MM, bottomMargin=20.5 * MM,
                            title=title, author="GF — projekt do przeglądu",
                            subject=f"Regulamin dla pracowników; {version}; {status}",
                            creator="GF employee-regulations/generate-pdf.mjs + ReportLab",
                            invariant=1, pageCompression=1)

    def page_frame(canvas, current_doc):
        canvas.saveState()
        width, height = A4
        left, right = 21 * MM, width - 21 * MM
        canvas.drawImage(logo, left, height - 17 * MM, width=8 * MM, height=8 * MM, mask="auto")
        canvas.setFont("GFBold", 9)
        canvas.setFillColor(ink)
        canvas.drawString(left + 11 * MM, height - 13.4 * MM, "GF")
        canvas.setFont("GFBody", 8)
        canvas.setFillColor(muted)
        canvas.drawString(left + 21 * MM, height - 13.4 * MM, "Regulamin dla pracowników")
        canvas.drawRightString(right, height - 13.4 * MM, f"{version} · PL")
        canvas.setStrokeColor(HexColor("#e1e2e7"))
        canvas.setLineWidth(0.5)
        canvas.line(left, height - 19 * MM, right, height - 19 * MM)
        canvas.line(left, 15 * MM, right, 15 * MM)
        canvas.setFont("GFBody", 7.2)
        canvas.drawString(left, 10.4 * MM, status)
        canvas.drawRightString(right, 10.4 * MM, f"{current_doc.page} / 6")
        canvas.restoreState()

    story = []
    for index, page in enumerate(parser.pages):
        if index:
            story.append(PageBreak())
        for block in page:
            style = styles.get(block["class"], styles[block["tag"]])
            story.append(Paragraph(block["markup"], style))
    # Fix the technical metadata epoch for byte-for-byte rebuilds; it is not an effective date.
    os.environ["SOURCE_DATE_EPOCH"] = str(calendar.timegm((2026, 10, 19, 0, 0, 0)))
    doc.build(story, onFirstPage=page_frame, onLaterPages=page_frame, canvasmaker=Canvas)
    data = output.getvalue()
    reader = PdfReader(BytesIO(data), strict=True)
    if len(reader.pages) != 6:
        sections = [re.findall(r"§ \d+\. [^\n]+", page.extract_text()) for page in reader.pages]
        raise ValueError(f"Editorial groups overflowed: {len(reader.pages)} pages (expected 6). Sections per page: {sections}")
    if len(data) > 10 * 1024 * 1024 or not data.startswith(b"%PDF-"):
        raise ValueError("PDF fails the existing RegulationService size/signature constraints.")
    pdf_path = DIRECTORY / f"{STEM}.pdf"
    digest = hashlib.sha256(data).hexdigest()
    sha_path = DIRECTORY / f"{STEM}.pdf.sha256"
    sha_content = f"{digest}  {pdf_path.name}\n"
    if "--check" in sys.argv:
        if not pdf_path.exists() or pdf_path.read_bytes() != data or sha_path.read_text(encoding="utf-8") != sha_content:
            raise ValueError("PDF/hash are stale or runtime/fonts differ; rebuild with the documented environment.")
    else:
        pdf_path.write_bytes(data)
        sha_path.write_text(sha_content, encoding="utf-8")
    print(json.dumps({"pages": len(reader.pages), "bytes": len(data), "sha256": digest,
                      "reportlab": REPORTLAB_VERSION, "font": regular.name,
                      "font_sha256": hashlib.sha256(regular.read_bytes()).hexdigest(),
                      "status": status, "check": "--check" in sys.argv}, ensure_ascii=False))


if __name__ == "__main__":
    build()
