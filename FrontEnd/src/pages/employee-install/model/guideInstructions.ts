type Instruction = { instruction: string; result?: string };

export const guideInstructions: Readonly<Record<string, Instruction>> = {
  "ios-open": {
    instruction: "Open Safari on your iPhone. Enter the GF website address your employer gave you and wait for the sign-in page to load.",
  },
  "ios-share": {
    instruction: "Tap the square with an upward arrow in Safari’s toolbar. If you don’t see it, open the Page Menu next to the address bar and choose Share.",
    result: "The iPhone opens the Share sheet with GF at the top.",
  },
  "ios-add": {
    instruction: "Swipe up inside Safari’s Share sheet to scroll down through the actions. Keep scrolling until you see Add to Home Screen, then tap it.",
  },
  "ios-confirm": {
    instruction: "Check the GF icon, app name, and website address on the confirmation screen. If Open as Web App appears, turn it on. Then tap Add in the upper-right corner.",
  },
  "ios-launch": {
    instruction: "After tapping Add, return to your iPhone’s Home Screen. Find the GF icon and tap it once.",
    result: "If GF was installed as a web app, it opens in its own window without Safari’s address bar.",
  },
  "ios-help": {
    instruction: "Open the GF address in Safari and check that the page loads. Look for Share in the toolbar or Page Menu, then check Edit Actions if Add to Home Screen is missing. You can reopen Safari and try again.",
    result: "Once the option appears, return to the Add to Home Screen step.",
  },
  "android-open": {
    instruction: "Open Google Chrome on your Android phone. Enter app-gf.com in the address bar and wait for the GF sign-in page to load.",
  },
  "android-menu": {
    instruction: "Tap the three-dot button at the top right of Chrome. If Chrome shows a red update icon in that spot, tap it to open the same menu. Swipe up inside the menu to see more options.",
  },
  "android-add": {
    instruction: "Scroll down inside the Chrome menu until you see Add to Home screen, then tap it. Some versions show Install and create shortcut or Install app instead.",
  },
  "android-install": {
    instruction: "If Chrome offers Install and Create shortcut, tap Install. If it takes you straight to the confirmation window, continue there.",
    result: "Choose Install to use GF as a standalone app. Create shortcut opens GF in Chrome.",
  },
  "android-confirm": {
    instruction: "In the Install app window, check the GF icon, app name, and app-gf.com address. Tap Install in the lower-right corner to confirm.",
  },
  "android-launch": {
    instruction: "Return to your Android Home Screen or open the app launcher. Find the GF icon and tap it.",
    result: "When installed as a web app, GF opens in its own window without the normal Chrome tab.",
  },
  "login": {
    instruction: "Open GF and enter the username your manager gave you. Below Username, the Password section offers PC and PIN.",
  },
  "login-modes": {
    instruction: "Both modes use the same six-digit account password. You don’t need to set up a separate PIN.",
  },
  "login-password": {
    instruction: "Select PC, tap the password field and type your six-digit password. GF hides the digits as you type. Tap Sign in after entering all six digits.",
    result: "PC mode works on computers and phones.",
  },
  "login-pin-launch": {
    instruction: "Select PIN and tap Enter password after entering your username. On the touchscreen keypad, tap the six digits of your account password. The dots hide the digits. Use Delete to remove the last digit.",
    result: "GF automatically tries to sign you in after the sixth digit. If your username or password is incorrect, GF shows an error.",
  },
  "login-workspace": {
    instruction: "After you sign in, GF opens your employee workspace. Use the bottom navigation to open Schedules, Alerts, Availability, Swap, or Profile.",
  },
  "nav-profile": {
    instruction: "Tap the person icon at the bottom of the employee workspace. Look for Profile at the top of the next screen.",
    result: "You can see your name and username, App Guide, Account details, and Password & security. Log out is further down.",
  },
  "profile-edit": {
    instruction: "Find Account details below your name. Tap Edit account details to change your recovery email or phone number.",
    result: "The contact details become editable and Save profile appears.",
  },
  "profile-email": {
    instruction: "Tap Recovery email in the edit form. Enter an email address you can open.",
    result: "The address is ready to save. GF will use the saved address to deliver password codes.",
  },
  "profile-save": {
    instruction: "Tap Save profile below the contact fields. Check the Recovery email row when the form closes.",
    result: "The updated address is displayed in Account details.",
  },
  "profile-password": {
    instruction: "Scroll to Password & security in Profile. Once a recovery email is connected, tap Change password.",
    result: "GF opens the code and password form.",
  },
  "profile-code": {
    instruction: "Tap Send code in the password form. Open the inbox for your recovery email and look for the latest message from GF.",
    result: "The email contains the verification code you need for the next step.",
  },
  "profile-verification": {
    instruction: "Return to GF after reading the email. Enter its code in Verification code.",
    result: "The code is ready to submit with your new password.",
  },
  "profile-new-password": {
    instruction: "Tap New password. Enter a new password made up of exactly six digits.",
    result: "The form can be submitted when both fields are valid.",
  },
  "profile-password-confirm": {
    instruction: "Tap Change password below the fields. If GF asks you to sign in again, use the new password.",
    result: "After a successful change, the new password is used for sign-in.",
  },
  "nav-alerts": {
    instruction: "Tap Alerts in the bottom navigation. Look for the inbox-shaped icon.",
    result: "The Notifications page opens with Inbox and News.",
  },
  "alerts-tabs": {
    instruction: "Look at the two tabs near the top. Tap Inbox for updates about schedules, availability, and shifts. Use News for announcements.",
  },
  "alerts-unread": {
    instruction: "Look for the dot beside the notification title. Read the title and message directly on the card.",
    result: "The dot identifies an unread item. The expiry text shows how much longer it stays in Inbox, usually about five days from the event.",
  },
  "alerts-link": {
    instruction: "Read the card title, message, and any shown time or expiry. Tap Open schedule on a publication alert to see the shifts.",
    result: "The related GF section opens.",
  },
  "alerts-example-schedule": {
    instruction: "Find New schedule published in Inbox. Read the schedule name and month, then tap Open schedule.",
    result: "The published schedule is available in Shifts.",
  },
  "alerts-example-availability": {
    instruction: "Read the deadline on New availability published. Tap Open availability to find the period and enter your available days.",
    result: "If the period is still open, you can submit your availability there.",
  },
  "alerts-example-swap": {
    instruction: "Find Shift swap available and note the date and hours. Tap Open swap to review the coworker’s offer.",
  },
  "alerts-example-open-shift": {
    instruction: "Read Open shift posted for the schedule and hours. Tap Open swap to check whether you can take the shift posted by your manager.",
  },
  "alerts-example-accepted": {
    instruction: "When Your shift was accepted appears, read who took the shift. Tap View swap to see the related activity.",
    result: "You can also check the updated assignment in Shifts.",
  },
  "alerts-read": {
    instruction: "Find Mark as read on the card you have reviewed. Tap it once.",
    result: "That card loses its unread marker.",
  },
  "alerts-all-read": {
    instruction: "Open Inbox or News, depending on what you want to clear. Tap Mark all read above the list.",
    result: "The unread markers in the selected tab are cleared.",
  },
  "alerts-news": {
    instruction: "Tap News at the top of Notifications. Read the announcement and use its media controls if it contains a video.",
  },
  "alerts-news-read": {
    instruction: "Find Mark as read on the announcement you have finished. Tap it to clear its unread marker.",
  },
  "nav-schedule": {
    instruction: "Find the calendar icon labeled Shifts in the bottom navigation. Tap it once.",
    result: "The page shows the current date and time, Now and Tomorrow, the selected schedule, and Matrix or Daily view. Work hours is further down.",
  },
  "schedule-overview": {
    instruction: "Find Now near the top. Read the shift’s hours and schedule there. Check Tomorrow for the next day.",
    result: "When a shift is in progress, the page can show a countdown.",
  },
  "schedule-day-off": {
    instruction: "Check the same overview when there is no assigned shift. Look for Day off in place of the shift hours.",
  },
  "schedule-selector": {
    instruction: "Find the current schedule name above the matrix, for example ShopDemo1. Tap the name.",
    result: "The Select schedule dialog opens.",
  },
  "schedule-picker": {
    instruction: "Find the month you need in Select schedule. The current month comes first when available, with schedules ordered by your assigned shift count. Tap the schedule you want.",
    result: "The checkmark shows the selected schedule. GF initially opens the top-ranked schedule. You can change the view without changing assignments.",
  },
  "schedule-matrix": {
    instruction: "Read the date in the left column and follow that row to the employee’s name. A cell such as 09:00–15:00 is a shift. A dash means no assigned shift in that cell, and an extra schedule name can indicate work elsewhere. Swipe sideways for more employees and scroll down for more dates.",
  },
  "schedule-eye": {
    instruction: "Find the eye icon beside PDF above the schedule. Tap the eye once.",
    result: "The monthly matrix changes to Daily view.",
  },
  "schedule-daily": {
    instruction: "Swipe along the date strip and tap the day you need. Check the employee names and shift hours below it. Tap the eye again to return to the matrix.",
    result: "The highlighted date is the day shown in the list.",
  },
  "schedule-pdf": {
    instruction: "Open the schedule you want to export. Tap the PDF icon beside the eye and wait while GF prepares the file.",
    result: "Use the share options on supported devices or find the file in your downloads. If GF shows the PDF is ready and asks you to tap again, tap PDF again.",
  },
  "schedule-more": {
    instruction: "Open Select schedule. Scroll below your schedule cards and tap View More.",
    result: "Public schedules opens in the same dialog.",
  },
  "schedule-public": {
    instruction: "Find the published schedule you want to inspect. Tap its name.",
    result: "Schedule Preview opens without selecting that schedule as your main view.",
  },
  "schedule-preview": {
    instruction: "Read the preview matrix, use the eye for Daily view, or tap PDF if needed. Use Back to return to the public list or close the dialog to return to your schedule.",
  },
  "schedule-correction": {
    instruction: "Find the orange clock beside a shift. Tap it to read the correction status.",
  },
  "schedule-open": {
    instruction: "Find the blue swap icon beside a shift and tap it. It identifies an offer that has yet to transfer the shift.",
  },
  "schedule-complete": {
    instruction: "Find the green checkmark beside a shift and tap it. It identifies a transfer or exchange that was applied.",
  },
  "schedule-correction-bubble": {
    instruction: "Read the message beside the orange clock. The shift stays as scheduled while the manager considers the request.",
  },
  "schedule-open-bubble": {
    instruction: "Read the message beside the blue icon. The offered shift is waiting for acceptance.",
  },
  "schedule-complete-bubble": {
    instruction: "Read the message beside the green checkmark. The transfer or exchange has been applied.",
  },
  "availability-period": {
    instruction: "Tap the published card that matches your month or availability group. Check its month and opening status before expanding it.",
    result: "The calendar and the submission deadline appear.",
  },
  "availability-day": {
    instruction: "Find your column and the date you want. Tap that cell to edit the day.",
    result: "An editor opens with availability options for that date.",
  },
  "availability-editor": {
    instruction: "Choose Available (whole day), Not available, or Custom time in the editor. For Custom time, use the From and To fields or pick a quick preset.",
    result: "Check that your own cell shows the saved choice while submissions are open.",
  },
  "availability-urlop": {
    instruction: "Open Availability, then tap Urlop beside its main tab. Use this tab for vacation requests.",
  },
  "urlop-panel": {
    instruction: "Tap Shop above the vacation calendar and choose the shop for which you want to request time off.",
    result: "The calendar and available months update for that shop.",
  },
  "urlop-date": {
    instruction: "Tap each available date and check the highlighted selection. Past dates and dates already covered by an active request cannot be selected. A closed month cannot be edited.",
  },
  "urlop-submit": {
    instruction: "Tap Submit request below the calendar. Scroll to Your requests to check the dates and status.",
    result: "A successful submission appears in Your requests. Check there for the manager’s decision.",
  },
  "hours-month": {
    instruction: "Open Shifts and scroll down to Work hours, then find Summary. Tap the month selector at the top of this section.",
  },
  "hours-month-open": {
    instruction: "Tap the month you want in the list. If there is only one available month, GF shows it without a choice.",
    result: "The rows refresh for the selected period.",
  },
  "hours-year-open": {
    instruction: "Tap the year selector beside the month. Choose the year you need, then check the month again.",
    result: "The summary now shows the selected period.",
  },
  "hours-columns": {
    instruction: "Read the date in Day, then the scheduled hours and contributing schedules on the same line. Six hours in ShopDemo1 and six in ShopDemo2 may appear as 12 hours with both schedules listed.",
  },
  "hours-total": {
    instruction: "Scroll to Total below the dates. Check the month and year before comparing the total with your schedule.",
    result: "The total sums scheduled hours. For a wrong shift, check contributing schedules and use Adjust. This summary does not verify attendance or payroll.",
  },
  "adjust": {
    instruction: "Open Shifts and find Adjust beside the schedule controls. Tap it once.",
    result: "The correction wizard opens on its schedule step.",
  },
  "adjust-schedule": {
    instruction: "Find the published schedule that contains the shift you need to change. Tap its card, then tap Continue.",
    result: "GF shows the month’s dates and shifts for that schedule.",
  },
  "adjust-shift": {
    instruction: "Find the date and tap the right shift or an eligible day off. An orange entry can already have a pending request. GF can also show that you work in another schedule that day. Tap Continue after selecting a valid item.",
    result: "The time editor opens for the chosen date.",
  },
  "adjust-time": {
    instruction: "Compare Original shift and Requested shift. If you worked until 16:00 instead of 15:00, tap End time and enter 16:00, or tap +15 four times. Use Start time to change the beginning.",
    result: "Requested shift shows the hours you are proposing.",
  },
  "adjust-day-off": {
    instruction: "Use Request day off when your assigned shifts should be removed on that date. Tap the option and check Requested shift.",
    result: "A dash appears. The assigned shifts stay in place until the manager approves the request.",
  },
  "adjust-add-shift": {
    instruction: "Go back to the date list and choose an eligible Day off. Enter the Start and End times you actually want to request.",
    result: "The original shows Day off and the requested shift shows your entered hours.",
  },
  "adjust-comment": {
    instruction: "Tap the optional comment field if the manager needs context. For example, explain that you worked until 16:00 instead of 15:00. Tap Continue when ready.",
  },
  "adjust-review": {
    instruction: "Check the schedule, date, original shift, and requested shift on Review. Read the comment if you entered one. Use Back if anything needs changing.",
  },
  "adjust-send": {
    instruction: "Tap Send request once. Wait for the green confirmation before leaving the wizard.",
  },
  "adjust-success": {
    instruction: "Check that the confirmation says Pending approval. Tap Done to return to Shifts.",
    result: "An approved correction changes the schedule. A rejected correction leaves the original shift in place.",
  },
  "nav-swap": {
    instruction: "Tap Swap in the bottom navigation. Look for the two-arrow icon.",
    result: "The page opens with available offers and the Create offer and Swap history controls.",
  },
  "swap-offer": {
    instruction: "Read the schedule, date, employee name, and hours on an offer card. Tap the card to see the full details.",
    result: "The details show whether you can take the shift and whether it allows a shorter interval.",
  },
  "swap-accept": {
    instruction: "If the offer is available to you, check the Full shift option. Where allowed, tap Custom period and choose an eligible time range instead.",
  },
  "swap-confirm": {
    instruction: "Use the acceptance button and check the final times in the confirmation prompt. Confirm only if the details match the shift you want.",
    result: "Some offers require manager approval before the schedule changes.",
  },
  "swap-page": {
    instruction: "On Swap, tap Create offer, the plus button near the top. Use this when you want to offer a shift assigned to you.",
    result: "The wizard opens with schedules you can use for an offer.",
  },
  "swap-schedule": {
    instruction: "Tap the schedule that contains the shift you want to offer. Check the month and year before selecting it.",
    result: "GF opens the shift picker for that schedule.",
  },
  "swap-shift": {
    instruction: "Find the date and time in the shift picker. Tap the shift, then tap Confirm shift if you want to offer it in full.",
  },
  "swap-partial": {
    instruction: "If Custom period is available, tap it and set the part of the shift you want to offer. Tap Confirm shift and then Continue.",
    result: "The selected time range carries over to the recipient step.",
  },
  "swap-everyone": {
    instruction: "Tap Everyone to offer the shift to any eligible coworker. Check whether the offer allows only the exact shift or also custom time, then tap Continue.",
  },
  "swap-specific": {
    instruction: "Tap Specific employee and then choose the coworker from the list. To give them the shift without an exchange, choose Give shift and save the selection.",
  },
  "swap-exchange": {
    instruction: "After choosing a coworker, select Swap shifts. Choose the shift you want to receive in return. Save the selection and continue to Review.",
  },
  "swap-review": {
    instruction: "Check the schedule, date, hours, recipient, and request type. For an exchange, also check the shift you will receive. Use Back if anything is wrong.",
  },
  "swap-send": {
    instruction: "Tap Send request once. If the wizard offers Request exception instead, use it to ask for manager review.",
    result: "Wait for confirmation before closing the wizard.",
  },
  "swap-success": {
    instruction: "Read the status on the green confirmation screen. Tap Done to return to Swap.",
    result: "Pending acceptance means the offer is waiting for a coworker. Pending manager approval means it is waiting for the manager. Neither is a completed transfer.",
  },
  "swap-history-button": {
    instruction: "Return to Swap and tap Swap history near Create offer. Scroll inside the panel to find a past offer.",
  },
  "swap-history": {
    instruction: "Find the offer by its date and schedule. Read its status before checking the updated schedule.",
    result: "Accepted means the transfer was applied. Cancelled means the offer is no longer active.",
  },
  "profile-overview": {
    instruction: "Look beneath your display name to find your username. Scroll down to Account details, Password & security, or Log out.",
  },
  "profile-guide": {
    instruction: "Look above your profile details for the App Guide card with a book icon. Tap the card.",
    result: "The seven guide topics open.",
  },
  "profile-language-open": {
    instruction: "Open the language selector at the top of Account details. Tap Polski, Українська, or English in the list.",
    result: "The interface and the App Guide switch to your selected language.",
  },
  "profile-logout": {
    instruction: "Scroll to the bottom of Profile. Tap Log out when you are finished using GF.",
    result: "Your session ends. Use your credentials to sign in again.",
  },
  "pdf-download-example": {
    instruction: "If GF offers a second tap, choose PDF again to share. Use Download PDF to save the file instead.",
  },
  "urlop-history": {
    instruction: "Find Your requests below the calendar. Match the listed dates to the request you sent. Read the status next to them.",
  },
  "availability-nav": {
    instruction: "Tap the calendar with a checkmark in the bottom navigation. It may be labeled Avail. on a phone.",
    result: "The page opens with Availability and Urlop tabs and the published periods below.",
  },
  "availability-calendar": {
    instruction: "Expand the published period to see the calendar. Dates run down the left, and your saved values appear in the column marked You. A plus means available all day, a minus means unavailable, and a time range shows your available hours.",
    result: "Only enabled cells in your own column can be edited. A vacation can appear as UR.",
  },
  "availability-presets": {
    instruction: "Select Custom time. Tap a range under Quick presets to fill From and To, or enter the times yourself. When Edit presets is enabled, tap it to edit a range or choose Add preset to create one.",
    result: "After editing a preset, tap Save preset. In the day editor, tap Save to apply the selected time to the day.",
  },
  "availability-multiple": {
    instruction: "Press and hold the first date in your own column. Drag up or down to the last date, then release. This selects a continuous range of days.",
    result: "The editor shows how many days are selected. Choose one availability value and tap Save to apply it to the whole range.",
  },
  "availability-save": {
    instruction: "Tap Save in the day editor to apply the choice to the calendar. Then tap Save availability below the calendar to submit your changes.",
    result: "After saving, check the edited dates in your own column.",
  },
  "availability-restrictions": {
    instruction: "Check the label on the period card. Open means eligible employees can edit before the deadline. Closed means submissions are over. Locked can mean a manager is editing the period.",
    result: "Automatic means availability is set by employment-contract rules and cannot be edited in this period.",
  },
  "urlop-month": {
    instruction: "Tap Month beside Shop. Choose the entry with the month and year you need, then check that it is open for requests.",
  },
  "urlop-status": {
    instruction: "In Your requests, find the dates you submitted. Pending means the request is awaiting a decision. Approved means it was accepted. Rejected means it was declined.",
  },
  "urlop-colleagues": {
    instruction: "Scroll below Your requests to Approved vacations of colleagues. When the selected shop and month have approved colleague entries, the list shows their names and dates.",
    result: "UR marks approved vacation on the calendar.",
  },
};
