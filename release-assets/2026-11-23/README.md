# GF employee release package · 23 November 2026

The ready-to-copy drafts are in [announcements](./announcements/). Each language has a main announcement and six short feature announcements. Polish is the primary draft. [The GIFs](./gifs/) use fictitious employee data and ShopDemo schedules.

## Prepare a communication

1. Open **Communications** as a manager. Copy the heading into **Title** and paste the announcement into a **Text** block. Leave out the Markdown heading marks and attachment note. A message requires text.
2. Add **Text**, **Photo**, and **GIF** blocks in **Message Content**. A message supports up to 20 ordered blocks. New files are limited to 3 MiB each and 8 MiB combined per save. Use **Replace** or **Remove** on individual blocks as needed.
3. Move blocks with the up/down controls or the drag handle. Open **Preview** to check the current draft order and media. Set **Visible From** and **Visible To**, then choose **Create** or **Save changes**. Communications displays the same saved text to all recipients.
4. Employees see the blocks in the saved order. Photos can be enlarged. GIFs loop and can be hidden. Reduced-motion users can choose when to play an animation. Select **Don't show this again** before closing to dismiss the message permanently for that employee.

The main announcement can be sent without a GIF or paired with **08**. The feature drafts list the recommended attachment. Use **07** as a reminder about an existing feature.

## GIF files

All are 390 × 844 pixels, 8 fps, and loop. Sizes use KiB. These are edited sequences of authentic screenshots captured while exercising the local GF frontend. API responses came from a loopback-only fixture. No real announcement, correction or swap was submitted.

| File | Announcement | Duration | Size |
| --- | --- | ---: | ---: |
| [01-schedule-selection.gif](./gifs/01-schedule-selection.gif) | Choose a schedule by month | 6.13 s | 175 KiB |
| [02-schedule-views.gif](./gifs/02-schedule-views.gif) | Choose a schedule by month, with the focus on Daily View | 6.01 s | 127 KiB |
| [03-shift-correction.gif](./gifs/03-shift-correction.gif) | Request a shift correction or a day off | 7.51 s | 167 KiB |
| [04-request-day-off.gif](./gifs/04-request-day-off.gif) | Request a shift correction or a day off, with the focus on removal | 6.01 s | 174 KiB |
| [05-partial-shift-swap.gif](./gifs/05-partial-shift-swap.gif) | Offer part of a shift | 7.76 s | 164 KiB |
| [06-swap-status.gif](./gifs/06-swap-status.gif) | Check your swap status in the schedule | 6.13 s | 95 KiB |
| [07-work-hours.gif](./gifs/07-work-hours.gif) | Where to check your work hours | 5.88 s | 140 KiB |
| [08-app-guide.gif](./gifs/08-app-guide.gif) | Explore the GF App Guide | 7.38 s | 260 KiB |

In **05**, the demo creates the offer through the normal interface with an intercepted request, then reopens the picker to show the still-available hours. **06** shows two different demo entries and their details. **08** records the guide with its genuine Safari capture; it does not record a system installation in Chromium. The correction demos stop at Review.

## Release inventory

The [release QA report](../../docs/release-2026-11-23-qa.md) records the pinned source/target comparison and validation evidence.

| Area | What changed |
| --- | --- |
| Schedule selection | New month grouping and cards. The Warsaw current month is prioritized, and the default is ranked by the employee's assigned shift count. A manager's schedule remains unchanged when an employee previews a published schedule through **View More**. Preview access is restricted to related published schedules, with matrix, daily and PDF views. |
| Schedule views | Regular matrix and Daily View, drag-to-reorder columns, regular PDF export, and related schedule names already existed. The read-only preview reuses the views and PDF export; the main matrix and Daily View gain correction and swap badges. |
| Corrections | New five-step workflow with schedule, day or shift, time, comment, and review. Includes 15-minute adjustments, request removal of assigned shifts for the selected day and schedule, submission confirmation, and pending badges. Requesting a shift on a day off already existed. Removal applies only after manager approval. |
| Swaps | Separate non-overlapping offers on one original shift where there is free time and the rules permit it. New confirmation after sending and new pending/completed schedule badges. Full and custom periods, everyone and specific recipients, exchanges, and allowing eligible partial acceptance were already present on PROD2. |
| Work hours | Month/year selector, scheduled-hours totals across schedules, and the monthly summary already existed. The accompanying draft is a reminder. Scheduled hours do not certify attendance or pay. |
| Guide and PWA | New eight-topic App Guide from Profile and the browser install invitation. The tutorial has an independent language setting and real Safari/Chrome installation captures. Login's existing numeric entry mode is now labeled **PIN**. The installed-app splash exits more smoothly; this release makes no startup speed claim. |
| Alerts and News | The existing Inbox and News tabs, read actions, and News workflow remain available. The guide explains the existing controls; they are not presented as new. |

## Communications implementation and validation

The editor persists an ordered list of blocks with opaque media keys. SQLite stores the references, and the binaries live in `communication-gifs/` beside the SQLite file on the existing persistent volume. Older text-only messages and legacy single attachments remain readable.

The API accepts animated GIFs up to 3 MiB and 900 × 1600 pixels, with frame-count and decoded-canvas limits. It validates GIF structure and normalizes the loop. Photos accept JPEG, PNG and WebP, enforce a 16 MP limit, apply the source orientation and produce WebP from decoded pixels. The media endpoints check the attachment key, publication window and dismissal status. If an attachment is unavailable, the other blocks and dismissal controls stay accessible.

Coordinated SQLite and media backup and restore are release prerequisites. Replacing or removing an attachment clears its current reference. Older files may remain on disk for backups. Preserve media referenced by retained database backups.

The comparison with PROD2 includes three nullable columns for communications and non-additive index and constraint changes for swaps and corrections. The protected nullable-column maintenance path blocks the full chain. The production release gate remains **BLOCKED**.

See the [release QA report](../../docs/release-2026-11-23-qa.md) for the test results for this candidate. Physical iPhone Safari, Android Chrome, and installed PWA playback still require device verification.
