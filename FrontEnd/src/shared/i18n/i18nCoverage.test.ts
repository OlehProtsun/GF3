import { readdirSync, readFileSync } from "node:fs";
import { join, relative, resolve } from "node:path";
import ts from "typescript";
import { expect, it } from "vitest";
import pl from "./pl.json";
import uk from "./uk.json";
import serverMessages from "./serverMessages.json";
import { guideTopics, platformSteps } from "../../pages/employee-install/model/guideContent";
import { guideInstructions } from "../../pages/employee-install/model/guideInstructions";
import { shiftSwapScenarios } from "../../pages/employee-install/model/shiftSwapGuide";

const root = resolve("src");
const catalogs: Record<string, Record<string, string>> = { pl, uk };
function sourceFiles(directory: string): string[] {
  return readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
    const path = join(directory, entry.name);
    if (entry.isDirectory()) return /^(promo|fixtures|__fixtures__|generated|__generated__)$/.test(entry.name) ? [] : sourceFiles(path);
    return /\.tsx?$/.test(path) && !/\.(test|spec|d|generated|fixture)\.tsx?$/.test(path) ? [path] : [];
  });
}
const placeholders = (value: string) => [...new Set(value.match(/\{\d+\}/g) ?? [])].sort();
const holidayNames = ["New Year's Day", "Epiphany", "Labour Day", "Constitution Day", "Assumption of Mary", "All Saints' Day", "National Independence Day", "Christmas Eve", "Christmas Day", "Second Day of Christmas", "Easter Sunday", "Easter Monday", "Pentecost", "Corpus Christi"];
const guideGroups = guideTopics.flatMap(topic => topic.groups);
const guideSteps = [...Object.values(platformSteps).flat(), ...guideGroups.flatMap(group => group.steps)];
const guideStepTitles = guideSteps.map(step => step.title);
const guideVisualLabels = guideSteps.flatMap(step => [step.control ?? step.title,
  ...(step.extraVisuals ?? []).map(visual => visual.control ?? step.title)]);
const guideTitles = guideTopics.map(topic => topic.title);
const guideRowLabels = ["Page Menu", "Share", "GF", "Add", "Open as Web App"];
const guideTimelineTitles = ["Offered interval", "Only the exact shift", "Allow custom time", "Accepted"];
const guideTimelineLabels = ["Offered", "Not offered", "Accepted", "Remaining offer", "Allow custom time"];
const correctionSteps = ["Schedule", "Day", "Shift", "Comment", "Review"];
const correctionTitles = ["Choose a schedule", "Choose a day", "Adjust shift time", "Add a comment", "Review your request"];
const guideSuppliedLabels = new Set([
  "pages/employee-install/ui/GuideVisual.tsx",
  "pages/employee-install/ui/GuideInterfaceIllustration.tsx",
  "pages/employee-install/ui/GuideLanguageDialog.tsx",
]);

