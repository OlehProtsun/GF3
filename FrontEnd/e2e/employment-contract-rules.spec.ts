import { expect, test, type Request } from "@playwright/test";
import { readFileSync } from "node:fs";
import { execFileSync } from "node:child_process";

if (process.env.GF3_PICKUP_LIVE_CONFIG) test.use({ trace: "off", video: "off", screenshot: "off" });

test("isolated contract workflow: manager settings, pending pickup, publication review, exception and availability", async ({ page }) => {
  test.setTimeout(180_000);
  await page.setViewportSize({ width: 1440, height: 1000 });
  let actor: "manager" | "employee" = "manager";
  let managerMode: "pc" | "phone" = "pc";
  let contractType = "zlecenie";
  let settings = { restrictSwapsToEightHours: true, requirePickupApproval: true, requireBelowMinimumGiveApproval: true, automaticWeekdayAvailability: true };
  let versionWrites = 0, directAcceptWrites = 0, directGiveWrites = 0;
  const approvals: Record<string, unknown>[] = [];
  const graph = { id: 1, containerId: 1, shopId: 1, name: "Contract graph", year: 2026, month: 12, publicationStatus: "public", allowSwap: true,
    peoplePerShift: 1, shift1Time: "09:00 - 17:00", shift2Time: "17:00 - 21:00", maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5,
    maxConsecutiveFull: 3, maxFullPerMonth: 10, availabilityGroupId: 1, note: null };
  const slots = [
    { id: 1, scheduleId: 1, dayOfMonth: 1, slotNo: 1, employeeId: 2, fromTime: "09:00", toTime: "17:00", status: "ASSIGNED" },
    { id: 2, scheduleId: 1, dayOfMonth: 2, slotNo: 1, employeeId: 1, fromTime: "09:00", toTime: "17:00", status: "ASSIGNED" },
  ];
  const employee = (id: number) => ({ id, firstName: id === 1 ? "Ada" : "Grace", lastName: "Synthetic", contractType: id === 1 ? contractType : "zlecenie", hasLoginAccount: false, isOnline: false });
  const offer = (id: number, slot: typeof slots[number], owner: number) => ({ id, scheduleId: 1, scheduleSlotId: slot.id, scheduleName: graph.name, containerName: "Synthetic container", shopName: "Synthetic shop",
    year: 2026, month: 12, dayOfMonth: slot.dayOfMonth, fromTime: slot.fromTime, toTime: slot.toTime, fromEmployeeId: owner, fromEmployeeName: owner === 1 ? "Ada Synthetic" : "Grace Synthetic",
    targetEmployeeId: null, targetEmployeeName: null, visibility: "public", status: "open", requestKind: "give", createdAtUtc: "2026-12-01T00:00:00Z", shiftHours: 8,
    currentEmployeeHoursBefore: 8, currentEmployeeHoursAfter: 16, currentEmployeeWorkDaysBefore: 1, currentEmployeeWorkDaysAfter: 2, currentEmployeeFreeDaysBefore: 30, currentEmployeeFreeDaysAfter: 29,
    fromEmployeeHoursBefore: 8, fromEmployeeHoursAfter: 0, canAccept: owner !== 1, canAcceptFull: true, allowPartialAcceptance: false,
    eligibleClaimWindows: [], canCancel: owner === 1, isCreatedByCurrentEmployee: owner === 1, isManagerCreated: false, isScheduleLocked: false });
  const offers: Record<string, unknown>[] = [offer(10, slots[0], 2)];
  const availabilityGroup = { id: 1, name: "December availability", year: 2026, month: 12, publicationStatus: "private" };
  const availabilityMembers = [{ id: 2, employeeId: 2, displayOrder: 0, employeeLastModifiedAtUtc: null }];
  const availabilitySlots: Array<{ id: number; availabilityGroupMemberId: number; dayOfMonth: number; kind: number; intervalStr: string | null }> = [];
  const errors: string[] = []; page.on("pageerror", error => errors.push(error.message));
  const activeRequests = new Set<Request>();
  page.on("request", request => { if (new URL(request.url()).pathname.startsWith("/api/")) activeRequests.add(request); });
  page.on("requestfinished", request => activeRequests.delete(request));
  page.on("requestfailed", request => activeRequests.delete(request));
  await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "isolated-contract-browser-test"));
  await page.route("**/api/**", async route => {
    const path = new URL(route.request().url()).pathname, method = route.request().method(); let body: unknown = [];
    if (!path.startsWith("/api/")) return route.continue();
    if (path === "/api/auth/session") body = actor === "manager" ? { role: "manager", workspaceMode: managerMode, managerId: 1, userName: "manager", displayName: "Synthetic" }
      : { role: "employee", employeeId: 1, userName: "ada", displayName: "Ada Synthetic" };
    else if (path === "/api/account-language") body = { language: "en" };
    else if (path === "/api/public-holiday-settings") body = { showPublicHolidays: true };
    else if (path === "/api/priority-generation-settings") body = { shiftHours: 8, weekendsOff: true, respectAvailability: true };
    else if (path === "/api/employment-contract-settings") { if (method === "PUT") settings = route.request().postDataJSON(); body = settings; }
    else if (path === "/api/employees") body = [employee(1),employee(2)];
    else if (path === "/api/employees/1") { if (method === "PUT") { contractType = route.request().postDataJSON().contractType; return route.fulfill({ status: 204 }); } body = employee(1); }
    else if (path === "/api/employee-profile/me") body = { employeeId: 1, username: "ada", displayName: "Ada Synthetic", contractType };
    else if (path === "/api/employee-ui-state") body = { scheduleColumnOrders: {}, readNotificationIds: [], pinnedSwapIds: [] };
    else if (path === "/api/employee-shift-swaps/employees") body = [{ ...employee(2), displayName: "Grace Synthetic" }];
    else if (path === "/api/employee-shift-swaps") {
      if (method === "POST") directGiveWrites++;
      body = offers.map(item => {
        const held = item.status === "open" && approvals.some(request => request.kind === "claim" && request.shiftSwapRequestId === item.id && request.status === "pending");
        return { ...item, requiresManagerApproval: item.status === "open" && contractType === "o_prace" && settings.requirePickupApproval,
          pickupApprovalPolicyApplies: settings.requirePickupApproval, isAwaitingManagerApproval: held,
          canAccept: !held && item.canAccept, canAcceptFull: !held, canRequestManagerApproval: !held,
          approvalPendingForCurrentEmployee: held };
      });
    }
    else if (path.endsWith("/accept")) { directAcceptWrites++; return route.fulfill({ status: 409, json: { detail: "Approval required" } }); }
    else if (path === "/api/employee-shift-swaps/preview") {
      const request = route.request().postDataJSON();
      body = { ...request, requestKind: "give", targetEmployeeName: "Everyone", isValid: true, requiresManagerException: settings.requireBelowMinimumGiveApproval,
        minimumMinutes: 1440, projectedMinutes: 480, projectedMonths: [], source: { scheduleId: 1, slotId: 2, scheduleName: graph.name, shopName: "Synthetic shop", year:2026,month:12,dayOfMonth:2,
          fromTime:request.fromTime,toTime:request.toTime,durationMinutes:480,canSelect:true } };
    }
    else if (path === "/api/employee-shift-swap-approvals") body = approvals;
    else if (path === "/api/employee-shift-swap-approvals/claims" || path.endsWith("/giveaway-exceptions")) {
      const command = route.request().postDataJSON(), claim = path.endsWith("/claims");
      const row = { id: approvals.length + 1, kind: claim ? "claim" : "below_minimum_give", status: "pending", employeeId:1,employeeName:"Ada Synthetic",scheduleId:1,scheduleSlotId:claim ? 1:2,
        scheduleName:graph.name,shopId:1,shopName:"Synthetic shop",year:2026,month:12,dayOfMonth:claim ? 1:2,fromTime:"09:00",toTime:"17:00",createdAtUtc:"2026-12-01T00:00:00Z",isStale:false,
        ...(claim ? {shiftSwapRequestId:command.shiftSwapRequestId} : {minimumMinutes:1440,projectedMinutes:480}) };
      approvals.push(row); return route.fulfill({status:201,json:row});
    }
    else if (path === "/api/employee-schedules") body = [{...graph,containerName:"Synthetic container",shopName:"Synthetic shop",employees:[1,2].map(id=>({...employee(id),employeeId:id,displayOrder:id,displayName:`${employee(id).firstName} Synthetic`})),slots}];
    else if (path.includes("/shift-swap-approvals")) {
      if (method === "POST") {
        const id = Number(path.split("/").at(-2)), row = approvals.find(request => request.id === id)!;
        row.status = path.endsWith("/approve") ? "approved" : "rejected";
        if (row.status === "approved" && row.kind === "claim") { slots[0].employeeId = 1; offers[0].status = "accepted"; versionWrites++; }
        if (row.status === "approved" && row.kind === "below_minimum_give") { offers.push(offer(11,slots[1],1)); row.resultingShiftSwapRequestId = 11; }
        body = row;
      } else body = approvals;
    }
    else if (path === "/api/shops") body = [{id:1,name:"Synthetic shop",address:"Synthetic"}];
    else if (path === "/api/containers/1") body = {id:1,name:"Synthetic container"};
    else if (path === "/api/containers/1/graphs") body = [graph];
    else if (path === "/api/containers/1/graphs/1") body = graph;
    else if (path === "/api/containers/1/graphs/1/employees") body = [1,2].map(id=>({id,scheduleId:1,employeeId:id,displayOrder:id,minHoursMonth:24,isPriority:false}));
    else if (path === "/api/containers/1/graphs/1/slots") body = slots;
    else if (path.includes("versions") && method === "POST") { versionWrites++; return route.fulfill({status:500,json:{detail:"Unexpected duplicate version write"}}); }
    else if (path === "/api/availability-groups") body = [availabilityGroup];
    else if (path === "/api/availability-groups/1") body = availabilityGroup;
    else if (path === "/api/availability-groups/1/items") body = availabilitySlots.map(slot => ({ ...slot, dayId: slot.id, memberId: slot.availabilityGroupMemberId,
      employeeId: availabilityMembers.find(member => member.id === slot.availabilityGroupMemberId)!.employeeId, displayOrder: availabilityMembers.find(member => member.id === slot.availabilityGroupMemberId)!.displayOrder }));
    else if (path === "/api/availability-groups/1/members") {
      if (method === "POST") { const member = { ...route.request().postDataJSON(), id: availabilityMembers.length + 2, employeeLastModifiedAtUtc: null }; availabilityMembers.push(member); body = member; }
      else body = availabilityMembers;
    }
    else if (path === "/api/availability-groups/1/slots") {
      if (method === "POST") { const slot = { ...route.request().postDataJSON(), id: availabilitySlots.length + 1 }; availabilitySlots.push(slot); body = slot; }
      else body = availabilitySlots;
    }
    else if (path === "/api/employee-availability") {
      const automatic = contractType === "o_prace" && settings.automaticWeekdayAvailability;
      const days = automatic ? Array.from({length:31},(_,i)=>({id:0,availabilityGroupMemberId:1,dayOfMonth:i+1,kind:[0,6].includes(new Date(2026,11,i+1).getDay())?1:0})) : [{id:1,availabilityGroupMemberId:1,dayOfMonth:1,kind:1}];
      body = [{id:1,name:"Contract availability",year:2026,month:12,isAutomatic:automatic,canSubmit:!automatic,isEditLocked:false,slots:days,members:[{employeeId:1,firstName:"Ada",lastName:"Synthetic",displayOrder:1,slots:days}]}];
    }
    else if (path.includes("negotiate")) return route.fulfill({status:503});
    else if (path.includes("notepad")) body = {notes:[],state:{isExpanded:false,isPinned:false,height:400}};
    else if (path === "/api/manager-profile/me") body = {id:1,userName:"manager",displayName:"Synthetic",createdAtUtc:"2026-01-01T00:00:00Z"};
    await route.fulfill({json:body});
  });
  const navigate = async (path: string) => {
    await page.evaluate(() => new Promise<void>(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve()))));
    await expect.poll(() => [...activeRequests].map(request => new URL(request.url()).pathname), {timeout:15000}).toEqual([]);
    await page.goto(path);
    await page.getByRole("main").first().waitFor({state:"visible"});
    const collapse = page.getByRole("button",{name:"Collapse sidebar",exact:true});
    if (await collapse.isVisible()) await collapse.click();
    await page.mouse.move(1350,900);
  };
  await navigate("/employee/1/edit");
  await page.getByRole("radio",{name:"Umowa o pracę",exact:true}).check();
  await page.getByRole("button",{name:"Save",exact:true}).click();
  await expect.poll(()=>contractType).toBe("o_prace");
  await expect(page).toHaveURL(/\/employee\/1$/);
  await expect(page.getByRole("heading",{name:"Employee Profile",exact:true})).toBeVisible();
  await navigate("/employee/1/edit"); await expect(page.getByRole("radio",{name:"Umowa o pracę",exact:true})).toBeChecked({timeout:15000});
  await navigate("/settings"); const eight = page.getByRole("switch",{name:"Whole eight-hour shifts only"});
  await eight.click(); await expect.poll(()=>settings.restrictSwapsToEightHours).toBe(false);
  expect(settings.requirePickupApproval).toBe(true); await expect(eight).toBeEnabled(); await eight.click();
  await expect.poll(()=>settings.restrictSwapsToEightHours).toBe(true);
  await navigate("/availability/1/edit");
  await page.getByRole("button", { name: "Expand Employee", exact: true }).click();
  await page.getByRole("button", { name: "employee list", exact: true }).click();
  await page.getByRole("option", { name: /Ada Synthetic/ }).click();
  await page.locator("section").filter({ has: page.getByRole("button", { name: "employee list", exact: true }) }).getByRole("button", { name: "Add", exact: true }).click();
  await expect(page.getByRole("button", { name: "Ada Synthetic day 1", exact: true })).toHaveText("+");
  await expect(page.getByRole("button", { name: "Ada Synthetic day 5", exact: true })).toHaveText("-");
  await expect(page.getByRole("textbox", { name: "Ada Synthetic day 1", exact: true })).toHaveCount(0);
  await page.getByRole("button", { name: "Save Changes", exact: true }).click();
  await expect(page).toHaveURL(/\/availability\/1$/);
  expect(availabilitySlots).toHaveLength(62);
  expect(availabilitySlots.every(slot => slot.kind === 1)).toBe(true);
  const managerCell = (day: number) => page.locator("tbody tr").nth(day - 1).locator("td").nth(1);
  await expect(managerCell(1)).toHaveText("+"); await expect(managerCell(5)).toHaveText("-");
  managerMode = "phone"; await page.setViewportSize({ width: 390, height: 844 }); await navigate("/availability/1");
  await expect(managerCell(1)).toHaveText("+"); await expect(managerCell(5)).toHaveText("-");
  managerMode = "pc"; await page.setViewportSize({ width: 1440, height: 1000 });
  actor="employee"; await navigate("/swap");
  await page.getByRole("button",{name:"View offer details for Contract graph"}).click();
  await page.getByRole("button",{name:"Request manager approval",exact:true}).click();
  await page.getByRole("dialog",{name:"Request manager approval?"}).getByRole("button",{name:"Request manager approval",exact:true}).click();
  await expect.poll(()=>approvals.length).toBe(1); expect(slots[0].employeeId).toBe(2); expect(directAcceptWrites).toBe(0);
  await expect(page.getByText("Awaiting approval", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "View offer details for Contract graph" }).click();
  await expect(page.getByRole("button", { name: "Awaiting manager approval", exact: true })).toBeDisabled();
  await page.getByRole("dialog", { name: "Offer details" }).getByRole("button", { name: "Close", exact: true }).click();
  actor="manager"; await navigate("/container/1/graphs/1/edit");
  await page.getByRole("button", { name: "Expand Publication", exact: true }).waitFor({ state: "visible", timeout: 30_000 });
  const preview = page.locator("section").filter({ has: page.getByText("Availability Preview", { exact: true }) });
  await expect(preview.getByRole("button", { name: "Ada Synthetic day 1", exact: true })).toHaveText("+");
  await expect(preview.getByRole("button", { name: "Ada Synthetic day 5", exact: true })).toHaveText("-");
  await page.getByRole("button",{name:"Expand Publication",exact:true}).click();
  const reviews=page.getByRole("region",{name:"Contract approvals",exact:true});
  await reviews.getByRole("button",{name:/Ada Synthetic.*Pending/}).click();
  await reviews.getByRole("button",{name:"Approve",exact:true}).click();
  await page.getByRole("dialog",{name:"Approve shift transfer?"}).getByRole("button",{name:"Approve",exact:true}).click();
  await expect.poll(()=>slots[0].employeeId).toBe(1); expect(versionWrites).toBe(1);
  actor="employee"; await navigate("/swap"); await page.getByRole("button",{name:"Create offer",exact:true}).click();
  await page.getByRole("button",{name:/Contract graph/}).click();
  await page.getByRole("radio",{name:/02\.12\.2026.*09:00.*17:00/}).click();
  await page.getByRole("button",{name:"Confirm shift",exact:true}).click();
  await page.getByRole("button",{name:"Continue",exact:true}).click(); await page.getByRole("button",{name:"Continue",exact:true}).click();
  await expect(page.getByRole("note")).toHaveText("Manager approval is required when the shift owner or recipient has an employment contract.");
  await expect(page.getByRole("button",{name:"Send request",exact:true})).toBeDisabled();
  await page.getByRole("button",{name:"Request exception",exact:true}).click();
  await expect.poll(()=>approvals.length).toBe(2); expect(directGiveWrites).toBe(0); expect(offers).toHaveLength(1);
  actor="manager"; await navigate("/container/1/graphs/1/edit"); await page.getByRole("button",{name:"Expand Publication",exact:true}).click();
  await page.getByRole("region",{name:"Contract approvals",exact:true}).getByRole("button",{name:/Ada Synthetic.*Pending/}).click();
  await page.getByRole("region",{name:"Contract approvals",exact:true}).getByRole("button",{name:"Approve",exact:true}).click();
  await expect.poll(()=>offers.length).toBe(2); expect(slots[1].employeeId).toBe(1); expect(versionWrites).toBe(1);
  actor="employee"; await page.setViewportSize({width:390,height:844}); await navigate("/availability"); await page.getByRole("button",{name:/Contract availability/}).click();
  await expect(page.getByText("Availability set automatically by contract: + Mon–Fri, − Sat–Sun",{exact:true})).toBeVisible();
  expect(await page.getByRole("button",{name:"Save",exact:true}).count()).toBe(0);
  actor="manager"; await page.setViewportSize({width:1440,height:1000}); await navigate("/settings"); await page.getByRole("switch",{name:"Automatic weekday availability",exact:true}).click();
  await expect.poll(()=>settings.automaticWeekdayAvailability).toBe(false);
  await navigate("/availability/1"); await expect(managerCell(1)).toHaveText("-");
  expect(availabilitySlots.every(slot => slot.kind === 1)).toBe(true);
  expect(availabilityMembers.every(member => member.employeeLastModifiedAtUtc === null)).toBe(true);
  actor="employee"; await page.setViewportSize({width:390,height:844}); await navigate("/availability"); await page.getByRole("button",{name:/Contract availability/}).click();
  await expect(page.getByText("Availability set automatically by contract: + Mon–Fri, − Sat–Sun",{exact:true})).toHaveCount(0);
  expect(errors).toEqual([]);
});

