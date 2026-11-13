# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: manager-workspace-modes.spec.ts >> Manager schedule sticky axes and mobile summary at 430px with 8 long names
- Location: e2e\manager-workspace-modes.spec.ts:319:3

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
        - link "Back" [ref=e9] [cursor=pointer]:
          - /url: /container/1
          - img [ref=e10]
          - text: Back
        - navigation [ref=e12]:
          - link "Public schedule" [ref=e13] [cursor=pointer]:
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
                  - generic [ref=e34]: "Total Employees: 8"
                  - generic [ref=e35]: "Total Hours: 64h 0m"
              - generic [ref=e36]:
                - paragraph [ref=e37]: This schedule is read-only.
                - region "Schedule Matrix" [ref=e39]:
                  - table [ref=e40]:
                    - rowgroup [ref=e52]:
                      - row "Day Worker A very long employee surname 1 8h 0m Worker A very long employee surname 2 8h 0m Worker A very long employee surname 3 8h 0m Worker A very long employee surname 4 8h 0m Worker A very long employee surname 5 8h 0m Worker A very long employee surname 6 8h 0m Worker A very long employee surname 7 8h 0m Worker A very long employee surname 8 8h 0m Manual coverage" [ref=e53]:
                        - columnheader "Day" [ref=e54]
                        - columnheader "Worker A very long employee surname 1 8h 0m" [ref=e55]:
                          - generic [ref=e56]:
                            - generic [ref=e57]: Worker A very long employee surname 1
                            - generic [ref=e58]: 8h 0m
                        - columnheader "Worker A very long employee surname 2 8h 0m" [ref=e59]:
                          - generic [ref=e60]:
                            - generic [ref=e61]: Worker A very long employee surname 2
                            - generic [ref=e62]: 8h 0m
                        - columnheader "Worker A very long employee surname 3 8h 0m" [ref=e63]:
                          - generic [ref=e64]:
                            - generic [ref=e65]: Worker A very long employee surname 3
                            - generic [ref=e66]: 8h 0m
                        - columnheader "Worker A very long employee surname 4 8h 0m" [ref=e67]:
                          - generic [ref=e68]:
                            - generic [ref=e69]: Worker A very long employee surname 4
                            - generic [ref=e70]: 8h 0m
                        - columnheader "Worker A very long employee surname 5 8h 0m" [ref=e71]:
                          - generic [ref=e72]:
                            - generic [ref=e73]: Worker A very long employee surname 5
                            - generic [ref=e74]: 8h 0m
                        - columnheader "Worker A very long employee surname 6 8h 0m" [ref=e75]:
                          - generic [ref=e76]:
                            - generic [ref=e77]: Worker A very long employee surname 6
                            - generic [ref=e78]: 8h 0m
                        - columnheader "Worker A very long employee surname 7 8h 0m" [ref=e79]:
                          - generic [ref=e80]:
                            - generic [ref=e81]: Worker A very long employee surname 7
                            - generic [ref=e82]: 8h 0m
                        - columnheader "Worker A very long employee surname 8 8h 0m" [ref=e83]:
                          - generic [ref=e84]:
                            - generic [ref=e85]: Worker A very long employee surname 8
                            - generic [ref=e86]: 8h 0m
                        - columnheader "Manual coverage" [ref=e87]:
                          - generic [ref=e89]: Manual coverage
                    - rowgroup [ref=e90]:
                      - 'row "Shift 1: 8 employees; Shift 2: 0 employees th./01.10 08:00 - 16:00, Worker A very long employee surname 1 day 1 08:00 - 16:00, Worker A very long employee surname 2 day 1 08:00 - 16:00, Worker A very long employee surname 3 day 1 08:00 - 16:00, Worker A very long employee surname 4 day 1 08:00 - 16:00, Worker A very long employee surname 5 day 1 08:00 - 16:00, Worker A very long employee surname 6 day 1 08:00 - 16:00, Worker A very long employee surname 7 day 1 08:00 - 16:00, Worker A very long employee surname 8 day 1 Manual coverage day 1" [ref=e91]':
                        - 'rowheader "Shift 1: 8 employees; Shift 2: 0 employees th./01.10" [ref=e92] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 8 employees; Shift 2: 0 employees"': 8,0
                            - generic: th./01.10
                        - cell "08:00 - 16:00, Worker A very long employee surname 1 day 1" [ref=e93]:
                          - generic [ref=e94]:
                            - 'generic "Also works in: Public schedule. Click for details."':
                              - generic: 08:00 - 16:00,
                              - button "Worker A very long employee surname 1 day 1" [ref=e95] [cursor=pointer]: Public schedule
                        - cell "08:00 - 16:00, Worker A very long employee surname 2 day 1" [ref=e96]:
                          - generic [ref=e97]:
                            - 'generic "Also works in: Public schedule. Click for details."':
                              - generic: 08:00 - 16:00,
                              - button "Worker A very long employee surname 2 day 1" [ref=e98] [cursor=pointer]: Public schedule
                        - cell "08:00 - 16:00, Worker A very long employee surname 3 day 1" [ref=e99]:
                          - generic [ref=e100]:
                            - 'generic "Also works in: Public schedule. Click for details."':
                              - generic: 08:00 - 16:00,
                              - button "Worker A very long employee surname 3 day 1" [ref=e101] [cursor=pointer]: Public schedule
                        - cell "08:00 - 16:00, Worker A very long employee surname 4 day 1" [ref=e102]:
                          - generic [ref=e103]:
                            - 'generic "Also works in: Public schedule. Click for details."':
                              - generic: 08:00 - 16:00,
                              - button "Worker A very long employee surname 4 day 1" [ref=e104] [cursor=pointer]: Public schedule
                        - cell "08:00 - 16:00, Worker A very long employee surname 5 day 1" [ref=e105]:
                          - generic [ref=e106]:
                            - 'generic "Also works in: Public schedule. Click for details."':
                              - generic: 08:00 - 16:00,
                              - button "Worker A very long employee surname 5 day 1" [ref=e107] [cursor=pointer]: Public schedule
                        - cell "08:00 - 16:00, Worker A very long employee surname 6 day 1" [ref=e108]:
                          - generic [ref=e109]:
                            - 'generic "Also works in: Public schedule. Click for details."':
                              - generic: 08:00 - 16:00,
                              - button "Worker A very long employee surname 6 day 1" [ref=e110] [cursor=pointer]: Public schedule
                        - cell "08:00 - 16:00, Worker A very long employee surname 7 day 1" [ref=e111]:
                          - generic [ref=e112]:
                            - 'generic "Also works in: Public schedule. Click for details."':
                              - generic: 08:00 - 16:00,
                              - button "Worker A very long employee surname 7 day 1" [ref=e113] [cursor=pointer]: Public schedule
                        - cell "08:00 - 16:00, Worker A very long employee surname 8 day 1" [ref=e114]:
                          - generic [ref=e115]:
                            - 'generic "Also works in: Public schedule. Click for details."':
                              - generic: 08:00 - 16:00,
                              - button "Worker A very long employee surname 8 day 1" [ref=e116] [cursor=pointer]: Public schedule
                        - cell "Manual coverage day 1" [ref=e117]:
                          - button "Manual coverage day 1" [ref=e119]: Reserve coverage
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees fr./02.10 Worker A very long employee surname 1 day 2 Worker A very long employee surname 2 day 2 Worker A very long employee surname 3 day 2 Worker A very long employee surname 4 day 2 Worker A very long employee surname 5 day 2 Worker A very long employee surname 6 day 2 Worker A very long employee surname 7 day 2 Worker A very long employee surname 8 day 2 Manual coverage day 2" [ref=e120]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees fr./02.10" [ref=e121] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: fr./02.10
                        - cell "Worker A very long employee surname 1 day 2" [ref=e122]:
                          - button "Worker A very long employee surname 1 day 2" [ref=e124]: "-"
                        - cell "Worker A very long employee surname 2 day 2" [ref=e125]:
                          - button "Worker A very long employee surname 2 day 2" [ref=e127]: "-"
                        - cell "Worker A very long employee surname 3 day 2" [ref=e128]:
                          - button "Worker A very long employee surname 3 day 2" [ref=e130]: "-"
                        - cell "Worker A very long employee surname 4 day 2" [ref=e131]:
                          - button "Worker A very long employee surname 4 day 2" [ref=e133]: "-"
                        - cell "Worker A very long employee surname 5 day 2" [ref=e134]:
                          - button "Worker A very long employee surname 5 day 2" [ref=e136]: "-"
                        - cell "Worker A very long employee surname 6 day 2" [ref=e137]:
                          - button "Worker A very long employee surname 6 day 2" [ref=e139]: "-"
                        - cell "Worker A very long employee surname 7 day 2" [ref=e140]:
                          - button "Worker A very long employee surname 7 day 2" [ref=e142]: "-"
                        - cell "Worker A very long employee surname 8 day 2" [ref=e143]:
                          - button "Worker A very long employee surname 8 day 2" [ref=e145]: "-"
                        - cell "Manual coverage day 2" [ref=e146]:
                          - button "Manual coverage day 2" [ref=e148]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees sa./03.10 Worker A very long employee surname 1 day 3 Worker A very long employee surname 2 day 3 Worker A very long employee surname 3 day 3 Worker A very long employee surname 4 day 3 Worker A very long employee surname 5 day 3 Worker A very long employee surname 6 day 3 Worker A very long employee surname 7 day 3 Worker A very long employee surname 8 day 3 Manual coverage day 3" [ref=e149]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees sa./03.10" [ref=e150] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: sa./03.10
                        - cell "Worker A very long employee surname 1 day 3" [ref=e151]:
                          - button "Worker A very long employee surname 1 day 3" [ref=e153]: "-"
                        - cell "Worker A very long employee surname 2 day 3" [ref=e154]:
                          - button "Worker A very long employee surname 2 day 3" [ref=e156]: "-"
                        - cell "Worker A very long employee surname 3 day 3" [ref=e157]:
                          - button "Worker A very long employee surname 3 day 3" [ref=e159]: "-"
                        - cell "Worker A very long employee surname 4 day 3" [ref=e160]:
                          - button "Worker A very long employee surname 4 day 3" [ref=e162]: "-"
                        - cell "Worker A very long employee surname 5 day 3" [ref=e163]:
                          - button "Worker A very long employee surname 5 day 3" [ref=e165]: "-"
                        - cell "Worker A very long employee surname 6 day 3" [ref=e166]:
                          - button "Worker A very long employee surname 6 day 3" [ref=e168]: "-"
                        - cell "Worker A very long employee surname 7 day 3" [ref=e169]:
                          - button "Worker A very long employee surname 7 day 3" [ref=e171]: "-"
                        - cell "Worker A very long employee surname 8 day 3" [ref=e172]:
                          - button "Worker A very long employee surname 8 day 3" [ref=e174]: "-"
                        - cell "Manual coverage day 3" [ref=e175]:
                          - button "Manual coverage day 3" [ref=e177]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees su./04.10 Worker A very long employee surname 1 day 4 Worker A very long employee surname 2 day 4 Worker A very long employee surname 3 day 4 Worker A very long employee surname 4 day 4 Worker A very long employee surname 5 day 4 Worker A very long employee surname 6 day 4 Worker A very long employee surname 7 day 4 Worker A very long employee surname 8 day 4 Manual coverage day 4" [ref=e178]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees su./04.10" [ref=e179] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: su./04.10
                        - cell "Worker A very long employee surname 1 day 4" [ref=e180]:
                          - button "Worker A very long employee surname 1 day 4" [ref=e182]: "-"
                        - cell "Worker A very long employee surname 2 day 4" [ref=e183]:
                          - button "Worker A very long employee surname 2 day 4" [ref=e185]: "-"
                        - cell "Worker A very long employee surname 3 day 4" [ref=e186]:
                          - button "Worker A very long employee surname 3 day 4" [ref=e188]: "-"
                        - cell "Worker A very long employee surname 4 day 4" [ref=e189]:
                          - button "Worker A very long employee surname 4 day 4" [ref=e191]: "-"
                        - cell "Worker A very long employee surname 5 day 4" [ref=e192]:
                          - button "Worker A very long employee surname 5 day 4" [ref=e194]: "-"
                        - cell "Worker A very long employee surname 6 day 4" [ref=e195]:
                          - button "Worker A very long employee surname 6 day 4" [ref=e197]: "-"
                        - cell "Worker A very long employee surname 7 day 4" [ref=e198]:
                          - button "Worker A very long employee surname 7 day 4" [ref=e200]: "-"
                        - cell "Worker A very long employee surname 8 day 4" [ref=e201]:
                          - button "Worker A very long employee surname 8 day 4" [ref=e203]: "-"
                        - cell "Manual coverage day 4" [ref=e204]:
                          - button "Manual coverage day 4" [ref=e206]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees mo./05.10 Worker A very long employee surname 1 day 5 Worker A very long employee surname 2 day 5 Worker A very long employee surname 3 day 5 Worker A very long employee surname 4 day 5 Worker A very long employee surname 5 day 5 Worker A very long employee surname 6 day 5 Worker A very long employee surname 7 day 5 Worker A very long employee surname 8 day 5 Manual coverage day 5" [ref=e207]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees mo./05.10" [ref=e208] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: mo./05.10
                        - cell "Worker A very long employee surname 1 day 5" [ref=e209]:
                          - button "Worker A very long employee surname 1 day 5" [ref=e211]: "-"
                        - cell "Worker A very long employee surname 2 day 5" [ref=e212]:
                          - button "Worker A very long employee surname 2 day 5" [ref=e214]: "-"
                        - cell "Worker A very long employee surname 3 day 5" [ref=e215]:
                          - button "Worker A very long employee surname 3 day 5" [ref=e217]: "-"
                        - cell "Worker A very long employee surname 4 day 5" [ref=e218]:
                          - button "Worker A very long employee surname 4 day 5" [ref=e220]: "-"
                        - cell "Worker A very long employee surname 5 day 5" [ref=e221]:
                          - button "Worker A very long employee surname 5 day 5" [ref=e223]: "-"
                        - cell "Worker A very long employee surname 6 day 5" [ref=e224]:
                          - button "Worker A very long employee surname 6 day 5" [ref=e226]: "-"
                        - cell "Worker A very long employee surname 7 day 5" [ref=e227]:
                          - button "Worker A very long employee surname 7 day 5" [ref=e229]: "-"
                        - cell "Worker A very long employee surname 8 day 5" [ref=e230]:
                          - button "Worker A very long employee surname 8 day 5" [ref=e232]: "-"
                        - cell "Manual coverage day 5" [ref=e233]:
                          - button "Manual coverage day 5" [ref=e235]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees tu./06.10 Worker A very long employee surname 1 day 6 Worker A very long employee surname 2 day 6 Worker A very long employee surname 3 day 6 Worker A very long employee surname 4 day 6 Worker A very long employee surname 5 day 6 Worker A very long employee surname 6 day 6 Worker A very long employee surname 7 day 6 Worker A very long employee surname 8 day 6 Manual coverage day 6" [ref=e236]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees tu./06.10" [ref=e237] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: tu./06.10
                        - cell "Worker A very long employee surname 1 day 6" [ref=e238]:
                          - button "Worker A very long employee surname 1 day 6" [ref=e240]: "-"
                        - cell "Worker A very long employee surname 2 day 6" [ref=e241]:
                          - button "Worker A very long employee surname 2 day 6" [ref=e243]: "-"
                        - cell "Worker A very long employee surname 3 day 6" [ref=e244]:
                          - button "Worker A very long employee surname 3 day 6" [ref=e246]: "-"
                        - cell "Worker A very long employee surname 4 day 6" [ref=e247]:
                          - button "Worker A very long employee surname 4 day 6" [ref=e249]: "-"
                        - cell "Worker A very long employee surname 5 day 6" [ref=e250]:
                          - button "Worker A very long employee surname 5 day 6" [ref=e252]: "-"
                        - cell "Worker A very long employee surname 6 day 6" [ref=e253]:
                          - button "Worker A very long employee surname 6 day 6" [ref=e255]: "-"
                        - cell "Worker A very long employee surname 7 day 6" [ref=e256]:
                          - button "Worker A very long employee surname 7 day 6" [ref=e258]: "-"
                        - cell "Worker A very long employee surname 8 day 6" [ref=e259]:
                          - button "Worker A very long employee surname 8 day 6" [ref=e261]: "-"
                        - cell "Manual coverage day 6" [ref=e262]:
                          - button "Manual coverage day 6" [ref=e264]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees we./07.10 Worker A very long employee surname 1 day 7 Worker A very long employee surname 2 day 7 Worker A very long employee surname 3 day 7 Worker A very long employee surname 4 day 7 Worker A very long employee surname 5 day 7 Worker A very long employee surname 6 day 7 Worker A very long employee surname 7 day 7 Worker A very long employee surname 8 day 7 Manual coverage day 7" [ref=e265]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees we./07.10" [ref=e266] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: we./07.10
                        - cell "Worker A very long employee surname 1 day 7" [ref=e267]:
                          - button "Worker A very long employee surname 1 day 7" [ref=e269]: "-"
                        - cell "Worker A very long employee surname 2 day 7" [ref=e270]:
                          - button "Worker A very long employee surname 2 day 7" [ref=e272]: "-"
                        - cell "Worker A very long employee surname 3 day 7" [ref=e273]:
                          - button "Worker A very long employee surname 3 day 7" [ref=e275]: "-"
                        - cell "Worker A very long employee surname 4 day 7" [ref=e276]:
                          - button "Worker A very long employee surname 4 day 7" [ref=e278]: "-"
                        - cell "Worker A very long employee surname 5 day 7" [ref=e279]:
                          - button "Worker A very long employee surname 5 day 7" [ref=e281]: "-"
                        - cell "Worker A very long employee surname 6 day 7" [ref=e282]:
                          - button "Worker A very long employee surname 6 day 7" [ref=e284]: "-"
                        - cell "Worker A very long employee surname 7 day 7" [ref=e285]:
                          - button "Worker A very long employee surname 7 day 7" [ref=e287]: "-"
                        - cell "Worker A very long employee surname 8 day 7" [ref=e288]:
                          - button "Worker A very long employee surname 8 day 7" [ref=e290]: "-"
                        - cell "Manual coverage day 7" [ref=e291]:
                          - button "Manual coverage day 7" [ref=e293]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees th./08.10 Worker A very long employee surname 1 day 8 Worker A very long employee surname 2 day 8 Worker A very long employee surname 3 day 8 Worker A very long employee surname 4 day 8 Worker A very long employee surname 5 day 8 Worker A very long employee surname 6 day 8 Worker A very long employee surname 7 day 8 Worker A very long employee surname 8 day 8 Manual coverage day 8" [ref=e294]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees th./08.10" [ref=e295] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: th./08.10
                        - cell "Worker A very long employee surname 1 day 8" [ref=e296]:
                          - button "Worker A very long employee surname 1 day 8" [ref=e298]: "-"
                        - cell "Worker A very long employee surname 2 day 8" [ref=e299]:
                          - button "Worker A very long employee surname 2 day 8" [ref=e301]: "-"
                        - cell "Worker A very long employee surname 3 day 8" [ref=e302]:
                          - button "Worker A very long employee surname 3 day 8" [ref=e304]: "-"
                        - cell "Worker A very long employee surname 4 day 8" [ref=e305]:
                          - button "Worker A very long employee surname 4 day 8" [ref=e307]: "-"
                        - cell "Worker A very long employee surname 5 day 8" [ref=e308]:
                          - button "Worker A very long employee surname 5 day 8" [ref=e310]: "-"
                        - cell "Worker A very long employee surname 6 day 8" [ref=e311]:
                          - button "Worker A very long employee surname 6 day 8" [ref=e313]: "-"
                        - cell "Worker A very long employee surname 7 day 8" [ref=e314]:
                          - button "Worker A very long employee surname 7 day 8" [ref=e316]: "-"
                        - cell "Worker A very long employee surname 8 day 8" [ref=e317]:
                          - button "Worker A very long employee surname 8 day 8" [ref=e319]: "-"
                        - cell "Manual coverage day 8" [ref=e320]:
                          - button "Manual coverage day 8" [ref=e322]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees fr./09.10 Worker A very long employee surname 1 day 9 Worker A very long employee surname 2 day 9 Worker A very long employee surname 3 day 9 Worker A very long employee surname 4 day 9 Worker A very long employee surname 5 day 9 Worker A very long employee surname 6 day 9 Worker A very long employee surname 7 day 9 Worker A very long employee surname 8 day 9 Manual coverage day 9" [ref=e323]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees fr./09.10" [ref=e324] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: fr./09.10
                        - cell "Worker A very long employee surname 1 day 9" [ref=e325]:
                          - button "Worker A very long employee surname 1 day 9" [ref=e327]: "-"
                        - cell "Worker A very long employee surname 2 day 9" [ref=e328]:
                          - button "Worker A very long employee surname 2 day 9" [ref=e330]: "-"
                        - cell "Worker A very long employee surname 3 day 9" [ref=e331]:
                          - button "Worker A very long employee surname 3 day 9" [ref=e333]: "-"
                        - cell "Worker A very long employee surname 4 day 9" [ref=e334]:
                          - button "Worker A very long employee surname 4 day 9" [ref=e336]: "-"
                        - cell "Worker A very long employee surname 5 day 9" [ref=e337]:
                          - button "Worker A very long employee surname 5 day 9" [ref=e339]: "-"
                        - cell "Worker A very long employee surname 6 day 9" [ref=e340]:
                          - button "Worker A very long employee surname 6 day 9" [ref=e342]: "-"
                        - cell "Worker A very long employee surname 7 day 9" [ref=e343]:
                          - button "Worker A very long employee surname 7 day 9" [ref=e345]: "-"
                        - cell "Worker A very long employee surname 8 day 9" [ref=e346]:
                          - button "Worker A very long employee surname 8 day 9" [ref=e348]: "-"
                        - cell "Manual coverage day 9" [ref=e349]:
                          - button "Manual coverage day 9" [ref=e351]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees sa./10.10 Worker A very long employee surname 1 day 10 Worker A very long employee surname 2 day 10 Worker A very long employee surname 3 day 10 Worker A very long employee surname 4 day 10 Worker A very long employee surname 5 day 10 Worker A very long employee surname 6 day 10 Worker A very long employee surname 7 day 10 Worker A very long employee surname 8 day 10 Manual coverage day 10" [ref=e352]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees sa./10.10" [ref=e353] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: sa./10.10
                        - cell "Worker A very long employee surname 1 day 10" [ref=e354]:
                          - button "Worker A very long employee surname 1 day 10" [ref=e356]: "-"
                        - cell "Worker A very long employee surname 2 day 10" [ref=e357]:
                          - button "Worker A very long employee surname 2 day 10" [ref=e359]: "-"
                        - cell "Worker A very long employee surname 3 day 10" [ref=e360]:
                          - button "Worker A very long employee surname 3 day 10" [ref=e362]: "-"
                        - cell "Worker A very long employee surname 4 day 10" [ref=e363]:
                          - button "Worker A very long employee surname 4 day 10" [ref=e365]: "-"
                        - cell "Worker A very long employee surname 5 day 10" [ref=e366]:
                          - button "Worker A very long employee surname 5 day 10" [ref=e368]: "-"
                        - cell "Worker A very long employee surname 6 day 10" [ref=e369]:
                          - button "Worker A very long employee surname 6 day 10" [ref=e371]: "-"
                        - cell "Worker A very long employee surname 7 day 10" [ref=e372]:
                          - button "Worker A very long employee surname 7 day 10" [ref=e374]: "-"
                        - cell "Worker A very long employee surname 8 day 10" [ref=e375]:
                          - button "Worker A very long employee surname 8 day 10" [ref=e377]: "-"
                        - cell "Manual coverage day 10" [ref=e378]:
                          - button "Manual coverage day 10" [ref=e380]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees su./11.10 Worker A very long employee surname 1 day 11 Worker A very long employee surname 2 day 11 Worker A very long employee surname 3 day 11 Worker A very long employee surname 4 day 11 Worker A very long employee surname 5 day 11 Worker A very long employee surname 6 day 11 Worker A very long employee surname 7 day 11 Worker A very long employee surname 8 day 11 Manual coverage day 11" [ref=e381]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees su./11.10" [ref=e382] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: su./11.10
                        - cell "Worker A very long employee surname 1 day 11" [ref=e383]:
                          - button "Worker A very long employee surname 1 day 11" [ref=e385]: "-"
                        - cell "Worker A very long employee surname 2 day 11" [ref=e386]:
                          - button "Worker A very long employee surname 2 day 11" [ref=e388]: "-"
                        - cell "Worker A very long employee surname 3 day 11" [ref=e389]:
                          - button "Worker A very long employee surname 3 day 11" [ref=e391]: "-"
                        - cell "Worker A very long employee surname 4 day 11" [ref=e392]:
                          - button "Worker A very long employee surname 4 day 11" [ref=e394]: "-"
                        - cell "Worker A very long employee surname 5 day 11" [ref=e395]:
                          - button "Worker A very long employee surname 5 day 11" [ref=e397]: "-"
                        - cell "Worker A very long employee surname 6 day 11" [ref=e398]:
                          - button "Worker A very long employee surname 6 day 11" [ref=e400]: "-"
                        - cell "Worker A very long employee surname 7 day 11" [ref=e401]:
                          - button "Worker A very long employee surname 7 day 11" [ref=e403]: "-"
                        - cell "Worker A very long employee surname 8 day 11" [ref=e404]:
                          - button "Worker A very long employee surname 8 day 11" [ref=e406]: "-"
                        - cell "Manual coverage day 11" [ref=e407]:
                          - button "Manual coverage day 11" [ref=e409]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees mo./12.10 Worker A very long employee surname 1 day 12 Worker A very long employee surname 2 day 12 Worker A very long employee surname 3 day 12 Worker A very long employee surname 4 day 12 Worker A very long employee surname 5 day 12 Worker A very long employee surname 6 day 12 Worker A very long employee surname 7 day 12 Worker A very long employee surname 8 day 12 Manual coverage day 12" [ref=e410]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees mo./12.10" [ref=e411] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: mo./12.10
                        - cell "Worker A very long employee surname 1 day 12" [ref=e412]:
                          - button "Worker A very long employee surname 1 day 12" [ref=e414]: "-"
                        - cell "Worker A very long employee surname 2 day 12" [ref=e415]:
                          - button "Worker A very long employee surname 2 day 12" [ref=e417]: "-"
                        - cell "Worker A very long employee surname 3 day 12" [ref=e418]:
                          - button "Worker A very long employee surname 3 day 12" [ref=e420]: "-"
                        - cell "Worker A very long employee surname 4 day 12" [ref=e421]:
                          - button "Worker A very long employee surname 4 day 12" [ref=e423]: "-"
                        - cell "Worker A very long employee surname 5 day 12" [ref=e424]:
                          - button "Worker A very long employee surname 5 day 12" [ref=e426]: "-"
                        - cell "Worker A very long employee surname 6 day 12" [ref=e427]:
                          - button "Worker A very long employee surname 6 day 12" [ref=e429]: "-"
                        - cell "Worker A very long employee surname 7 day 12" [ref=e430]:
                          - button "Worker A very long employee surname 7 day 12" [ref=e432]: "-"
                        - cell "Worker A very long employee surname 8 day 12" [ref=e433]:
                          - button "Worker A very long employee surname 8 day 12" [ref=e435]: "-"
                        - cell "Manual coverage day 12" [ref=e436]:
                          - button "Manual coverage day 12" [ref=e438]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees tu./13.10 Worker A very long employee surname 1 day 13 Worker A very long employee surname 2 day 13 Worker A very long employee surname 3 day 13 Worker A very long employee surname 4 day 13 Worker A very long employee surname 5 day 13 Worker A very long employee surname 6 day 13 Worker A very long employee surname 7 day 13 Worker A very long employee surname 8 day 13 Manual coverage day 13" [ref=e439]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees tu./13.10" [ref=e440] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: tu./13.10
                        - cell "Worker A very long employee surname 1 day 13" [ref=e441]:
                          - button "Worker A very long employee surname 1 day 13" [ref=e443]: "-"
                        - cell "Worker A very long employee surname 2 day 13" [ref=e444]:
                          - button "Worker A very long employee surname 2 day 13" [ref=e446]: "-"
                        - cell "Worker A very long employee surname 3 day 13" [ref=e447]:
                          - button "Worker A very long employee surname 3 day 13" [ref=e449]: "-"
                        - cell "Worker A very long employee surname 4 day 13" [ref=e450]:
                          - button "Worker A very long employee surname 4 day 13" [ref=e452]: "-"
                        - cell "Worker A very long employee surname 5 day 13" [ref=e453]:
                          - button "Worker A very long employee surname 5 day 13" [ref=e455]: "-"
                        - cell "Worker A very long employee surname 6 day 13" [ref=e456]:
                          - button "Worker A very long employee surname 6 day 13" [ref=e458]: "-"
                        - cell "Worker A very long employee surname 7 day 13" [ref=e459]:
                          - button "Worker A very long employee surname 7 day 13" [ref=e461]: "-"
                        - cell "Worker A very long employee surname 8 day 13" [ref=e462]:
                          - button "Worker A very long employee surname 8 day 13" [ref=e464]: "-"
                        - cell "Manual coverage day 13" [ref=e465]:
                          - button "Manual coverage day 13" [ref=e467]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees we./14.10 Worker A very long employee surname 1 day 14 Worker A very long employee surname 2 day 14 Worker A very long employee surname 3 day 14 Worker A very long employee surname 4 day 14 Worker A very long employee surname 5 day 14 Worker A very long employee surname 6 day 14 Worker A very long employee surname 7 day 14 Worker A very long employee surname 8 day 14 Manual coverage day 14" [ref=e468]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees we./14.10" [ref=e469] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: we./14.10
                        - cell "Worker A very long employee surname 1 day 14" [ref=e470]:
                          - button "Worker A very long employee surname 1 day 14" [ref=e472]: "-"
                        - cell "Worker A very long employee surname 2 day 14" [ref=e473]:
                          - button "Worker A very long employee surname 2 day 14" [ref=e475]: "-"
                        - cell "Worker A very long employee surname 3 day 14" [ref=e476]:
                          - button "Worker A very long employee surname 3 day 14" [ref=e478]: "-"
                        - cell "Worker A very long employee surname 4 day 14" [ref=e479]:
                          - button "Worker A very long employee surname 4 day 14" [ref=e481]: "-"
                        - cell "Worker A very long employee surname 5 day 14" [ref=e482]:
                          - button "Worker A very long employee surname 5 day 14" [ref=e484]: "-"
                        - cell "Worker A very long employee surname 6 day 14" [ref=e485]:
                          - button "Worker A very long employee surname 6 day 14" [ref=e487]: "-"
                        - cell "Worker A very long employee surname 7 day 14" [ref=e488]:
                          - button "Worker A very long employee surname 7 day 14" [ref=e490]: "-"
                        - cell "Worker A very long employee surname 8 day 14" [ref=e491]:
                          - button "Worker A very long employee surname 8 day 14" [ref=e493]: "-"
                        - cell "Manual coverage day 14" [ref=e494]:
                          - button "Manual coverage day 14" [ref=e496]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees th./15.10 Worker A very long employee surname 1 day 15 Worker A very long employee surname 2 day 15 Worker A very long employee surname 3 day 15 Worker A very long employee surname 4 day 15 Worker A very long employee surname 5 day 15 Worker A very long employee surname 6 day 15 Worker A very long employee surname 7 day 15 Worker A very long employee surname 8 day 15 Manual coverage day 15" [ref=e497]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees th./15.10" [ref=e498] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: th./15.10
                        - cell "Worker A very long employee surname 1 day 15" [ref=e499]:
                          - button "Worker A very long employee surname 1 day 15" [ref=e501]: "-"
                        - cell "Worker A very long employee surname 2 day 15" [ref=e502]:
                          - button "Worker A very long employee surname 2 day 15" [ref=e504]: "-"
                        - cell "Worker A very long employee surname 3 day 15" [ref=e505]:
                          - button "Worker A very long employee surname 3 day 15" [ref=e507]: "-"
                        - cell "Worker A very long employee surname 4 day 15" [ref=e508]:
                          - button "Worker A very long employee surname 4 day 15" [ref=e510]: "-"
                        - cell "Worker A very long employee surname 5 day 15" [ref=e511]:
                          - button "Worker A very long employee surname 5 day 15" [ref=e513]: "-"
                        - cell "Worker A very long employee surname 6 day 15" [ref=e514]:
                          - button "Worker A very long employee surname 6 day 15" [ref=e516]: "-"
                        - cell "Worker A very long employee surname 7 day 15" [ref=e517]:
                          - button "Worker A very long employee surname 7 day 15" [ref=e519]: "-"
                        - cell "Worker A very long employee surname 8 day 15" [ref=e520]:
                          - button "Worker A very long employee surname 8 day 15" [ref=e522]: "-"
                        - cell "Manual coverage day 15" [ref=e523]:
                          - button "Manual coverage day 15" [ref=e525]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees fr./16.10 Worker A very long employee surname 1 day 16 Worker A very long employee surname 2 day 16 Worker A very long employee surname 3 day 16 Worker A very long employee surname 4 day 16 Worker A very long employee surname 5 day 16 Worker A very long employee surname 6 day 16 Worker A very long employee surname 7 day 16 Worker A very long employee surname 8 day 16 Manual coverage day 16" [ref=e526]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees fr./16.10" [ref=e527] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: fr./16.10
                        - cell "Worker A very long employee surname 1 day 16" [ref=e528]:
                          - button "Worker A very long employee surname 1 day 16" [ref=e530]: "-"
                        - cell "Worker A very long employee surname 2 day 16" [ref=e531]:
                          - button "Worker A very long employee surname 2 day 16" [ref=e533]: "-"
                        - cell "Worker A very long employee surname 3 day 16" [ref=e534]:
                          - button "Worker A very long employee surname 3 day 16" [ref=e536]: "-"
                        - cell "Worker A very long employee surname 4 day 16" [ref=e537]:
                          - button "Worker A very long employee surname 4 day 16" [ref=e539]: "-"
                        - cell "Worker A very long employee surname 5 day 16" [ref=e540]:
                          - button "Worker A very long employee surname 5 day 16" [ref=e542]: "-"
                        - cell "Worker A very long employee surname 6 day 16" [ref=e543]:
                          - button "Worker A very long employee surname 6 day 16" [ref=e545]: "-"
                        - cell "Worker A very long employee surname 7 day 16" [ref=e546]:
                          - button "Worker A very long employee surname 7 day 16" [ref=e548]: "-"
                        - cell "Worker A very long employee surname 8 day 16" [ref=e549]:
                          - button "Worker A very long employee surname 8 day 16" [ref=e551]: "-"
                        - cell "Manual coverage day 16" [ref=e552]:
                          - button "Manual coverage day 16" [ref=e554]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees sa./17.10 Worker A very long employee surname 1 day 17 Worker A very long employee surname 2 day 17 Worker A very long employee surname 3 day 17 Worker A very long employee surname 4 day 17 Worker A very long employee surname 5 day 17 Worker A very long employee surname 6 day 17 Worker A very long employee surname 7 day 17 Worker A very long employee surname 8 day 17 Manual coverage day 17" [ref=e555]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees sa./17.10" [ref=e556] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: sa./17.10
                        - cell "Worker A very long employee surname 1 day 17" [ref=e557]:
                          - button "Worker A very long employee surname 1 day 17" [ref=e559]: "-"
                        - cell "Worker A very long employee surname 2 day 17" [ref=e560]:
                          - button "Worker A very long employee surname 2 day 17" [ref=e562]: "-"
                        - cell "Worker A very long employee surname 3 day 17" [ref=e563]:
                          - button "Worker A very long employee surname 3 day 17" [ref=e565]: "-"
                        - cell "Worker A very long employee surname 4 day 17" [ref=e566]:
                          - button "Worker A very long employee surname 4 day 17" [ref=e568]: "-"
                        - cell "Worker A very long employee surname 5 day 17" [ref=e569]:
                          - button "Worker A very long employee surname 5 day 17" [ref=e571]: "-"
                        - cell "Worker A very long employee surname 6 day 17" [ref=e572]:
                          - button "Worker A very long employee surname 6 day 17" [ref=e574]: "-"
                        - cell "Worker A very long employee surname 7 day 17" [ref=e575]:
                          - button "Worker A very long employee surname 7 day 17" [ref=e577]: "-"
                        - cell "Worker A very long employee surname 8 day 17" [ref=e578]:
                          - button "Worker A very long employee surname 8 day 17" [ref=e580]: "-"
                        - cell "Manual coverage day 17" [ref=e581]:
                          - button "Manual coverage day 17" [ref=e583]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees su./18.10 Worker A very long employee surname 1 day 18 Worker A very long employee surname 2 day 18 Worker A very long employee surname 3 day 18 Worker A very long employee surname 4 day 18 Worker A very long employee surname 5 day 18 Worker A very long employee surname 6 day 18 Worker A very long employee surname 7 day 18 Worker A very long employee surname 8 day 18 Manual coverage day 18" [ref=e584]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees su./18.10" [ref=e585] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: su./18.10
                        - cell "Worker A very long employee surname 1 day 18" [ref=e586]:
                          - button "Worker A very long employee surname 1 day 18" [ref=e588]: "-"
                        - cell "Worker A very long employee surname 2 day 18" [ref=e589]:
                          - button "Worker A very long employee surname 2 day 18" [ref=e591]: "-"
                        - cell "Worker A very long employee surname 3 day 18" [ref=e592]:
                          - button "Worker A very long employee surname 3 day 18" [ref=e594]: "-"
                        - cell "Worker A very long employee surname 4 day 18" [ref=e595]:
                          - button "Worker A very long employee surname 4 day 18" [ref=e597]: "-"
                        - cell "Worker A very long employee surname 5 day 18" [ref=e598]:
                          - button "Worker A very long employee surname 5 day 18" [ref=e600]: "-"
                        - cell "Worker A very long employee surname 6 day 18" [ref=e601]:
                          - button "Worker A very long employee surname 6 day 18" [ref=e603]: "-"
                        - cell "Worker A very long employee surname 7 day 18" [ref=e604]:
                          - button "Worker A very long employee surname 7 day 18" [ref=e606]: "-"
                        - cell "Worker A very long employee surname 8 day 18" [ref=e607]:
                          - button "Worker A very long employee surname 8 day 18" [ref=e609]: "-"
                        - cell "Manual coverage day 18" [ref=e610]:
                          - button "Manual coverage day 18" [ref=e612]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees mo./19.10 Worker A very long employee surname 1 day 19 Worker A very long employee surname 2 day 19 Worker A very long employee surname 3 day 19 Worker A very long employee surname 4 day 19 Worker A very long employee surname 5 day 19 Worker A very long employee surname 6 day 19 Worker A very long employee surname 7 day 19 Worker A very long employee surname 8 day 19 Manual coverage day 19" [ref=e613]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees mo./19.10" [ref=e614] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: mo./19.10
                        - cell "Worker A very long employee surname 1 day 19" [ref=e615]:
                          - button "Worker A very long employee surname 1 day 19" [ref=e617]: "-"
                        - cell "Worker A very long employee surname 2 day 19" [ref=e618]:
                          - button "Worker A very long employee surname 2 day 19" [ref=e620]: "-"
                        - cell "Worker A very long employee surname 3 day 19" [ref=e621]:
                          - button "Worker A very long employee surname 3 day 19" [ref=e623]: "-"
                        - cell "Worker A very long employee surname 4 day 19" [ref=e624]:
                          - button "Worker A very long employee surname 4 day 19" [ref=e626]: "-"
                        - cell "Worker A very long employee surname 5 day 19" [ref=e627]:
                          - button "Worker A very long employee surname 5 day 19" [ref=e629]: "-"
                        - cell "Worker A very long employee surname 6 day 19" [ref=e630]:
                          - button "Worker A very long employee surname 6 day 19" [ref=e632]: "-"
                        - cell "Worker A very long employee surname 7 day 19" [ref=e633]:
                          - button "Worker A very long employee surname 7 day 19" [ref=e635]: "-"
                        - cell "Worker A very long employee surname 8 day 19" [ref=e636]:
                          - button "Worker A very long employee surname 8 day 19" [ref=e638]: "-"
                        - cell "Manual coverage day 19" [ref=e639]:
                          - button "Manual coverage day 19" [ref=e641]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees tu./20.10 Worker A very long employee surname 1 day 20 Worker A very long employee surname 2 day 20 Worker A very long employee surname 3 day 20 Worker A very long employee surname 4 day 20 Worker A very long employee surname 5 day 20 Worker A very long employee surname 6 day 20 Worker A very long employee surname 7 day 20 Worker A very long employee surname 8 day 20 Manual coverage day 20" [ref=e642]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees tu./20.10" [ref=e643] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: tu./20.10
                        - cell "Worker A very long employee surname 1 day 20" [ref=e644]:
                          - button "Worker A very long employee surname 1 day 20" [ref=e646]: "-"
                        - cell "Worker A very long employee surname 2 day 20" [ref=e647]:
                          - button "Worker A very long employee surname 2 day 20" [ref=e649]: "-"
                        - cell "Worker A very long employee surname 3 day 20" [ref=e650]:
                          - button "Worker A very long employee surname 3 day 20" [ref=e652]: "-"
                        - cell "Worker A very long employee surname 4 day 20" [ref=e653]:
                          - button "Worker A very long employee surname 4 day 20" [ref=e655]: "-"
                        - cell "Worker A very long employee surname 5 day 20" [ref=e656]:
                          - button "Worker A very long employee surname 5 day 20" [ref=e658]: "-"
                        - cell "Worker A very long employee surname 6 day 20" [ref=e659]:
                          - button "Worker A very long employee surname 6 day 20" [ref=e661]: "-"
                        - cell "Worker A very long employee surname 7 day 20" [ref=e662]:
                          - button "Worker A very long employee surname 7 day 20" [ref=e664]: "-"
                        - cell "Worker A very long employee surname 8 day 20" [ref=e665]:
                          - button "Worker A very long employee surname 8 day 20" [ref=e667]: "-"
                        - cell "Manual coverage day 20" [ref=e668]:
                          - button "Manual coverage day 20" [ref=e670]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees we./21.10 Worker A very long employee surname 1 day 21 Worker A very long employee surname 2 day 21 Worker A very long employee surname 3 day 21 Worker A very long employee surname 4 day 21 Worker A very long employee surname 5 day 21 Worker A very long employee surname 6 day 21 Worker A very long employee surname 7 day 21 Worker A very long employee surname 8 day 21 Manual coverage day 21" [ref=e671]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees we./21.10" [ref=e672] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: we./21.10
                        - cell "Worker A very long employee surname 1 day 21" [ref=e673]:
                          - button "Worker A very long employee surname 1 day 21" [ref=e675]: "-"
                        - cell "Worker A very long employee surname 2 day 21" [ref=e676]:
                          - button "Worker A very long employee surname 2 day 21" [ref=e678]: "-"
                        - cell "Worker A very long employee surname 3 day 21" [ref=e679]:
                          - button "Worker A very long employee surname 3 day 21" [ref=e681]: "-"
                        - cell "Worker A very long employee surname 4 day 21" [ref=e682]:
                          - button "Worker A very long employee surname 4 day 21" [ref=e684]: "-"
                        - cell "Worker A very long employee surname 5 day 21" [ref=e685]:
                          - button "Worker A very long employee surname 5 day 21" [ref=e687]: "-"
                        - cell "Worker A very long employee surname 6 day 21" [ref=e688]:
                          - button "Worker A very long employee surname 6 day 21" [ref=e690]: "-"
                        - cell "Worker A very long employee surname 7 day 21" [ref=e691]:
                          - button "Worker A very long employee surname 7 day 21" [ref=e693]: "-"
                        - cell "Worker A very long employee surname 8 day 21" [ref=e694]:
                          - button "Worker A very long employee surname 8 day 21" [ref=e696]: "-"
                        - cell "Manual coverage day 21" [ref=e697]:
                          - button "Manual coverage day 21" [ref=e699]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees th./22.10 Worker A very long employee surname 1 day 22 Worker A very long employee surname 2 day 22 Worker A very long employee surname 3 day 22 Worker A very long employee surname 4 day 22 Worker A very long employee surname 5 day 22 Worker A very long employee surname 6 day 22 Worker A very long employee surname 7 day 22 Worker A very long employee surname 8 day 22 Manual coverage day 22" [ref=e700]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees th./22.10" [ref=e701] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: th./22.10
                        - cell "Worker A very long employee surname 1 day 22" [ref=e702]:
                          - button "Worker A very long employee surname 1 day 22" [ref=e704]: "-"
                        - cell "Worker A very long employee surname 2 day 22" [ref=e705]:
                          - button "Worker A very long employee surname 2 day 22" [ref=e707]: "-"
                        - cell "Worker A very long employee surname 3 day 22" [ref=e708]:
                          - button "Worker A very long employee surname 3 day 22" [ref=e710]: "-"
                        - cell "Worker A very long employee surname 4 day 22" [ref=e711]:
                          - button "Worker A very long employee surname 4 day 22" [ref=e713]: "-"
                        - cell "Worker A very long employee surname 5 day 22" [ref=e714]:
                          - button "Worker A very long employee surname 5 day 22" [ref=e716]: "-"
                        - cell "Worker A very long employee surname 6 day 22" [ref=e717]:
                          - button "Worker A very long employee surname 6 day 22" [ref=e719]: "-"
                        - cell "Worker A very long employee surname 7 day 22" [ref=e720]:
                          - button "Worker A very long employee surname 7 day 22" [ref=e722]: "-"
                        - cell "Worker A very long employee surname 8 day 22" [ref=e723]:
                          - button "Worker A very long employee surname 8 day 22" [ref=e725]: "-"
                        - cell "Manual coverage day 22" [ref=e726]:
                          - button "Manual coverage day 22" [ref=e728]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees fr./23.10 Worker A very long employee surname 1 day 23 Worker A very long employee surname 2 day 23 Worker A very long employee surname 3 day 23 Worker A very long employee surname 4 day 23 Worker A very long employee surname 5 day 23 Worker A very long employee surname 6 day 23 Worker A very long employee surname 7 day 23 Worker A very long employee surname 8 day 23 Manual coverage day 23" [ref=e729]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees fr./23.10" [ref=e730] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: fr./23.10
                        - cell "Worker A very long employee surname 1 day 23" [ref=e731]:
                          - button "Worker A very long employee surname 1 day 23" [ref=e733]: "-"
                        - cell "Worker A very long employee surname 2 day 23" [ref=e734]:
                          - button "Worker A very long employee surname 2 day 23" [ref=e736]: "-"
                        - cell "Worker A very long employee surname 3 day 23" [ref=e737]:
                          - button "Worker A very long employee surname 3 day 23" [ref=e739]: "-"
                        - cell "Worker A very long employee surname 4 day 23" [ref=e740]:
                          - button "Worker A very long employee surname 4 day 23" [ref=e742]: "-"
                        - cell "Worker A very long employee surname 5 day 23" [ref=e743]:
                          - button "Worker A very long employee surname 5 day 23" [ref=e745]: "-"
                        - cell "Worker A very long employee surname 6 day 23" [ref=e746]:
                          - button "Worker A very long employee surname 6 day 23" [ref=e748]: "-"
                        - cell "Worker A very long employee surname 7 day 23" [ref=e749]:
                          - button "Worker A very long employee surname 7 day 23" [ref=e751]: "-"
                        - cell "Worker A very long employee surname 8 day 23" [ref=e752]:
                          - button "Worker A very long employee surname 8 day 23" [ref=e754]: "-"
                        - cell "Manual coverage day 23" [ref=e755]:
                          - button "Manual coverage day 23" [ref=e757]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees sa./24.10 Worker A very long employee surname 1 day 24 Worker A very long employee surname 2 day 24 Worker A very long employee surname 3 day 24 Worker A very long employee surname 4 day 24 Worker A very long employee surname 5 day 24 Worker A very long employee surname 6 day 24 Worker A very long employee surname 7 day 24 Worker A very long employee surname 8 day 24 Manual coverage day 24" [ref=e758]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees sa./24.10" [ref=e759] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: sa./24.10
                        - cell "Worker A very long employee surname 1 day 24" [ref=e760]:
                          - button "Worker A very long employee surname 1 day 24" [ref=e762]: "-"
                        - cell "Worker A very long employee surname 2 day 24" [ref=e763]:
                          - button "Worker A very long employee surname 2 day 24" [ref=e765]: "-"
                        - cell "Worker A very long employee surname 3 day 24" [ref=e766]:
                          - button "Worker A very long employee surname 3 day 24" [ref=e768]: "-"
                        - cell "Worker A very long employee surname 4 day 24" [ref=e769]:
                          - button "Worker A very long employee surname 4 day 24" [ref=e771]: "-"
                        - cell "Worker A very long employee surname 5 day 24" [ref=e772]:
                          - button "Worker A very long employee surname 5 day 24" [ref=e774]: "-"
                        - cell "Worker A very long employee surname 6 day 24" [ref=e775]:
                          - button "Worker A very long employee surname 6 day 24" [ref=e777]: "-"
                        - cell "Worker A very long employee surname 7 day 24" [ref=e778]:
                          - button "Worker A very long employee surname 7 day 24" [ref=e780]: "-"
                        - cell "Worker A very long employee surname 8 day 24" [ref=e781]:
                          - button "Worker A very long employee surname 8 day 24" [ref=e783]: "-"
                        - cell "Manual coverage day 24" [ref=e784]:
                          - button "Manual coverage day 24" [ref=e786]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees su./25.10 Worker A very long employee surname 1 day 25 Worker A very long employee surname 2 day 25 Worker A very long employee surname 3 day 25 Worker A very long employee surname 4 day 25 Worker A very long employee surname 5 day 25 Worker A very long employee surname 6 day 25 Worker A very long employee surname 7 day 25 Worker A very long employee surname 8 day 25 Manual coverage day 25" [ref=e787]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees su./25.10" [ref=e788] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: su./25.10
                        - cell "Worker A very long employee surname 1 day 25" [ref=e789]:
                          - button "Worker A very long employee surname 1 day 25" [ref=e791]: "-"
                        - cell "Worker A very long employee surname 2 day 25" [ref=e792]:
                          - button "Worker A very long employee surname 2 day 25" [ref=e794]: "-"
                        - cell "Worker A very long employee surname 3 day 25" [ref=e795]:
                          - button "Worker A very long employee surname 3 day 25" [ref=e797]: "-"
                        - cell "Worker A very long employee surname 4 day 25" [ref=e798]:
                          - button "Worker A very long employee surname 4 day 25" [ref=e800]: "-"
                        - cell "Worker A very long employee surname 5 day 25" [ref=e801]:
                          - button "Worker A very long employee surname 5 day 25" [ref=e803]: "-"
                        - cell "Worker A very long employee surname 6 day 25" [ref=e804]:
                          - button "Worker A very long employee surname 6 day 25" [ref=e806]: "-"
                        - cell "Worker A very long employee surname 7 day 25" [ref=e807]:
                          - button "Worker A very long employee surname 7 day 25" [ref=e809]: "-"
                        - cell "Worker A very long employee surname 8 day 25" [ref=e810]:
                          - button "Worker A very long employee surname 8 day 25" [ref=e812]: "-"
                        - cell "Manual coverage day 25" [ref=e813]:
                          - button "Manual coverage day 25" [ref=e815]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees mo./26.10 Worker A very long employee surname 1 day 26 Worker A very long employee surname 2 day 26 Worker A very long employee surname 3 day 26 Worker A very long employee surname 4 day 26 Worker A very long employee surname 5 day 26 Worker A very long employee surname 6 day 26 Worker A very long employee surname 7 day 26 Worker A very long employee surname 8 day 26 Manual coverage day 26" [ref=e816]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees mo./26.10" [ref=e817] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: mo./26.10
                        - cell "Worker A very long employee surname 1 day 26" [ref=e818]:
                          - button "Worker A very long employee surname 1 day 26" [ref=e820]: "-"
                        - cell "Worker A very long employee surname 2 day 26" [ref=e821]:
                          - button "Worker A very long employee surname 2 day 26" [ref=e823]: "-"
                        - cell "Worker A very long employee surname 3 day 26" [ref=e824]:
                          - button "Worker A very long employee surname 3 day 26" [ref=e826]: "-"
                        - cell "Worker A very long employee surname 4 day 26" [ref=e827]:
                          - button "Worker A very long employee surname 4 day 26" [ref=e829]: "-"
                        - cell "Worker A very long employee surname 5 day 26" [ref=e830]:
                          - button "Worker A very long employee surname 5 day 26" [ref=e832]: "-"
                        - cell "Worker A very long employee surname 6 day 26" [ref=e833]:
                          - button "Worker A very long employee surname 6 day 26" [ref=e835]: "-"
                        - cell "Worker A very long employee surname 7 day 26" [ref=e836]:
                          - button "Worker A very long employee surname 7 day 26" [ref=e838]: "-"
                        - cell "Worker A very long employee surname 8 day 26" [ref=e839]:
                          - button "Worker A very long employee surname 8 day 26" [ref=e841]: "-"
                        - cell "Manual coverage day 26" [ref=e842]:
                          - button "Manual coverage day 26" [ref=e844]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees tu./27.10 Worker A very long employee surname 1 day 27 Worker A very long employee surname 2 day 27 Worker A very long employee surname 3 day 27 Worker A very long employee surname 4 day 27 Worker A very long employee surname 5 day 27 Worker A very long employee surname 6 day 27 Worker A very long employee surname 7 day 27 Worker A very long employee surname 8 day 27 Manual coverage day 27" [ref=e845]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees tu./27.10" [ref=e846] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: tu./27.10
                        - cell "Worker A very long employee surname 1 day 27" [ref=e847]:
                          - button "Worker A very long employee surname 1 day 27" [ref=e849]: "-"
                        - cell "Worker A very long employee surname 2 day 27" [ref=e850]:
                          - button "Worker A very long employee surname 2 day 27" [ref=e852]: "-"
                        - cell "Worker A very long employee surname 3 day 27" [ref=e853]:
                          - button "Worker A very long employee surname 3 day 27" [ref=e855]: "-"
                        - cell "Worker A very long employee surname 4 day 27" [ref=e856]:
                          - button "Worker A very long employee surname 4 day 27" [ref=e858]: "-"
                        - cell "Worker A very long employee surname 5 day 27" [ref=e859]:
                          - button "Worker A very long employee surname 5 day 27" [ref=e861]: "-"
                        - cell "Worker A very long employee surname 6 day 27" [ref=e862]:
                          - button "Worker A very long employee surname 6 day 27" [ref=e864]: "-"
                        - cell "Worker A very long employee surname 7 day 27" [ref=e865]:
                          - button "Worker A very long employee surname 7 day 27" [ref=e867]: "-"
                        - cell "Worker A very long employee surname 8 day 27" [ref=e868]:
                          - button "Worker A very long employee surname 8 day 27" [ref=e870]: "-"
                        - cell "Manual coverage day 27" [ref=e871]:
                          - button "Manual coverage day 27" [ref=e873]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees we./28.10 Worker A very long employee surname 1 day 28 Worker A very long employee surname 2 day 28 Worker A very long employee surname 3 day 28 Worker A very long employee surname 4 day 28 Worker A very long employee surname 5 day 28 Worker A very long employee surname 6 day 28 Worker A very long employee surname 7 day 28 Worker A very long employee surname 8 day 28 Manual coverage day 28" [ref=e874]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees we./28.10" [ref=e875] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: we./28.10
                        - cell "Worker A very long employee surname 1 day 28" [ref=e876]:
                          - button "Worker A very long employee surname 1 day 28" [ref=e878]: "-"
                        - cell "Worker A very long employee surname 2 day 28" [ref=e879]:
                          - button "Worker A very long employee surname 2 day 28" [ref=e881]: "-"
                        - cell "Worker A very long employee surname 3 day 28" [ref=e882]:
                          - button "Worker A very long employee surname 3 day 28" [ref=e884]: "-"
                        - cell "Worker A very long employee surname 4 day 28" [ref=e885]:
                          - button "Worker A very long employee surname 4 day 28" [ref=e887]: "-"
                        - cell "Worker A very long employee surname 5 day 28" [ref=e888]:
                          - button "Worker A very long employee surname 5 day 28" [ref=e890]: "-"
                        - cell "Worker A very long employee surname 6 day 28" [ref=e891]:
                          - button "Worker A very long employee surname 6 day 28" [ref=e893]: "-"
                        - cell "Worker A very long employee surname 7 day 28" [ref=e894]:
                          - button "Worker A very long employee surname 7 day 28" [ref=e896]: "-"
                        - cell "Worker A very long employee surname 8 day 28" [ref=e897]:
                          - button "Worker A very long employee surname 8 day 28" [ref=e899]: "-"
                        - cell "Manual coverage day 28" [ref=e900]:
                          - button "Manual coverage day 28" [ref=e902]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees th./29.10 Worker A very long employee surname 1 day 29 Worker A very long employee surname 2 day 29 Worker A very long employee surname 3 day 29 Worker A very long employee surname 4 day 29 Worker A very long employee surname 5 day 29 Worker A very long employee surname 6 day 29 Worker A very long employee surname 7 day 29 Worker A very long employee surname 8 day 29 Manual coverage day 29" [ref=e903]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees th./29.10" [ref=e904] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: th./29.10
                        - cell "Worker A very long employee surname 1 day 29" [ref=e905]:
                          - button "Worker A very long employee surname 1 day 29" [ref=e907]: "-"
                        - cell "Worker A very long employee surname 2 day 29" [ref=e908]:
                          - button "Worker A very long employee surname 2 day 29" [ref=e910]: "-"
                        - cell "Worker A very long employee surname 3 day 29" [ref=e911]:
                          - button "Worker A very long employee surname 3 day 29" [ref=e913]: "-"
                        - cell "Worker A very long employee surname 4 day 29" [ref=e914]:
                          - button "Worker A very long employee surname 4 day 29" [ref=e916]: "-"
                        - cell "Worker A very long employee surname 5 day 29" [ref=e917]:
                          - button "Worker A very long employee surname 5 day 29" [ref=e919]: "-"
                        - cell "Worker A very long employee surname 6 day 29" [ref=e920]:
                          - button "Worker A very long employee surname 6 day 29" [ref=e922]: "-"
                        - cell "Worker A very long employee surname 7 day 29" [ref=e923]:
                          - button "Worker A very long employee surname 7 day 29" [ref=e925]: "-"
                        - cell "Worker A very long employee surname 8 day 29" [ref=e926]:
                          - button "Worker A very long employee surname 8 day 29" [ref=e928]: "-"
                        - cell "Manual coverage day 29" [ref=e929]:
                          - button "Manual coverage day 29" [ref=e931]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees fr./30.10 Worker A very long employee surname 1 day 30 Worker A very long employee surname 2 day 30 Worker A very long employee surname 3 day 30 Worker A very long employee surname 4 day 30 Worker A very long employee surname 5 day 30 Worker A very long employee surname 6 day 30 Worker A very long employee surname 7 day 30 Worker A very long employee surname 8 day 30 Manual coverage day 30" [ref=e932]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees fr./30.10" [ref=e933] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: fr./30.10
                        - cell "Worker A very long employee surname 1 day 30" [ref=e934]:
                          - button "Worker A very long employee surname 1 day 30" [ref=e936]: "-"
                        - cell "Worker A very long employee surname 2 day 30" [ref=e937]:
                          - button "Worker A very long employee surname 2 day 30" [ref=e939]: "-"
                        - cell "Worker A very long employee surname 3 day 30" [ref=e940]:
                          - button "Worker A very long employee surname 3 day 30" [ref=e942]: "-"
                        - cell "Worker A very long employee surname 4 day 30" [ref=e943]:
                          - button "Worker A very long employee surname 4 day 30" [ref=e945]: "-"
                        - cell "Worker A very long employee surname 5 day 30" [ref=e946]:
                          - button "Worker A very long employee surname 5 day 30" [ref=e948]: "-"
                        - cell "Worker A very long employee surname 6 day 30" [ref=e949]:
                          - button "Worker A very long employee surname 6 day 30" [ref=e951]: "-"
                        - cell "Worker A very long employee surname 7 day 30" [ref=e952]:
                          - button "Worker A very long employee surname 7 day 30" [ref=e954]: "-"
                        - cell "Worker A very long employee surname 8 day 30" [ref=e955]:
                          - button "Worker A very long employee surname 8 day 30" [ref=e957]: "-"
                        - cell "Manual coverage day 30" [ref=e958]:
                          - button "Manual coverage day 30" [ref=e960]: "-"
                      - 'row "Shift 1: 0 employees; Shift 2: 0 employees sa./31.10 Worker A very long employee surname 1 day 31 Worker A very long employee surname 2 day 31 Worker A very long employee surname 3 day 31 Worker A very long employee surname 4 day 31 Worker A very long employee surname 5 day 31 Worker A very long employee surname 6 day 31 Worker A very long employee surname 7 day 31 Worker A very long employee surname 8 day 31 Manual coverage day 31" [ref=e961]':
                        - 'rowheader "Shift 1: 0 employees; Shift 2: 0 employees sa./31.10" [ref=e962] [cursor=pointer]':
                          - generic:
                            - 'generic "Shift 1: 0 employees; Shift 2: 0 employees"': 0,0
                            - generic: sa./31.10
                        - cell "Worker A very long employee surname 1 day 31" [ref=e963]:
                          - button "Worker A very long employee surname 1 day 31" [ref=e965]: "-"
                        - cell "Worker A very long employee surname 2 day 31" [ref=e966]:
                          - button "Worker A very long employee surname 2 day 31" [ref=e968]: "-"
                        - cell "Worker A very long employee surname 3 day 31" [ref=e969]:
                          - button "Worker A very long employee surname 3 day 31" [ref=e971]: "-"
                        - cell "Worker A very long employee surname 4 day 31" [ref=e972]:
                          - button "Worker A very long employee surname 4 day 31" [ref=e974]: "-"
                        - cell "Worker A very long employee surname 5 day 31" [ref=e975]:
                          - button "Worker A very long employee surname 5 day 31" [ref=e977]: "-"
                        - cell "Worker A very long employee surname 6 day 31" [ref=e978]:
                          - button "Worker A very long employee surname 6 day 31" [ref=e980]: "-"
                        - cell "Worker A very long employee surname 7 day 31" [ref=e981]:
                          - button "Worker A very long employee surname 7 day 31" [ref=e983]: "-"
                        - cell "Worker A very long employee surname 8 day 31" [ref=e984]:
                          - button "Worker A very long employee surname 8 day 31" [ref=e986]: "-"
                        - cell "Manual coverage day 31" [ref=e987]:
                          - button "Manual coverage day 31" [ref=e989]: "-"
          - generic [ref=e990]:
            - generic [ref=e991]:
              - generic [ref=e992]:
                - img [ref=e993]
                - generic [ref=e995]: Schedule Summary
              - generic [ref=e997]:
                - generic [ref=e998]:
                  - img
                  - searchbox "Search schedule summary by employee name or surname" [ref=e999]
                - generic [ref=e1000]:
                  - generic [ref=e1001]: "Employees: 8"
                  - generic [ref=e1002]: "Hours: 64h 0m"
            - generic [ref=e1003]:
              - article [ref=e1004]:
                - heading "Worker A very long employee surname 1" [level=3] [ref=e1005]
                - generic [ref=e1006]:
                  - generic [ref=e1007]:
                    - term [ref=e1008]: Work Days
                    - definition [ref=e1009]: "1"
                  - generic [ref=e1010]:
                    - term [ref=e1011]: Free Days
                    - definition [ref=e1012]: "30"
                  - generic [ref=e1013]:
                    - term [ref=e1014]: Hours
                    - definition [ref=e1015]: "8"
                - group [ref=e1016]:
                  - generic "Schedule details" [ref=e1017] [cursor=pointer]
              - article [ref=e1018]:
                - heading "Worker A very long employee surname 2" [level=3] [ref=e1019]
                - generic [ref=e1020]:
                  - generic [ref=e1021]:
                    - term [ref=e1022]: Work Days
                    - definition [ref=e1023]: "1"
                  - generic [ref=e1024]:
                    - term [ref=e1025]: Free Days
                    - definition [ref=e1026]: "30"
                  - generic [ref=e1027]:
                    - term [ref=e1028]: Hours
                    - definition [ref=e1029]: "8"
                - group [ref=e1030]:
                  - generic "Schedule details" [ref=e1031] [cursor=pointer]
              - article [ref=e1032]:
                - heading "Worker A very long employee surname 3" [level=3] [ref=e1033]
                - generic [ref=e1034]:
                  - generic [ref=e1035]:
                    - term [ref=e1036]: Work Days
                    - definition [ref=e1037]: "1"
                  - generic [ref=e1038]:
                    - term [ref=e1039]: Free Days
                    - definition [ref=e1040]: "30"
                  - generic [ref=e1041]:
                    - term [ref=e1042]: Hours
                    - definition [ref=e1043]: "8"
                - group [ref=e1044]:
                  - generic "Schedule details" [ref=e1045] [cursor=pointer]
              - article [ref=e1046]:
                - heading "Worker A very long employee surname 4" [level=3] [ref=e1047]
                - generic [ref=e1048]:
                  - generic [ref=e1049]:
                    - term [ref=e1050]: Work Days
                    - definition [ref=e1051]: "1"
                  - generic [ref=e1052]:
                    - term [ref=e1053]: Free Days
                    - definition [ref=e1054]: "30"
                  - generic [ref=e1055]:
                    - term [ref=e1056]: Hours
                    - definition [ref=e1057]: "8"
                - group [ref=e1058]:
                  - generic "Schedule details" [ref=e1059] [cursor=pointer]
              - article [ref=e1060]:
                - heading "Worker A very long employee surname 5" [level=3] [ref=e1061]
                - generic [ref=e1062]:
                  - generic [ref=e1063]:
                    - term [ref=e1064]: Work Days
                    - definition [ref=e1065]: "1"
                  - generic [ref=e1066]:
                    - term [ref=e1067]: Free Days
                    - definition [ref=e1068]: "30"
                  - generic [ref=e1069]:
                    - term [ref=e1070]: Hours
                    - definition [ref=e1071]: "8"
                - group [ref=e1072]:
                  - generic "Schedule details" [ref=e1073] [cursor=pointer]
              - article [ref=e1074]:
                - heading "Worker A very long employee surname 6" [level=3] [ref=e1075]
                - generic [ref=e1076]:
                  - generic [ref=e1077]:
                    - term [ref=e1078]: Work Days
                    - definition [ref=e1079]: "1"
                  - generic [ref=e1080]:
                    - term [ref=e1081]: Free Days
                    - definition [ref=e1082]: "30"
                  - generic [ref=e1083]:
                    - term [ref=e1084]: Hours
                    - definition [ref=e1085]: "8"
                - group [ref=e1086]:
                  - generic "Schedule details" [ref=e1087] [cursor=pointer]
              - article [ref=e1088]:
                - heading "Worker A very long employee surname 7" [level=3] [ref=e1089]
                - generic [ref=e1090]:
                  - generic [ref=e1091]:
                    - term [ref=e1092]: Work Days
                    - definition [ref=e1093]: "1"
                  - generic [ref=e1094]:
                    - term [ref=e1095]: Free Days
                    - definition [ref=e1096]: "30"
                  - generic [ref=e1097]:
                    - term [ref=e1098]: Hours
                    - definition [ref=e1099]: "8"
                - group [ref=e1100]:
                  - generic "Schedule details" [ref=e1101] [cursor=pointer]
              - article [ref=e1102]:
                - heading "Worker A very long employee surname 8" [level=3] [ref=e1103]
                - generic [ref=e1104]:
                  - generic [ref=e1105]:
                    - term [ref=e1106]: Work Days
                    - definition [ref=e1107]: "1"
                  - generic [ref=e1108]:
                    - term [ref=e1109]: Free Days
                    - definition [ref=e1110]: "30"
                  - generic [ref=e1111]:
                    - term [ref=e1112]: Hours
                    - definition [ref=e1113]: "8"
                - group [ref=e1114]:
                  - generic "Schedule details" [ref=e1115] [cursor=pointer]
    - navigation "Manager navigation" [ref=e1116]:
      - link "Home" [ref=e1117] [cursor=pointer]:
        - /url: /
        - img [ref=e1118]
      - link "Containers" [ref=e1120] [cursor=pointer]:
        - /url: /container
        - img [ref=e1121]
        - generic [ref=e1126]: Containers
      - link "Dispo" [ref=e1127] [cursor=pointer]:
        - /url: /availability
        - img [ref=e1128]
      - link "Employees" [ref=e1132] [cursor=pointer]:
        - /url: /employee
        - img [ref=e1133]
      - link "More" [ref=e1137] [cursor=pointer]:
        - /url: /more
        - img [ref=e1138]
      - button "Collapse navigation" [ref=e1140] [cursor=pointer]:
        - img [ref=e1141]
