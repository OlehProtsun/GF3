import { expect, test } from "@playwright/test";

test("phone keypad accepts presses along all four edges", async ({ page, isMobile }) => {
  await page.addInitScript(() => {
    localStorage.removeItem("gf3.auth.access-token");
    localStorage.setItem("gf3.auth.last-username", "keypad-test");
    localStorage.setItem("gf3.auth.password-mode", "phone");
  });
  await page.goto("/login");
  await page.getByRole("button", { name: "Enter password", exact: true }).click();
  const key = page.getByRole("button", { name: "1", exact: true });
  await expect(key).toBeEnabled();
  const box = await key.boundingBox();
  if (!box) throw new Error("Keypad button is not visible");
  const edges = [
    { x: box.x + 1, y: box.y + box.height / 2 },
    { x: box.x + box.width - 1, y: box.y + box.height / 2 },
    { x: box.x + box.width / 2, y: box.y + 1 },
    { x: box.x + box.width / 2, y: box.y + box.height - 1 },
  ];
  for (const [index, point] of edges.entries()) {
    if (isMobile) await page.touchscreen.tap(point.x, point.y);
    else {
      await page.mouse.move(point.x, point.y);
      await page.mouse.down();
      // Hold through the pressed-state transition: the hit area must remain stable.
      await page.waitForTimeout(150);
      await page.mouse.up();
    }
    await expect(page.getByRole("status", { name: `${index + 1} of 6 digits entered` })).toBeVisible();
  }
});
