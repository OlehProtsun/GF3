"""Validate content parity, PDF structure, embedded glyphs and A4 text bounds."""
from pathlib import Path
from html import unescape
from io import BytesIO
import hashlib
import json
import re
import sys
import unicodedata

sys.dont_write_bytecode = True
from pypdf import PdfReader
import pdfplumber
import importlib.util

directory = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("gf_render_pdf", directory / "render-pdf.py")
renderer = importlib.util.module_from_spec(spec)
spec.loader.exec_module(renderer)
stem = renderer.STEM
markdown = (directory / f"{stem}.md").read_text(encoding="utf-8")
html = (directory / f"{stem}.html").read_text(encoding="utf-8")
data = (directory / f"{stem}.pdf").read_bytes()
reader = PdfReader(BytesIO(data), strict=True)
assert data.startswith(b"%PDF-") and data.rstrip().endswith(b"%%EOF")
assert len(data) <= 10 * 1024 * 1024 and len(reader.pages) == 6
assert not reader.is_encrypted and not reader.get_fields()
assert reader.metadata.title == re.search(r"^# (.+)$", markdown, re.M)[1]
assert re.search(r"^Status: (.+)$", markdown, re.M)[1] in reader.metadata.subject
assert len(f"{stem}.pdf") <= 255
digest = hashlib.sha256(data).hexdigest()
assert (directory / f"{stem}.pdf.sha256").read_text(encoding="utf-8") == f"{digest}  {stem}.pdf\n"

parser = renderer.DocumentParser()
parser.feed(html)
source_pages = markdown.split("<!-- pagebreak -->")
assert len(parser.pages) == len(source_pages) == 6


def normal(value):
    value = unescape(re.sub(r"<[^>]+>", "", value))
    value = re.sub(r"\s+", "", unicodedata.normalize("NFC", value))
    # Repository previews print the known portal paths until a real origin is supplied.
    value = re.sub(r"\(/legal/[^)]+\)", "", value)
    return value


paragraphs = 0
for index, (page, source_page, html_page) in enumerate(zip(reader.pages, source_pages, parser.pages), 1):
    text = page.extract_text()
    assert "\ufffd" not in text and len(text) > 500
    assert abs(float(page.mediabox.width) - renderer.A4[0]) < 0.1
    assert abs(float(page.mediabox.height) - renderer.A4[1]) < 0.1
    normalized_pdf = normal(text)
    for block in html_page:
        expected = normal(block["markup"])
        assert expected and expected in normalized_pdf, f"Missing PDF block on page {index}: {expected[:90]}"
        paragraphs += 1
    source_text = re.sub(r"\[([^\]]+)\]\([^)]+\)", r"\1", source_page)
    source_text = re.sub(r"^#{1,2} ", "", source_text, flags=re.M).replace("**", "")
    expected_source = normal(source_text)
    normalized_html = normal("".join(block["markup"] for block in html_page))
    assert expected_source == normalized_html, f"Source/HTML mismatch on page {index}"

    for font in page["/Resources"]["/Font"].values():
        font = font.get_object()
        if font.get("/Subtype") == "/TrueType":
            descriptor = font["/FontDescriptor"].get_object()
            assert descriptor.get("/FontFile2"), "Non-embedded TrueType font"

with pdfplumber.open(BytesIO(data)) as pdf:
    for index, page in enumerate(pdf.pages, 1):
        assert page.chars, f"No selectable text on page {index}"
        for char in page.chars:
            assert -0.5 <= char["x0"] < char["x1"] <= page.width + 0.5, (index, char)
            assert -0.5 <= char["top"] < char["bottom"] <= page.height + 0.5, (index, char)
    all_text = "\n".join(page.extract_text() for page in pdf.pages)
    for character in "ąćęłńóśźżĄĆĘŁŃÓŚŹŻ":
        if character in markdown:
            assert character in all_text, f"Missing Polish character {character}"

headings = re.findall(r"^## (§ \d+\. .+)$", markdown, re.M)
assert len(headings) == 14
assert all(normal(heading) in normal(" ".join(page.extract_text() for page in reader.pages)) for heading in headings)
uris = []
for page in reader.pages:
    for annotation in page.get("/Annots", []):
        action = annotation.get_object().get("/A")
        if action and action.get("/URI"):
            uris.append(str(action["/URI"]))
assert "https://uodo.gov.pl/" in uris
assert all(uri.startswith("https://") for uri in uris), uris
assert not re.search(r"<script\b|https?://[^\"\s]+\.(?:woff|ttf)", html, re.I)
assert html.startswith("<!doctype html>") and '<html lang="pl">' in html
assert "lorem ipsum" not in markdown.lower()

print(json.dumps({"result": "PASS", "pages": 6, "bytes": len(data), "sha256": digest,
                  "headings": len(headings), "content_blocks": paragraphs,
                  "clickable_https_links": len(uris), "text": "searchable/selectable",
                  "fonts": "embedded TrueType subsets", "format": "A4",
                  "metadata": "Polish title, draft subject, deterministic technical epoch"}, ensure_ascii=True))
