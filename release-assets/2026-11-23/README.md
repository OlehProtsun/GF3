# GF employee release package · 23 November 2026

The ready-to-copy drafts are in [announcements](./announcements/). Each language has a main announcement and six short feature announcements. Polish is the primary draft. [The GIFs](./gifs/) use fictitious employee data and ShopDemo schedules.

## Prepare a communication

1. Open **Communications** as a manager. Copy the heading and body into **Title** and **Message**. Leave the Markdown heading marks and attachment note out of the message.
2. Choose **Upload GIF** under **Attachment (optional)**. Select one file from the table and check its preview, filename and size. One communication supports one GIF.
3. Set **Visible From** and **Visible To**, then save when it is time to publish. Use the version suitable for your audience. Communications displays the same saved text to all recipients, regardless of their app language.
4. Employees see the text as soon as the communication opens. The GIF loads separately and plays on repeat. Employees can hide it, and reduced-motion users can choose to show it. **Close** and **Don't show this again** still work as before.

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

Compared the checked-out release commit `7482e29` with the local `origin/PROD2` reference `1175c394` and inspected the owning code.

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

Before this change, the manager editor, Communications DTOs and message table were text-only. The separate News system was outside this change. The manager saves an uploaded GIF with the message. A nullable SQLite reference stores the generated key, filename and size. The binary is kept in `communication-gifs/` beside the active SQLite file, on the existing persistent volume.

The API accepts animated GIFs up to 3 MiB and 900 × 1600 pixels, with frame-count and decoded-canvas limits. It checks the file bytes and GIF block structure and writes the animation with an infinite loop. The authenticated media endpoint checks the active attachment key and the employee's publication window and dismissal status. A missing image shows a fallback while the text and dismissal controls stay available. Older text-only communications remain compatible.

Apply the Communications migration before serving the new code. Include the media directory with database backups and restores. Replacing or removing an attachment clears its current reference. Older files may remain on disk for backups, and old attachment links are no longer served. Avoid pruning files that retained database backups still reference.

The Communications migration is a nullable column addition. The full comparison with PROD2 also includes pre-existing index and constraint changes for swaps and corrections. The protected nullable-column deployment path cannot apply those migrations. This comparison is a release inventory, and no production rollout was attempted.

The eight GIFs were inspected and measured locally. Eleven focused Communications tests passed against temporary SQLite databases. A loopback Chromium demo verified manager preview, multipart submission and removal, and employee text-before-image rendering, autoplay, loading failure, reduced-motion behavior and dismissal. Its browser API responses were intercepted. Physical iPhone Safari, Android Chrome, and installed PWA playback still need a device check. The release package contains no production data and has not been published.
