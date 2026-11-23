import type { GuideGroup } from "./guideContent";

export const shiftSwapGuideGroups: readonly GuideGroup[] = [
  {
    id: "understanding",
    title: "Understanding Swap",
    steps: [
      {
        title: "Open Swap from the navigation", visual: "nav-swap",
        instruction: "Tap Swap in the bottom navigation. The page shows open offers and the controls for making your own offer or checking past activity.",
      },
      {
        title: "Find offers", visual: "swap-manual-filters",
        instruction: "Use All to browse, My offers to see your own, Can to narrow to offers you can act on, or Pinned to find saved cards. Use the search box to narrow the list. Pin a useful card or copy its link using the card controls.",
      },
      {
        title: "Choose what you want to do", visual: "swap-offer",
        instruction: "Open a card to read its date, hours, and availability. Use Create offer to give away one of your shifts or propose an exchange. For receiving a shift, start from an eligible offer instead.",
      },
      {
        title: "Copy swap link", visual: "swap-manual-copy",
        instruction: "Tap the link icon on the offer card. Share the copied link with someone who already has access to that offer; a link does not make a private offer public.",
      },
    ],
  },
  {
    id: "open-offer",
    title: "How to Open a Shift Offer",
    steps: [
      { title: "Open Swap", visual: "swap-guide-list",
        instruction: "Tap Swap in the bottom navigation. Scan the rounded cards for a shift you might want. The list shows the schedule, date, offered hours, and current availability." },
      { title: "Find a shift and check its labels", visual: "swap-guide-labels-public",
        instruction: "Start with the schedule name, date, hours and From line. Under these details, Public means eligible coworkers can see the offer. Can means you can currently take action, subject to any required approval. Partial means eligible employees can claim a shorter period." },
      { title: "Private", visual: "swap-guide-labels-private",
        instruction: "A private offer is directed to a specific employee. Check the details for the recipient and whether this is a giveaway or an exchange." },
      { title: "My offer", visual: "swap-guide-labels-mine",
        instruction: "My offer identifies a shift you offered. Use My offers in the list to find your own cards and open one to manage it." },
      { title: "Can’t", visual: "swap-guide-labels-locked",
        instruction: "Can’t means you cannot accept this offer right now. Open the details and read the explanation when one appears. Locked means the schedule is locked, so some actions may be unavailable." },
      { title: "Manager approval", visual: "swap-guide-labels-approval",
        instruction: "Manager approval marks an offer that needs an additional approval step. Open the offer and check whether Request manager approval is available." },
      { title: "Awaiting approval", visual: "swap-guide-labels-held",
        instruction: "Awaiting approval means the request is already waiting for a manager’s decision. The transfer has not yet been applied." },
      { title: "Finished", visual: "swap-guide-labels-finished",
        instruction: "Finished identifies a manager-posted open shift whose time has ended. It is no longer available to accept." },
      { title: "Accepted and cancelled offers", visual: "swap-guide-labels-history",
        instruction: "Look in Swap history for Accepted or Cancelled. Accepted means the transfer or exchange was applied. Cancelled means that offer ended without a transfer.",
        extraVisuals: [{ visual: "swap-guide-labels-accepted", control: "Accepted" }] },
      { title: "Tap the main offer card", visual: "swap-guide-open-card",
        instruction: "Tap the area with the schedule name, date, or hours. The entire highlighted card body opens Offer details. Pin and Copy Link are separate controls beside it." },
      { title: "Read Offer details", visual: "swap-guide-details",
        instruction: "Check the employee, schedule, date and interval. Read the request type, visibility and status. For an exchange, review the return shift as well before taking any action." },
      { title: "Check the available action", visual: "swap-guide-actions",
        instruction: "Read the button at the bottom. Accept shift is available when you can take the shift. If the action is disabled, read the reason in the dialog before trying another offer.",
        extraVisuals: [{ visual: "swap-guide-unavailable", control: "Locked" }] },
      { title: "Check flexible acceptance", visual: "swap-manual-claim",
        instruction: "If the offer allows custom time, look for Full shift or Custom period. Only eligible hours are selectable." },
      { title: "Request manager approval", visual: "swap-manual-approval-action",
        instruction: "If the button says Request manager approval, review the interval and follow that workflow. The manager must decide before the transfer can be applied." },
      { title: "Confirm after checking the hours", visual: "swap-guide-confirm",
        instruction: "Check the hours one last time in the prompt. Follow the action shown to accept the shift or submit the required approval request." },
      { title: "Close", visual: "swap-guide-close",
        instruction: "If you decide against taking the shift, tap Close at the top of Offer details to return to the list." },
    ],
  },
  {
    id: "full",
    title: "Giving away a full shift",
    steps: [
      {
        title: "Create offer", visual: "swap-page",
        instruction: "On Swap, tap Create offer. The wizard starts with a list of published schedules that allow swaps.",
      },
      {
        title: "Choose a schedule", visual: "swap-schedule",
        instruction: "Choose ShopDemo1 for November. The shift picker lists your assigned shifts in that schedule.",
      },
      {
        title: "Choose your shift", visual: "swap-manual-shift",
        instruction: "Tap November 9, 09:00–21:00. Check the day and hours before selecting how much to offer.",
      },
      {
        title: "Full shift", visual: "swap-manual-full",
        instruction: "Below the selected shift, leave Full shift selected. Tap Confirm shift, then Continue. The entire 12-hour interval will be offered.",
      },
      {
        title: "Only the exact shift", visual: "swap-manual-exact",
        instruction: "On Recipient, choose Everyone and leave Only the exact shift selected. A coworker must accept the complete offered interval, 09:00–21:00.",
      },
      {
        title: "Review", visual: "swap-manual-full-review",
        instruction: "Tap Continue and check ShopDemo1, November 9, 09:00–21:00, Everyone, and the acceptance mode. Use Back if any detail needs changing.",
      },
      {
        title: "Send request", visual: "swap-manual-full-success",
        instruction: "Tap Send request once and wait for the green confirmation. Tap Done. An ordinary offer stays open until it is accepted, cancelled, or otherwise resolved; sending it does not transfer the shift.",
      },
    ],
  },
  {
    id: "part",
    title: "Giving away only part of a shift",
    steps: [
      {
        title: "Custom period", visual: "swap-manual-time",
        instruction: "In Create offer, choose ShopDemo1 and the November 9 shift 09:00–21:00. Under it, choose Custom period. Set From to 09:00 and To to 15:00; you can use the time fields or their 15-minute buttons.",
      },
      {
        title: "Check the offered interval", visual: "swap-timeline-part",
        instruction: "Tap Confirm shift once the displayed interval is 09:00–15:00. The later hours, 15:00–21:00, are still assigned to you and are outside this offer.",
      },
      {
        title: "Review", visual: "swap-manual-half-review",
        instruction: "Continue to Recipient and choose who can receive the offer. On Review, check that only 09:00–15:00 is listed. Send the request and wait for acceptance or any required approval.",
      },
    ],
  },
  {
    id: "separate",
    title: "Giving away separate parts of the same shift",
    steps: [
      {
        title: "Send the first offer", visual: "swap-manual-first-open",
        instruction: "In the wizard, use ShopDemo1, November 9, and Custom period to offer 09:00–15:00. Choose a recipient, review and send. After Done, look for this interval under My offers.",
      },
      {
        title: "Find reserved and available hours", visual: "swap-manual-reserved",
        instruction: "Start another offer for the same schedule and original shift. Awaiting confirmation marks 09:00–15:00 as reserved. Available marks 15:00–21:00. You cannot make a second offer that overlaps the reserved time.",
      },
      {
        title: "Select the second half", visual: "swap-manual-second-time",
        instruction: "Tap the original shift again. Choose or keep Custom period and check From 15:00 and To 21:00. Confirm the shift, choose a recipient, check Review, and send.",
      },
      {
        title: "Check both offers", visual: "swap-manual-two",
        instruction: "Under My offers, check that 09:00–15:00 and 15:00–21:00 appear separately. Each can be accepted or cancelled on its own. Assignments change only after the corresponding transfer is applied.",
      },
    ],
  },
  {
    id: "everyone",
    title: "Offering a shift to everyone",
    steps: [
      {
        title: "Everyone", visual: "swap-everyone",
        instruction: "After confirming your shift or custom interval, tap Everyone under Recipient. Choose this for any eligible coworker. To choose one person instead, use Specific employee.",
      },
      {
        title: "Choose the acceptance mode", visual: "swap-manual-flexible",
        instruction: "Check the acceptance mode that appears below Everyone. Tap Continue and check the audience before sending. A public offer still has to pass the recipient’s eligibility checks.",
      },
    ],
  },
  {
    id: "acceptance-mode",
    title: "Only the exact shift or Allow custom time",
    steps: [
      {
        title: "Only the exact shift", visual: "swap-manual-exact",
        instruction: "After selecting Everyone, choose Only the exact shift if the recipient must take the entire period shown in the offer. A 09:00–15:00 custom offer can still require acceptance of that exact interval.",
      },
      {
        title: "Allow custom time", visual: "swap-manual-flexible",
        instruction: "Choose Allow custom time to let an eligible coworker take a smaller valid interval. It is available here for Everyone giveaways when permitted; private giveaways and exchanges use exact acceptance in this wizard.",
      },
      {
        title: "Compare the acceptance modes", visual: "swap-timeline-modes",
        instruction: "Full shift and Custom period set what you offer. The acceptance mode sets what someone else can take. Whole-shift employment rules can disable custom time or make an offer unavailable to a particular employee.",
      },
    ],
  },
  {
    id: "specific",
    title: "Giving a shift to a specific employee",
    steps: [
      {
        title: "Specific employee", visual: "swap-specific",
        instruction: "Choose your schedule and assigned shift first. Confirm Full shift or an eligible Custom period. On Recipient, tap Specific employee instead of Everyone.",
      },
      {
        title: "Search for the recipient", visual: "swap-manual-search",
        instruction: "Use Search employee above the list, then tap the coworker’s name. A request type screen opens for that person.",
      },
      {
        title: "Give shift", visual: "swap-manual-give",
        instruction: "Select Give shift, then tap Save selection. This asks the coworker to take your shift without giving you one in return. Tap Continue to review the private offer.",
      },
      {
        title: "Review the private offer", visual: "swap-manual-specific-review",
        instruction: "Check the recipient, date, interval, and Give shift on Review. Tap Send request and wait for the confirmation. The specified employee still needs to be eligible, and approval may be required.",
      },
    ],
  },
  {
    id: "exchange",
    title: "Exchanging shifts",
    steps: [
      {
        title: "Swap shifts", visual: "swap-exchange",
        instruction: "In the wizard, choose your ShopDemo1 shift for November 9 and confirm 09:00–15:00. Select Specific employee and choose the coworker. On Request type, select Swap shifts.",
      },
      {
        title: "Choose their shift", visual: "swap-manual-return-action",
        instruction: "Tap Choose their shift under Swap shifts. GF opens the list of the coworker’s schedules with return candidates.",
      },
      {
        title: "Choose their schedule", visual: "swap-manual-return-schedule",
        instruction: "Tap ShopDemo2 in the coworker’s schedule list. The next screen lists possible return shifts and marks unavailable choices.",
      },
      {
        title: "Confirm selection", visual: "swap-manual-return-shift",
        instruction: "Choose the compatible November 11 shift, 15:00–21:00. Use an eligible interval if the picker permits one. Tap Confirm selection, then Save selection and Continue.",
      },
      {
        title: "Check both directions", visual: "swap-manual-exchange-review",
        instruction: "In Review, check You give: ShopDemo1, 09:00–15:00 and You receive: ShopDemo2, 15:00–21:00. Tap Send request if both are right. GF checks both assignments and applies the two transfers together after the required acceptance or approval.",
      },
    ],
  },
  {
    id: "accept-full",
    title: "Accepting a full shift",
    steps: [
      {
        title: "Open an offer", visual: "swap-manual-incoming",
        instruction: "Open Swap and find an offer you can take. Tap the card and check Offer details for the schedule, date, who offered it, and its interval.",
      },
      {
        title: "Accept shift", visual: "swap-manual-accept-full",
        instruction: "For a flexible offer, leave Full shift selected if the whole interval is available to you. An exact offer already uses its entire offered interval. Tap Accept shift when enabled.",
      },
      {
        title: "Confirm acceptance", visual: "swap-manual-confirm-full",
        instruction: "Check the hours again in the confirmation and tap Accept shift. If you see Request manager approval instead, submitting it starts a review. Check the schedule after the transfer is applied.",
      },
    ],
  },
  {
    id: "accept-part",
    title: "Accepting only a custom period",
    steps: [
      {
        title: "Custom period", visual: "swap-manual-claim",
        instruction: "In Offer details for the flexible 09:00–21:00 offer, choose Custom period. Set From to 15:00 and To to 21:00. If your existing work blocks earlier hours, use the eligible window the app shows.",
      },
      {
        title: "Check the eligible window", visual: "swap-claim-window",
        instruction: "If more than one window appears, choose one continuous interval within a single window. You cannot bridge a gap or take time that overlaps another assignment. If Full shift is missing, the whole offer is not selectable for you.",
      },
      {
        title: "Confirm acceptance", visual: "swap-manual-confirm-part",
        instruction: "Tap Accept shift and verify 15:00–21:00 in the confirmation. Confirm to take only those hours, or submit the required manager request if the button asks for it.",
      },
    ],
  },
  {
    id: "remainder",
    title: "Understanding remaining available time",
    steps: [
      {
        title: "Before someone accepts", visual: "swap-timeline-before",
        instruction: "Start with one Everyone offer for 09:00–21:00 and Allow custom time enabled. A coworker chooses 09:00–15:00, and GF applies that acceptance.",
      },
      {
        title: "Find the remaining offer", visual: "swap-manual-residual",
        instruction: "After 09:00–15:00 transfers, look for the new open offer for 15:00–21:00. If someone accepts an interval in the middle, GF can create separate offers for the time before and after it.",
      },
      {
        title: "Check the accepted interval", visual: "swap-manual-accepted",
        instruction: "Open Swap history and verify that the accepted entry covers 09:00–15:00. The remaining offer was created automatically after acceptance. When you manually create two offers, both are already separate before either is accepted.",
      },
    ],
  },
  {
    id: "restrictions",
    title: "Pending offers, restrictions, approval, and cancellation",
    steps: [
      {
        title: "Check why you cannot accept", visual: "swap-manual-conflict",
        instruction: "Open Offer details and read the reason above the disabled action. Existing overlapping work can block the full shift or restrict a flexible claim to free windows. An already accepted or cancelled offer is no longer open.",
      },
      {
        title: "Check schedule and contract rules", visual: "swap-rule-example",
        instruction: "Choose your own assigned shift in a published schedule that allows swaps. A locked schedule or reserved interval can block the action. A whole-shift contract rule can require exactly eight hours. A return shift also needs to remain compatible with both employees’ assignments.",
      },
      {
        title: "Request manager approval", visual: "swap-manual-approval-action",
        instruction: "If Offer details says Request manager approval, check the hours and submit that action. During review, GF holds the offer; a pending request is not a completed transfer.",
      },
      {
        title: "Awaiting manager approval", visual: "swap-manual-held",
        instruction: "Read Awaiting manager approval in the offer or check the approval entry in Swap history. Where Cancel request is available, use it to withdraw your pending approval request.",
      },
      {
        title: "Check your approval request", visual: "swap-manual-approval-history",
        instruction: "In Swap history, find My approval requests. Pending means the manager has yet to decide. Approved records a positive decision. Rejected, Cancelled, and Expired mean that request will not proceed.",
      },
      {
        title: "Request exception", visual: "swap-exception-example",
        instruction: "If offering the shift would lower scheduled hours below a configured monthly minimum and Review offers Request exception, tap it to send the exception for review. If approved, GF publishes the offer. The shift still needs acceptance before it is transferred.",
      },
      {
        title: "Cancel offer", visual: "swap-manual-cancel",
        instruction: "In Swap, choose My offers, then open your open offer. Tap Cancel offer if shown. In the current GF interface this sends the cancellation directly, so check the offer before tapping.",
      },
      {
        title: "Verify the cancellation", visual: "swap-manual-cancelled",
        instruction: "Open Swap history and look for Cancelled on the matching interval. A cancelled offer cannot be accepted. Cancelling one independently created interval does not cancel the other.",
      },
    ],
  },
  {
    id: "history",
    title: "Checking swap statuses and history",
    steps: [
      {
        title: "Open Swap history", visual: "swap-history-button",
        instruction: "On Swap, tap Swap history beside Create offer. The panel lists past offers and approval requests.",
      },
      {
        title: "Check past offers", visual: "swap-history",
        instruction: "Find the correct date, hours and schedule. Open its card to inspect the details. Accepted means the transfer was applied; Cancelled means the offer stopped without transferring that interval.",
      },
      {
        title: "Swap pending", visual: "schedule-open",
        instruction: "In the Schedule Matrix, find the blue swap icon beside the affected shift. Tap it to see that the offer is waiting for acceptance.",
      },
      {
        title: "Read the status message", visual: "schedule-open-bubble",
        instruction: "Read the blue message. It describes a pending offer; the assigned hours may still be unchanged.",
      },
      {
        title: "Swap completed", visual: "schedule-complete",
        instruction: "Find the green checkmark beside the transferred or exchanged shift. Tap it to check the completed status.",
      },
      {
        title: "Read the status message", visual: "schedule-complete-bubble",
        instruction: "The green message confirms that GF applied the transfer or exchange. Read the current schedule cell for the actual hours.",
      },
      {
        title: "See pending status in Daily view", visual: "swap-daily-pending",
        instruction: "Tap the Eye above the schedule and select the date. Find the blue icon on the affected worker card and tap it to read the status.",
      },
      {
        title: "See completed status in Daily view", visual: "swap-daily-completed-bubble",
        instruction: "Select the day of an applied transfer in Daily view. Tap the green icon on the corresponding card to read the completion message.",
      },
      {
        title: "Read the pending message in Daily view", visual: "swap-daily-pending-bubble",
        instruction: "The blue message has the same meaning as in the monthly matrix. Check the displayed worker and date before using it to identify an offer.",
      },
    ],
  },
];

export const shiftSwapScenarios = [
  { id: "full", title: "Give away your entire shift", choice: "Full shift" },
  { id: "part", title: "Give away only a few hours", choice: "Custom period" },
  { id: "everyone", title: "Offer to any eligible coworker", choice: "Everyone" },
  { id: "specific", title: "Offer to one coworker", choice: "Specific employee" },
  { id: "acceptance-mode", title: "Let someone take part of the offer", choice: "Allow custom time" },
  { id: "acceptance-mode", title: "Require the exact offered hours", choice: "Only the exact shift" },
  { id: "exchange", title: "Receive another shift in return", choice: "Swap shifts" },
  { id: "separate", title: "Offer the other half separately", choice: "Create another offer for the available interval" },
] as const;
