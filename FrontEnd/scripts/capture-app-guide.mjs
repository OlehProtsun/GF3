import { mkdir, readFile, writeFile } from "node:fs/promises";
import { chromium } from "@playwright/test";
import { createServer } from "vite";

// Run from FrontEnd. Every API response is synthetic and all off-origin requests are blocked.
const origin = "http://127.0.0.1:43883";
const destination = new URL("../public/guide/", import.meta.url);
const manifestFile = new URL("../src/pages/employee-install/model/guideCaptures.json", import.meta.url);
const manifest = await readFile(manifestFile, "utf8").then(JSON.parse, error => {
  if (error.code !== "ENOENT") throw error;
  return {};
});
const selected = new Set(process.argv.slice(2));
const captureFolders = [
  [/^login/, "getting-started"],
  [/^(alerts|nav-alerts)/, "notifications"],
  [/^(hours)/, "work-hours"],
  [/^(adjust)/, "corrections"],
  [/^(swap|nav-swap)/, "swaps"],
  [/^(profile|nav-profile)/, "profile"],
];
const current = "2026-11-09T10:00:00Z";
const timestamp = "2026-11-09T08:00:00Z";
const people = ["Alex Demo", "Sam Demo", "Jamie Demo"].map((displayName, index) => ({
  id: index + 1, employeeId: index + 1, displayName, firstName: displayName.split(" ")[0], lastName: "Demo", displayOrder: index,
}));
const slots = [
  [21, 2, 1, "09:00", "15:00"], [22, 2, 2, "15:00", "21:00"], [32, 3, 2, "09:00", "15:00"],
  [91, 9, 1, "09:00", "15:00"], [92, 9, 2, "15:00", "21:00"], [101, 10, 1, "09:00", "15:00"],
  [102, 10, 2, "15:00", "21:00"], [111, 11, 2, "09:00", "15:00"], [121, 12, 1, "09:00", "15:00"],
  [131, 13, 1, "09:00", "15:00"], [141, 14, 1, "09:00", "15:00"], [152, 15, 2, "09:00", "15:00"],
].map(([id, dayOfMonth, employeeId, fromTime, toTime]) => ({ id, dayOfMonth, employeeId, fromTime, toTime, slotNo: employeeId, status: "ASSIGNED" }));
const schedule = {
  id: 10, containerId: 2, containerName: "Demo schedules", shopId: 4, shopName: "ShopDemo6",
  name: "ShopDemo1", year: 2026, month: 11, publicationStatus: "public", allowSwap: true,
  publishedAtUtc: timestamp, lastUpdatedAtUtc: timestamp, employees: people, slots,
  relatedScheduleAssignments: [{ employeeId: 1, dayOfMonth: 2, scheduleId: 14, scheduleName: "ShopDemo2" }],
};
const secondSchedule = { ...schedule, id: 14, name: "ShopDemo2", slots: [
  { id: 213, employeeId: 1, dayOfMonth: 2, slotNo: 1, fromTime: "15:00", toTime: "21:00", status: "ASSIGNED" },
], relatedScheduleAssignments: [] };
const published = { ...schedule, id: 13, name: "ShopDemo4", employees: people.slice(1), slots: slots.filter(slot => slot.employeeId === 2) };
const openOffer = {
  id: 51, scheduleId: 10, scheduleSlotId: 111, scheduleName: "ShopDemo1", containerName: schedule.containerName,
  shopName: schedule.shopName, year: 2026, month: 11, dayOfMonth: 11, fromTime: "09:00", toTime: "15:00",
  fromEmployeeId: 2, fromEmployeeName: "Sam Demo", visibility: "public", status: "open", createdAtUtc: timestamp,
  shiftHours: 6, canAccept: true, canAcceptFull: true, canCancel: false, allowPartialAcceptance: true,
  eligibleClaimWindows: [{ fromTime: "09:00", toTime: "15:00" }], requestKind: "give",
  currentEmployeeHoursBefore: 30, currentEmployeeHoursAfter: 36,
  currentEmployeeWorkDaysBefore: 5, currentEmployeeWorkDaysAfter: 6,
  currentEmployeeFreeDaysBefore: 25, currentEmployeeFreeDaysAfter: 24, fromEmployeeHoursBefore: 18, fromEmployeeHoursAfter: 12,
};
const openOwn = { ...openOffer, id: 52, scheduleSlotId: 101, dayOfMonth: 10, fromEmployeeId: 1,
  fromEmployeeName: "Alex Demo", isCreatedByCurrentEmployee: true, canAccept: false, canCancel: true };
const complete = { ...openOffer, id: 53, scheduleSlotId: 121, dayOfMonth: 12, status: "accepted",
  acceptedByEmployeeId: 1, acceptedByEmployeeName: "Alex Demo", acceptedAtUtc: timestamp, canAccept: false };
const outgoing = { ...complete, id: 54, dayOfMonth: 8, scheduleSlotId: 81, fromEmployeeId: 1,
  fromEmployeeName: "Alex Demo", acceptedByEmployeeId: 2, acceptedByEmployeeName: "Sam Demo", isCreatedByCurrentEmployee: true };
const managerOffer = { ...openOffer, id: 55, dayOfMonth: 15, scheduleSlotId: 152, isManagerCreated: true,
  fromEmployeeId: null, manualColumnName: "Open shift" };
