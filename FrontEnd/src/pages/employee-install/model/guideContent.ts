import { shiftSwapGuideGroups } from "./shiftSwapGuide";

export type GuideStep = {
  title: string;
  visual: string;
  control?: string;
  instruction?: string;
  result?: string;
  passwordMode?: "pc" | "pin";
  extraVisuals?: readonly { visual: string; control: string }[];
};
export type GuideGroup = { title: string; steps: readonly GuideStep[]; id?: string };
export type GuideTopic = {
  id: "getting-started" | "notifications" | "schedules" | "work-hours" | "shift-corrections" | "shift-swaps" | "availability-urlop" | "profile";
  title: string;
  subtitle: string;
  groups: readonly GuideGroup[];
};

export const platformSteps: Record<"ios" | "android", readonly GuideStep[]> = {
  ios: [
    { title: "Open GF in Safari", visual: "ios-open", control: "Safari address bar" },
    { title: "Open the Share menu", visual: "ios-share", control: "Share",
      extraVisuals: [{ visual: "ios-share-sheet", control: "Safari Share sheet" }] },
    { title: "Find Add to Home Screen", visual: "ios-add", control: "Add to Home Screen" },
    { title: "Confirm installation", visual: "ios-confirm", control: "Add" },
    { title: "Open GF from your Home Screen", visual: "ios-launch", control: "GF icon on the Home Screen" },
    { title: "If installation does not work", visual: "ios-help", control: "Add to Home Screen" }
  ],
  android: [
    { title: "Open GF in Chrome", visual: "android-open", control: "Chrome menu button" },
    { title: "Open the browser menu", visual: "android-menu", control: "Browser menu" },
    { title: "Find Add to Home screen", visual: "android-add", control: "Add to Home screen" },
    { title: "Choose Install", visual: "android-install", control: "Install" },
    { title: "Confirm installation", visual: "android-confirm", control: "Install" },
    { title: "Open GF from your Home Screen", visual: "android-launch", control: "GF" }
  ]
};