```

# Test source

```ts
  225 |   await expect(page.locator("nav[aria-label='Employee sections']:visible").first()).toBeVisible(); await expect(page.getByRole("heading", { name: "Choose workspace" })).toHaveCount(0);
  226 | });
  227 | 
  228 | test("chooser keyboard focus and reduced motion; phone nav touch targets", async ({ page }, testInfo) => {
  229 |   await page.emulateMedia({ reducedMotion: "reduce" }); await fixture(page, "choose"); await page.waitForLoadState("networkidle"); await page.goto("/");
  230 |   const phone = page.getByRole("button", { name: "Phone Read only", exact: true }); await expect(phone).toBeVisible(); await phone.focus(); await expect(phone).toBeFocused();
  231 |   await page.screenshot({ path: testInfo.outputPath("chooser.png") }); await page.keyboard.press("Enter"); await expect(page.locator("[data-manager-phone]")).toBeVisible();
  232 |   const nav = page.getByRole("navigation", { name: "Manager navigation" });
  233 |   await page.getByRole("button", { name: "Collapse navigation", exact: true }).focus();
  234 |   await page.keyboard.press("Space"); await expect(nav).toHaveAttribute("inert", "");
  235 |   await expect(page.getByRole("button", { name: "Open navigation", exact: true })).toBeFocused();
  236 |   await page.keyboard.press("Tab"); expect(await nav.locator("a").evaluateAll(elements => elements.some(element => element === document.activeElement))).toBe(false);
  237 |   await page.getByRole("button", { name: "Open navigation", exact: true }).focus();
  238 |   await page.keyboard.press("Enter"); await expect(nav.locator('[aria-current="page"]')).toBeFocused();
  239 |   await expect(nav).not.toHaveAttribute("inert");
  240 |   await page.keyboard.press("Tab");
  241 |   await expect(nav.getByRole("link", { name: "Containers", exact: true })).toBeFocused();
  242 |   await page.keyboard.press("Enter"); await expect(page).toHaveURL(/\/container$/);
  243 |   for (const link of await page.locator('nav[aria-label="Manager navigation"] a').all()) {
  244 |     const box = await link.boundingBox(); expect(box!.height).toBeGreaterThanOrEqual(44); expect(box!.width).toBeGreaterThanOrEqual(44);
  245 |   }
  246 | });
  247 | 
  248 | 
  249 | for (const workerCount of [0, 1, 6]) {
  250 |   test(`Phone matrix preserves ${workerCount} workers and optional manual column`, async ({ page }, testInfo) => {
  251 |     await page.setViewportSize({ width: 320, height: 650 });
  252 |     const f = await fixture(page, "phone", "manager", { workerCount, manualColumn: workerCount === 6, longNames: true });
  253 |     await page.waitForLoadState("networkidle"); await page.goto("/container/1/graphs/3"); await expect(page.getByText("Schedule Summary", { exact: true })).toBeVisible();
  254 |     if (workerCount === 0) {
  255 |       await expect(page.getByText("No employees are assigned to this schedule yet.")).toBeVisible();
  256 |       await expect(page.locator("[data-phone-matrix-scroll]")).toHaveCount(0);
  257 |     } else {
  258 |       await recordGeometry(testInfo, "edge-scroll-geometry", await scrollMatrix(page, workerCount === 6 ? 8 : 2));
  259 |       if (workerCount === 6) {
  260 |         await expect(page.locator("[data-phone-matrix-scroll] thead th").last()).toHaveText("Manual coverage");
  261 |         await expect(page.locator("[data-phone-matrix-scroll] tbody tr").first().locator("td").last()).toContainText("Reserve coverage");
  262 |       }
  263 |     }
  264 |     await noOverflow(page); await noManagement(page); expect(f.unsafe).toEqual([]); expect(f.errors).toEqual([]);
  265 |   });
  266 | }
  267 | 
  268 | 
  269 | test("rounded chooser logout ends the session", async ({ page }) => {
  270 |  await fixture(page, "choose"); await page.goto("/");
  271 |  const logout = page.getByRole("button", { name: "Log out", exact: true }); await expect(logout).toBeVisible();
  272 |  expect((await logout.boundingBox())!.height).toBeGreaterThanOrEqual(44);
  273 |  await expect(logout).toHaveCSS("border-radius", "999px"); await logout.click(); await expect(page).toHaveURL(/\/login$/);
  274 | });
  275 | 
  276 | for (const width of [320, 375, 390, 430]) {
  277 |  for (const workerCount of [0, 1, 6]) {
  278 |   test(`Availability ${workerCount} workers visible at ${width}px with collapsed information`, async ({ page }, testInfo) => {
  279 |    await page.setViewportSize({ width, height: 850 }); const f = await fixture(page, "phone", "manager", { workerCount, longNames: true });
  280 |    await page.goto("/availability/5"); await expect(page.getByText("Availability Schedule", { exact: true })).toBeVisible();
  281 |    const disclosure = page.locator("details").first(); await expect(disclosure).not.toHaveAttribute("open");
  282 |    if (workerCount === 0) {
  283 |     await expect(page.getByText("No employees are assigned to this availability group yet.")).toBeVisible();
  284 |     await expect(page.locator("[data-phone-matrix-scroll]")).toHaveCount(0);
  285 |    } else {
  286 |     const scroll = page.locator("[data-phone-matrix-scroll]");
  287 |     await scroll.locator("thead").scrollIntoViewIfNeeded();
  288 |     expect((await scroll.locator("thead").boundingBox())!.y).toBeLessThan(700);
  289 |     await recordGeometry(testInfo, "availability-first-screen", await scrollMatrix(page, workerCount + 1));
  290 |     await scroll.locator("tbody tr").last().scrollIntoViewIfNeeded(); await expect(scroll.locator("tbody tr").last()).toContainText("31");
  291 |     await disclosure.locator("summary").click(); await noOverflow(page); await disclosure.locator("summary").click();
  292 |    }
  293 |    if (width === 390) await page.screenshot({ path: testInfo.outputPath("phone-availability.png"), fullPage: true });
  294 |    await noOverflow(page); await noManagement(page); expect(f.errors).toEqual([]); expect(f.unsafe).toEqual([]);
  295 |   });
  296 |  }
  297 | }
  298 | 
  299 | test("phone search, descending containers, pin priority, metrics and long employee fields", async ({ page }, testInfo) => {
  300 |  await page.setViewportSize({ width: 390, height: 850 }); const f = await fixture(page, "phone", "manager", { longNames: true });
  301 |  await page.route("**/api/containers", route => route.fulfill({ contentType: "application/json", body: JSON.stringify([{ id: 1, name: "All manager container" }, { id: 9, name: "Container nine" }, { id: 4, name: "Container four" }]) }));
  302 |  await page.goto("/container"); await expect(page.getByText("Container nine", { exact: true })).toBeVisible();
  303 |  const titles = page.locator('main article[role="button"] [class*="_title_"]'); await expect(titles).toHaveText(["Container nine", "Container four", "All manager container"]);
  304 |  await page.getByRole("button", { name: /Pin All manager container/ }).click(); await expect(titles).toHaveText(["All manager container", "Container nine", "Container four"]);
  305 |  const search = page.getByRole("searchbox", { name: "Search", exact: true }); await search.fill("nine"); await expect(titles).toHaveText(["Container nine"]);
  306 |  await page.getByRole("button", { name: "Clear Search", exact: true }).click(); await expect(titles).toHaveCount(3);
  307 |  await page.getByText("All manager container", { exact: true }).click(); await expect(page.getByRole("heading", { name: "Statistics" })).toBeVisible();
  308 |  const stats = page.getByRole("heading", { name: "Statistics" }).locator(".."); await expect(stats.locator("table")).toHaveCount(0);
  309 |  await expect(stats.locator("article")).toHaveCount(7); await stats.locator("article details summary").first().click(); await expect(stats.locator("article").first()).toContainText("Central"); await noOverflow(page);
  310 |  await page.getByRole("link", { name: "Back", exact: true }).click(); await expect(page).toHaveURL(/\/container$/);
  311 |  await page.goto("/employee/2"); await expect(page.getByText("Worker A very long employee surname 1", { exact: true })).toBeVisible(); await noOverflow(page); await noManagement(page);
  312 |  await page.goto("/more"); await expect(page.getByRole("button", { name: /Switch to PC/ }).locator("svg")).toHaveCSS("transform", "matrix(1, 0, 0, -1, 0, 0)");
  313 |  await page.screenshot({ path: testInfo.outputPath("phone-more.png"), fullPage: true }); await page.getByRole("button", { name: "Log out", exact: true }).click(); await expect(page).toHaveURL(/\/login$/); expect(f.unsafe).toEqual([]);
  314 | });
  315 | 
  316 | 
  317 | for (const width of [320, 375, 390, 430]) {
  318 |  for (const workerCount of [0, 1, 8]) {
  319 |   test(`Manager schedule sticky axes and mobile summary at ${width}px with ${workerCount} long names`, async ({ page }, testInfo) => {
  320 |    await page.setViewportSize({ width, height: 740 });
  321 |    const f = await fixture(page, "phone", "manager", { workerCount, longNames: true, manualColumn: workerCount === 8 });
  322 |    await page.goto("/container/1/graphs/3"); await expect(page.getByText("Schedule Summary", { exact: true })).toBeVisible(); await page.waitForLoadState("networkidle");
  323 |    if (workerCount > 0) {
  324 |     const scroll = page.locator("[data-phone-matrix-scroll]");
> 325 |     await scroll.scrollIntoViewIfNeeded();
      |                  ^ Error: locator.scrollIntoViewIfNeeded: Element is not attached to the DOM
  326 |     const before = await scroll.evaluate(element => ({ height: element.clientHeight, scrollHeight: element.scrollHeight, width: element.clientWidth, scrollWidth: element.scrollWidth }));
  327 |     expect(before.height).toBeLessThanOrEqual(560); expect(before.scrollHeight).toBeGreaterThan(before.height);
  328 |     const nameHeader = scroll.locator("thead th").nth(1); expect((await nameHeader.boundingBox())!.width).toBeLessThanOrEqual(157);
  329 |     expect(await nameHeader.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
  330 |     await scroll.evaluate(element => { element.scrollLeft = element.scrollWidth; element.scrollTop = element.scrollHeight; });
  331 |     const day = scroll.locator("tbody th").last(); const corner = scroll.locator("thead th").first(); const header = scroll.locator("thead th").last();
  332 |     const geometry = await scroll.evaluate(element => ({ left: element.scrollLeft, top: element.scrollTop, box: element.getBoundingClientRect().toJSON() }));
  333 |     if (before.scrollWidth > before.width) expect(geometry.left).toBeGreaterThan(0);
  334 |     else expect(geometry.left).toBe(0);
  335 |     expect(geometry.top).toBeGreaterThan(0);
  336 |     const cornerBox = (await corner.boundingBox())!; const headerBox = (await header.boundingBox())!; const dayBox = (await day.boundingBox())!;
  337 |     expect(Math.abs(cornerBox.x - geometry.box.x)).toBeLessThanOrEqual(2); expect(Math.abs(dayBox.x - cornerBox.x)).toBeLessThanOrEqual(2);
  338 |     expect(Math.abs(headerBox.y - geometry.box.y)).toBeLessThanOrEqual(2); expect(Math.abs(cornerBox.y - headerBox.y)).toBeLessThanOrEqual(2);
  339 |     expect(await corner.evaluate(element => Number(getComputedStyle(element).zIndex))).toBeGreaterThan(await header.evaluate(element => Number(getComputedStyle(element).zIndex)));
  340 |     await reachesViewport(scroll, scroll.locator("tbody tr").last().locator("td").last());
  341 |     await recordGeometry(testInfo, "sticky-matrix-axes", { before, geometry, cornerBox, headerBox, dayBox });
  342 |     const summary = page.locator("[data-phone-schedule-summary]"); await expect(summary.locator("article")).toHaveCount(workerCount);
  343 |     expect(await summary.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true); await expect(summary.locator("table")).toHaveCount(0);
  344 |     const search = page.getByRole("searchbox", { name: "Search schedule summary by employee name or surname" });
  345 |     await search.fill("surname 1"); await expect(summary.locator("article")).toHaveCount(1); await expect(summary).toContainText("Worker A very long employee surname 1");
  346 |     await summary.locator("summary").click(); await expect(summary).toContainText("08:00"); await expect(summary).toContainText("16:00"); await expect(summary.locator("article > dl dd").nth(2)).toHaveText("8");
  347 |     await search.fill("missing employee"); await expect(page.getByRole("status")).toContainText('No employees found');
  348 |     await search.fill(""); await expect(summary.locator("article")).toHaveCount(workerCount);
  349 |     await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight));
  350 |     const last = (await summary.locator("article").last().boundingBox())!; const nav = (await page.getByRole("navigation", { name: "Manager navigation" }).boundingBox())!;
  351 |     expect(last.y + last.height).toBeLessThanOrEqual(nav.y);
  352 |     if (width === 390 && workerCount === 8) await page.screenshot({ path: testInfo.outputPath("manager-phone-summary.png"), fullPage: true });
  353 |    } else {
  354 |     await expect(page.locator("[data-phone-matrix-scroll]")).toHaveCount(0); await expect(page.getByText("No employees are assigned to this schedule yet.")).toBeVisible();
  355 |    }
  356 |    await noOverflow(page); await noManagement(page); expect(f.unsafe).toEqual([]); expect(f.errors).toEqual([]);
  357 |   });
  358 |  }
  359 | }
  360 | 
```