// Each mapping is finite application text, reviewed at its declaration/callers.
// API reasons use translateServerMessage instead, preserving unknown record content.
const notepadColors = ["Ink", "Slate", "Blue", "Violet", "Pink", "Red", "Orange", "Gold", "Green", "Teal", "White"];
const mappings: Record<string, Record<string, readonly string[]>> = {
  "features/manager-notepad/ui/NotepadEditorView.tsx": {
    "menuPresentation.label": ["Line spacing", "Text size", "Text color", "More formatting actions"],
    "spacingLabels[value]": ["Single", "Compact", "Normal", "Relaxed", "Double"],
    "paletteColor.label": notepadColors,
    "color.label": notepadColors,
  },
  "features/manager-notepad/ui/NotepadListView.tsx": {
    "option.label": ["Last updated — newest first", "Last updated — oldest first", "Date created — newest first",
      "Date created — oldest first", "Title — A to Z", "Title — Z to A"],
  },
  "features/manager-notepad/ui/NotepadVersionHistoryView.tsx": {
    group: ["Today", "Yesterday", "Older"],
    change: ["Title changed", "Text updated", "Formatting changed", "Checklist updated", "Note color changed"],
    "changeDetails[change]": ["The note title was edited.", "The note body was edited.", "The body formatting changed.",
      "Checklist items or their completion states changed.", "The note color was changed."],
  },
  "features/manager-notepad/ui/NotepadVisibilityControl.tsx": {
    "selected.label": ["Private", "Public", "Custom"],
    "option.label": ["Private", "Public", "Custom"],
    "option.detail": ["Only you can read this note.", "All signed-in managers can read this note.", "Selected managers can read this note."],
  },
  "entities/containers/ui/ContainerGraphMatrix.tsx": { "day.holiday.name": holidayNames },
  "pages/employee-availability/ui/EmployeeAvailabilityMatrix.tsx": { "holiday.name": holidayNames },
  "pages/employee-schedule/ui/EmployeeSchedulePage.tsx": { "holiday.name": holidayNames },
  "pages/employee-schedule/ui/EmployeeScheduleDailyBody.tsx": { "holiday.name": holidayNames },
  "pages/employee-schedule/ui/EmployeeShiftCorrectionDialog.tsx": {
    "item.label": correctionSteps, "steps[step].title": correctionTitles, label: ["Start time", "End time"],
  },
  "pages/employee-install/ui/EmployeeInstallPage.tsx": {
    "step.title": guideStepTitles,
    instruction: guideSteps.flatMap(step => step.instruction ?? guideInstructions[step.visual]?.instruction ?? []),
    result: guideSteps.flatMap(step => step.result ?? (!step.instruction ? guideInstructions[step.visual]?.result : undefined) ?? []),
    "scenario.title": shiftSwapScenarios.map(scenario => scenario.title),
    "scenario.choice": shiftSwapScenarios.map(scenario => scenario.choice),
    'activeTopic?.title ?? "GF App Guide"': [...guideTitles, "GF App Guide"],
    "group.title": guideGroups.map(group => group.title), "nextTopic.title": guideTitles, "topic.title": guideTitles,
    "topic.subtitle": guideTopics.map(topic => topic.subtitle),
  },
  "pages/employee-install/ui/GuideInterfaceIllustration.tsx": { title: guideTimelineTitles, label: guideTimelineLabels },
  "pages/employee-install/ui/GuideLanguageDialog.tsx": { label: ["Polish", "Ukrainian", "English"] },
  "pages/employee-install/ui/GuideVisual.tsx": {
    label: [...guideRowLabels, ...guideVisualLabels], "step.title": guideStepTitles, "step.control ?? step.title": guideVisualLabels,
  },
  "pages/manager-settings/ui/PublicHolidayListDialog.tsx": { "holiday.name": holidayNames },
  "app/layouts/manager-phone-layout/ManagerPhoneLayout.tsx": { label: ["Home", "Containers", "Dispo", "Employees", "More"] },
  "pages/home/ui/HomePage.tsx": { label: ["All Activity", "Swaps", "Vacations", "Corrections", "Requires Approval"] },
  "pages/manager-phone/ui/ManagerPhoneContainerDetailPage.tsx": { label: ["Work Days", "Free Days", "Urlop Days", "Hours"] },
  "entities/availability-groups/ui/AvailabilityPublicationCard.tsx": { value: ["Mo", "Tu", "We", "Th", "Fr", "Sa", "Su"] },
  "entities/containers/ui/ContainerGraphPresetSelect.tsx": {
    "MONTH_LABELS[Math.min(Math.max(month - 1, 0), MONTH_LABELS.length - 1)]": ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"],
  },
  "pages/container-graph-edit/ui/ContainerGraphEditPage.tsx": { GENERATE_AVAILABILITY_GROUP_REQUIRED_MESSAGE: ["Select an availability group before generating a schedule."] },
  "pages/employee-swap/ui/EmployeeSwapPage.tsx": { label: ["All", "My offers", "Can", "Pinned"] },
  "pages/employee-swap/ui/SwapCreateWizard.tsx": { label: ["Schedule", "Recipient", "Review"] },
  "pages/employee-swap/ui/SwapSelectionContext.tsx": { message: ["Select a schedule for {0}.", "Select a shift from {0}'s schedule to swap with yours.", "Choose a shift from {0}'s schedule."] },
  "pages/manager-account/ui/ManagerAccountPage.tsx": { PASSWORD_VALIDATION_MESSAGE: ["Password must contain exactly 6 digits."] },
  "shared/i18n/index.ts": { "entry.message": serverMessages },
};

