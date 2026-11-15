# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: manager-workspace-modes.spec.ts >> Manager schedule sticky axes and mobile summary at 375px with 1 long names
- Location: e2e\manager-workspace-modes.spec.ts:318:3

# Error details

```
Error: locator.scrollIntoViewIfNeeded: Element is not attached to the DOM
Call log:
  - attempting scroll into view action
    - waiting for element to be stable

```

# Page snapshot

```yaml
- generic [ref=e3]:
  - button "Open navigation":
    - img
  - generic [ref=e4]:
    - main [ref=e5]:
      - generic [ref=e7]:
        - link "Back" [ref=e9]:
          - /url: /container/1
          - img [ref=e10]
          - text: Back
        - navigation [ref=e12]:
          - link "Public schedule" [ref=e13]:
            - /url: /container/1/graphs/6
        - generic [ref=e14]:
          - generic [ref=e15]:
            - complementary [ref=e16]:
              - generic [ref=e17]:
                - button "Expand Schedule Information" [ref=e18] [cursor=pointer]:
                  - img [ref=e19]
                - generic:
                  - generic:
                    - generic:
                      - generic:
                        - generic:
                          - img
                          - generic: Schedule Information
                        - generic:
                          - button "Collapse Schedule Information":
                            - img
                      - generic:
                        - generic:
                          - generic: PS
                          - generic:
                            - heading "Private schedule" [level=2]
                            - paragraph: ID 3
                        - generic:
                          - generic: Note
                          - generic: No notes yet.
                        - group "Schedule details":
                          - generic:
                            - generic: Month
                            - strong: October
                          - generic:
                            - generic: Year
                            - strong: "2026"
                          - generic:
                            - generic: Shop
                            - strong: Central
                          - generic:
                            - generic: Status
                            - strong: Private
                          - generic:
                            - generic: People on Shift
                            - strong: "1"
                          - generic:
                            - generic: Shift1
                            - strong: 08:00 - 16:00
                          - generic:
                            - generic: Shift2
                            - strong: 16:00 - 20:00
                          - generic:
                            - generic: Max Consecutive Days
                            - strong: "5"
                          - generic:
                            - generic: Max Consecutive Full
                            - strong: "3"
                          - generic:
                            - generic: Max Full
                            - strong: "10"
                          - generic:
                            - generic: Availability
                            - strong: None
                        - 'generic "Last Update: Not recorded yet"':
                          - generic: Last Update
                          - strong: Not recorded yet
            - generic [ref=e22]:
              - generic [ref=e23]:
                - generic [ref=e24]:
                  - img [ref=e25]
                  - generic [ref=e31]: Schedule Matrix
                - generic [ref=e33]:
                  - generic [ref=e34]: "Total Employees: 1"
                  - generic [ref=e35]: "Total Hours: 8h 0m"
              - generic [ref=e36]:
                - paragraph [ref=e37]: This schedule is read-only.
                - region "Schedule Matrix" [ref=e39]:
                  - table [ref=e40]:
                    - rowgroup [ref=e41]:
                      - row "Day Worker A very long employee surname 1 8h 0m" [ref=e42]:
                        - columnheader "Day" [ref=e43]
                        - columnheader "Worker A very long employee surname 1 8h 0m" [ref=e44]:
                          - generic [ref=e45]:
                            - generic [ref=e46]: Worker A very long employee surname 1
                            - generic [ref=e47]: 8h 0m
                    - rowgroup [ref=e48]:
                      - 'row "Shift 1: 1 employee; Shift 2: 0 employees th./01.10 08:00 - 16:00, Worker A very long employee surname 1 day 1" [ref=e49]':
                        - 'rowheader "Shift 1: 1 employee; Shift 2: 0 employees th./01.10" [ref=e50] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 1 employee; Shift 2: 0 employees"': 1,0
                            - generic: th./01.10
                        - cell "08:00 - 16:00, Worker A very long employee surname 1 day 1" [ref=e51]:
                          - generic [ref=e52]:
                            - 'generic "Also works in: Public schedule. Click for details."':
                              - generic: 08:00 - 16:00,
                              - button "Worker A very long employee surname 1 day 1" [ref=e53] [cursor=pointer]: Public schedule
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees fr./02.10 Worker A very long employee surname 1 day 2" [ref=e54]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees fr./02.10" [ref=e55] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: fr./02.10
                        - cell "Worker A very long employee surname 1 day 2" [ref=e56]:
                          - button "Worker A very long employee surname 1 day 2" [ref=e58]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees sa./03.10 Worker A very long employee surname 1 day 3" [ref=e59]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees sa./03.10" [ref=e60] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: sa./03.10
                        - cell "Worker A very long employee surname 1 day 3" [ref=e61]:
                          - button "Worker A very long employee surname 1 day 3" [ref=e63]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees su./04.10 Worker A very long employee surname 1 day 4" [ref=e64]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees su./04.10" [ref=e65] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: su./04.10
                        - cell "Worker A very long employee surname 1 day 4" [ref=e66]:
                          - button "Worker A very long employee surname 1 day 4" [ref=e68]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees mo./05.10 Worker A very long employee surname 1 day 5" [ref=e69]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees mo./05.10" [ref=e70] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: mo./05.10
                        - cell "Worker A very long employee surname 1 day 5" [ref=e71]:
                          - button "Worker A very long employee surname 1 day 5" [ref=e73]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees tu./06.10 Worker A very long employee surname 1 day 6" [ref=e74]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees tu./06.10" [ref=e75] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: tu./06.10
                        - cell "Worker A very long employee surname 1 day 6" [ref=e76]:
                          - button "Worker A very long employee surname 1 day 6" [ref=e78]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees we./07.10 Worker A very long employee surname 1 day 7" [ref=e79]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees we./07.10" [ref=e80] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: we./07.10
                        - cell "Worker A very long employee surname 1 day 7" [ref=e81]:
                          - button "Worker A very long employee surname 1 day 7" [ref=e83]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees th./08.10 Worker A very long employee surname 1 day 8" [ref=e84]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees th./08.10" [ref=e85] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: th./08.10
                        - cell "Worker A very long employee surname 1 day 8" [ref=e86]:
                          - button "Worker A very long employee surname 1 day 8" [ref=e88]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees fr./09.10 Worker A very long employee surname 1 day 9" [ref=e89]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees fr./09.10" [ref=e90] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: fr./09.10
                        - cell "Worker A very long employee surname 1 day 9" [ref=e91]:
                          - button "Worker A very long employee surname 1 day 9" [ref=e93]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees sa./10.10 Worker A very long employee surname 1 day 10" [ref=e94]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees sa./10.10" [ref=e95] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: sa./10.10
                        - cell "Worker A very long employee surname 1 day 10" [ref=e96]:
                          - button "Worker A very long employee surname 1 day 10" [ref=e98]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees su./11.10 Worker A very long employee surname 1 day 11" [ref=e99]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees su./11.10" [ref=e100] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: su./11.10
                        - cell "Worker A very long employee surname 1 day 11" [ref=e101]:
                          - button "Worker A very long employee surname 1 day 11" [ref=e103]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees mo./12.10 Worker A very long employee surname 1 day 12" [ref=e104]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees mo./12.10" [ref=e105] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: mo./12.10
                        - cell "Worker A very long employee surname 1 day 12" [ref=e106]:
                          - button "Worker A very long employee surname 1 day 12" [ref=e108]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees tu./13.10 Worker A very long employee surname 1 day 13" [ref=e109]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees tu./13.10" [ref=e110] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: tu./13.10
                        - cell "Worker A very long employee surname 1 day 13" [ref=e111]:
                          - button "Worker A very long employee surname 1 day 13" [ref=e113]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees we./14.10 Worker A very long employee surname 1 day 14" [ref=e114]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees we./14.10" [ref=e115] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: we./14.10
                        - cell "Worker A very long employee surname 1 day 14" [ref=e116]:
                          - button "Worker A very long employee surname 1 day 14" [ref=e118]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees th./15.10 Worker A very long employee surname 1 day 15" [ref=e119]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees th./15.10" [ref=e120] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: th./15.10
                        - cell "Worker A very long employee surname 1 day 15" [ref=e121]:
                          - button "Worker A very long employee surname 1 day 15" [ref=e123]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees fr./16.10 Worker A very long employee surname 1 day 16" [ref=e124]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees fr./16.10" [ref=e125] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: fr./16.10
                        - cell "Worker A very long employee surname 1 day 16" [ref=e126]:
                          - button "Worker A very long employee surname 1 day 16" [ref=e128]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees sa./17.10 Worker A very long employee surname 1 day 17" [ref=e129]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees sa./17.10" [ref=e130] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: sa./17.10
                        - cell "Worker A very long employee surname 1 day 17" [ref=e131]:
                          - button "Worker A very long employee surname 1 day 17" [ref=e133]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees su./18.10 Worker A very long employee surname 1 day 18" [ref=e134]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees su./18.10" [ref=e135] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: su./18.10
                        - cell "Worker A very long employee surname 1 day 18" [ref=e136]:
                          - button "Worker A very long employee surname 1 day 18" [ref=e138]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees mo./19.10 Worker A very long employee surname 1 day 19" [ref=e139]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees mo./19.10" [ref=e140] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: mo./19.10
                        - cell "Worker A very long employee surname 1 day 19" [ref=e141]:
                          - button "Worker A very long employee surname 1 day 19" [ref=e143]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees tu./20.10 Worker A very long employee surname 1 day 20" [ref=e144]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees tu./20.10" [ref=e145] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: tu./20.10
                        - cell "Worker A very long employee surname 1 day 20" [ref=e146]:
                          - button "Worker A very long employee surname 1 day 20" [ref=e148]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees we./21.10 Worker A very long employee surname 1 day 21" [ref=e149]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees we./21.10" [ref=e150] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: we./21.10
                        - cell "Worker A very long employee surname 1 day 21" [ref=e151]:
                          - button "Worker A very long employee surname 1 day 21" [ref=e153]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees th./22.10 Worker A very long employee surname 1 day 22" [ref=e154]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees th./22.10" [ref=e155] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: th./22.10
                        - cell "Worker A very long employee surname 1 day 22" [ref=e156]:
                          - button "Worker A very long employee surname 1 day 22" [ref=e158]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees fr./23.10 Worker A very long employee surname 1 day 23" [ref=e159]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees fr./23.10" [ref=e160] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: fr./23.10
                        - cell "Worker A very long employee surname 1 day 23" [ref=e161]:
                          - button "Worker A very long employee surname 1 day 23" [ref=e163]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees sa./24.10 Worker A very long employee surname 1 day 24" [ref=e164]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees sa./24.10" [ref=e165] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: sa./24.10
                        - cell "Worker A very long employee surname 1 day 24" [ref=e166]:
                          - button "Worker A very long employee surname 1 day 24" [ref=e168]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees su./25.10 Worker A very long employee surname 1 day 25" [ref=e169]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees su./25.10" [ref=e170] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: su./25.10
                        - cell "Worker A very long employee surname 1 day 25" [ref=e171]:
                          - button "Worker A very long employee surname 1 day 25" [ref=e173]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees mo./26.10 Worker A very long employee surname 1 day 26" [ref=e174]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees mo./26.10" [ref=e175] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: mo./26.10
                        - cell "Worker A very long employee surname 1 day 26" [ref=e176]:
                          - button "Worker A very long employee surname 1 day 26" [ref=e178]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees tu./27.10 Worker A very long employee surname 1 day 27" [ref=e179]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees tu./27.10" [ref=e180] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: tu./27.10
                        - cell "Worker A very long employee surname 1 day 27" [ref=e181]:
                          - button "Worker A very long employee surname 1 day 27" [ref=e183]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees we./28.10 Worker A very long employee surname 1 day 28" [ref=e184]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees we./28.10" [ref=e185] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: we./28.10
                        - cell "Worker A very long employee surname 1 day 28" [ref=e186]:
                          - button "Worker A very long employee surname 1 day 28" [ref=e188]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees th./29.10 Worker A very long employee surname 1 day 29" [ref=e189]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees th./29.10" [ref=e190] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: th./29.10
                        - cell "Worker A very long employee surname 1 day 29" [ref=e191]:
                          - button "Worker A very long employee surname 1 day 29" [ref=e193]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees fr./30.10 Worker A very long employee surname 1 day 30" [ref=e194]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees fr./30.10" [ref=e195] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: fr./30.10
                        - cell "Worker A very long employee surname 1 day 30" [ref=e196]:
                          - button "Worker A very long employee surname 1 day 30" [ref=e198]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees sa./31.10 Worker A very long employee surname 1 day 31" [ref=e199]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees sa./31.10" [ref=e200] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: sa./31.10
                        - cell "Worker A very long employee surname 1 day 31" [ref=e201]:
                          - button "Worker A very long employee surname 1 day 31" [ref=e203]: "-"
          - generic [ref=e204]:
            - generic [ref=e205]:
              - generic [ref=e206]:
                - img [ref=e207]
                - generic [ref=e209]: Schedule Summary
              - generic [ref=e211]:
                - generic [ref=e212]:
                  - img
                  - searchbox "Search schedule summary by employee name or surname" [ref=e213]
                - generic [ref=e214]:
                  - generic [ref=e215]: "Employees: 1"
                  - generic [ref=e216]: "Hours: 8h 0m"
            - article [ref=e218]:
              - heading "Worker A very long employee surname 1" [level=3] [ref=e219]
              - generic [ref=e220]:
                - generic [ref=e221]:
                  - term [ref=e222]: Work Days
                  - definition [ref=e223]: "1"
                - generic [ref=e224]:
                  - term [ref=e225]: Free Days
                  - definition [ref=e226]: "30"
                - generic [ref=e227]:
                  - term [ref=e228]: Sum
                  - definition [ref=e229]: "8"
              - group [ref=e230]:
                - generic "Schedule details" [ref=e231] [cursor=pointer]
    - navigation "Manager navigation" [ref=e232]:
      - link "Home" [ref=e233]:
        - /url: /
        - img [ref=e234]
      - link "Containers" [ref=e236]:
        - /url: /container
        - img [ref=e237]
        - generic [ref=e242]: Containers
      - link "Dispo" [ref=e243]:
        - /url: /availability
        - img [ref=e244]
      - link "Employees" [ref=e248]:
        - /url: /employee
        - img [ref=e249]
      - link "More" [ref=e253]:
        - /url: /more
        - img [ref=e254]
      - button "Collapse navigation" [ref=e256] [cursor=pointer]:
        - img [ref=e257]
```