export const guideTopics: readonly GuideTopic[] = [
  {
    id: "getting-started",
    title: "Getting Started",
    subtitle: "Install GF, sign in, and secure your account.",
    groups: [
      {
        title: "Sign in to your account",
        steps: [
          { title: "Enter your username", visual: "login", control: "Username" },
          { title: "Choose PC or PIN", visual: "login-modes", control: "Password input mode" },
          { title: "PC — Enter password using your keyboard", visual: "login-password", control: "Password", passwordMode: "pc",
            extraVisuals: [{ visual: "login-submit", control: "Sign in" }] },
          { title: "PIN — Enter password using the number keypad", visual: "login-pin-launch", control: "Enter password", passwordMode: "pin",
            extraVisuals: [{ visual: "login-pin-keypad", control: "Numeric keypad" }] },
          { title: "Open your employee workspace", visual: "login-workspace", control: "Employee sections" }
        ]
      },
      {
        title: "Set up your recovery email",
        steps: [
          { title: "Open Profile", visual: "nav-profile", control: "Profile" },
          { title: "Edit account details", visual: "profile-edit", control: "Edit account details" },
          { title: "Add a recovery email", visual: "profile-email", control: "Recovery email" },
          { title: "Save profile", visual: "profile-save", control: "Save profile" }
        ]
      },
      {
        title: "Change password",
        steps: [
          { title: "Change password", visual: "profile-password", control: "Change password" },
          { title: "Send code", visual: "profile-code", control: "Send code" },
          { title: "Enter the email code", visual: "profile-verification", control: "Verification code" },
          { title: "New password", visual: "profile-new-password", control: "New password" },
          { title: "Change password", visual: "profile-password-confirm", control: "Change password" }
        ],
        id: "password"
      }
    ]
  },
  {
    id: "notifications",
    title: "Notifications",
    subtitle: "Stay informed about schedules, shift offers, and important updates.",
    groups: [
      {
        title: "Find your alerts",
        steps: [
          { title: "Tap Alerts", visual: "nav-alerts", control: "Alerts" },
          { title: "Open Inbox", visual: "alerts-tabs", control: "Inbox" },
          { title: "Look for unread items", visual: "alerts-unread", control: "New schedule published" },
          { title: "Mark as read", visual: "alerts-read", control: "Mark as read" },
          { title: "Mark all read", visual: "alerts-all-read", control: "Mark all read" },
          { title: "Open the related item", visual: "alerts-link", control: "Open schedule" },
          { title: "New schedule published", visual: "alerts-example-schedule", control: "New schedule published" },
          { title: "New availability published", visual: "alerts-example-availability", control: "New availability published" },
          { title: "Shift swap available", visual: "alerts-example-swap", control: "Shift swap available" },
          { title: "Open shift posted", visual: "alerts-example-open-shift", control: "Open shift posted" },
          { title: "Your shift was accepted", visual: "alerts-example-accepted", control: "Your shift was accepted" },
          { title: "Switch to News", visual: "alerts-news", control: "News" },
          { title: "Mark as read", visual: "alerts-news-read", control: "Mark as read" }
        ]
      }
    ]
  },
  {
    id: "schedules",
    title: "Schedules",
    subtitle: "View your shifts, browse schedules, and understand schedule statuses.",
    groups: [
      {
        title: "See your shifts",
        steps: [
          { title: "Open Schedules", visual: "nav-schedule", control: "Shifts" },
          { title: "Today and tomorrow", visual: "schedule-overview", control: "Tomorrow" },
          { title: "Day-off example", visual: "schedule-day-off", control: "Day off" },
          { title: "Open the schedule selector", visual: "schedule-selector", control: "Select schedule" },
          { title: "Choose a schedule", visual: "schedule-picker", control: "Select schedule" },
          { title: "Schedule Matrix", visual: "schedule-matrix", control: "Schedule Matrix" },
          { title: "Tap the eye icon", visual: "schedule-eye", control: "Show daily schedule view" },
          { title: "Daily schedule", visual: "schedule-daily", control: "Daily schedule" },
          { title: "Export a PDF", visual: "schedule-pdf", control: "PDF" },
          { title: "Download PDF", visual: "pdf-download-example", control: "Download PDF" }
        ]
      },
      {
        title: "Customize Schedule Layout",
        steps: [
          { title: "Tap an employee’s name", visual: "schedule-order-header", control: "Customize schedule",
            instruction: "Open Schedules. If you are in Daily View, use the eye button to return to the Schedule Matrix. Tap an employee’s name at the top of their column." },
          { title: "Customize schedule", visual: "schedule-order-dialog", control: "Customize schedule",
            instruction: "The dialog lists employees in their current display order. The number on the left is the position. The dotted handle on the right moves that employee.",
            extraVisuals: [{ visual: "schedule-order-handle", control: "Customize schedule" }] },
          { title: "Drag the employee to a new position", visual: "schedule-order-drag", control: "Customize schedule",
            instruction: "Press and hold the dotted handle on the row you want to move. Drag it up or down without releasing. Let go when the row reaches the desired position." },
          { title: "Apply the new order", visual: "schedule-order-apply", control: "Apply",
            instruction: "Tap Apply at the bottom of Customize schedule. The matrix displays the employees in your chosen order.",
            result: "GF saves the order as a personal display preference. It does not change anyone’s shifts or the manager’s schedule." },
          { title: "Reset default", visual: "schedule-order-reset", control: "Reset default",
            instruction: "Open Customize schedule again and tap Reset default. Then tap Apply if it is enabled to save the restored order.",
            extraVisuals: [{ visual: "schedule-order-reset-apply", control: "Apply" }] },
        ]
      },
      {
        title: "Public schedules",
        steps: [
          { title: "View More", visual: "schedule-more", control: "View More" },
          { title: "Tap a public schedule", visual: "schedule-public", control: "Public schedules" },
          { title: "Schedule Preview", visual: "schedule-preview", control: "Schedule Preview" }
        ]
      },
      {
        title: "Schedule status indicators",
        steps: [
          { title: "Correction pending", visual: "schedule-correction", control: "Correction pending" },
          { title: "Read the status message", visual: "schedule-correction-bubble", control: "Correction pending" },
          { title: "Swap pending", visual: "schedule-open", control: "Swap pending" },
          { title: "Read the status message", visual: "schedule-open-bubble", control: "Swap pending" },
          { title: "Swap completed", visual: "schedule-complete", control: "Swap completed" },
          { title: "Read the status message", visual: "schedule-complete-bubble", control: "Swap completed" }
        ]
      }
    ]
  },
  {
    id: "work-hours",
    title: "Work Hours",
    subtitle: "Understand your scheduled hours and monthly totals.",
    groups: [
      {
        title: "How to find your working hours",
        steps: [
          { title: "Open Schedules", visual: "nav-schedule", control: "Shifts",
            instruction: "Find the calendar icon labeled Shifts in the bottom navigation. Tap it to open Schedules. Work hours is on that page, below the schedule. There is no separate Work Hours tab." },
          { title: "Scroll below the schedule", visual: "hours-location", control: "Work hours",
            instruction: "Pass the shift overview and the Schedule Matrix or Daily View. Look immediately below the schedule for Work hours and the Summary heading." },
          { title: "Choose the correct period", visual: "hours-period-controls", control: "Summary",
            instruction: "At the top of Summary, tap the month selector. Use the year selector beside it for a different year, then check the month again." },
          { title: "Read the table and total", visual: "hours-read-total", control: "Total",
            instruction: "Below the selectors, Day is the date, Hours are the scheduled hours, and Schedule lists their source schedules. Scroll to Total for the full selected month." },
        ]
      },
      {
        title: "Summary",
        steps: [
          { title: "Choose the month", visual: "hours-month-open", control: "Summary month" },
          { title: "Choose the year", visual: "hours-year-open", control: "Summary year" },
          { title: "Read the summary", visual: "hours-columns", control: "Hours" },
          { title: "Check the monthly total", visual: "hours-total", control: "Total" }
        ]
      }
    ]
  },
  {
    id: "shift-corrections",
    title: "Shift Corrections",
    subtitle: "Request changes when your scheduled working time is incorrect.",
    groups: [
      {
        title: "Send a correction",
        steps: [
          { title: "Adjust", visual: "adjust", control: "Adjust" },
          { title: "Choose a schedule", visual: "adjust-schedule", control: "Choose a schedule" },
          { title: "Choose a day", visual: "adjust-shift", control: "Choose a day" },
          { title: "Adjust shift time", visual: "adjust-time", control: "End time" },
          { title: "Request day off", visual: "adjust-day-off", control: "Request day off" },
          { title: "Request a shift on a day off", visual: "adjust-add-shift", control: "Start time" },
          { title: "Add a comment", visual: "adjust-comment", control: "Comment (optional)" },
          { title: "Review your request", visual: "adjust-review", control: "Requested shift" },
          { title: "Send request", visual: "adjust-send", control: "Send request" },
          { title: "Request sent successfully!", visual: "adjust-success", control: "Pending approval" },
          { title: "Correction pending", visual: "schedule-correction", control: "Correction requested" }
        ]
      }
    ]
  },
  {
    id: "shift-swaps",
    title: "Shift Swaps",
    subtitle: "Give away, exchange, or accept available shifts.",
    groups: shiftSwapGuideGroups
  },
  {
    id: "availability-urlop",
    title: "Availability & Urlop",
    subtitle: "Manage your availability and request time off.",
    groups: [
      {
        title: "Availability",
        steps: [
          { title: "Open Availability", visual: "availability-nav", control: "Availability" },
          { title: "Choose a published period", visual: "availability-period", control: "Availability" },
          { title: "Open the availability calendar", visual: "availability-calendar", control: "Team availability" },
          { title: "Select a day", visual: "availability-day", control: "Day" },
          { title: "Set your availability", visual: "availability-editor", control: "Available (whole day)" },
          { title: "Enter custom hours", visual: "availability-custom", control: "Custom time",
            instruction: "In the day editor choose Custom time. Enter the start and end under From and To. Tap Save to put the range into the calendar draft." },
          { title: "Use quick presets", visual: "availability-presets", control: "Quick presets" },
          { title: "Edit or add presets", visual: "availability-edit-presets", control: "Edit presets",
            instruction: "Tap Edit presets, then tap a saved range to change it or Add preset to create another. Save the preset and use Done to return to the time choices." },
          { title: "Select multiple days", visual: "availability-multiple", control: "Availability" },
          { title: "Save availability", visual: "availability-save", control: "Save availability" },
          { title: "Check the saved dates", visual: "availability-saved", control: "Availability",
            instruction: "After Save availability finishes, find the edited rows in your column. Confirm the new hours or availability symbols are shown for the correct dates." },
          { title: "Check editing restrictions", visual: "availability-restrictions", control: "Open" }
        ]
      },
      {
        title: "Urlop",
        steps: [
          { title: "Open the Urlop tab", visual: "availability-urlop", control: "Urlop" },
          { title: "Choose a shop", visual: "urlop-panel", control: "Shop",
            extraVisuals: [{ visual: "urlop-shops-open", control: "Shop" }] },
          { title: "Choose the month", visual: "urlop-month", control: "Month",
            extraVisuals: [{ visual: "urlop-month-open", control: "Month" }] },
          { title: "Choose vacation dates", visual: "urlop-date", control: "Day" },
          { title: "Check your selected dates", visual: "urlop-selected", control: "Vacation calendar",
            instruction: "Selected days are highlighted on the vacation calendar. Tap a selected day again to remove it before submitting." },
          { title: "Submit request", visual: "urlop-submit", control: "Submit request" },
          { title: "Find your vacation request", visual: "urlop-status", control: "Your requests" },
          { title: "Approved vacations of colleagues", visual: "urlop-colleagues", control: "Approved vacations of colleagues" }
        ]
      }
    ]
  },
  {
    id: "profile",
    title: "Your Profile",
    subtitle: "Manage personal information, security, language, and account preferences.",
    groups: [
      {
        title: "Profile",
        steps: [
          { title: "Open Profile", visual: "nav-profile", control: "Profile" },
          { title: "Check your sign-in details", visual: "profile-overview", control: "Profile" },
          { title: "App Guide", visual: "profile-guide", control: "App Guide" },
          { title: "Change the app language", visual: "profile-language-open", control: "Language" },
          { title: "Edit account details", visual: "profile-edit", control: "Account details" },
          { title: "Recovery email", visual: "profile-email", control: "Recovery email" },
          { title: "Save profile", visual: "profile-save", control: "Save profile" },
          { title: "Change password", visual: "profile-password", control: "Change password" },
          { title: "Send code", visual: "profile-code", control: "Send code" },
          { title: "Verification code", visual: "profile-password-fields", control: "Verification code",
            instruction: "Enter the verification code from your email, then enter your new six-digit password." },
          { title: "Change password", visual: "profile-password-confirm", control: "Change password" },
          { title: "Log out", visual: "profile-logout", control: "Log out" }
        ]
      }
    ]
  }
];
