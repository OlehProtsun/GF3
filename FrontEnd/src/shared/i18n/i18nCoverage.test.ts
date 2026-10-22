import { readdirSync, readFileSync } from "node:fs";
import { join, relative, resolve } from "node:path";
import ts from "typescript";
import { expect, it } from "vitest";
import pl from "./pl.json";
import serverMessages from "./serverMessages.json";

const root = resolve("src");
const catalog: Record<string, string> = pl;
function sourceFiles(directory: string): string[] {
  return readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
    const path = join(directory, entry.name);
    if (entry.isDirectory()) return /^(promo|fixtures|__fixtures__|generated|__generated__)$/.test(entry.name) ? [] : sourceFiles(path);
    return /\.tsx?$/.test(path) && !/\.(test|spec|d|generated|fixture)\.tsx?$/.test(path) ? [path] : [];
  });
}
const placeholders = (value: string) => [...new Set(value.match(/\{\d+\}/g) ?? [])].sort();

// Each mapping is finite application text, reviewed at its declaration/callers.
// API reasons use translateServerMessage instead, preserving unknown record content.
const mappings: Record<string, Record<string, readonly string[]>> = {
  "app/layouts/manager-phone-layout/ManagerPhoneLayout.tsx": { label: ["Home", "Containers", "Dispo", "Employees", "More"] },
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
  "entities/availability-groups/ui/AvailabilityPublicationCard.tsx": "weekdayLabels",
  "entities/containers/ui/ContainerGraphPresetSelect.tsx": "MONTH_LABELS",
  "pages/container-graph-edit/ui/ContainerGraphEditPage.tsx": "GENERATE_AVAILABILITY_GROUP_REQUIRED_MESSAGE",
  "pages/manager-account/ui/ManagerAccountPage.tsx": "PASSWORD_VALIDATION_MESSAGE",
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
          if (declarations[name] === "tabs") {
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
      if (!Object.hasOwn(catalog, key)) failures.push(`${location}: missing Polish key ${JSON.stringify(key)}`);
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
          if (argument.getText(source) === "label" && !declarations[name]) {
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
      ts.forEachChild(node, visit);
    };
    visit(source);
  }
  expect(failures, failures.join("\n")).toEqual([]);
});

it("covers every server template and preserves nonblank Polish placeholder sets", () => {
  const failures: string[] = [];
  const serverLines = readFileSync(resolve("src/shared/i18n/serverMessages.json"), "utf8").split("\n");
  for (const message of serverMessages) {
    const line = serverLines.findIndex(value => value.trimStart().startsWith(JSON.stringify(message))) + 1;
    if (!Object.hasOwn(catalog, message)) failures.push(`serverMessages.json:${line}: missing ${JSON.stringify(message)}`);
  }
  const lines = readFileSync(resolve("src/shared/i18n/pl.json"), "utf8").split("\n");
  for (const [english, polish] of Object.entries(catalog)) {
    const location = `pl.json:${lines.findIndex(line => line.trimStart().startsWith(JSON.stringify(english) + ":")) + 1}: ${JSON.stringify(english)}`;
    if (!polish.trim() || /[\u0400-\u04ff]/.test(polish)) failures.push(`${location}: blank or Cyrillic translation`);
    if (JSON.stringify(placeholders(english)) !== JSON.stringify(placeholders(polish))) failures.push(`${location}: placeholder mismatch`);
  }
  expect(failures, failures.join("\n")).toEqual([]);
});