function stringLiterals(node: ts.Node): string[] {
  const result: string[] = [];
  const visit = (child: ts.Node) => {
    if (ts.isStringLiteral(child)) result.push(child.text);
    ts.forEachChild(child, visit);
  };
  visit(node);
  return result;
}

// Check the declarations too: adding a new label to a finite map requires review.
const declarations: Record<string, string> = {
  "app/layouts/manager-phone-layout/ManagerPhoneLayout.tsx": "tabs",
  "pages/home/ui/HomePage.tsx": "activityTabs",
  "pages/manager-phone/ui/ManagerPhoneContainerDetailPage.tsx": "statisticMetrics",
  "entities/availability-groups/ui/AvailabilityPublicationCard.tsx": "weekdayLabels",
  "entities/containers/ui/ContainerGraphPresetSelect.tsx": "MONTH_LABELS",
  "pages/container-graph-edit/ui/ContainerGraphEditPage.tsx": "GENERATE_AVAILABILITY_GROUP_REQUIRED_MESSAGE",
  "pages/manager-account/ui/ManagerAccountPage.tsx": "PASSWORD_VALIDATION_MESSAGE",
  "pages/employee-install/ui/GuideLanguageDialog.tsx": "options",
};

it("covers literal and reviewed finite translation calls in application source", () => {
  const failures: string[] = [];
  for (const path of sourceFiles(root)) {
    const name = relative(root, path).replaceAll("\\", "/");
    const source = ts.createSourceFile(path, readFileSync(path, "utf8"), ts.ScriptTarget.Latest, true);
    if (declarations[name]) {
      let values: string[] = [];
      const visitDeclaration = (node: ts.Node) => {
        if (ts.isVariableDeclaration(node) && node.name.getText(source) === declarations[name] && node.initializer) {
          if (["tabs", "activityTabs", "statisticMetrics", "options"].includes(declarations[name])) {
            const labels = (child: ts.Node) => {
              if (ts.isPropertyAssignment(child) && child.name.getText(source) === "label") values.push(...stringLiterals(child.initializer));
              ts.forEachChild(child, labels);
            };
            labels(node.initializer);
          } else values = stringLiterals(node.initializer);
        }
        ts.forEachChild(node, visitDeclaration);
      };
      visitDeclaration(source);
      expect(values, `${name}: finite label declaration changed`).toEqual(Object.values(mappings[name])[0]);
    }
    const checkKey = (key: string, location: string) => {
      for (const [language, catalog] of Object.entries(catalogs)) {
        if (!Object.hasOwn(catalog, key)) failures.push(`${location}: missing ${language} key ${JSON.stringify(key)}`);
      }
    };
    const checkArgument = (argument: ts.Expression, location: string) => {
      if (ts.isStringLiteral(argument) || ts.isNoSubstitutionTemplateLiteral(argument)) checkKey(argument.text, location);
      else if (ts.isConditionalExpression(argument)) {
        checkArgument(argument.whenTrue, location);
        checkArgument(argument.whenFalse, location);
      } else {
        const values = mappings[name]?.[argument.getText(source)];
        if (!values) failures.push(`${location}: unreviewed dynamic translation ${argument.getText(source)}`);
        else {
          values.forEach(key => checkKey(key, location));
          if (argument.getText(source) === "label" && !declarations[name] && !guideSuppliedLabels.has(name)) {
            let ancestor: ts.Node | undefined = argument.parent;
            while (ancestor && !(ts.isCallExpression(ancestor) && ts.isPropertyAccessExpression(ancestor.expression) && ancestor.expression.name.text === "map")) ancestor = ancestor.parent;
            if (ancestor && ts.isCallExpression(ancestor) && ts.isPropertyAccessExpression(ancestor.expression)) {
              let array = ancestor.expression.expression;
              while (ts.isParenthesizedExpression(array) || ts.isAsExpression(array)) array = array.expression;
              const labels = ts.isArrayLiteralExpression(array) ? array.elements.flatMap(item => stringLiterals(ts.isArrayLiteralExpression(item) ? item.elements[1] : item)) : [];
              expect(labels, `${location}: finite inline labels changed`).toEqual(values);
            } else failures.push(`${location}: display label is no longer a finite inline map`);
          }
          if (name.endsWith("SwapSelectionContext.tsx")) {
            const labels: string[] = [];
            const visitType = (node: ts.Node) => {
              if (ts.isPropertySignature(node) && node.name.getText(source) === "message" && node.type) labels.push(...stringLiterals(node.type));
              ts.forEachChild(node, visitType);
            };
            visitType(source);
            expect(labels, `${location}: message union changed`).toEqual(values);
          }
        }
      }
    };
    const visit = (node: ts.Node) => {
      if (ts.isCallExpression(node) && ts.isIdentifier(node.expression) && node.expression.text === "t" && node.arguments[0]) {
        checkArgument(node.arguments[0], `${name}:${source.getLineAndCharacterOfPosition(node.getStart(source)).line + 1}`);
      }
      if (name.endsWith("GuideVisual.tsx") && ts.isCallExpression(node) && ts.isIdentifier(node.expression) && node.expression.text === "row") {
        checkArgument(node.arguments[0], name);
      }
      if (name.endsWith("GuideInterfaceIllustration.tsx") && ts.isJsxAttribute(node) && ts.isJsxSelfClosingElement(node.parent.parent) &&
          node.parent.parent.tagName.getText(source) === "SwapTimeline") {
        if (node.name.getText(source) === "title" && node.initializer && ts.isStringLiteral(node.initializer)) checkKey(node.initializer.text, name);
        if (node.name.getText(source) === "parts" && node.initializer && ts.isJsxExpression(node.initializer) &&
            node.initializer.expression && ts.isArrayLiteralExpression(node.initializer.expression)) {
          for (const part of node.initializer.expression.elements) {
            if (ts.isArrayLiteralExpression(part) && part.elements[2] && ts.isStringLiteral(part.elements[2])) checkKey(part.elements[2].text, name);
          }
        }
      }
      ts.forEachChild(node, visit);
    };
    visit(source);
  }
  expect(failures, failures.join("\n")).toEqual([]);
});