test.describe("real pickup approval with local accounts", () => {
  test.skip(!process.env.GF3_PICKUP_LIVE_CONFIG, "Requires an isolated local database and temporary account configuration.");
  test.describe.configure({ mode: "serial" });

  const translations: Record<string, string>[] = ["pl", "uk"].map(language =>
    JSON.parse(readFileSync(new URL(`../src/shared/i18n/${language}.json`, import.meta.url), "utf8")));
  const label = (key: string) => new RegExp(`^(?:${[key, ...translations.map(messages => messages[key] ?? key)]
    .map(value => value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")).join("|")})$`);
  type Credentials = { username: string; password: string };
  type LiveFixture = { containerId: number; scheduleId: number; slotIds: number[]; days: number[]; year: number; month: number; name: string };
  type LiveConfig = { baseURL: string; databasePath: string; ownerId: number; recipientId: number;
    credentials: Record<"manager" | "owner" | "recipient", Credentials>; fixtures: Record<string, LiveFixture> };
  type DbState = { owner: number; versions: number; histories: number; indexExists: boolean;
    offer: { status: string; acceptedBy: number | null } | null;
    approvals: Array<{ id: number; status: string; employeeId: number }> };
  const readState = (config: LiveConfig, fixture: LiveFixture, slotId: number, offerId: number): DbState => JSON.parse(execFileSync(
    process.env.GF3_PICKUP_PYTHON ?? "python", ["-c", `
import json,sqlite3,sys
from pathlib import Path
with sqlite3.connect(Path(sys.argv[1]).as_uri()+'?mode=ro',uri=True) as db:
    sid,slot,offer=map(int,sys.argv[2:])
    row=db.execute('select status,accepted_by_employee_id from shift_swap_request where id=?',(offer,)).fetchone()
    print(json.dumps({'owner':db.execute('select employee_id from schedule_slot where id=?',(slot,)).fetchone()[0],
      'versions':db.execute('select count(*) from schedule_version where schedule_id=?',(sid,)).fetchone()[0],
      'histories':db.execute('select count(*) from shift_swap_history where source_shift_swap_request_id=?',(offer,)).fetchone()[0],
      'indexExists':bool(db.execute("select 1 from sqlite_master where type='index' and name='ux_contract_approval_one_pending_claim_per_offer'").fetchone()),
      'offer':{'status':row[0],'acceptedBy':row[1]} if row else None,
      'approvals':[{'id':r[0],'status':r[1],'employeeId':r[2]} for r in db.execute('select id,status,employee_id from contract_shift_approval_request where shift_swap_request_id=? order by id',(offer,))]}))
`, config.databasePath, String(fixture.scheduleId), String(slotId), String(offerId)], { encoding: "utf8" }));

  for (const action of ["approve", "reject"] as const) {
    test(`opr publishes, amin requests, manager ${action}s using the actual API and database`, async ({ browser }, testInfo) => {
      test.setTimeout(180_000);
      const config: LiveConfig = JSON.parse(readFileSync(process.env.GF3_PICKUP_LIVE_CONFIG!, "utf8").replace(/^\uFEFF/, ""));
      expect(["127.0.0.1", "localhost"]).toContain(new URL(config.baseURL).hostname);
      const fixture = config.fixtures[testInfo.project.name];
      expect(fixture.name).toMatch(/^__pickup_debug_/);
      const slotIndex = action === "approve" ? 0 : 1, slotId = fixture.slotIds[slotIndex], day = fixture.days[slotIndex];
      const contexts: Awaited<ReturnType<typeof browser.newContext>>[] = [];
      const evidence: Record<string, unknown> = { action, scheduleId: fixture.scheduleId, slotId, ownerId: config.ownerId, recipientId: config.recipientId };
      const api = async (page: import("@playwright/test").Page, method: string, path: string, body?: unknown) => page.evaluate(async input => {
        const response = await fetch(`/api${input.path}`, { method: input.method,
          headers: { Authorization: `Bearer ${localStorage.getItem("gf3.auth.access-token")}`, "Content-Type": "application/json" },
          ...(input.body === undefined ? {} : { body: JSON.stringify(input.body) }) });
        return { status: response.status, body: await response.json() };
      }, { method, path, body });
      const login = async (role: "manager" | "owner" | "recipient") => {
        const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } }); contexts.push(context);
        const page = await context.newPage(); page.setDefaultTimeout(15_000);
        try {
          await page.goto(`${config.baseURL}/login`);
          await page.locator("#login-username").fill(config.credentials[role].username);
          await page.locator("#login-password").fill(config.credentials[role].password);
          const [loginResponse] = await Promise.all([
            page.waitForResponse(response => response.request().method() === "POST" && new URL(response.url()).pathname === "/api/auth/login"),
            page.locator("button[type=submit]").click(),
          ]);
          expect(loginResponse.status(), `Local ${role} test account login must succeed before exercising pickups`).toBe(200);
          await page.waitForURL(url => !url.pathname.includes("login"));
          if (role === "manager") {
            await Promise.all([
              page.waitForResponse(response => response.request().method() === "POST" && new URL(response.url()).pathname === "/api/auth/manager-mode"),
              page.getByRole("button").filter({ has: page.getByText(label("PC"), { exact: true }) }).click(),
            ]);
            await expect(page.getByRole("heading", { name: label("Choose workspace") })).toHaveCount(0);
          }
          return page;
        } catch (error) {
          const message = Object.values(config.credentials).reduce((text, credentials) => text.split(credentials.password).join("[redacted]"), String(error));
          throw new Error(message);
        }
      };
      try {
        const manager = await login("manager"); console.log(`pickup-live ${action}: manager authenticated`); await manager.goto(`${config.baseURL}/settings`);
        await expect(manager.getByRole("switch", { name: label("Manager approval for pickups") })).toBeChecked();
        const owner = await login("owner"), recipient = await login("recipient");
        expect((await api(owner, "GET", "/employee-profile/me")).body.contractType).toBe("o_prace");
        expect((await api(recipient, "GET", "/employee-profile/me")).body.contractType).toBe("zlecenie");
        await owner.goto(`${config.baseURL}/swap`);
        await owner.getByRole("button", { name: label("Create offer") }).click();
        await owner.getByRole("dialog").getByRole("button", { name: new RegExp(fixture.name) }).click();
        const date = `${String(day).padStart(2, "0")}\\.${String(fixture.month).padStart(2, "0")}\\.${fixture.year}`;
        await owner.getByRole("radio", { name: new RegExp(`${date}.*09:00.*17:00`) }).click();
        await owner.getByRole("button", { name: label("Confirm shift") }).click();
        await owner.getByRole("button", { name: label("Continue") }).click(); await owner.getByRole("button", { name: label("Continue") }).click();
        const [published] = await Promise.all([
          owner.waitForResponse(response => response.request().method() === "POST" && new URL(response.url()).pathname === "/api/employee-shift-swaps"),
          owner.getByRole("button", { name: label("Send request") }).click(),
        ]);
        expect(published.status()).toBe(201); const offer = await published.json(); evidence.offerId = offer.id; console.log(`pickup-live ${action}: offer ${offer.id} published`);
        const before = readState(config, fixture, slotId, offer.id); evidence.before = before;
        expect(before.owner).toBe(config.ownerId); expect(before.histories).toBe(0); expect(before.approvals).toHaveLength(0); expect(before.indexExists).toBe(true);
        await recipient.goto(`${config.baseURL}/swap`);
        const visible = (await api(recipient, "GET", "/employee-shift-swaps")).body.find((item: { id: number }) => item.id === offer.id);
        expect(visible.requiresManagerApproval).toBe(true); expect(visible.canRequestManagerApproval).toBe(true);
        const bypass = await api(recipient, "POST", `/employee-shift-swaps/${offer.id}/accept`, { requiresManagerApproval: false, managerApproved: true });
        expect(bypass.status).toBe(409); expect(bypass.body.code).toBe("CONTRACT_CLAIM_APPROVAL_REQUIRED");
        expect(readState(config, fixture, slotId, offer.id)).toEqual(before);
        const card = recipient.locator(`[data-swap-id="${offer.id}"]`);
        await expect(card.getByText(label("Manager approval"), { exact: true })).toBeVisible();
        await card.locator("button").first().click();
        expect(await recipient.getByRole("button", { name: label("Accept shift") }).count()).toBe(0);
        await recipient.getByRole("button", { name: label("Request manager approval") }).click();
        const [claimResponse] = await Promise.all([
          recipient.waitForResponse(response => response.request().method() === "POST" && new URL(response.url()).pathname === "/api/employee-shift-swap-approvals/claims"),
          recipient.getByRole("dialog").getByRole("button", { name: label("Request manager approval") }).click(),
        ]);
        expect(claimResponse.status()).toBe(201); const claim = await claimResponse.json(); evidence.approvalId = claim.id; console.log(`pickup-live ${action}: claim ${claim.id} pending`);
        const pending = readState(config, fixture, slotId, offer.id); evidence.pending = pending;
        expect(pending.owner).toBe(config.ownerId); expect(pending.versions).toBe(before.versions); expect(pending.histories).toBe(0);
        expect(pending.offer?.status).toBe("Open"); expect(pending.approvals).toEqual([{ id: claim.id, status: "pending", employeeId: config.recipientId }]);
        const duplicate = await api(recipient, "POST", "/employee-shift-swap-approvals/claims", { shiftSwapRequestId: offer.id });
        expect(duplicate.status).toBe(409);
        const heldAccept = await api(recipient, "POST", `/employee-shift-swaps/${offer.id}/accept`);
        expect(heldAccept.status).toBe(409); expect(heldAccept.body.code).toBe("CONTRACT_OFFER_PENDING_APPROVAL");
        expect(readState(config, fixture, slotId, offer.id)).toEqual(pending);
        const ownVisible = (await api(owner, "GET", "/employee-shift-swaps")).body.find((item: { id: number }) => item.id === offer.id);
        expect(ownVisible.isAwaitingManagerApproval).toBe(true);
        await manager.goto(`${config.baseURL}/container/${fixture.containerId}/graphs/${fixture.scheduleId}/edit`);
        const collapseSidebar = manager.getByRole("button", { name: label("Collapse sidebar") });
        await collapseSidebar.waitFor({ state: "visible" });
        await collapseSidebar.click();
        await expect(manager.getByRole("button", { name: label("Open sidebar") })).toBeVisible();
        await manager.mouse.move(1350, 900);
        await manager.getByRole("button", { name: label("Expand Publication") }).click(); console.log(`pickup-live ${action}: Publication opened`);
        const reviews = manager.getByRole("region", { name: label("Contract approvals") });
        await reviews.locator(`button[aria-controls="contract-approval-${claim.id}"]`).click();
        const row = reviews.locator("article").filter({ hasText: new RegExp(`#${offer.id}(?!\\d)`) });
        await expect(row.getByText(label("Pending"), { exact: true })).toBeVisible();
        console.log(`pickup-live ${action}: review row visible, approve disabled=${await row.getByRole("button", { name: label("Approve") }).isDisabled()}`);
        await testInfo.attach("manager-publication-state", { body: await reviews.innerText(), contentType: "text/plain" });
        const [reviewResponse] = await Promise.all([
          manager.waitForResponse(response => response.request().method() === "POST" && new URL(response.url()).pathname.endsWith(`/shift-swap-approvals/${claim.id}/${action}`)),
          (async () => {
            await row.getByRole("button", { name: label(action === "approve" ? "Approve" : "Reject") }).click();
            if (action === "approve") await manager.getByRole("dialog", { name: label("Approve shift transfer?") }).getByRole("button", { name: label("Approve") }).click();
          })(),
        ]);
        expect(reviewResponse.status()).toBe(200); console.log(`pickup-live ${action}: review HTTP 200`);
        const final = readState(config, fixture, slotId, offer.id); evidence.final = final;
        expect(final.approvals[0].status).toBe(action === "approve" ? "approved" : "rejected");
        if (action === "approve") {
          expect(final.owner).toBe(config.recipientId); expect(final.histories).toBe(1); expect(final.versions).toBeGreaterThan(before.versions);
          expect(final.offer?.status).toBe("Accepted"); expect(final.offer?.acceptedBy).toBe(config.recipientId);
          const repeated = await api(manager, "POST", `/containers/${fixture.containerId}/graphs/${fixture.scheduleId}/shift-swap-approvals/${claim.id}/approve`);
          expect(repeated.status).toBe(409); expect(readState(config, fixture, slotId, offer.id)).toEqual(final);
          for (const page of [owner, recipient]) {
            const schedules = await api(page, "GET", "/employee-schedules");
            expect(schedules.body.find((schedule: { id: number }) => schedule.id === fixture.scheduleId).slots
              .find((slot: { id: number }) => slot.id === slotId).employeeId).toBe(config.recipientId);
            await page.goto(`${config.baseURL}/swap`); await page.getByRole("button", { name: label("Swap history") }).click();
            await expect(page.locator(`[data-swap-id="${offer.id}"]`).getByText(label("Accepted"), { exact: true })).toBeVisible();
          }
        } else {
          expect(final.owner).toBe(config.ownerId); expect(final.histories).toBe(0); expect(final.versions).toBe(before.versions); expect(final.offer?.status).toBe("Open");
          await manager.goto(`${config.baseURL}/settings`);
          await expect.poll(async () => {
            const reopened = (await api(recipient, "GET", "/employee-shift-swaps")).body.find((item: { id: number }) => item.id === offer.id);
            expect(reopened.isAwaitingManagerApproval).toBe(false);
            return reopened.canRequestManagerApproval;
          }, { timeout: 30_000 }).toBe(true);
          await recipient.goto(`${config.baseURL}/swap`);
          await recipient.locator(`[data-swap-id="${offer.id}"]`).locator("button").first().click();
          await expect(recipient.getByRole("button", { name: label("Request manager approval") })).toBeEnabled();
        }
      } finally {
        await testInfo.attach("pickup-database-evidence", { body: JSON.stringify(evidence, null, 2), contentType: "application/json" });
        await Promise.all(contexts.map(context => context.close()));
      }
    });
  }
});

