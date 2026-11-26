import { describe, expect, it } from "vitest";
import { formatNoteExport } from "./notepadExport";
import { contentToEditorHtml, richTextToPlainText, sanitizeRichText } from "./notepadRichText";

describe("notepad rich text", () => {
  it("preserves formatting and exact checklist attributes", () => {
    const clean = sanitizeRichText('<ul data-checklist="true" data-other="x"><li data-checked="true"><b>Done</b></li><li data-checked="false">Next</li></ul><font size="4" color="#2563eb">Blue</font><ol><li>Ordered</li></ol>');
    expect(clean).toBe('<ul data-checklist="true"><li data-checked="true"><b>Done</b></li><li data-checked="false">Next</li></ul><font color="#2563eb" size="4">Blue</font><ol><li>Ordered</li></ol>');
    expect(sanitizeRichText(clean)).toBe(clean);
    expect(sanitizeRichText('<ul data-checklist="false"><li data-checked="yes">A</li></ul>')).toBe("<ul><li>A</li></ul>");
  });

  it("strips unsafe elements, declarations, styles, handlers and URLs from pasted HTML", () => {
    const clean = sanitizeRichText('<!--Office--><p id="note" style="color:red; background-image:url(example)" onclick="invalid"><b>Keep</b></p><script>Discard</script><svg><title>Discard</title></svg><object>Discard</object><a href="javascript:invalid">Link text</a><img src="example">');
    expect(clean).toBe('<p style="color: red;"><b>Keep</b></p>Link text');
    expect(sanitizeRichText("<div><p><em>Recover")).toBe("<div><p><em>Recover</em></p></div>");
  });

  it("preserves line breaks, angle brackets and escaped ampersands", () => {
    const plain = "A < B\nC & D";
    const html = contentToEditorHtml(plain);
    expect(html).toBe("A &lt; B<br>C &amp; D");
    expect(contentToEditorHtml(html)).toBe(html);
    expect(richTextToPlainText(html)).toBe(plain);
    expect(contentToEditorHtml("<unknown>literal")).toBe("&lt;unknown&gt;literal");
  });

  it("exports safe text and Markdown checklist syntax", () => {
    const note = { title: "Tasks <today>", content: '<p><strong>Start</strong></p><ul data-checklist="true"><li data-checked="true">Done</li><li data-checked="false">[Later] &amp; more</li></ul>' };
    expect(formatNoteExport(note, "txt")).toBe("Tasks <today>\n\nStart\n\n- [x] Done\n- [ ] [Later] & more\n");
    expect(formatNoteExport(note, "md")).toBe("# Tasks &lt;today&gt;\n\n**Start**\n\n- [x] Done\n- [ ] \\[Later\\] &amp; more\n");
  });
});
