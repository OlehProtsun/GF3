# GF — User documentation (EN)

Technical project: GF3. Documented version: `DEV2 @ 06cb43083939fecab4c95bb27bfa094fec13055f`.

[README](../../README.md) · [Polski](01-Dokumentacja-uzytkownika-PL.md)

## 1. Using this guide

Open “Dokumentacja akademicka (IDEIS UDSW)” in README and select PL or EN. This guide covers employees, PC managers and managers in phone mode. The organization assigns accounts; self-registration is unavailable. Bold names identify interface controls; the organization supplies shop/schedule names and message content.

- [Access](#2-access)
- [PC manager](#3-pc-manager)
- [Schedules](#4-schedules)
- [Phone](#5-phone)
- [Employee](#6-employee)
- [Procedures](#7-procedures)
- [Problems](#8-problems)

## 2. Access

At `/login` enter **Username** and **Password** (exactly six digits), then **Sign in**. The PC / **Phone** switch selects password input, not permissions. For phone keypad input choose **Enter password**; the sixth digit starts verification. **Delete** removes the last digit; **Close** closes the keypad. Wait for the result.

After sign-in managers choose **Choose workspace**: PC / **Full access** or **Phone** / **Read only**. Employees enter notifications. Select English / Polski through **Application language** (employee: profile; PC manager: navigation panel). End the session with **Log out**.

| Screen | Button or field | Action | Condition or result |
| --- | --- | --- | --- |
| Sign-in | **Username**, **Password**, **Sign in** | Opens the account's assigned role | Nonempty username and six-digit password |
| Recovery `/password-recovery` | **Forgot password?**, **Send code**, **Send code again** | Sends code to saved recovery email | Enter username; missing email or working mail service requires operator assistance |
| Recovery | **Code**, **New password**, **Change password** | Confirms code and changes password | Six-character code, six-digit new password; return to sign-in after success |
| Regulation | Download PDF, read confirmation, **Accept and continue** | Records personal acceptance of this version | Read downloaded PDF and check the box; further documents can appear separately |

The regulation gate appears only for a required document. Acceptance is personal, not GDPR consent or a company contract. Retry download/save after resolving an error.

## 3. PC manager

Open the required area through navigation. Lists support search, clearing filters and opening details; pinning organizes supported cards. Use **Add New** for a new record. Correct highlighted editor fields and save. Cancel/back does not confirm a save. Wait for another manager's edit lock to be released.

| Screen | Button or field | Action | Condition or result |
| --- | --- | --- | --- |
| **Home** `/` | **Who Works Today?**, **This Month**, **Expand all**, **Collapse all**, open schedule/container | Shows today's assignments, totals and previews | Empty results are possible without assignments |
| **Shop List** `/shop`; profile `/shop/:shopId`; forms `/shop/new`, `/shop/:shopId/edit` | **Name**, **Address**, **Description**, **Edit Shop**, **Delete Shop** | Creates/edits; deletes after confirmation | Correct name/address errors; dependencies may prevent deletion |
| **Employee List** `/employee`; profile `/employee/:employeeId`; forms `/employee/new`, `/employee/:employeeId/edit` | **First Name**, **Last Name**, **Email**, **Phone**, **Username**, **Password** | Manages employee details/access | Correct email, duplicate username or non-six-digit password; do not change password unnecessarily when editing |
| Employee profile | **Edit Employee**, **Kick Employee**, **Delete Employee** | Edits, ends all sessions, deletes/archives according to dependencies | Confirm action; ending sessions does not prevent signing in again |
| **Availability List** `/availability`; profile `/availability/:availabilityId`; forms `/availability/new`, `/availability/:availabilityId/edit` | **Availability Name**, **Availability Month**, **Availability Year**, employee selection, **Add**, remove employee | Creates monthly group and assigns people | Name and at least one employee required; no duplicates |
| Availability editor | **Publication**, **Private**, **Public**, **Visible From**, **Visible To**, **Save Changes** | Publishes access within a time window | End after start; assigned employees gain access after saving |
| Availability | Day matrix, **Bind Information**, **Key**, **Value**, **Active**, **Add**, **Delete**, **Choose from another** | Enters codes/hours, defines shortcuts, selects another available availability source | Key/value required; valid unique key; review source before confirming |
| **Container List** `/container` | **Add New**, **Name**, **Note**, open/edit/delete card | Creates container, shows profile with schedules/totals on the same page | Container must exist before saving schedule; deletion after confirmation |
| **Communications** `/communications` | **Title**, **Message**, **Visible From**, **Visible To**, **Create**, **Edit**, **Save changes**, **Cancel edit**, **Delete** | Manages employee messages | Title/message required; end future and after start; deleted message disappears for employees |
| **Manager profile** `/manager-profile` | **Display name**, **Username**, **Recovery email**, **New password**, **Save changes** | Saves own details | Six-digit password; server environment manages system account details |
| Manager profile | **Active managers**, **Create manager**, **Temporary password**, **Delete**, **My regulation history** | Shows activity, creates/deletes allowed accounts, shows acceptances | New account needs display name, username and password; protected accounts cannot be deleted |
| PC panel | **Open notepad**, **New**, **Note title**, **Note content**, **Save**, **Delete** | Creates/edits own notes, deletes after confirmation | Check saved state; excessive content length shows an error |
| Notepad | **Search notes**, **Pin notepad**, **Unpin notepad**, **Resize notepad**, **Collapse notepad** | Searches, pins, resizes/collapses panel | Panel state can be saved; retry failed save |
| Notepad | **Bold**, **Italic**, **Underline**, **Strikethrough**, **Text size**, **Text color**, **Bulleted list**, **Numbered list**, **Undo**, **Redo**, **Clear formatting** | Formats selected content | Save note after changes |
| PC panel | **Open system news**, **Mark as read**, **Mark all read**, close | Opens system news and records reading | Shows messages for account audience |
| News — developer access | **News settings**, **Developer password**, **Unlock** | Opens management after additional verification | Operator with host password only; ordinary manager access does not replace it |
| News management | **New update**, **Title**, **Text**, **Visible to**, **Everyone**, **Managers only**, **Employees only**, **Image URL**, **Upload image**, **Remove**, **YouTube link**, **Clear**, **Save update**, **Delete** | Creates/edits content and audience, adds image/video, deletes after confirmation | Title/body required; media format/limits checked; never enter password in content |



### System functions

`/information` and `/database` are system-manager-only. Knowing their addresses does not grant employees or ordinary managers access.

| Screen | Button or field | Action | Condition or result |
| --- | --- | --- | --- |
| **Information** | **Logging control**, **Everyone**, **Managers**, **Employees** | Controls new activity recording | Disabling retains history |
| Information | Day, name/time search, **From**, **Through**, **Delete day**, **Delete range**, **Delete all** | Reviews/deletes logs | Local dates, inclusive end; deletion after confirmation |
| **Database** | **Developer password**, **Unlock database** | Unlocks tools when host policy permits | Additional administrative restrictions; tools may be disabled in production |
| Database | **Use Selected DB**, **Create Manual Copy**, **Database Viewer**, **Use In Executor** | Selects database, makes copy, inspects schema/records | Database switching affects the whole application; authorized operator only |
| Database | **SQL command**, **Execute**, **Clear Output**, **Choose File**, **Import script**, **Run Import** | Query/import, clear output | Writes depend on host policy/limits; import supports own code export, not arbitrary external systems |

Employee deletion confirmation currently remains Polish even in English UI: “Czy na pewno chcesz usunąć tego pracownika? Ta operacja jest nieodwracalna.”, button “Usuń”.

Do not use production data to learn administrative operations.

## 4. Schedules

Within a container open `/container/:containerId/graphs/:graphId`, create at `/container/:containerId/graphs/new` or edit at `/container/:containerId/graphs/:graphId/edit`. The profile shows matrix, conflicts and totals. **Edit all** opens indicated session schedules after confirmation.

| Screen | Button or field | Action | Condition or result |
| --- | --- | --- | --- |
| **Schedule Details** | **Name**, **Shop**, **Month**, **Year**, **Availability group** | Defines schedule and availability source | Existing shop and matching period |
| Details | **People per shift**, **Max hours / employee**, **Shift 1**, **Shift 2**, **Max consecutive days**, **Max consecutive full**, **Max full / month** | Sets staffing, times and generator limits | Conflicting limits may prevent staffing |
| Preset | Selector, **Add preset**, **Preset name**, **Graph name**, **Save preset** | Loads/saves settings and employee overrides | Selectable across containers; do not assume globally unique names; preset save does not publish schedule |
| **Employees** | Selector, **Add**, remove, individual limits | Sets participants | At least one employee, no duplicates |
| Matrix | **Generate**, cell entries, **Undo**, **Fill**, **Text**, **C. Selected**, **C. All** | Generates proposal, edits content/color, clears selected scope | Inspect results, conflicts and totals; error/empty result needs corrected inputs |
| Settings | **Bind Information**, **Manual Columns**, **Note** | Defines shortcuts, extra columns and description | Correct key/field errors before saving |
| **Availability Preview** | Source selector, side/below layout | Shows availability while editing | Preview does not replace saving |
| **Versions** | List, preview, checkout | Shows history and restores selected version for work | Check version/confirmations; preview is not publication |
| **Publication** | **Private**, **Public**, **Allow swap**, **On**, **Off**, accepted-swap color, **Save** | Saves visibility and swap permission | Form changes require saving; employees see only shared public state |
| **Shift Corrections** | Request decision/comparison | Approves or rejects correction | Check freshness; request itself does not change hours |
| Swaps | **Swap log**, **View before and after**, posting a manual shift to swaps | Shows accepted changes, offers open shift | Saved schedule and enabled swaps; duplicate open offer blocked |
| Schedule/container profile | **Export** — Excel and code icons, delete | Downloads workbook or SQL; deletes after confirmation | Export error is not a download; SQL for authorized operator |
| Container — schedules | **MultiOpen**, **Open**, **Add New**, **Publish all**, **Make private**, **Can swap**, **Yes**, **No** | Selects session schedules; adds schedule; publishes/hides all after confirmation | Publication needs swap permission choice; making private removes employee visibility |
| Container — swaps | Search, **View**, **Cancel**, **Delete** | Compares offer, cancels open offer, deletes swap record | Deleting history does not reverse an accepted schedule change |
| Manual columns | **Add Column**, matrix header/cells, delete column, **Choose shift**, **Everyone**, **Specific employee**, **Publish shift**, **Remove**, **Cancel** | Adds column, enters times, selects shift/recipient; removes pending proposal/cancels published offer | Enter interval such as 09:00-15:00, save schedule and enable swaps; check pending saves before publication |
| Schedule history | **Schedule history**, commit selection, **Switch**, **Checkout**, **Delete** | Switches saved state or deletes snapshot after confirmation | Checkout may discard unsaved draft; next save creates a branch. Deleting version keeps child commits and reconnects them to parent |
| Schedule corrections | **Current**, **Requested**, **Approve**, **Reject**, approval color | Compares hours and decides request | Save current schedule edits before approval; original shift may no longer match request |


Generation/working preview does not guarantee feasibility for every constraint set. Saving persists state; publishing saved state exposes it to employees. Check staffing, hours and conflicts before publishing. Phone mode does not expose these write operations.

## 5. Phone

**Phone** mode is read only. Routes: `/` (day/totals), `/container` (list), `/container/:containerId` (schedules/statistics), `/container/:containerId/graphs/:graphId` (matrix), `/availability` and `/availability/:availabilityId`, `/employee` and `/employee/:employeeId`, `/shop` and `/shop/:shopId` (lists/details), `/more` (account/additional navigation).

| Screen | Button or field | Action | Condition or result |
| --- | --- | --- | --- |
| Lists/details | **Search**, **Clear Search**, **Back**, **Retry**, card | Searches, returns, retries reads | Invalid ID/missing record shows message; no creation, editing, generation or deletion |
| Container | **Statistics**, **Shops**, schedule selection | Shows totals/details | Existing data |
| **More** | **Switch to Desktop**, **Log out** | Switches to PC or ends session | Wait for completion; retry failed switch |

To edit choose **Switch to Desktop** in **More**. PC offers **Switch to Phone**. Personal regulation acceptance, mode switching and logout are permitted write-restriction exceptions, not edits to organization data.

## 6. Employee

Navigation: `/notifications` (also `/`), `/schedule`, `/availability`, `/swap`, `/profile`. Data/actions are limited to your account and shared records.

| Screen | Button or field | Action | Condition or result |
| --- | --- | --- | --- |
| Notifications | **Inbox**, **News**, **Mark as read**, **Mark all read** | Changes tab, records reading | Follow content link where available; empty list is valid |
| Schedule | **Select schedule**, day, view toggle, **Customize schedule**, **Apply**, **Cancel** | Chooses schedule/column order | Published data only; summary month/year determines totals |
| Schedule | **Adjust**, **Send request**, **Cancel**, PDF | Requests time-boundary correction; exports/prints PDF | Choose working day, shift, start/end and time; manager decision needed; PDF save through browser print dialog |
| Availability | Group, day, from/to hours, presets, **Clear**, **Save** | Sets day and saves group form | Public assigned group and correct window; save preset separately, then check availability save confirmation |
| Swaps | Search, filters, **Pin**, **Unpin**, copy link, **Swap history**, details | Organizes offers/history | Link grants no access and does not reserve shift |
| Offer creation | **Create offer**, **Schedule**, **Recipient**, **Review**, **Everyone**, **Specific employee**, **Give shift**, **Swap shifts** | Selects own shift/period, recipient/type | Public schedule with swaps allowed; reciprocal exchange needs specific person/their shift |
| Wizard | **Save selection**, **Continue**, **Send request**, **Back**, **Close** | Confirms selection, submits or leaves | Review both parties/periods; submission is not acceptance |
| Offer | **Accept shift**, **Cancel offer** | Accepts eligible offer or cancels permitted offer | Confirm acceptance; conflict/stale offer can be rejected; reciprocal acceptance transfers both shifts |
| Profile | **Edit**, **Recovery email**, **Phone**, **Save profile**, **Cancel** | Changes own contact details | Correct email; cancel restores form |
| Profile | **Send code**, **Verification code**, **New password**, **Change password** | Changes password after email code | Recovery email required; contacts saved before code request; six-digit new password |
| Profile | **Application language**, **Push notifications**, regulation history, **Log out** | Language, device push, acceptances, logout | Push optional, does not replace inbox |

A flame marks a manager-posted shift from a manual column. Seeing a card does not itself grant acceptance permission.

Push requires browser support, server configuration, HTTPS and system permission. iPhone/iPad require iOS 16.4+ and GF added to Home Screen. Enable/disable the profile switch on each device. If denied change browser/system settings; report missing configuration to operator. Delivery may be delayed by the system.

## 7. Procedures

### First setup

1. Sign in, choose PC, read required document if shown.
2. In shops use **Add New**, enter name/address/description, save and check profile.
3. In employees use **Add New**, fill details/access and save. Share credentials according to organization rules.
4. Add container name/note and save; continue with availability.

### Collect availability

1. Add group name, month/year and participants.
2. Set day/hour shortcuts if needed; choose **Public**, valid **Visible From**–**Visible To**, save.
3. Employee selects group, day, hours/preset in `/availability`; saves day, then group form and checks confirmation.
4. Manager opens group profile and checks collected entries before generation.

### Generate and publish

1. Add schedule inside existing container.
2. Choose preset or enter shop, period, times/limits. Select matching availability group.
3. Add employees/constraints; optionally save settings as preset.
4. Run **Generate**, wait, inspect matrix, conflicts/totals. Correct inputs on failure; do not publish unverified results.
5. Correct cells, save, confirm session save. Check profile/versions.
6. Choose **Public** in publication, set **Allow swap** as needed, save.

### Check employee schedule

1. Select a shared schedule in `/schedule`.
2. Check period, shop and daily hours; optionally adjust view/columns.
3. Check month/year totals. Report absence to manager; no publication does not confirm a day off.

### Give or exchange shift

1. In `/swap` select **Create offer**, public schedule and own shift/time range.
2. Choose **Everyone** or **Specific employee**. For a person choose **Give shift** or **Swap shifts**; for exchange select their shift/period and save selection.
3. Continue to **Review**, inspect person, date, schedule/hours, submit; go back for corrections.
4. Recipient opens eligible offer, chooses **Accept shift** and confirms. Both parties check updated schedules after success.
5. Owner uses **Cancel offer** if still allowed. Review **Swap history**.

### Correction

1. Select schedule and **Adjust** in `/schedule`.
2. Select working day, shift, start or end; set proposed time (±30 min).
3. Choose **Send request**, wait; **Cancel** closes without sending.
4. Manager reviews in schedule editor. Check decision/published schedule; submission is not automatic approval.

### Change password

1. Employee chooses **Edit** in profile, checks **Recovery email**, selects **Send code** (contacts saved first).
2. Enter **Verification code** and six-digit **New password**, choose **Change password**, wait for success.
3. Without session: **Forgot password?** → username → send code → code/new password → confirm → sign in again. Missing email requires operator.
4. Nonsystem manager changes own password via **New password** and **Save changes** in profile; environment administrator manages system account password.

## 8. Problems

| Problem | Action |
| --- | --- |
| Sign-in error | Check username/six digits. Wait as instructed if rate limited; use recovery. |
| Missing schedule | Check period/selection; ask manager to check assignment/publication. Private draft unavailable. |
| No availability group | Manager checks assignment/publication/time window; employee cannot create own group. |
| Manager phone write | Use **Switch to Desktop** in **More**. |
| Push unavailable | Check permission, HTTPS, support/iOS installation; report missing configuration. Read in-app inbox. |
| Duplicates/invalid values | Correct highlighted fields, keys, username/dates; do not add same person again. |
| Network/save failure/lock | Keep text, resolve cause, retry. Read first to see whether save already succeeded. Wait for another person's lock. |
| Stale offer/conflict | Refresh list/check schedule; link does not reserve shift. |

Report unresolved problems to the organization's operator/support contact. Include screen, time/error without passwords/codes/other people's data. This guide defines no SLA.

## 9. Sources and presentation coverage

[Routing](../../FrontEnd/src/app/router/AppRouter.tsx), [screens](../../FrontEnd/src/pages/), [components](../../FrontEnd/src/entities/), [PL labels](../../FrontEnd/src/shared/i18n/pl.json), [mode picker](../../FrontEnd/src/features/manager-workspace-mode/ui/ManagerWorkspaceModePicker.tsx), [regulation](../../FrontEnd/src/entities/regulations/ui/RegulationAcceptanceGate.tsx).

Presentation 1: p. 3 — README access (§1); p. 4 — roles/languages (§1–6); p. 5 — screens/fields/actions (§2–6); p. 6 — HOW-TO (§7); p. 7 — FAQ/navigation (§1, §8). Video/chatbot/separate help platform are course alternatives, not GF features.

Additional controls: [notepad](../../FrontEnd/src/features/manager-notepad/ui/ManagerNotepad.tsx), [system news](../../FrontEnd/src/features/manager-system-news/ui/ManagerSystemNews.tsx), [exports](../../FrontEnd/src/entities/exports/ui/ProfileExportActions/ProfileExportActions.tsx).