const correction = {
  id: 81, scheduleId: 10, scheduleSlotId: 91, employeeId: 1, employeeName: "Alex Demo", year: 2026, month: 11,
  dayOfMonth: 9, originalFromTime: "09:00", originalToTime: "15:00", requestedFromTime: "08:30",
  requestedToTime: "16:00", status: "pending", requestType: "adjustShift", createdAtUtc: timestamp,
};
const profile = { ...people[0], userName: "alex.demo", recoveryEmail: "alex@example.test", phone: null, contractType: "other" };
const availability = {
  id: 5, name: "Demo availability", year: 2026, month: 11, canSubmit: true, isEditLocked: false,
  visibleFromUtc: timestamp, visibleToUtc: "2026-11-30T18:00:00Z", slots: [],
  members: people.map(person => ({ ...person, slots: [] })),
};
const server = await createServer({
  server: { host: "127.0.0.1", port: 43883, strictPort: true, hmr: false, proxy: {} },
  plugins: [{ name: "guide-isolation", configureServer(server) {
    server.middlewares.use((request, response, next) => {
      if (!request.url?.startsWith("/api")) return next();
      response.writeHead(403).end();
    });
  } }],
});
await server.listen();
let browser;
try {
  browser = await chromium.launch();
  await mkdir(destination, { recursive: true });
  const context = await browser.newContext({
    viewport: { width: 390, height: 844 }, deviceScaleFactor: 1, isMobile: true, hasTouch: true,
    reducedMotion: "reduce", locale: "en-GB", timezoneId: "Europe/Warsaw", serviceWorkers: "block",
  });
  await context.routeWebSocket("**/*", socket => socket.close());
  await context.addInitScript(() => {
    sessionStorage.setItem("gf3.pwa.install.dismissed", "1");
    localStorage.setItem("gf3.language", "en");
    if (location.pathname !== "/login") localStorage.setItem("gf3.auth.access-token", "guide-demo");
  });
  const page = await context.newPage();
  let vacationRequest = null;
  let availabilityDemo;
  let swapDemo;
  page.setDefaultTimeout(4000);
  page.on("pageerror", error => console.log("Demo page error:", error.message));
  await page.clock.setFixedTime(new Date(current));
  await page.route("**/*", async route => {
    const request = route.request();
    const url = new URL(request.url());
    if (url.origin !== origin) return route.abort();
    if (!url.pathname.startsWith("/api/")) return route.continue();
    const api = url.pathname.slice(5);
    if (api.includes("negotiate")) return route.fulfill({ status: 503 });
    let data = [];
    if (api === "auth/session") data = { ...profile, role: "employee", employeeId: 1 };
    else if (api === "account-language") data = { language: "en" };
    else if (api === "employment-contract-settings" && swapDemo) data = { restrictSwapsToEightHours: Boolean(swapDemo.wholeShiftOnly) };
    else if (api === "employee-shift-swap-approvals" && swapDemo) data = swapDemo.approvals ?? [];
    else if (api === "employee-ui-state") data = { readNotificationIds: [], pinnedSwapIds: [], scheduleColumnOrders: {} };
    else if (api === "employee-schedules") data = swapDemo?.schedules ?? [schedule, secondSchedule, { ...schedule, id: 11, month: 10 }, { ...schedule, id: 12, year: 2025 }];
    else if (api === "employee-schedules/public") data = [published];
    else if (api === "employee-schedules/public/13") data = published;
    else if (api === "employee-shift-corrections") data = request.method() === "POST" ? { ...correction, ...request.postDataJSON(), id: 82 } : [correction];
    else if (api === "employee-shift-swaps") {
      if (request.method() === "POST" && swapDemo) {
        const payload = request.postDataJSON();
        const minutes = value => Number(value.slice(0, 2)) * 60 + Number(value.slice(3));
        data = { ...openOwn, ...payload, id: 70 + swapDemo.offers.length, dayOfMonth: 9,
          shiftHours: (minutes(payload.toTime) - minutes(payload.fromTime)) / 60 };
        swapDemo.offers.push(data);
      } else data = request.method() === "POST" ? openOwn : swapDemo?.offers ?? [openOffer, openOwn, complete, outgoing, managerOffer];
    }
    else if (api === "employee-shift-swaps/employees") data = people;
    else if (api === "employee-shift-swaps/preview") {
      const payload = request.postDataJSON();
      const source = (swapDemo?.schedules[0].slots ?? slots).find(slot => slot.id === payload.scheduleSlotId);
      data = { ...payload, requestKind: payload.requestKind, targetEmployeeId: payload.targetEmployeeId, isValid: true,
        targetEmployeeName: payload.targetEmployeeId ? "Sam Demo" : "",
        source: { ...source, scheduleId: 10, year: 2026, month: 11, slotId: source.id, scheduleName: "ShopDemo1",
          durationMinutes: (Number(payload.toTime.slice(0, 2)) - Number(payload.fromTime.slice(0, 2))) * 60,
          fromTime: payload.fromTime, toTime: payload.toTime } };
      if (payload.requestKind === "exchange") data.return = { ...swapDemo.returnShift,
        fromTime: payload.returnFromTime, toTime: payload.returnToTime };
      if (swapDemo?.exception) Object.assign(data, {
        requiresManagerException: true, minimumMinutes: 7200, projectedMinutes: 6840,
        managerExceptionReason: "Monthly minimum exception required.",
      });
    }
    else if (api === "employee-shift-swaps/return-candidates" && swapDemo) data = { items: [swapDemo.returnShift], hasMore: false };
    else if (/^employee-shift-swaps\/\d+\/cancel$/.test(api) && swapDemo) {
      const offer = swapDemo.offers.find(item => item.id === Number(api.split("/")[1]));
      if (offer) offer.status = "cancelled";
      data = offer;
    }
    else if (api === "system-news") data = [{ id: 1, title: "Team update", description: "Check the published demo schedule.", createdAtUtc: timestamp, isRead: false }];
    else if (api === "employee-ui-state/availability-time-presets") data = { presets: ["09:00 - 15:00", "15:00 - 21:00", "09:00 - 21:00"] };
    else if (api === "employee-availability") data = availabilityDemo?.periods ?? [availability];
    else if (api === "employee-availability/5/slots" && availabilityDemo) {
      const period = availabilityDemo.periods[0];
      period.slots = request.postDataJSON().slots;
      period.employeeLastModifiedAtUtc = timestamp;
      data = period;
    }
    else if (api === "employee-availability/5") data = availabilityDemo?.periods[0] ?? availability;
    else if (api === "employee-availability/6" && availabilityDemo) data = availabilityDemo.periods[1];
    else if (api === "employee-urlop") data = availabilityDemo?.vacation() ?? { shops: [{ id: 4, name: "ShopDemo6" }],
      months: [{ shopId: 4, year: 2026, month: 11, isOpen: true, approved: [], requests: vacationRequest ? [vacationRequest] : [] }] };
    else if (api === "employee-urlop/requests") data = vacationRequest = { ...request.postDataJSON(), requestId: "demo-vacation", status: "pending" };
    else if (api.includes("password") && api.includes("code")) data = { deliveryHint: "a***@example.test", expiresAtUtc: "2026-11-09T10:10:00Z" };
    else if (api.includes("profile") || api === "employee-account") data = swapDemo?.wholeShiftOnly ? { ...profile, contractType: "o_prace" } : profile;
    return route.fulfill({ json: data });
  });

  const button = name => page.getByRole("button", { name, exact: typeof name === "string" }).first();
  const label = value => page.getByText(value, { exact: typeof value === "string" }).first();
  const byClass = value => page.locator(`[class*="_${value}_"]`).first();
  const nav = route => page.locator(`nav[data-employee-nav]:visible a[href="${route}"]`).last();
  async function go(route) {
    await page.goto(origin + route, { waitUntil: "domcontentloaded", timeout: 30000 });
    const prompt = page.getByRole("dialog").getByRole("button", { name: /Not now|Nie teraz/ });
    if (route !== "/login") await page.locator("html[lang='en']").waitFor();
    if (await prompt.isVisible()) await prompt.click();
  }
  async function shot(name, target, region = target, room = 48, roomBelow = room, center = false) {
    try {
      await target.waitFor({ state: "visible" });
      await target.scrollIntoViewIfNeeded();
      if (center) await target.evaluate(element => element.scrollIntoView({ block: "center", inline: "nearest", behavior: "instant" }));
      await page.evaluate(() => document.fonts.ready);
      const [focus, outer] = await Promise.all([target.boundingBox(), region.boundingBox()]);
      if (!focus || !outer || focus.width < 2 || focus.height < 2) throw new Error("Missing visible crop");
      const x = Math.max(0, Math.floor(Math.min(outer.x, focus.x) - 8));
      const y = Math.max(0, Math.floor(Math.max(outer.y - 6, focus.y - room)));
      const width = Math.min(390, Math.ceil(Math.max(outer.x + outer.width, focus.x + focus.width) + 8)) - x;
      const height = Math.min(844, Math.ceil(Math.min(outer.y + outer.height + 6, focus.y + focus.height + roomBelow))) - y;
      if (width < 20 || height < 20 || focus.y + focus.height <= y || focus.y >= y + height) throw new Error("Empty crop");
      const png = await page.screenshot({ clip: { x, y, width, height }, animations: "disabled", caret: "hide" });
      const webp = await page.evaluate(async base64 => {
        const image = new Image();
        image.src = "data:image/png;base64," + base64;
        await image.decode();
        const canvas = document.createElement("canvas");
        canvas.width = image.naturalWidth;
        canvas.height = image.naturalHeight;
        canvas.getContext("2d").drawImage(image, 0, 0);
        return canvas.toDataURL("image/webp", .88).split(",")[1];
      }, png.toString("base64"));
      const folder = /^(availability-|urlop-)/.test(name) ? "availability-urlop"
        : captureFolders.find(([pattern]) => pattern.test(name))?.[1] ?? "schedules";
      const src = `${folder}/${name}.webp`;
      await mkdir(new URL(`${folder}/`, destination), { recursive: true });
      await writeFile(new URL(src, destination), Buffer.from(webp, "base64"));
      const rounded = metric => Math.round(metric * 1000) / 10;
      const left = Math.max(0, focus.x - x);
      const top = Math.max(0, focus.y - y);
      manifest[name] = { src, width, height, highlight: [
        rounded(left / width), rounded(top / height),
        rounded((Math.min(focus.x + focus.width, x + width) - x - left) / width),
        rounded((Math.min(focus.y + focus.height, y + height) - y - top) / height),
      ] };
      console.log("Captured", name);
    } catch (error) { console.log("Skipped", name, error.message.split("\n")[0]); }
  }
  async function flow(name, task) {
    if (selected.size && !selected.has(name)) return;
    try { await task(); }
    catch (error) { console.log("Stopped", name, error.message.split("\n")[0], (await page.locator("body").innerText()).slice(-330)); }
  }

  const section = (heading, name) => page.getByRole("heading", { name: heading, exact: true }).first().locator(`xpath=ancestor::*[contains(@class, "_${name}_")][1]`);
  const openDialog = () => page.locator("dialog[open], [role=dialog][aria-modal=true]").last();
  await flow("login", async () => {
    await go("/login");
    const login = page.getByRole("textbox", { name: /Username|Nazwa użytkownika/ });
    const password = page.locator('input[type="password"]');
    const card = byClass("card");
    const modes = byClass("modeSwitch");
    await login.fill("alex.demo");
    await shot("login", login, card, 99, 126, true);
    await shot("login-modes", modes, card, 45, 58, true);
    await password.fill("000000");
    await shot("login-password", password, card, 75, 34, true);
    await shot("login-submit", button(/^(Sign in|Zaloguj się)$/), card, 68, 20, true);
    await button("PIN").click();
    const launch = byClass("pinLaunchButton");
    await shot("login-pin-launch", launch, card, 78, 50, true);
    await launch.click();
    const dialog = openDialog();
    await dialog.getByRole("button", { name: "1", exact: true }).tap();
    await dialog.getByRole("button", { name: "2", exact: true }).tap();
    await shot("login-pin-keypad", byClass("pinDots"), dialog, 120, 524);
    await go("/schedule");
    await label("Work hours").waitFor();
    const mobileNav = page.locator("nav[data-employee-nav]:visible").last();
    await shot("login-workspace", mobileNav, page.locator("html"), 145, 24);
  });
  await flow("profile", async () => {
    await go("/profile");
    await shot("nav-profile", nav("/profile"), page.locator("nav[data-employee-nav]:visible").last());
    await shot("profile-guide", page.getByRole("link", { name: /App Guide/ }), byClass("panelHeader").locator("xpath=../.."), 25);
    await shot("profile-overview", byClass("profileSummaryBlock"), byClass("panelHeader"), 56);
    await shot("profile-edit", button("Edit account details"), section("Account details", "detailsSection"), 175);
    await shot("profile-language", byClass("languageRow"), section("Account details", "detailsSection"), 40);
    await byClass("languageRow").getByRole("button").first().click();
    const options = page.getByRole("listbox").last();
    await shot("profile-language-open", options, options, 16);
    await page.keyboard.press("Escape");
    await button("Edit account details").click();
    await shot("profile-email", page.getByRole("textbox", { name: /Recovery email/ }), section("Account details", "detailsSection"), 106);
    await shot("profile-save", button("Save profile"), section("Account details", "detailsSection"), 104);
    await button("Cancel").click();
    await shot("profile-password", button("Change password"), section("Password & security", "securitySection"), 70);
    await button("Change password").click();
    await shot("profile-code", button("Send code"), section("Password & security", "securitySection"), 65);
    await button("Send code").click();
    await shot("profile-password-fields", byClass("passwordFormGrid"), section("Password & security", "securitySection"), 35);
    await shot("profile-verification", page.getByRole("textbox", { name: "Verification code" }), byClass("securityEditor"), 48);
    await shot("profile-new-password", page.getByLabel("New password", { exact: true }), byClass("securityEditor"), 48);
    await shot("profile-password-confirm", button("Change password"), byClass("securityEditor"), 55);
    await shot("profile-logout", button("Log out"), byClass("profilePanel"), 32);
  });
  await flow("notifications", async () => {
    await go("/notifications");
    const alertsNav = page.locator("nav[data-employee-nav]:visible").last().getByRole("link", { name: /Alerts/ });
    await shot("nav-alerts", alertsNav, page.locator("nav[data-employee-nav]:visible").last());
    const inbox = page.getByRole("tab", { name: /Inbox/ });
    const news = page.getByRole("tab", { name: /News/ });
    await shot("alerts-tabs", inbox, inbox.locator("xpath=../.."), 30);
    await shot("alerts-unread", page.getByLabel("Unread", { exact: true }).first().locator("xpath=.."), byClass("notificationItem"), 100);
    await shot("alerts-read", button("Mark as read"), button("Mark as read").locator("xpath=../../.."), 84);
    await shot("alerts-all-read", button("Mark all read"), button("Mark all read").locator("xpath=../.."), 75);
    await shot("alerts-link", page.getByRole("link", { name: /Open schedule|Open swap/ }).first(), byClass("notificationItem"), 80);
    for (const [id, title] of [["schedule","New schedule published"],["availability","New availability published"],
      ["swap","Shift swap available"],["open-shift","Open shift posted"],["accepted","Your shift was accepted"]]) {
      const card = page.getByRole("article").filter({ has: page.getByText(title, { exact: true }) }).first();
      await shot(`alerts-example-${id}`, card.getByRole("link").first(), card, 140);
    }
    await news.click();
    await shot("alerts-news-read", button("Mark as read"), button("Mark as read").locator("xpath=ancestor::article"), 100);
    await shot("alerts-news", label("Team update"), label("Team update").locator("xpath=../../.."), 72);
  });
  await flow("schedule", async () => {
    await go("/schedule");
    await shot("nav-schedule", nav("/schedule"), page.locator("nav[data-employee-nav]:visible").last());
    await shot("schedule-overview", label("Tomorrow"), label("Tomorrow").locator("xpath=../../../.."), 146);
    await page.clock.setFixedTime(new Date("2026-11-11T10:00:00Z"));
    await go("/schedule");
    await shot("schedule-day-off", label("Now").locator("xpath=../.."), label("Now").locator("xpath=../../.."), 60);
    await page.clock.setFixedTime(new Date(current));
    await go("/schedule");
    const selector = button(/Select schedule\. Current schedule/);
    await shot("schedule-selector", selector, byClass("openScheduleHeader"), 40);
    const table = page.locator("table").first();
    await shot("schedule-matrix", table.locator("thead"), table, 150);
    for (const [id, title] of [["correction","Correction requested"],["open","Swap pending"],["complete","Swap completed"]]) {
      const badge = button(title);
      await shot(`schedule-${id}`, badge, badge.locator("xpath=ancestor::tr"), 20);
      if (await badge.isVisible()) {
        await badge.click();
        await shot(`schedule-${id}-bubble`, page.getByRole("status").filter({ hasText: title }).last());
        await page.keyboard.press("Escape");
      }
    }
    const eye = button("Show daily schedule view");
    await shot("schedule-eye", eye, byClass("openScheduleHeader"), 38);
    const exportPdf = button("Export schedule to PDF");
    await shot("schedule-pdf", exportPdf, byClass("openScheduleHeader"), 38);
    await eye.click();
    await shot("schedule-daily", page.getByRole("tablist", { name: "Schedule days" }), byClass("dailyScheduleShell"), 160);
    await selector.click();
    await shot("schedule-picker", openDialog().getByRole("heading", { name: "Select schedule" }), byClass("sheet"), 215);
    await shot("schedule-more", button("View More"), byClass("sheet"), 90, 8);
    await button("View More").click();
    await shot("schedule-public", label("ShopDemo4"), byClass("sheet"), 95);
    await label("ShopDemo4").click();
    await shot("schedule-preview", openDialog().getByRole("heading", { name: "Schedule Preview" }), byClass("sheet"), 270);
  });
  await flow("hours", async () => {
    await go("/schedule");
    const month = button("Summary month");
    const year = button("Summary year");
    await shot("hours-month", month, byClass("hoursSummaryPanel"), 85);
    await shot("hours-year", year, byClass("hoursSummaryPanel"), 85);
    await month.click();
    const periodOptions = page.getByRole("listbox").last();
    await shot("hours-month-open", periodOptions, periodOptions, 20);
    await page.keyboard.press("Escape");
    await year.click();
    await shot("hours-year-open", periodOptions, periodOptions, 20);
    await page.keyboard.press("Escape");
    const columns = page.getByRole("row").filter({ has: page.getByRole("columnheader", { name: "Hours", exact: true }) }).last();
    await shot("hours-columns", columns, byClass("hoursSummaryGrid"), 90);
    await shot("hours-total", byClass("hoursSummaryTotalRow"), byClass("hoursSummaryGrid"), 56);
  });
  await flow("availability", async () => {
    await go("/availability");
    const period = button(/Demo availability/);
    await shot("availability-period", period, page.locator("[data-employee-content]"), 125);
    if (await period.getAttribute("aria-expanded") === "false") await period.click();
    const available = page.locator("[data-own-availability-cell='12']");
    await shot("availability-day", available, available.locator("xpath=ancestor::table"), 86);
    await available.click();
    await shot("availability-editor", openDialog().getByRole("radio", { name: /Available \(whole day\)/ }), byClass("dayDialog"), 140);
    await page.keyboard.press("Escape");
    const urlop = page.getByRole("tab", { name: "Urlop" });
    await shot("availability-urlop", urlop, urlop.locator("xpath=../.."), 65);
    await urlop.click();
    await shot("urlop-panel", page.getByRole("heading", { name: "Urlop", exact: true }).first(),
      page.getByRole("tabpanel", { name: "Urlop" }), 235);
    const vacationDay = page.getByRole("button", { name: /2026-11-16|^16$/ }).first();
    await shot("urlop-date", vacationDay, byClass("calendarSurface"), 64);
    if (await vacationDay.isVisible()) await vacationDay.click();
    await shot("urlop-submit", button("Submit request"), button("Submit request").locator("xpath=../.."), 95);
    await button("Submit request").click();
    const yourRequests = page.getByRole("heading", { name: "Your requests" });
    await shot("urlop-history", yourRequests.locator("xpath=following-sibling::article[1]"), yourRequests.locator("xpath=.."), 68);
  });
  await flow("correction", async () => {
    await go("/schedule");
    const adjust = button("Request a shift correction");
    await shot("adjust", adjust, byClass("openScheduleHeader"), 50);
    await adjust.click();
    const dialog = openDialog();
    const selection = dialog.getByRole("button", { name: /ShopDemo1.*November/ }).first();
    await shot("adjust-schedule", selection, dialog, 60);
    await selection.click();
    await button("Continue").click();
    const shift = dialog.getByRole("radio", { name: /12 November.*09:00|November 12.*09:00/ }).first();
    await shot("adjust-shift", shift, dialog, 44);
    await shift.click();
    await button("Continue").click();
    const editor = dialog.locator('input[type="time"]').last();
    await shot("adjust-time", editor, dialog, 135);
    const toggle = label("Request day off").locator("xpath=../..");
    await toggle.click();
    await shot("adjust-day-off", toggle, dialog, 115);
    await toggle.click();
    await button(/Start time.*15 minutes earlier/).click();
    await button("Continue").click();
    const note = dialog.getByRole("textbox", { name: "Comment (optional)" });
    await shot("adjust-comment", note, dialog, 75);
    await note.fill("I started 15 minutes earlier.");
    await button("Continue").click();
    await shot("adjust-review", dialog.getByRole("heading", { name: "Review your request" }), dialog, 220);
    await shot("adjust-send", button("Send request"), dialog, 55);
    await button("Send request").click();
    await shot("adjust-success", label("Request sent successfully!"), dialog, 115);
    await button("Done").click();
    await adjust.click();
    await selection.click();
    await button("Continue").click();
    await dialog.getByRole("radio", { name: /16 November.*Day off|November 16.*Day off/ }).click();
    await button("Continue").click();
    await shot("adjust-add-shift", dialog.locator('input[type="time"]').first(), dialog, 145);
  });
  await flow("swap", async () => {
    await go("/swap");
    const launcher = button("Create offer");
    await shot("nav-swap", nav("/swap"), page.locator("nav[data-employee-nav]:visible").last());
    await shot("swap-page", launcher, page.locator("[data-employee-content]"), 160);
    const history = button("Swap history");
    await shot("swap-history-button", history, page.locator("[data-employee-content]"), 46);
    await history.click();
    await shot("swap-history", page.locator("[data-swap-history-scroll]"), openDialog(), 32);
    await button("Close").click();
    await launcher.click();
    const dialog = openDialog();
    const scheduleChoice = dialog.getByRole("button", { name: /ShopDemo1/ }).first();
    await shot("swap-schedule", scheduleChoice, dialog, 85);
    await scheduleChoice.click();
    const shift = dialog.getByRole("radio", { name: /13\.11\.2026/ }).first();
    await shot("swap-shift", shift, dialog, 75);
    await shift.click();
    await shot("swap-partial", button("Custom period"), dialog, 80);
    await button("Confirm shift").click();
    await button("Continue").click();
    await shot("swap-everyone", button("Everyone"), dialog, 75);
    await button("Specific employee").click();
    await shot("swap-specific", button(/Sam Demo/), dialog, 75);
    await button(/Sam Demo/).click();
    await shot("swap-exchange", dialog.getByRole("radio", { name: "Swap shifts" }), dialog, 85);
    await button("Back").click();
    await button("Everyone").click();
    await button("Continue").click();
    await shot("swap-review", label("Your shift (you give)"), dialog, 160);
    await shot("swap-send", button("Send request"), dialog, 45);
    await button("Send request").click();
    await shot("swap-success", label("Swap request sent successfully!"), dialog, 115);
  });
  await flow("accept", async () => {
    await go("/swap");
    await page.locator('[data-swap-id="51"]').getByRole("button", { name: /View offer details/ }).click();
    const dialog = openDialog();
    await shot("swap-offer", dialog.getByRole("heading", { name: "Offer details" }), dialog, 210);
    const take = dialog.getByRole("button", { name: /Accept|Take shift/ }).last();
    await shot("swap-accept", dialog.getByRole("button", { name: "Custom period" }), dialog, 105);
    await take.click();
    await shot("swap-confirm", label("Accept this shift?"), openDialog(), 100);
  });
  await flow("swap-manual", async () => {
    const longSlot = { ...slots.find(slot => slot.id === 91), toTime: "21:00" };
    const returnShift = { scheduleId: 14, scheduleName: "ShopDemo2", shopName: "ShopDemo6", year: 2026, month: 11,
      dayOfMonth: 11, slotId: 214, fromTime: "15:00", toTime: "21:00", durationMinutes: 360, canSelect: true,
      eligibleWindows: [{ fromTime: "15:00", toTime: "21:00" }] };
    swapDemo = { schedules: [{ ...schedule, slots: slots.map(slot => slot.id === 91 ? longSlot :
      slot.id === 111 ? { ...slot, toTime: "21:00" } : slot) }], offers: [], returnShift };
    const firstHalf = { ...openOwn, id: 62, scheduleSlotId: 91, dayOfMonth: 9, allowPartialAcceptance: false };
    const secondHalf = { ...firstHalf, id: 63, fromTime: "15:00", toTime: "21:00" };
    const flexible = { ...openOffer, toTime: "21:00", shiftHours: 12,
      eligibleClaimWindows: [{ fromTime: "09:00", toTime: "21:00" }] };
    const offerCard = id => page.locator(`[data-swap-id="${id}"]`);
    const openOfferDetails = id => offerCard(id).getByRole("button", { name: /View offer details/ }).click();
    const scoped = () => page.locator("#create-swap-panel");
    const source = () => scoped().getByRole("radio", { name: /09\.11\.2026/ }).first();
    async function picker() {
      await button("Create offer").click();
      await scoped().getByRole("button", { name: /ShopDemo1/ }).first().click();
      await source().click();
    }
    async function recipient() {
      await button("Confirm shift").click();
      await button("Continue").click();
    }
    async function review() { await button("Continue").click(); }
    async function leave() { await go("/swap"); }
    try {
      await leave();
      await picker();
      await shot("swap-manual-shift", source(), scoped(), 75);
      await shot("swap-manual-full", button("Full shift"), scoped(), 85);
      await recipient();
      await shot("swap-manual-exact", scoped().getByRole("radio", { name: "Only the exact shift" }), scoped(), 65);
      await scoped().getByRole("radio", { name: "Allow custom time" }).check();
      await shot("swap-manual-flexible", scoped().getByRole("radio", { name: "Allow custom time" }), scoped(), 65);
      await scoped().getByRole("radio", { name: "Only the exact shift" }).check();
      await review();
      await shot("swap-manual-full-review", label("Your shift (you give)"), scoped(), 160);
      await button("Send request").click();
      await shot("swap-manual-full-success", label("Swap request sent successfully!"), scoped(), 125);
      swapDemo.offers = [];
      await leave();
      await picker();
      await button("Custom period").click();
      const times = scoped().locator('input[type="time"]');
      await times.nth(1).fill("15:00");
      await shot("swap-manual-time", times.nth(1), scoped(), 120);
      await recipient();
      await review();
      await shot("swap-manual-half-review", label("Your shift (you give)"), scoped(), 145);
      await button("Send request").click();
      await label("Swap request sent successfully!").waitFor();
      swapDemo.offers = [firstHalf];
      await leave();
      await shot("swap-manual-first-open", offerCard(62), offerCard(62), 20);
      await picker();
      await shot("swap-manual-reserved", source(), scoped(), 50);
      await shot("swap-manual-second-time", scoped().locator('input[type="time"]').first(), scoped(), 118);
      await recipient();
      await review();
      await button("Send request").click();
      await label("Swap request sent successfully!").waitFor();
    } catch (error) { console.log("Stopped manual split sequence:", error.message.split("\n")[0]); }

    swapDemo.offers = [firstHalf, secondHalf];
    await leave();
    await shot("swap-manual-two", offerCard(62), offerCard(62).locator("xpath=.."), 190);
    await openOfferDetails(62);
    await shot("swap-manual-cancel", button("Cancel offer"), openDialog(), 52);
    await button("Cancel offer").click();
    await button("Swap history").click();
    await shot("swap-manual-cancelled", offerCard(62), openDialog(), 70);

    try {
      swapDemo.offers = [];
      await leave();
      await picker();
      await button("Custom period").click();
      await scoped().locator('input[type="time"]').nth(1).fill("15:00");
      await recipient();
      await button("Specific employee").click();
      await shot("swap-manual-search", scoped().getByRole("searchbox"), scoped(), 70);
      await button(/Sam Demo/).click();
      const give = scoped().getByRole("radio", { name: "Give shift" });
      await shot("swap-manual-give", give, scoped(), 110);
      await button("Save selection").click();
      await review();
      await shot("swap-manual-specific-review", label("Your shift (you give)"), scoped(), 180);
      await leave();
      await picker();
      await button("Custom period").click();
      await scoped().locator('input[type="time"]').nth(1).fill("15:00");
      await recipient();
      await button("Specific employee").click();
      await button(/Sam Demo/).click();
      await scoped().getByRole("radio", { name: "Swap shifts" }).click();
      await shot("swap-manual-return-action", button("Choose their shift"), scoped(), 85);
      await button("Choose their shift").click();
      const returnSchedule = button(/ShopDemo2/);
      await shot("swap-manual-return-schedule", returnSchedule, scoped(), 85);
      await returnSchedule.click();
      const returned = scoped().getByRole("radio", { name: /11\.11\.2026/ }).first();
      await returned.click();
      await shot("swap-manual-return-shift", returned, scoped(), 115);
      await button("Confirm selection").click();
      await button("Save selection").click();
      await review();
      await shot("swap-manual-exchange-review", label("Employee shift (they give)"), scoped(), 190);
    } catch (error) { console.log("Stopped targeted offer sequence:", error.message.split("\n")[0]); }

    swapDemo.offers = [flexible];
    await leave();
    await shot("swap-manual-incoming", offerCard(51), offerCard(51), 30);
    await openOfferDetails(51);
    await shot("swap-manual-accept-full", button("Accept shift"), openDialog(), 64);
    await button("Accept shift").click();
    await shot("swap-manual-confirm-full", label("Accept this shift?"), openDialog(), 105);
    swapDemo.offers = [{ ...flexible, canAcceptFull: false, eligibleClaimWindows: [{ fromTime: "15:00", toTime: "21:00" }] }];
    await leave();
    await openOfferDetails(51);
    await shot("swap-manual-claim", openDialog().getByRole("region", { name: "Claim period" }), openDialog(), 64);
    await button("Accept shift").click();
    await shot("swap-manual-confirm-part", label("Accept this shift?"), openDialog(), 105);
    swapDemo.offers = [
      { ...flexible, status: "accepted", toTime: "15:00", acceptedByEmployeeId: 1, acceptedByEmployeeName: "Alex Demo", acceptedAtUtc: timestamp, shiftHours: 6 },
      { ...flexible, id: 56, fromTime: "15:00", shiftHours: 6, eligibleClaimWindows: [{ fromTime: "15:00", toTime: "21:00" }] },
      { ...flexible, id: 57, isAwaitingManagerApproval: true, approvalPendingForCurrentEmployee: true, canAccept: false, acceptanceUnavailableReason: "This offer is awaiting manager approval." },
      { ...flexible, id: 58, canAccept: false, acceptanceUnavailableReason: "You already work during this time." },
    ];
    await leave();
    await shot("swap-manual-residual", offerCard(56), offerCard(56), 25);
    await openOfferDetails(57);
    await shot("swap-manual-held", openDialog().getByRole("status").first(), openDialog(), 85);
    await leave();
    await openOfferDetails(58);
    await shot("swap-manual-conflict", label("You already work during this time."), openDialog(), 85);
    await leave();
    await button("Swap history").click();
    await shot("swap-manual-accepted", offerCard(51), openDialog(), 95);
    swapDemo = undefined;
  });
  await flow("swap-extra", async () => {
    swapDemo = {
      schedules: [schedule],
      offers: [openOwn, complete, { ...openOffer, requiresManagerApproval: true, canRequestManagerApproval: true }],
      approvals: [{ ...openOffer, id: 74, kind: "claim", status: "pending", employeeId: 1,
        employeeName: "Alex Demo", shiftSwapRequestId: 51, isStale: false }],
    };
    try {
      await go("/swap");
      await shot("swap-manual-filters", page.getByRole("group", { name: "Filter swap offers" }), page.locator("[data-employee-content]"), 88);
      const card = page.locator('[data-swap-id="51"]');
      await shot("swap-manual-copy", card.getByRole("button", { name: /Copy swap link/ }), card, 60);
      await card.getByRole("button", { name: /View offer details/ }).click();
      await shot("swap-manual-approval-action", button("Request manager approval"), openDialog(), 60);
      await go("/swap");
      await button("Swap history").click();
      const approvals = openDialog().getByRole("region", { name: "My approval requests" });
      await shot("swap-manual-approval-history", approvals.getByRole("button", { name: "Cancel request" }), approvals, 105);
      await go("/schedule");
      await button("Show daily schedule view").click();
      for (const [day, title, state] of [[10, "Swap pending", "pending"], [12, "Swap completed", "completed"]]) {
        await page.getByRole("tab", { name: new RegExp(`\\b${day}\\b`) }).click();
        const indicator = button(title);
        await shot(`swap-daily-${state}`, indicator, indicator.locator("xpath=ancestor::article"), 40);
        await indicator.click();
        const bubble = page.getByRole("status").filter({ hasText: title }).last();
        await shot(`swap-daily-${state}-bubble`, bubble);
        await page.keyboard.press("Escape");
      }
    } finally {
      swapDemo = undefined;
    }
  });
  await flow("swap-restrictions", async () => {
    const flexible = { ...openOffer, toTime: "21:00", shiftHours: 12, canAcceptFull: false,
      eligibleClaimWindows: [{ fromTime: "09:00", toTime: "12:00" }, { fromTime: "15:00", toTime: "21:00" }] };
    swapDemo = { schedules: [schedule], offers: [flexible, { ...flexible, id: 58, canAccept: false,
      acceptanceUnavailableReason: "You already work during this time." }] };
    const offer = id => page.locator(`[data-swap-id="${id}"]`).getByRole("button", { name: /View offer details/ });
    const wizard = page.locator("#create-swap-panel");
    try {
      await go("/swap");
      await offer(51).click();
      const windows = openDialog().getByRole("group", { name: "Available claim windows" });
      await shot("swap-claim-window", windows, openDialog(), 85);
      await go("/swap");
      await offer(58).click();
      await shot("swap-manual-conflict", label("You already work during this time."), openDialog(), 85);
      swapDemo.offers = [];
      swapDemo.exception = true;
      await go("/swap");
      await button("Create offer").click();
      await wizard.getByRole("button", { name: /ShopDemo1/ }).first().click();
      await wizard.getByRole("radio", { name: /09\.11\.2026/ }).first().click();
      await button("Confirm shift").click();
      await button("Continue").click();
      await button("Continue").click();
      await shot("swap-exception-example", button("Request exception"), wizard, 68);
      swapDemo.exception = false;
      swapDemo.wholeShiftOnly = true;
      swapDemo.schedules = [{ ...schedule, slots: schedule.slots.map(slot => slot.id === 91 ? { ...slot, toTime: "17:00" } : slot) }];
      await go("/swap");
      await button("Create offer").click();
      await wizard.getByRole("button", { name: /ShopDemo1/ }).first().click();
      await wizard.getByRole("radio", { name: /09\.11\.2026/ }).first().click();
      await shot("swap-rule-example", button("Custom period"), wizard, 118);
    } finally { swapDemo = undefined; }
  });
  const hideGuideDock = () => page.locator("nav[data-employee-nav]").evaluateAll(nodes => {
    for (const node of nodes) node.style.visibility = "hidden";
  });
  const centerGuideControl = locator => locator.evaluate(element => element.scrollIntoView({ block: "center", inline: "nearest", behavior: "instant" }));
  const frame = (name, target, region = target, room = 70, below = room) => shot(name, target, region, room, below, true);

  await flow("profile-guide-refine", async () => {
    await go("/profile");
    await button("Edit account details").click();
    await page.locator('input[type="email"]').fill("alex.updated@example.test");
    const save = button("Save profile");
    await centerGuideControl(save);
    console.log("Save profile is reachable with the normal dock:", await save.evaluate(element => {
      const box = element.getBoundingClientRect();
      return element.contains(document.elementFromPoint(box.x + box.width / 2, box.y + box.height / 2));
    }));
    await hideGuideDock();
    await frame("profile-save", save, section("Account details", "detailsSection"), 125, 30);
    await button("Cancel").click();
    const security = section("Password & security", "securitySection");
    const password = button("Change password");
    await frame("profile-password", password, security, 125, 25);
    await password.click();
    const editor = byClass("securityEditor");
    await frame("profile-code", button("Send code"), editor, 60, 110);
    await button("Send code").click();
    await frame("profile-password-fields", byClass("passwordFormGrid"), editor, 30, 42);
    await frame("profile-verification", page.getByRole("textbox", { name: "Verification code" }), editor, 40, 105);
    await frame("profile-new-password", page.getByLabel("New password", { exact: true }), editor, 105, 40);
    await page.getByRole("textbox", { name: "Verification code" }).fill("000000");
    await page.getByLabel("New password", { exact: true }).fill("654321");
    await frame("profile-password-confirm", password, editor, 150, 22);
  });

  await flow("schedule-guide-refine", async () => {
    await go("/schedule");
    await hideGuideDock();
    const heading = label("Work hours");
    await frame("hours-location", heading, page.locator("[data-employee-content]"), 190, 90);
    const selectors = page.getByRole("group", { name: "Summary period" });
    await frame("hours-period-controls", selectors, byClass("hoursSummaryPanel"), 70, 85);
    await frame("hours-read-total", byClass("hoursSummaryTotalRow"), byClass("hoursSummaryGrid"), 340, 20);
    const worker = button(/Customize columns from.*Alex Demo/i);
    await frame("schedule-order-header", worker, page.locator("table").first(), 65);
    await worker.click();
    const dialog = openDialog();
    const rows = dialog.getByRole("list", { name: "Schedule columns" });
    const handle = button(/Reorder.*Alex Demo/i);
    await frame("schedule-order-dialog", rows, dialog, 105);
    await frame("schedule-order-handle", handle, dialog, 55);
    const from = await handle.boundingBox();
    const to = await rows.getByRole("listitem").last().boundingBox();
    await page.mouse.move(from.x + from.width / 2, from.y + from.height / 2);
    await page.mouse.down();
    await page.mouse.move(from.x + from.width / 2, to.y + to.height / 2, { steps: 18 });
    await shot("schedule-order-drag", byClass("dragPreview"), dialog, 95);
    await page.mouse.up();
    await frame("schedule-order-apply", button("Apply"), dialog, 38, 15);
    await button("Apply").click();
    await worker.click();
    await frame("schedule-order-reset", button("Reset default"), dialog, 48, 18);
    await button("Reset default").click();
    await frame("schedule-order-reset-apply", button("Apply"), dialog, 38, 16);
    await button("Apply").click();
  });

  await flow("availability-guide-refine", async () => {
    const requests = ["pending", "approved", "rejected"].map((status, index) => ({
      requestId: `demo-${status}`, employeeId: 1, employeeName: "Alex Demo", shopId: 4, year: 2026, month: 11,
      days: [17 + index], status, createdAtUtc: timestamp,
    }));
    availabilityDemo = {
      periods: [{ ...availability }, { ...availability, id: 6, name: "Automatic availability", isAutomatic: true, canSubmit: false }],
      vacation: () => ({
        shops: [{ id: 4, name: "ShopDemo6" }, { id: 5, name: "ShopDemo7" }],
        months: [{ shopId: 4, year: 2026, month: 11, isOpen: true, requests: [...requests, ...(vacationRequest ? [vacationRequest] : [])],
          approved: [{ employeeId: 2, employeeName: "Sam Demo", date: "2026-11-20", shopId: 4 }] },
        { shopId: 4, year: 2026, month: 12, isOpen: true, approved: [], requests: [] },
        { shopId: 5, year: 2026, month: 11, isOpen: true, approved: [], requests: [] }],
      }),
    };
    try {
      await go("/availability");
      await shot("availability-nav", nav("/availability"), page.locator("nav[data-employee-nav]:visible").last());
      await hideGuideDock();
      const period = button(/Demo availability/);
      await frame("availability-period", period, page.locator("[data-employee-content]"), 135, 45);
      if (await period.getAttribute("aria-expanded") === "false") await period.click();
      const grid = page.getByRole("region", { name: "Team availability" });
      await frame("availability-calendar", grid.locator("thead"), grid, 24, 205);
      const day = n => page.locator(`[data-own-availability-cell="${n}"]`);
      await frame("availability-day", day(12), grid);
      await day(12).click();
      const dialog = openDialog();
      const sheet = byClass("dayDialog");
      await frame("availability-editor", dialog.getByRole("radiogroup", { name: "Availability" }), sheet, 125);
      await dialog.getByRole("radio", { name: /Custom time/ }).click();
      await frame("availability-custom", dialog.locator('input[type="time"]').first(), sheet, 95);
      await frame("availability-presets", label("Quick presets").locator("xpath=.."), sheet, 54, 90);
      await button("Edit presets").click();
      await frame("availability-edit-presets", button("Add preset"), sheet, 120, 40);
      await button("Done").click();
      await button("09:00 – 15:00").click();
      await button("Save").click();
      await centerGuideControl(day(13));
      const first = await day(13).boundingBox();
      const last = await day(14).boundingBox();
      await page.mouse.move(first.x + first.width / 2, first.y + first.height / 2);
      await page.mouse.down();
      await page.waitForTimeout(400);
      await page.mouse.move(last.x + last.width / 2, last.y + last.height / 2, { steps: 8 });
      await page.mouse.up();
      const multi = page.getByRole("dialog", { name: /days selected/ });
      await frame("availability-multiple", multi.getByRole("heading").first(), sheet, 115, 190);
      if (await multi.isVisible()) {
        await multi.getByRole("radio", { name: /Available \(whole day\)/ }).click();
        await button("Save").click();
      }
      const save = button("Save availability");
      await frame("availability-save", save, save.locator("xpath=../.."), 112, 25);
      await save.click();
      await button("Close success message").click();
      await button("Close success message").waitFor({ state: "hidden" });
      await frame("availability-saved", day(12), grid, 65);
      const automatic = button(/Automatic availability/);
      await frame("availability-restrictions", automatic, page.locator("[data-employee-content]"), 70);
    } finally { if (await openDialog().isVisible()) await page.keyboard.press("Escape"); }

    try {
      const tab = page.getByRole("tab", { name: "Urlop" });
      await frame("availability-urlop", tab, tab.locator("xpath=../.."), 45);
      await tab.click();
      const panel = page.getByRole("tabpanel", { name: "Urlop" });
      await frame("urlop-panel", button("Shop"), panel, 75, 130);
      await button("Shop").click();
      await frame("urlop-shops-open", page.getByRole("listbox").last());
      await page.keyboard.press("Escape");
      await frame("urlop-month", button("Month"), panel, 75, 120);
      await button("Month").click();
      await frame("urlop-month-open", page.getByRole("listbox").last());
      await page.keyboard.press("Escape");
      const date = button("2026-11-16");
      const calendar = page.getByRole("group", { name: "Vacation calendar" });
      await frame("urlop-date", date, calendar, 110);
      await date.click();
      await frame("urlop-selected", date, calendar, 105);
      await frame("urlop-submit", button("Submit request"), panel, 68, 25);
      await button("Submit request").click();
      await frame("urlop-status", panel.locator("article").nth(1), panel, 110, 120);
      await frame("urlop-colleagues", label(/Sam Demo — 2026-11-20/), panel, 65, 22);
    } finally { availabilityDemo = undefined; vacationRequest = null; }
  });

  await flow("swap-offer-guide-refine", async () => {
    const privateOffer = { ...openOffer, id: 61, visibility: "private", targetEmployeeId: 1, targetEmployeeName: "Alex Demo", allowPartialAcceptance: false };
    const held = { ...openOffer, id: 62, canAccept: false, isAwaitingManagerApproval: true, approvalPendingForCurrentEmployee: true };
    const locked = { ...openOffer, id: 63, canAccept: false, isScheduleLocked: true, allowPartialAcceptance: false };
    const needsApproval = { ...openOffer, id: 64, requiresManagerApproval: true, canRequestManagerApproval: true };
    const finished = { ...managerOffer, id: 65, canAccept: false, isFinished: true, manualOfferRefreshStatus: "finished" };
    const cancelled = { ...openOwn, id: 66, status: "cancelled", allowPartialAcceptance: false };
    swapDemo = { schedules: [schedule], offers: [openOffer, openOwn, privateOffer, held, locked, needsApproval, finished, complete, cancelled] };
    const card = id => page.locator(`[data-swap-id="${id}"]`).first();
    const body = id => card(id).getByRole("button", { name: /View offer details/ });
    try {
      await go("/swap");
      await hideGuideDock();
      await frame("swap-guide-list", card(51), page.locator("[data-employee-content]"), 118, 235);
      for (const [id, key] of [[51, "public"], [52, "mine"], [61, "private"], [62, "held"],
        [63, "locked"], [64, "approval"], [65, "finished"]]) {
        await frame(`swap-guide-labels-${key}`, card(id).locator('[class*="_offerBadges_"]'), card(id), 93, 40);
      }
      await frame("swap-guide-open-card", body(51), card(51), 20);
      await body(51).click();
      const dialog = openDialog();
      await frame("swap-guide-close", button("Close"), dialog, 10, 90);
      await frame("swap-guide-details", byClass("metadata"), dialog, 125, 70);
      await frame("swap-guide-actions", button("Accept shift"), dialog, 108, 12);
      await button("Accept shift").click();
      const confirm = page.getByRole("dialog", { name: "Accept this shift?" });
      await frame("swap-guide-confirm", confirm.getByRole("button", { name: "Accept shift" }), confirm, 155, 10);
      await go("/swap");
      await hideGuideDock();
      await body(63).click();
      await frame("swap-guide-unavailable", label("Schedule is locked while a manager is editing it."), openDialog(), 35, 85);
      await button("Close").click();
      await button("Swap history").click();
      await frame("swap-guide-labels-accepted", card(53).locator('[class*="_offerBadges_"]'), card(53), 92, 25);
      await frame("swap-guide-labels-history", card(66).locator('[class*="_offerBadges_"]'), openDialog(), 135, 115);
    } finally { swapDemo = undefined; }
  });

  if (selected.has("guide-refine")) {
    await page.setViewportSize({ width: 375, height: 812 });
    const pages = [
      ["schedules", "Customize Schedule Layout", "schedule-order-drag"],
      ["work-hours", "How to find your working hours", "hours-location"],
      ["shift-swaps", "How to Open a Shift Offer", "swap-guide-open-card"],
      ["availability-urlop", "Availability", "availability-saved"],
      ["profile", "Profile", "profile-save"],
    ];
    for (const [topic, heading, capture] of pages) {
      await go(`/install?topic=${topic}`);
      const article = page.locator("article").filter({ has: page.getByRole("heading", { name: heading, exact: true }) });
      await article.waitFor();
      const paths = await article.locator('img[src^="/guide/"]').evaluateAll(images => images.map(image => image.getAttribute("src")));
      const failures = await page.evaluate(async sources => (await Promise.all(
        [...new Set(sources)].map(async source => {
          const img = new Image();
          img.src = source;
          return img.decode().then(() => null, () => source);
        }),
      )).filter(Boolean), paths);
      if (failures.length) throw new Error(`${topic}: unreadable images ${failures.join(", ")}`);
      const image = article.locator(`img[src$="/${capture}.webp"]`);
      await image.click();
      const preview = page.getByRole("dialog", { name: "Screenshot preview" });
      await preview.waitFor();
      await page.keyboard.press("Escape");
      await preview.waitFor({ state: "hidden" });
      console.log(`Checked ${topic}: ${new Set(paths).size} images and image preview at 375px`);
    }
  }

  if (selected.has("guide")) {
    await go("/install");
    await page.getByRole("heading", { name: "Welcome to GF" }).waitFor();
    await page.getByRole("link", { name: /^Getting Started/ }).click();
    await page.getByRole("heading", { name: "Install on your phone" }).waitFor();
    await button("Android").click();
    await page.getByRole("img", { name: /GF screenshot showing Username/ }).waitFor();
    await button("Enlarge visual for Enter your username").click();
    await page.getByRole("dialog", { name: "Screenshot preview" }).waitFor();
    await page.keyboard.press("Escape");
    await page.getByRole("dialog", { name: "Screenshot preview" }).waitFor({ state: "hidden" });
    await page.goBack();
    await page.getByRole("heading", { name: "Welcome to GF" }).waitFor();
    await go("/install?topic=getting-started&section=password");
    await page.locator("#guide-password").getByRole("heading", { name: "Change password", exact: true }).waitFor();
    for (const topic of ["notifications", "schedules", "work-hours", "shift-corrections", "shift-swaps", "profile"]) {
      await go(`/install?topic=${topic}`);
      await page.locator("[data-guide-heading]").waitFor();
      const image = page.locator("img").filter({ visible: true }).first();
      await image.scrollIntoViewIfNeeded();
      if (!(await image.evaluate(element => element.decode().then(() => element.naturalWidth)))) {
        throw new Error(`Guide illustration failed to load for ${topic}`);
      }
    }
    await go("/install?topic=shift-swaps");
    await page.getByRole("link", { name: /Give away your entire shift/ }).click();
    await page.locator("#guide-full").getByRole("heading", { name: "Giving away a full shift" }).waitFor();
    if (await page.locator("article section[id^='guide-']").count() !== 14) throw new Error("Expected 14 Swap walkthroughs");
    console.log("Checked guide navigation, platform switch, image preview, Escape, Back, and the password deep link.");
  }
} finally {
  await writeFile(manifestFile, JSON.stringify(manifest, null, 2) + "\n");
  await browser?.close();
  await server.close();
}
