import { expect, test, type Request } from "@playwright/test";

test("isolated contract workflow: manager settings, pending pickup, publication review, exception and availability", async ({ page }) => {
  test.setTimeout(120_000);
  await page.setViewportSize({ width: 1440, height: 1000 });
  let actor: "manager" | "employee" = "manager";
  let contractType = "zlecenie";
  let settings = { restrictSwapsToEightHours: true, requirePickupApproval: true, requireBelowMinimumGiveApproval: true, automaticWeekdayAvailability: true };
  let versionWrites = 0, directAcceptWrites = 0, directGiveWrites = 0;
  const approvals: Record<string, unknown>[] = [];
  const graph = { id: 1, containerId: 1, shopId: 1, name: "Contract graph", year: 2026, month: 10, publicationStatus: "public", allowSwap: true,
    peoplePerShift: 1, shift1Time: "09:00 - 17:00", shift2Time: "17:00 - 21:00", maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5,
    maxConsecutiveFull: 3, maxFullPerMonth: 10, availabilityGroupId: null, note: null };
  const slots = [
    { id: 1, scheduleId: 1, dayOfMonth: 1, slotNo: 1, employeeId: 2, fromTime: "09:00", toTime: "17:00", status: "ASSIGNED" },
    { id: 2, scheduleId: 1, dayOfMonth: 2, slotNo: 1, employeeId: 1, fromTime: "09:00", toTime: "17:00", status: "ASSIGNED" },
  ];
  const employee = (id: number) => ({ id, firstName: id === 1 ? "Ada" : "Grace", lastName: "Synthetic", contractType: id === 1 ? contractType : "zlecenie", hasLoginAccount: false, isOnline: false });
  const offer = (id: number, slot: typeof slots[number], owner: number) => ({ id, scheduleId: 1, scheduleSlotId: slot.id, scheduleName: graph.name, containerName: "Synthetic container", shopName: "Synthetic shop",
    year: 2026, month: 10, dayOfMonth: slot.dayOfMonth, fromTime: slot.fromTime, toTime: slot.toTime, fromEmployeeId: owner, fromEmployeeName: owner === 1 ? "Ada Synthetic" : "Grace Synthetic",
    targetEmployeeId: null, targetEmployeeName: null, visibility: "public", status: "open", requestKind: "give", createdAtUtc: "2026-10-01T00:00:00Z", shiftHours: 8,
    currentEmployeeHoursBefore: 8, currentEmployeeHoursAfter: 16, currentEmployeeWorkDaysBefore: 1, currentEmployeeWorkDaysAfter: 2, currentEmployeeFreeDaysBefore: 30, currentEmployeeFreeDaysAfter: 29,
    fromEmployeeHoursBefore: 8, fromEmployeeHoursAfter: 0, canAccept: owner !== 1, canAcceptFull: true, allowPartialAcceptance: false,
    eligibleClaimWindows: [], canCancel: owner === 1, isCreatedByCurrentEmployee: owner === 1, isManagerCreated: false, isScheduleLocked: false });
  const offers: Record<string, unknown>[] = [offer(10, slots[0], 2)];
  const errors: string[] = []; page.on("pageerror", error => errors.push(error.message));
  const activeRequests = new Set<Request>();
  page.on("request", request => { if (new URL(request.url()).pathname.startsWith("/api/")) activeRequests.add(request); });
  page.on("requestfinished", request => activeRequests.delete(request));
  page.on("requestfailed", request => activeRequests.delete(request));
  await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "isolated-contract-browser-test"));
  await page.route("**/api/**", async route => {
    const path = new URL(route.request().url()).pathname, method = route.request().method(); let body: unknown = [];
    if (!path.startsWith("/api/")) return route.continue();
    if (path === "/api/auth/session") body = actor === "manager" ? { role: "manager", workspaceMode: "pc", managerId: 1, userName: "manager", displayName: "Synthetic" }
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
      body = offers.map(item => ({ ...item, requiresManagerApproval: item.status === "open" && contractType === "o_prace" && settings.requirePickupApproval,
        canRequestManagerApproval: true, approvalPendingForCurrentEmployee: approvals.some(request => request.shiftSwapRequestId === item.id && request.status === "pending") }));
    }
    else if (path.endsWith("/accept")) { directAcceptWrites++; return route.fulfill({ status: 409, json: { detail: "Approval required" } }); }
    else if (path === "/api/employee-shift-swaps/preview") {
      const request = route.request().postDataJSON();
      body = { ...request, requestKind: "give", targetEmployeeName: "Everyone", isValid: true, requiresManagerException: settings.requireBelowMinimumGiveApproval,
        minimumMinutes: 1440, projectedMinutes: 480, projectedMonths: [], source: { scheduleId: 1, slotId: 2, scheduleName: graph.name, shopName: "Synthetic shop", year:2026,month:10,dayOfMonth:2,
          fromTime:request.fromTime,toTime:request.toTime,durationMinutes:480,canSelect:true } };
    }
    else if (path === "/api/employee-shift-swap-approvals") body = approvals;
    else if (path === "/api/employee-shift-swap-approvals/claims" || path.endsWith("/giveaway-exceptions")) {
      const command = route.request().postDataJSON(), claim = path.endsWith("/claims");
      const row = { id: approvals.length + 1, kind: claim ? "claim" : "below_minimum_give", status: "pending", employeeId:1,employeeName:"Ada Synthetic",scheduleId:1,scheduleSlotId:claim ? 1:2,
        scheduleName:graph.name,shopId:1,shopName:"Synthetic shop",year:2026,month:10,dayOfMonth:claim ? 1:2,fromTime:"09:00",toTime:"17:00",createdAtUtc:"2026-10-01T00:00:00Z",isStale:false,
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
    else if (path === "/api/employee-availability") {
      const automatic = contractType === "o_prace" && settings.automaticWeekdayAvailability;
      const days = automatic ? Array.from({length:31},(_,i)=>({id:0,availabilityGroupMemberId:1,dayOfMonth:i+1,kind:[0,6].includes(new Date(2026,9,i+1).getDay())?1:0})) : [{id:1,availabilityGroupMemberId:1,dayOfMonth:1,kind:1}];
      body = [{id:1,name:"Contract availability",year:2026,month:10,isAutomatic:automatic,canSubmit:!automatic,isEditLocked:false,slots:days,members:[{employeeId:1,firstName:"Ada",lastName:"Synthetic",displayOrder:1,slots:days}]}];
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
  actor="employee"; await navigate("/swap");
  await page.getByRole("button",{name:"View offer details for Contract graph"}).click();
  await page.getByRole("button",{name:"Request manager approval",exact:true}).click();
  await page.getByRole("dialog",{name:"Request manager approval?"}).getByRole("button",{name:"Request manager approval",exact:true}).click();
  await expect.poll(()=>approvals.length).toBe(1); expect(slots[0].employeeId).toBe(2); expect(directAcceptWrites).toBe(0);
  actor="manager"; await navigate("/container/1/graphs/1/edit");
  await page.getByRole("button",{name:"Expand Publication",exact:true}).click();
  const reviews=page.getByRole("region",{name:"Contract approvals",exact:true});
  await reviews.getByRole("button",{name:"Approve",exact:true}).click();
  await page.getByRole("dialog",{name:"Approve shift transfer?"}).getByRole("button",{name:"Approve",exact:true}).click();
  await expect.poll(()=>slots[0].employeeId).toBe(1); expect(versionWrites).toBe(1);
  actor="employee"; await navigate("/swap"); await page.getByRole("button",{name:"Create offer",exact:true}).click();
  await page.getByRole("button",{name:/Contract graph/}).click();
  await page.getByRole("radio",{name:/02\.10\.2026.*09:00.*17:00/}).click();
  await page.getByRole("button",{name:"Confirm shift",exact:true}).click();
  await page.getByRole("button",{name:"Continue",exact:true}).click(); await page.getByRole("button",{name:"Continue",exact:true}).click();
  await expect(page.getByRole("button",{name:"Send request",exact:true})).toBeDisabled();
  await page.getByRole("button",{name:"Request exception",exact:true}).click();
  await expect.poll(()=>approvals.length).toBe(2); expect(directGiveWrites).toBe(0); expect(offers).toHaveLength(1);
  actor="manager"; await navigate("/container/1/graphs/1/edit"); await page.getByRole("button",{name:"Expand Publication",exact:true}).click();
  await page.getByRole("region",{name:"Contract approvals",exact:true}).getByRole("button",{name:"Approve",exact:true}).click();
  await expect.poll(()=>offers.length).toBe(2); expect(slots[1].employeeId).toBe(1); expect(versionWrites).toBe(1);
  actor="employee"; await page.setViewportSize({width:390,height:844}); await navigate("/availability"); await page.getByRole("button",{name:/Contract availability/}).click();
  await expect(page.getByText("Availability set automatically by contract: + Mon–Fri, − Sat–Sun",{exact:true})).toBeVisible();
  expect(await page.getByRole("button",{name:"Save",exact:true}).count()).toBe(0);
  actor="manager"; await page.setViewportSize({width:1440,height:1000}); await navigate("/settings"); await page.getByRole("switch",{name:"Automatic weekday availability",exact:true}).click();
  await expect.poll(()=>settings.automaticWeekdayAvailability).toBe(false);
  actor="employee"; await page.setViewportSize({width:390,height:844}); await navigate("/availability"); await page.getByRole("button",{name:/Contract availability/}).click();
  await expect(page.getByText("Availability set automatically by contract: + Mon–Fri, − Sat–Sun",{exact:true})).toHaveCount(0);
  expect(errors).toEqual([]);
});