it.each(Object.entries(catalogs))("covers server templates and placeholder sets for %s", (language, catalog) => {
  expect(Object.keys(catalog).sort()).toEqual(Object.keys(pl).sort());
  const failures: string[] = [];
  const serverLines = readFileSync(resolve("src/shared/i18n/serverMessages.json"), "utf8").split("\n");
  for (const message of serverMessages) {
    const line = serverLines.findIndex(value => value.trimStart().startsWith(JSON.stringify(message))) + 1;
    if (!Object.hasOwn(catalog, message)) failures.push(`serverMessages.json:${line}: missing ${JSON.stringify(message)}`);
  }
  const lines = readFileSync(resolve(`src/shared/i18n/${language}.json`), "utf8").split("\n");
  for (const [english, translation] of Object.entries(catalog)) {
    const location = `${language}.json:${lines.findIndex(line => line.trimStart().startsWith(JSON.stringify(english) + ":")) + 1}: ${JSON.stringify(english)}`;
    if (!translation.trim() || (language === "pl" ? /[\u0400-\u04ff]/ : /[ыэъЫЭЪёЁ]/).test(translation)) failures.push(`${location}: blank or invalid translation script`);
    if (JSON.stringify(placeholders(english)) !== JSON.stringify(placeholders(translation))) failures.push(`${location}: placeholder mismatch`);
  }
  expect(failures, failures.join("\n")).toEqual([]);
});
