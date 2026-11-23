# App Guide screenshot sources

The WebP images were captured from the GF frontend on branch `dev22.11.2026` at a 390 × 844 Chromium viewport. Each capture is cropped around a real GF control or panel. The guide adds the highlight separately using coordinates in `src/pages/employee-install/model/guideCaptures.json`.

The capture script, `scripts/capture-app-guide.mjs`, serves the frontend on localhost and uses synthetic schedules, people, account details, notifications, and approval outcomes. Playwright blocks access outside localhost and intercepts every API call. The local server rejects any API request that reaches it. Sign-in screenshots show GF’s default Polish guest interface. The employee demo uses English, with the guide’s instructions and labels available in all supported languages.

The guide uses fictional ShopDemo names for shop and schedule examples. Existing screenshots were edited offline to update those labels while retaining their original crops.

The View More image is cropped from the original GF capture. The PDF-ready panel remains a labeled illustration. The Availability and Urlop tutorial uses GF captures for the calendar, editor, presets, multi-day selection, save result, shop and month menus, selected vacation dates, request statuses, and approved colleague vacations.

The Shift Swaps walkthroughs use captures of the actual GF components rendered with synthetic schedules and API responses. The fixture supplies the two independent offers, the automatically reopened remainder, and the approval outcomes so these views can be captured without submitting any real employee requests. The three time-range diagrams are labeled illustrations. Recapture the Swap panels with `node scripts/capture-app-guide.mjs swap-manual swap-extra swap-restrictions`.

To recapture the screenshots, run `node scripts/capture-app-guide.mjs` from `FrontEnd` with the existing Playwright Chromium browser installed. To recapture one flow, pass its name, for example `node scripts/capture-app-guide.mjs profile`.

The focused capture flows are `schedule-guide-refine`, `availability-guide-refine`, `swap-offer-guide-refine`, and `profile-guide-refine`. The Profile crops are centered on the actual controls, with the floating navigation hidden in the capture browser only.

The Safari sign-in page, Share controls, Share action list, Add to Home Screen confirmation, and GF Home Screen icon use screenshots supplied from an iPhone. The crops omit sharing suggestions, personal apps, badges, the keyboard, and the Home Screen long-press menu. The Home Screen photo shows the icon only. The login comparison and keypad are captured in the local demo. The five Chrome installation screenshots come from the supplied Android captures. The Android launcher uses a labeled illustration with the GF app icon. Safari troubleshooting retains its labeled illustration.

The illustrations follow [Apple’s Safari installation guide](https://support.apple.com/guide/iphone/open-as-web-app-iphea86e5236/ios), [Apple’s bookmarks guide](https://support.apple.com/guide/iphone/bookmark-a-website-iph42ab2f3a7/ios), and [Google’s Android Chrome guide](https://support.google.com/chrome/answer/9658361?co=GENIE.Platform%3DAndroid). The [MacRumors iOS 26 walkthrough](https://www.macrumors.com/how-to/save-safari-bookmark-web-app-iphone-home-screen/) and [iPhone-Tricks older Safari walkthrough](https://iphone-tricks.de/anleitung/2964-website-als-icon-am-homescreen) were used only as visual references.