test("contract approval cards show participants and remain usable at narrow sidebar widths", async ({ page }, testInfo) => {
  test.setTimeout(120_000);
  await page.setViewportSize({width:1440,height:1050});
  const graph={id:1,containerId:1,shopId:1,name:"F35",year:2026,month:10,publicationStatus:"public",allowSwap:true,peoplePerShift:1,
    shift1Time:"09:00 - 17:00",shift2Time:"17:00 - 21:00",maxHoursPerEmpMonth:160,maxConsecutiveDays:5,maxConsecutiveFull:3,maxFullPerMonth:10,availabilityGroupId:null,note:null};
  const base={kind:"claim",employeeId:2,employeeName:"ANNA MINIEIEVA",fromEmployeeId:1,fromEmployeeName:"OLEH PROTSUN",toEmployeeId:2,toEmployeeName:"ANNA MINIEIEVA",
    scheduleId:1,scheduleSlotId:1,scheduleName:"F35",shopId:1,shopName:"F35",year:2026,month:10,dayOfMonth:5,fromTime:"09:00",toTime:"15:00",createdAtUtc:"2026-10-03T10:20:00Z",isStale:false};
  const approvals=[{...base,id:1,status:"pending",shiftSwapRequestId:15},
    {...base,id:2,status:"pending",employeeName:"Aleksandra Katarzyna Verylongsurname-Piotrowska",fromEmployeeName:"Władysław Aleksander Verylongsurname-Kowalski",toEmployeeName:"Aleksandra Katarzyna Verylongsurname-Piotrowska",requestKind:"exchange",returnDayOfMonth:7,returnMonth:10,returnYear:2026,returnFromTime:"12:00",returnToTime:"18:00"},
    ...["approved","rejected","cancelled","expired"].map((status,index)=>({...base,id:index+3,status}))];
  const mutations:string[]=[]; const errors:string[]=[]; page.on("pageerror",error=>errors.push(error.message));
  await page.addInitScript(()=>localStorage.setItem("gf3.auth.access-token","isolated-approval-ui"));
  await page.route("**/api/**",async route=>{
    const path=new URL(route.request().url()).pathname; if(!path.startsWith("/api/")) return route.continue(); let body:unknown=[];
    if(path==="/api/auth/session") body={role:"manager",workspaceMode:"pc",managerId:1,userName:"synthetic",displayName:"Synthetic"};
    else if(path==="/api/account-language") body={language:"en"};
    else if(path==="/api/containers/1") body={id:1,name:"Synthetic container"};
    else if(path==="/api/shops") body=[{id:1,name:"F35",address:"Synthetic"}];
    else if(path==="/api/employees") body=[{id:1,firstName:"OLEH",lastName:"PROTSUN",contractType:"o_prace"},{id:2,firstName:"ANNA",lastName:"MINIEIEVA",contractType:"zlecenie"}];
    else if(path==="/api/containers/1/graphs") body=[graph];
    else if(path==="/api/containers/1/graphs/1") body=graph;
    else if(path.endsWith("/shift-swap-approvals")) body=approvals;
    else if(/\/shift-swap-approvals\/\d+\/(approve|reject)$/.test(path)) {
      const row=approvals.find(item=>item.id===Number(path.split("/").at(-2)))!;
      row.status=path.endsWith("/approve")?"approved":"rejected"; mutations.push(path);body=row;
    }
    else if(path.includes("negotiate")) return route.fulfill({status:503});
    else if(path.includes("notepad")) body={notes:[],state:{isExpanded:false,isPinned:false,height:400}};
    await route.fulfill({json:body});
  });
  await page.goto("/container/1/graphs/1/edit");
  const sidebar=page.getByRole("button",{name:"Collapse sidebar",exact:true}); await sidebar.waitFor({state:"visible"}); await sidebar.click();
  await expect(page.getByRole("button",{name:"Open sidebar",exact:true})).toBeVisible(); await page.mouse.move(1350,900);
  await page.getByRole("button",{name:"Expand Publication",exact:true}).click({timeout:15000});
  const section=page.getByRole("region",{name:"Contract approvals",exact:true});
  await expect(section.getByRole("button",{name:/^ANNA MINIEIEVA/}).first()).toBeVisible();
  for(const width of [280,320,380,450]) {
    await section.evaluate((element,width)=>{
      const sidebar=element.closest<HTMLElement>('[class*="sidebarColumn_"]')!;
      Object.assign(sidebar.style,{width:`${width}px`,maxWidth:`${width}px`,flexBasis:`${width}px`});
    },width);
    expect(await section.evaluate(element=>Math.round(element.closest('[class*="sidebarColumn_"]')!.getBoundingClientRect().width))).toBe(width);
    await section.screenshot({path:testInfo.outputPath(`approvals-${width}-collapsed.png`)});
    await section.getByRole("button",{name:/^ANNA MINIEIEVA/}).first().click();
    await expect(section.getByRole("group",{name:"Shift transfer"})).toContainText("OLEH PROTSUN");
    await expect(section.getByRole("group",{name:"Shift transfer"})).toContainText("ANNA MINIEIEVA");
    expect(await section.evaluate(element=>element.scrollWidth<=element.clientWidth+1)).toBe(true);
    expect(await section.locator("article").evaluate(element=>element.scrollWidth<=element.clientWidth+1)).toBe(true);
    await section.screenshot({path:testInfo.outputPath(`approvals-${width}-expanded.png`)});
    await section.getByRole("button",{name:/Collapse request/}).click();
    await section.getByRole("button",{name:/^Aleksandra/}).click();
    expect(await section.locator("article").evaluate(element=>element.scrollWidth<=element.clientWidth+1)).toBe(true);
    await expect(section.getByRole("group",{name:"Return shift"})).toContainText("Władysław");
    await section.screenshot({path:testInfo.outputPath(`approvals-${width}-exchange.png`)});
    await section.getByRole("button",{name:"Approve",exact:true}).scrollIntoViewIfNeeded();
    await section.screenshot({path:testInfo.outputPath(`approvals-${width}-exchange-actions.png`)});
    await section.getByRole("button",{name:/Collapse request/}).click();
  }
  await section.getByRole("button",{name:/^ANNA MINIEIEVA/}).first().click();
  await section.getByRole("button",{name:"Approve",exact:true}).click(); expect(mutations).toHaveLength(0);
  await page.getByRole("dialog",{name:"Approve shift transfer?"}).getByRole("button",{name:"Approve",exact:true}).click();
  await expect.poll(()=>mutations.length).toBe(1); await expect(section.locator("article")).toContainText("Approved");
  await section.getByRole("button",{name:/^Aleksandra/}).click(); await section.getByRole("button",{name:"Reject",exact:true}).click();
  await expect.poll(()=>mutations.length).toBe(2); await expect(section.locator("article")).toContainText("Rejected"); expect(errors).toEqual([]);
});