# Test source

```ts
  224 |   await expect(page.locator("nav[aria-label='Employee sections']:visible").first()).toBeVisible(); await expect(page.getByRole("heading", { name: "Choose workspace" })).toHaveCount(0);
  225 | });
  226 | 
  227 | test("chooser keyboard focus and reduced motion; phone nav touch targets", async ({ page }, testInfo) => {
  228 |   await page.emulateMedia({ reducedMotion: "reduce" }); await fixture(page, "choose"); await page.waitForLoadState("networkidle"); await page.goto("/");
  229 |   const phone = page.getByRole("button", { name: "Phone Read only", exact: true }); await expect(phone).toBeVisible(); await phone.focus(); await expect(phone).toBeFocused();
  230 |   await page.screenshot({ path: testInfo.outputPath("chooser.png") }); await page.keyboard.press("Enter"); await expect(page.locator("[data-manager-phone]")).toBeVisible();
  231 |   const nav = page.getByRole("navigation", { name: "Manager navigation" });
  232 |   await page.getByRole("button", { name: "Collapse navigation", exact: true }).focus();
  233 |   await page.keyboard.press("Space"); await expect(nav).toHaveAttribute("inert", "");
  234 |   await expect(page.getByRole("button", { name: "Open navigation", exact: true })).toBeFocused();
  235 |   await page.keyboard.press("Tab"); expect(await nav.locator("a").evaluateAll(elements => elements.some(element => element === document.activeElement))).toBe(false);
  236 |   await page.getByRole("button", { name: "Open navigation", exact: true }).focus();
  237 |   await page.keyboard.press("Enter"); await expect(nav.locator('[aria-current="page"]')).toBeFocused();
  238 |   await expect(nav).not.toHaveAttribute("inert");
  239 |   await page.keyboard.press("Tab");
  240 |   await expect(nav.getByRole("link", { name: "Containers", exact: true })).toBeFocused();
  241 |   await page.keyboard.press("Enter"); await expect(page).toHaveURL(/\/container$/);
  242 |   for (const link of await page.locator('nav[aria-label="Manager navigation"] a').all()) {
  243 |     const box = await link.boundingBox(); expect(box!.height).toBeGreaterThanOrEqual(44); expect(box!.width).toBeGreaterThanOrEqual(44);
  244 |   }
  245 | });
  246 | 
  247 | 
  248 | for (const workerCount of [0, 1, 6]) {
  249 |   test(`Phone matrix preserves ${workerCount} workers and optional manual column`, async ({ page }, testInfo) => {
  250 |     await page.setViewportSize({ width: 320, height: 650 });
  251 |     const f = await fixture(page, "phone", "manager", { workerCount, manualColumn: workerCount === 6, longNames: true });
  252 |     await page.waitForLoadState("networkidle"); await page.goto("/container/1/graphs/3"); await expect(page.getByText("Schedule Summary", { exact: true })).toBeVisible();
  253 |     if (workerCount === 0) {
  254 |       await expect(page.getByText("No employees are assigned to this schedule yet.")).toBeVisible();
  255 |       await expect(page.locator("[data-phone-matrix-scroll]")).toHaveCount(0);
  256 |     } else {
  257 |       await recordGeometry(testInfo, "edge-scroll-geometry", await scrollMatrix(page, workerCount === 6 ? 8 : 2));
  258 |       if (workerCount === 6) {
  259 |         await expect(page.locator("[data-phone-matrix-scroll] thead th").last()).toHaveText("Manual coverage");
  260 |         await expect(page.locator("[data-phone-matrix-scroll] tbody tr").first().locator("td").last()).toContainText("Reserve coverage");
  261 |       }
  262 |     }
  263 |     await noOverflow(page); await noManagement(page); expect(f.unsafe).toEqual([]); expect(f.errors).toEqual([]);
  264 |   });
  265 | }
  266 | 
  267 | 
  268 | test("rounded chooser logout ends the session", async ({ page }) => {
  269 |  await fixture(page, "choose"); await page.goto("/");
  270 |  const logout = page.getByRole("button", { name: "Log out", exact: true }); await expect(logout).toBeVisible();
  271 |  expect((await logout.boundingBox())!.height).toBeGreaterThanOrEqual(44);
  272 |  await expect(logout).toHaveCSS("border-radius", "999px"); await logout.click(); await expect(page).toHaveURL(/\/login$/);
  273 | });
  274 | 
  275 | for (const width of [320, 375, 390, 430]) {
  276 |  for (const workerCount of [0, 1, 6]) {
  277 |   test(`Availability ${workerCount} workers visible at ${width}px with collapsed information`, async ({ page }, testInfo) => {
  278 |    await page.setViewportSize({ width, height: 850 }); const f = await fixture(page, "phone", "manager", { workerCount, longNames: true });
  279 |    await page.goto("/availability/5"); await expect(page.getByText("Availability Schedule", { exact: true })).toBeVisible();
  280 |    const disclosure = page.locator("details").first(); await expect(disclosure).not.toHaveAttribute("open");
  281 |    if (workerCount === 0) {
  282 |     await expect(page.getByText("No employees are assigned to this availability group yet.")).toBeVisible();
  283 |     await expect(page.locator("[data-phone-matrix-scroll]")).toHaveCount(0);
  284 |    } else {
  285 |     const scroll = page.locator("[data-phone-matrix-scroll]");
  286 |     await scroll.locator("thead").scrollIntoViewIfNeeded();
  287 |     expect((await scroll.locator("thead").boundingBox())!.y).toBeLessThan(700);
  288 |     await recordGeometry(testInfo, "availability-first-screen", await scrollMatrix(page, workerCount + 1));
  289 |     await scroll.locator("tbody tr").last().scrollIntoViewIfNeeded(); await expect(scroll.locator("tbody tr").last()).toContainText("31");
  290 |     await disclosure.locator("summary").click(); await noOverflow(page); await disclosure.locator("summary").click();
  291 |    }
  292 |    if (width === 390) await page.screenshot({ path: testInfo.outputPath("phone-availability.png"), fullPage: true });
  293 |    await noOverflow(page); await noManagement(page); expect(f.errors).toEqual([]); expect(f.unsafe).toEqual([]);
  294 |   });
  295 |  }
  296 | }
  297 | 
  298 | test("phone search, descending containers, pin priority, metrics and long employee fields", async ({ page }, testInfo) => {
  299 |  await page.setViewportSize({ width: 390, height: 850 }); const f = await fixture(page, "phone", "manager", { longNames: true });
  300 |  await page.route("**/api/containers", route => route.fulfill({ contentType: "application/json", body: JSON.stringify([{ id: 1, name: "All manager container" }, { id: 9, name: "Container nine" }, { id: 4, name: "Container four" }]) }));
  301 |  await page.goto("/container"); await expect(page.getByText("Container nine", { exact: true })).toBeVisible();
  302 |  const titles = page.locator('main article[role="button"] [class*="_title_"]'); await expect(titles).toHaveText(["Container nine", "Container four", "All manager container"]);
  303 |  await page.getByRole("button", { name: /Pin All manager container/ }).click(); await expect(titles).toHaveText(["All manager container", "Container nine", "Container four"]);
  304 |  const search = page.getByRole("searchbox", { name: "Search", exact: true }); await search.fill("nine"); await expect(titles).toHaveText(["Container nine"]);
  305 |  await page.getByRole("button", { name: "Clear Search", exact: true }).click(); await expect(titles).toHaveCount(3);
  306 |  await page.getByText("All manager container", { exact: true }).click(); await expect(page.getByRole("heading", { name: "Statistics" })).toBeVisible();
  307 |  const stats = page.getByRole("heading", { name: "Statistics" }).locator(".."); await expect(stats.locator("table")).toHaveCount(0);
  308 |  await expect(stats.locator("article")).toHaveCount(7); await stats.locator("article details summary").first().click(); await expect(stats.locator("article").first()).toContainText("Central"); await noOverflow(page);
  309 |  await page.getByRole("link", { name: "Back", exact: true }).click(); await expect(page).toHaveURL(/\/container$/);
  310 |  await page.goto("/employee/2"); await expect(page.getByText("Worker A very long employee surname 1", { exact: true })).toBeVisible(); await noOverflow(page); await noManagement(page);
  311 |  await page.goto("/more"); await expect(page.getByRole("button", { name: /Switch to PC/ }).locator("svg")).toHaveCSS("transform", "matrix(1, 0, 0, -1, 0, 0)");
  312 |  await page.screenshot({ path: testInfo.outputPath("phone-more.png"), fullPage: true }); await page.getByRole("button", { name: "Log out", exact: true }).click(); await expect(page).toHaveURL(/\/login$/); expect(f.unsafe).toEqual([]);
  313 | });
  314 | 
  315 | 
  316 | for (const width of [320, 375, 390, 430]) {
  317 |  for (const workerCount of [0, 1, 8]) {
  318 |   test(`Manager schedule sticky axes and mobile summary at ${width}px with ${workerCount} long names`, async ({ page }, testInfo) => {
  319 |    await page.setViewportSize({ width, height: 740 });
  320 |    const f = await fixture(page, "phone", "manager", { workerCount, longNames: true, manualColumn: workerCount === 8 });
  321 |    await page.goto("/container/1/graphs/3"); await expect(page.getByText("Schedule Summary", { exact: true })).toBeVisible();
  322 |    if (workerCount > 0) {
  323 |     const scroll = page.locator("[data-phone-matrix-scroll]");
> 324 |     await scroll.scrollIntoViewIfNeeded();
      |                  ^ Error: locator.scrollIntoViewIfNeeded: Element is not attached to the DOM
  325 |     const before = await scroll.evaluate(element => ({ height: element.clientHeight, scrollHeight: element.scrollHeight, width: element.clientWidth, scrollWidth: element.scrollWidth }));
  326 |     expect(before.height).toBeLessThanOrEqual(560); expect(before.scrollHeight).toBeGreaterThan(before.height);
  327 |     const nameHeader = scroll.locator("thead th").nth(1); expect((await nameHeader.boundingBox())!.width).toBeLessThanOrEqual(157);
  328 |     expect(await nameHeader.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
  329 |     await scroll.evaluate(element => { element.scrollLeft = element.scrollWidth; element.scrollTop = element.scrollHeight; });
  330 |     const day = scroll.locator("tbody th").last(); const corner = scroll.locator("thead th").first(); const header = scroll.locator("thead th").last();
  331 |     const geometry = await scroll.evaluate(element => ({ left: element.scrollLeft, top: element.scrollTop, box: element.getBoundingClientRect().toJSON() }));
  332 |     if (before.scrollWidth > before.width) expect(geometry.left).toBeGreaterThan(0);
  333 |     else expect(geometry.left).toBe(0);
  334 |     expect(geometry.top).toBeGreaterThan(0);
  335 |     const cornerBox = (await corner.boundingBox())!; const headerBox = (await header.boundingBox())!; const dayBox = (await day.boundingBox())!;
  336 |     expect(Math.abs(cornerBox.x - geometry.box.x)).toBeLessThanOrEqual(2); expect(Math.abs(dayBox.x - cornerBox.x)).toBeLessThanOrEqual(2);
  337 |     expect(Math.abs(headerBox.y - geometry.box.y)).toBeLessThanOrEqual(2); expect(Math.abs(cornerBox.y - headerBox.y)).toBeLessThanOrEqual(2);
  338 |     expect(await corner.evaluate(element => Number(getComputedStyle(element).zIndex))).toBeGreaterThan(await header.evaluate(element => Number(getComputedStyle(element).zIndex)));
  339 |     await reachesViewport(scroll, scroll.locator("tbody tr").last().locator("td").last());
  340 |     await recordGeometry(testInfo, "sticky-matrix-axes", { before, geometry, cornerBox, headerBox, dayBox });
  341 |     const summary = page.locator("[data-phone-schedule-summary]"); await expect(summary.locator("article")).toHaveCount(workerCount);
  342 |     expect(await summary.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true); await expect(summary.locator("table")).toHaveCount(0);
  343 |     const search = page.getByRole("searchbox", { name: "Search schedule summary by employee name or surname" });
  344 |     await search.fill("surname 1"); await expect(summary.locator("article")).toHaveCount(1); await expect(summary).toContainText("Worker A very long employee surname 1");
  345 |     await summary.locator("summary").click(); await expect(summary).toContainText("08:00"); await expect(summary).toContainText("16:00"); await expect(summary.locator("article > dl dd").nth(2)).toHaveText("8");
  346 |     await search.fill("missing employee"); await expect(page.getByRole("status")).toContainText('No employees found');
  347 |     await search.fill(""); await expect(summary.locator("article")).toHaveCount(workerCount);
  348 |     await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight));
  349 |     const last = (await summary.locator("article").last().boundingBox())!; const nav = (await page.getByRole("navigation", { name: "Manager navigation" }).boundingBox())!;
  350 |     expect(last.y + last.height).toBeLessThanOrEqual(nav.y);
  351 |     if (width === 390 && workerCount === 8) await page.screenshot({ path: testInfo.outputPath("manager-phone-summary.png"), fullPage: true });
  352 |    } else {
  353 |     await expect(page.locator("[data-phone-matrix-scroll]")).toHaveCount(0); await expect(page.getByText("No employees are assigned to this schedule yet.")).toBeVisible();
  354 |    }
  355 |    await noOverflow(page); await noManagement(page); expect(f.unsafe).toEqual([]); expect(f.errors).toEqual([]);
  356 |   });
  357 |  }
  358 | }
  359 | 
```