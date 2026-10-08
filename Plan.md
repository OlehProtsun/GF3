# Plan.md — GF3: Polish B2B SaaS legal package and enforceable user acknowledgement

**Execution language:** English for implementation instructions; **Polish** for customer-facing legal texts.  
**Repository:** `https://github.com/OlehProtsun/GF3`  
**Branch:** `DEV2`  
**PINNED BASELINE:** `5980123995199dc4b6d40d2e3b17ff49731a6a39`  
**Research cutoff:** 2026-10-08 (Polish/EU law must be checked again before actual publication).  
**Deliverable:** draft legal documents, non-authenticated website access to public notices, links from the application, use of the existing regulation acceptance workflow, limited API-side enforcement, and a non-code go-live checklist. **Not a production deployment or legal certification.**

> **CRITICAL RELEASE RULE:** These documents are *drafts*, not lawyer-approved text. Do not claim that accepting a checkbox makes the product fully compliant, nor that an employee can automatically enter a binding B2B agreement for their employer. Do not publish placeholder-filled legal text to paying users. A Polish commercial/privacy lawyer must approve the operator-specific texts and data processing agreement before commercial launch. Codex produces drafts and application support, not legal conclusions.

## 1. Objective, operating model and boundaries

1. Prepare a coherent Polish-language set of documents for GF3's **existing functionality**: employee schedules, availability, shift swaps, manager/employee accounts, communications, PDF regulations, data exports and operational logs.
2. Supply Terms (`Regulamin`) before contract conclusion, operator identity and contact details, privacy information and browser storage disclosures without login. Provide clear in-app links for every role.
3. Ensure individual managers/employees demonstrably acknowledge the applicable published user-rule PDF via the **existing** `RegulationService` flow; reinforce the existing client-side gate with a narrowly scoped API guard. This is individual acknowledgement, **not** execution of a corporate SaaS order/DPA.
4. For the baseline single-instance product, contract with each **customer company** separately using a signed (or otherwise legally valid and demonstrable) `Zamówienie / Umowa B2B` and `Umowa powierzenia danych (DPA)` **before** provision of production access/data. Customer signatories must be verified outside the application.
5. Explicitly refrain from representing the current code as a safely isolated public multi-tenant SaaS. `deploy/docker-compose.yml` maps one GF3 service to one SQLite file; `EmployeeModel` has no tenant identifier. Until a separately designed and tested tenant-isolation change, support **one customer organization per independently isolated deployment** (isolated DB/volume, secrets, domain and backup), rather than mixing unrelated businesses in the same database.
6. Leave prices, payment processor, subscription/auto-renewal, SLA uptime, retention periods, hosting jurisdictions, cloud processors, legal entity/NIP, incident-support contacts and any future plans **unasserted** until supplied as verified business facts. Mark exact gaps for manual completion; do not invent them.

### Baseline facts (confirmed at SHA, do not re-discover architecture)

- `.NET 10` / ASP.NET Core API + React/Vite/TypeScript, EF Core/SQLite, xUnit and Vitest; production `GF3.WebApi/Program.cs` serves `FrontEnd/dist` via static files and SPA fallback.
- `FrontEnd/src/app/router/AppRouter.tsx` currently recognizes `/login`, `/password-recovery` for anonymous users, manager paths and employee paths; anonymous legal pages should **not** require adding a new React route: public static HTML under `FrontEnd/public/legal` is served by Vite in development and by ASP.NET Core in production.
- `FrontEnd/src/pages/login/ui/LoginPage.tsx` holds the public sign-in UI, including an existing support email (`support@app-gf.com`); this is a code string, **not verification** that the address belongs to the future legal operator.
- `FrontEnd/src/app/layouts/overlay-sidebar-layout/OverlaySidebarLayout.tsx` and `FrontEnd/src/app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout.tsx` own role-specific authenticated layouts.
- `GF3.WebApi/Controllers/AdminRegulationsController.cs` contains the system-manager-authorized versioned PDF upload/publish workflow. `RegulationsController.cs` provides `GET /api/regulations/pending`, `GET /api/regulations/{id}/pdf`, `POST /api/regulations/{id}/accept`, `GET /api/regulations/history/me`. `BusinessLogicLayer/Services/RegulationService.cs` persists immutable published PDF SHA-256/version and per-role/account acceptance timestamp/name snapshots; `FrontEnd/src/entities/regulations/ui/RegulationAcceptanceGate.tsx` already shows a download link and **unchecked** acknowledgement checkbox after login. The gate alone does **not** stop direct API calls.
- `docs/design-system.json` is an extensive non-runtime UI snapshot; `docs/deploy.md` describes single SQLite-volume production deployment, HTTPS, backup/update; `README.md` describes the app and stack. These documents are context, not evidence of legal compliance or a completed multi-tenant architecture. Historical `Plan.md` is about system-manager UI/design and must not be treated as this plan.
- `AuthProvider.tsx` stores a bearer access token in browser `localStorage`; login also stores last username/password-mode preferences. `ManagerAccountService.cs` has a **development fallback manager password** if the env override is omitted, and the login UI expects six-digit numeric credentials. Production Compose requires `GF3_BOOTSTRAP_MANAGER_PASSWORD`, but that alone is not a complete authentication-hardening assessment. Flag as a go-live security risk, do not alter auth in this legal-only plan.
- No self-service B2B onboarding, billing, organization-scoped tenant contract or tenant-isolation boundary was verified in the inspected sources. Do not describe any of these as implemented.

**Evidence limits:** Direct inspection of important files at the pinned raw GitHub revision and the commit diff was possible; a full Git clone/tree enumeration was blocked in this environment. Codex may inspect **only explicitly listed target files and their immediate dependencies** when editing; it must not assert an unverified exhaustive repository audit. If an exact existing path from this plan is absent in its checkout, stop that step and report the mismatch; never silently create a parallel implementation.

## 2. Mandatory operator/business facts — manual prerequisites, not guesses

**CREATE** `docs/legal/OPERATOR_INPUTS.md`. This is an internal completion form, no real personal secrets. All fields start `[DO UZUPEŁNIENIA]` until the operator supplies reliable details. Fields:

- legal operator name, legal form (JDG/sp. z o.o./other), registered address, CEIDG/KRS number as appropriate, NIP, REGON if applicable;
- legal contact and service/support email, privacy contact, breach/incident contact, complaints service channel;
- exact product/trade name, public domain, effective date, applicable language(s), jurisdiction, target market (Poland-only or EU), actual intended contracting counterparty;
- invoicing/B2B VAT and KSeF treatment verified by accountant; charging model and prices (if none yet, state `by individually signed order`, not a fictitious subscription);
- production hosting provider, physical storage region, all processors/subprocessors (hosting, SMTP, backups, analytics, support), subprocessor notification channel, verified third-country transfers and transfer mechanism if any;
- actual personal data categories (names, work contact, work schedules, availability, swaps, messages, auth/access logs), access roles, purposes, legal bases as controller, separate purposes as processor, security measures and verified retention schedules;
- recovery time, backup lifecycle, export/deletion capability, exact contractual termination process and actual service/support operating hours;
- public cookie/SDK/analytics inventory, browser storage keys, strict necessity classification, consent implications;
- business-client order signatory proof, processor instructions and worker-privacy-notice delivery by the employer;
- affirmative decision on whether the DSA, consumer-like sole-trader rules, EAA accessibility or special sector/employee-data obligations apply. The legal reviewer records reasons for applicability or non-applicability.

**CREATE** `docs/legal/LEGAL_REVIEW.md`: table of every public/private document, status `DRAFT | REVIEWED | APPROVED`, reviewer, review date, document semantic version, approved SHA-256, source of operator facts, date effective, publishing checklist, approvals and unresolved questions. All documents start `DRAFT`; do not mark them approved via generated content. Require a business owner and Polish-qualified legal/privacy reviewer to sign off the final reviewed texts before commercial launch.

## 3. Draft package to create — exact files, language, audiences and content

All texts in this section MUST be professional **Polish**. Clearly label drafts `WERSJA ROBOCZA — DO WERYFIKACJI PRAWNEJ` until approval. Use neutral, understandable writing. Do not fabricate legal entity data, consent, pricing, processor arrangements, audit certifications, security levels, or refund entitlements. Use stable semantic version `0.1.0-draft` and effective-date placeholder. Each draft has a version/date header and an operator placeholder. Individual GDPR notices are information; **do not** add “I consent to GDPR” checkboxes.

### A. Public site documents — CREATE canonical HTML files under `FrontEnd/public/legal/`

**A1.** `FrontEnd/public/legal/index.html` — Polish legal documents hub, operator contact card, links to all public pages, visible DRAFT warning until approval, link back to `/login`. Include version/effective date and a direct-print/save affordance via standard browser functionality (no JS required). Do not list private annexes containing sensitive security configurations.

**A2.** `FrontEnd/public/legal/regulamin.html` — **Regulamin świadczenia usług drogą elektroniczną i korzystania z GF3 (B2B)**. Mandatory numbered sections, in this order:

1. Provider identity, contact, definitions (`Operator`, `Klient`, `Użytkownik`, `Konto`, `Dane Klienta`, `Usługa`), applicability and how the terms relate to an individual signed B2B order and DPA. No claim that an employee's click binds the employer; the customer is bound by its authorized order/representative.
2. Actual scope: roster planning, schedule visualization, availability, shift-swap proposals/acceptance, messages/notifications, regulation PDF acknowledgements, exports. Specify that GF3 is a **planning tool**, not payroll, HR legal advice, medical service or legal guarantee of compliance with Kodeks pracy; employers remain responsible for legally compliant schedules/employee notices and decisions.
3. Technical requirements: supported modern browser, network access, HTTPS, account invitation/assigned credentials; internet/security risks; no fictitious compatibility guarantee.
4. Account opening only after customer authorization; roles manager/employee; account protection, security incident notification, authorized access, no account sharing; operator privileges only as documented.
5. Service ordering/contract formation through verified individual B2B `Zamówienie` (not automated signup at this commit), available terms before signing, order-of-precedence: individually signed Order → DPA for data processing matters → these Terms → published usage policy, except mandatory law.
6. Fees/duration, invoicing, renewals **only by signed order**; no invented pricing, trial period, automatic renewal or payment methods.
7. Customer content/data ownership, permitted processing, backup/export/retention per reviewed DPA/order, authorization to input employee data; customer responsible for appropriate legal bases and information duties.
8. Data protection roles and security overview; link `polityka-prywatnosci.html`, `podwykonawcy.html`, DPA annex. Disclose that Operator is independently controller for its own business account/support/billing data, generally processor for the employer's workforce records, subject to case-specific legal review.
9. Prohibited conduct (unlawful content, harassment, interference, account misuse, infringing material), proportionate remediation/suspension, user notice/appeal process where legally required; link `zasady-korzystania.html`.
10. Support/complaints procedure: submission email, information to include, investigation/response on documented terms (do **not** promise unverified number of days); operational outages/maintenance with realistic notices.
11. Availability and limitation of liability **subject to mandatory Polish law**, causation, confidentiality, IP/licence scope and business client responsibilities. Never exclude liability for intentional harm, rights that cannot be waived or GDPR duties; ask legal reviewer to approve any cap.
12. Termination, account deactivation, data retrieval window/deletion following signed order and DPA; user rights do not automatically erase employer-controlled employment records.
13. Change management: versioning, user notice, material change publication before effectiveness, record of accepted versions and reasonable contract termination options reviewed against the actual sales model.
14. Governing law/competent court clauses applicable to the actual customer; explicit reservation for mandatory protections including qualifying sole proprietors; publication date and contact.
15. Concise risk notice that swap requests and produced schedules do not by themselves authorize overtime, employee consent where required, or lawful working-time arrangements.

**A3.** `FrontEnd/public/legal/polityka-prywatnosci.html` — **Polityka prywatności / informacja RODO**. Include distinct controller-vs-processor tables (operator contacts/billing/support/security vs customer workforce data), data categories, real purposes, lawful basis **by processing activity (not blanket consent)**, recipients/subprocessors, transfer mechanism if any, actual storage and retention categories (unresolved values visibly blocked for release), GDPR rights/how to exercise, complaint to Polish UODO, right of access/correction/erasure/objection/restriction/portability only where applicable, mandatory/provided data, source of data (typically employer), no assertion of automated decisions unless verified, incident contact, link to workforce-employer notice template, updates. Do not claim GF3 can independently delete employer workforce records on any employee request; explain controller routing.

**A4.** `FrontEnd/public/legal/pliki-cookies.html` — **Polityka cookies i pamięci przeglądarki**. Explicitly distinguish actual cookie categories from current `localStorage` keys `gf3.auth.access-token`, `gf3.auth.last-username`, `gf3.auth.password-mode` and purposes. State storage duration only after verifying implementation; mention JWT-in-localStorage security implications internally, not as security marketing. Under PKE art. 399, strictly necessary storage may rely on the statutory exception, otherwise prior valid consent is required; never say that all storage always needs a banner or that all storage is exempt. No analytic/advertising SDK claim without evidence. Provide a mechanism/contact to withdraw optional consent *if* optional categories are deployed.

**A5.** `FrontEnd/public/legal/zasady-korzystania.html` — **Zasady akceptowalnego korzystania**; confidentiality of schedules, no credential-sharing or harassment, no unauthorized access/export, no malicious files/content, truthful swap requests, employer's final decision on working time, consequences and complaint avenue; neutral non-employment-contract wording. This text may be adapted into the published PDF acknowledgement (see Step 5) **only after legal review**.

**A6.** `FrontEnd/public/legal/podwykonawcy.html` — **Lista podmiotów przetwarzających (subprocessors)**. Table fields: legal provider name, function, data categories, place of processing, transfer basis (if outside EEA), date of publication/notification; if operator inventory is unknown, display an explicit `DO UZUPEŁNIENIA` and block commercial release. Do **not** invent AWS/Google/Stripe or call a processor fully EU-local without evidence.

**A7.** `FrontEnd/public/legal/bezpieczenstwo.html` — **Informacja o bezpieczeństwie usługi**. Public, non-sensitive description of confirmed practices only (HTTPS in deployment path, authorization/role separation, controlled access, per-instance database/backups where configured, incident contact); explicitly do not promise SOC 2/ISO 27001/zero breaches/24×7 monitoring. Distinguish planned controls from proven runtime controls.

For all A1–A7, create a shared lightweight `FrontEnd/public/legal/legal.css` (no CDN, trackers, external fonts or npm packages): responsive text-first layout, clear hierarchy, navigation, readable contrast, keyboard focus, print stylesheet. Static HTML must use `lang="pl"`, UTF-8, viewport meta, semantic heading levels, version, effective date, exact document title and canonical absolute-or-root-relative links. No forms collect personal data on these pages; no JavaScript is necessary. Public legal pages remain reachable without auth and downloadable/savable by the browser before contract formation.

### B. Private B2B agreement/annex templates — CREATE under `docs/legal/templates/`

These files are Markdown source templates for operator/qualified reviewer to complete and sign with each corporate customer. They must **not** be publicly hosted automatically.

**B1.** `Zamowienie_Umowa_B2B.md` — parties/IDs/authority evidence, provisioned company-specific instance/domain, scope/seats/features, exact price/currency/VAT or commercial offer reference, commencement/contract duration, support/SLA if any, payment/invoice terms, order-of-precedence, suspension/termination, export/deletion procedure, contact/notice addresses, written/e-signature acceptance, electronic copy exchange. Only include automatic renewal if explicitly negotiated; no one-sided irrevocable terms.

**B2.** `Umowa_Powierzenia_Danych_DPA.md` — GDPR **Article 28(3)** contract with annexes. Cover subject/duration/nature/purpose, categories of data subjects (employees/managers), actual data categories, employer's instructions/controller responsibilities, confidentiality of personnel, security/TOMs, authorized subprocessors and notice/object mechanism, third-country transfers, rights/DSAR assistance, breach assistance, DPIA/cooperation, return or deletion at termination (legal retention exceptions), audit/information duties without unlawfully barring audits, notifications/contact, signed order linkage and Annexes I–III. Include a clear data-processing locations/suppliers register placeholder. Explicitly include no use of customer data for training models or provider advertising without another valid basis and separate contractual authorization.

**B3.** `Zalacznik_TOMs_i_Retencja.md` — detailed technical/organizational measures and retention matrix for account data, shifts/swaps, uploads, logs, backups, expired credentials, acceptance evidence; measure owner, implemented/planned, tested-on date, deletion method and exception. Do not state controls not verified. Mark as confidential where it describes operational details.

**B4.** `Zalacznik_SLA_i_Wsparcie.md` — response process, maintenance window, severity definitions, backup and recovery commitments **as business-approved placeholders**. Explicitly state no binding SLA figure exists until completed and agreed. Match deployment reality; never claim 99.9% SLA by default.

**B5.** `Klauzula_Informacyjna_Dla_Pracownikow_Wzor.md` — sample **for customer/employer to personalize**, not a definitive notice automatically sent by Operator. Employer/controller identity, workforce scheduling purposes/legal bases, system visibility/recipients, how to exercise rights, employment-law record retention, transparency about swaps; link to Operator's processor role as applicable. Distinguish employee acknowledgement of app rules from GDPR consent/waiver of employee rights.

### C. Internal compliance procedures — CREATE under `docs/legal/internal/`

**C1.** `Rejestr_Czynnosci_Przetwarzania.md`: GDPR article 30 processing records template, split controller processing (lead/contact/support/billing/security) and processor categories (work schedules/workers). Include actual fields for legal basis/purpose/subjects/data/recipients/third-country transfers/retention/security and owner. No automatic assertion of the small-company exemption.

**C2.** `Procedura_Incydentow_i_Naruszen.md`: incident owner and detection/reporting, classification, containment/evidence and customer processor notification **without undue delay** as agreed in DPA; GDPR supervisory notification within 72 hours **when required for the controller** (Art. 33) and data-subject notification when high-risk (Art. 34), documentation of non-reportable events, communications/restore tests; distinguish operator controller role from employer controller role. Include contact and drill placeholders.

**C3.** `Procedura_DSAR_Retencji_Usuwania.md`: receive/verify request, decide data controller, forward employee data request to employer, export mechanics, retention schedule, deletion across live DB/backups and lawful retention exceptions; accountable operator roles, response SLAs anchored in GDPR where applicable, evidence log and audit trail handling.

**C4.** `Ocena_Ryzyka_i_DPIA_Screening.md`: risks around shift/attendance data, access to worker contacts, swap messages, unauthorized exports, default PINs, backups and single shared DB. Screening of whether Art. 35 DPIA is necessary; decision and sign-off by competent controller, no prefilled claim that DPIA is not needed.

**C5.** `Rejestr_Podwykonawcow_i_Transferow.md`: supplier due diligence, Article 28 agreements, region, onward transfers/SCC/TIA if necessary, processor monitoring, reassessment and notices.

**C6.** `CHECKLIST_PRZED_STARTEM.md`: **stop-ship** checklist (see Section 9) with owner, evidence URL/path, due date, status and legal/accounting sign-off.

## 4. Site integration — minimal, no SPA/routing redesign

**MODIFY** `FrontEnd/src/pages/login/ui/LoginPage.tsx`: underneath the existing login/support section add a small, keyboard-accessible group of normal `<a href>` links: `Regulamin` (`/legal/regulamin.html`), `Polityka prywatności` (`/legal/polityka-prywatnosci.html`), `Cookies` (`/legal/pliki-cookies.html`) and `Dokumenty prawne` (`/legal/index.html`). Keep login form, six-digit input modes and current navigation unchanged. Reuse `LoginPage.module.css` for local layout/style; do not create a new global provider.

**MODIFY** `FrontEnd/src/app/layouts/overlay-sidebar-layout/OverlaySidebarLayout.tsx` and `FrontEnd/src/app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout.tsx`: add small unobtrusive footer access to `/legal/index.html`, `/legal/regulamin.html`, `/legal/polityka-prywatnosci.html`. Add to existing visible layout area, accessible in desktop/mobile/collapsed states; no new sidebar nav privilege. Do not display hidden system-manager areas to ordinary users.

**REUSE AS-IS:** `FrontEnd/src/app/router/AppRouter.tsx`, `GF3.WebApi/Program.cs` static asset serving, existing Vite setup. Because each legal page is a real static `.html`, no new React route or API is required. Verify the production server actually returns HTML rather than the SPA fallback for these exact files; only alter static hosting if a test proves a failure.

**REUSE/REFRESH** `docs/design-system.json` only for the narrow addition of a cross-link/reference to the legal document footer/login links *if* its validator supports such entries. Do not re-catalog the entire UI; do not change unrelated scene/route IDs. If the snapshot is not structured to describe these static documents without new semantics, leave it intact and record the legal pages in `docs/legal/README.md` instead. (No separate architecture decision: the canonical runtime references are the static HTML files.)

## 5. Explicit acknowledgement and enforced access — extend the existing regulations mechanism

### 5.1 Correct distinction between legal instruments

- A **B2B company** contracts via signed `Zamowienie_Umowa_B2B.md` and DPA; the existing database acceptance by an employee or manager **cannot substitute** for proof of their employer's consent/authorized signature.
- Each individual login user acknowledges the **published platform-use rules** (e.g. a lawyer-approved version of `zasady-korzystania.html`, exported as immutable PDF) in the existing `RegulationAcceptanceGate`. If the operator separately wants to obtain explicit user acknowledgement of the entire `Regulamin`, use a separate approved PDF/version; never call this B2B contract formation automatically.
- Privacy notices and cookies disclosures are **made accessible**, not "consented to" as a condition of using necessary processing. Optional marketing or nonessential cookies need independently revocable opt-in if later added.

### 5.2 REUSE without new persistence or endpoints

- `BusinessLogicLayer/Services/RegulationService.cs` (existing version/hash/publish/accept handling).
- `GF3.WebApi/Controllers/AdminRegulationsController.cs` (`POST /api/admin/regulations`, `POST /api/admin/regulations/{documentId}/publish`, privileged as system manager).
- `GF3.WebApi/Controllers/RegulationsController.cs` (`GET pending`, `GET PDF`, `POST accept`, `GET personal history`); `FrontEnd/src/entities/regulations/ui/RegulationAcceptanceGate.tsx` (individual affirmative checkbox, download, error handling).
- Existing `RegulationDocumentModel` and `RegulationAcceptanceModel`, repository uniqueness/idempotency logic, existing xUnit tests. **No migration**, new table, extra legal-consent database, payment workflow or new DI service solely to hold acknowledgements.

**Operational onboarding:** (1) obtain legal approval for the specific PDF, (2) record its exact version and SHA-256 in `docs/legal/LEGAL_REVIEW.md`, (3) upload/publish through already-existing system-manager UI on each isolated customer instance, (4) verify `GET /api/regulations/pending` lists it for a newly provisioned manager and employee, (5) user sees checkbox unchecked and downloads/read the exact PDF, (6) POST acceptance persists their account ID/role, timestamp and PDF hash, (7) re-publishing a new version forces a new acknowledgement according to the current repository semantics. Preserve all existing acceptance history and immutable published PDFs.

### 5.3 CREATE a narrowly scoped API acknowledgement guard

**CREATE** `GF3.WebApi/Middleware/RegulationAcceptanceGuardMiddleware.cs` in namespace `WebApi.Middleware`.

**Constructor dependencies:** `RequestDelegate next`; resolve `IRegulationService` via `HttpContext.RequestServices` per request (request-scoped; do not inject a scoped service into a singleton middleware constructor). No new service abstraction.

**Exact behavior:**

1. Apply only to authenticated requests with a valid `manager` or `employee` role plus positive `manager_id`/`employee_id` claim. Let existing authentication/authorization decide 401/403 for anonymous/invalid tokens; never infer account ID from request input.
2. Exempt, case-insensitively on exact route segments: `GET /api/regulations/pending`, `GET /api/regulations/history/me`, `GET /api/regulations/{positiveInt}/pdf`, `POST /api/regulations/{positiveInt}/accept`, all `/api/auth/*`, `GET /api/health`; also exempt `/api/admin/regulations/*` **only for an authenticated system manager already carrying the trusted `gf3.system_manager=true` claim and manager role**, so the operator can publish the initial PDF. Let pre-existing ASP.NET policy and admin guard enforce actual privileged endpoints. No wildcard exempt for arbitrary `/api/regulations/` actions.
3. Construct the existing `RegulationSubject` with the same trusted role/account ID, username and display-name claims as `RegulationsController.GetSubject()`; never create a second business-level pending calculation. To avoid duplicating claim parsing, **EXTEND** `GF3.WebApi/Controllers/RegulationsController.cs` only if needed to extract its exact subject-construction logic into a small `GF3.WebApi/Services/RegulationSubjectResolver.cs` helper used by controller + middleware; helper must not access DB or accept user IDs from clients. No change to DTO routes/status of existing endpoints.
4. Call `IRegulationService.ListPendingAsync(subject, cancellationToken)`. If none pending, call `next(context)`. If one/more pending, return HTTP **428 Precondition Required**, `application/problem+json`, stable error code `regulations_acceptance_required` and safe message `Accept the currently published required documents to continue.` Include no PDF content or PII in response; request client to fetch `/api/regulations/pending`.
5. Database/pending-read errors: preserve existing API exception middleware handling; **fail closed** (never call next when status cannot be determined). Do not log PIN, JWT, PDF, names or customer schedule data. Use cancellation token.
6. Place guard after authentication/authorization within the API pipeline, before protected controller execution and under the existing `ApiExceptionMiddleware`; do not guard public static `/legal/*.html`, login or the download/acceptance endpoints. Scope to `/api` using current `UseWhen` infrastructure, not a new global router filter.

**MODIFY** `GF3.WebApi/Program.cs`: in existing API branch immediately after `ApiExceptionMiddleware` and before `EmployeePresenceMiddleware`/`AdminToolsGuardMiddleware`, add `UseMiddleware<RegulationAcceptanceGuardMiddleware>()`. Verify this position still preserves admin guard semantics; for an account with no pending documents behavior must be byte-for-byte unchanged.

**MODIFY (limited)** `FrontEnd/src/entities/regulations/ui/RegulationAcceptanceGate.tsx`: retain exact download + user-checked confirmation + mutation flow. Add a link to the public `/legal/index.html` and Polish-readable explanation (`accepted by user, not on behalf of employer`) without making privacy policy consent mandatory. Reuse existing i18n and CSS patterns; do not add a second checkbox or separate acceptance cache.

**Edge rules:** duplicate acceptance POST must follow repository's existing no-duplicate behavior; stale or unpublished PDF must not be accepted; user acceptance for one version must not cover another; old acceptance remains auditable. If a new published regulation arrives while an account is active, next protected API call must return 428 until acknowledgement. Admin can create first PDF even when existing pending PDFs exist because only trusted system manager publishing endpoints are exempt. Outage must not turn into a bypass.

## 6. Browser storage and marketing — strictly evidence-driven

**CREATE** `docs/legal/COOKIE_AUDIT.md` with the real current `document.cookie`, sessionStorage/localStorage, analytics, CDN/beacons, third-party scripts and marketing contact use discovered by **bounded scan of frontend entry/scripts and deployment HTTP headers**. Record each item as strictly necessary or optional, purpose, duration, third party, lawful route, effective blocking behavior.

At pinned baseline there is known localStorage of JWT/access token and username/password-mode; do not represent that as a nonessential advertising cookie. **Do not add a cookie consent banner** to the legal-only change while verified runtime has only necessary storage. If optional tracking is found, prevent it from running in production until a **separate** proper prior opt-in/withdrawal mechanism is implemented and tested; cookie policy alone is insufficient. Do not add analytics or tracking during this task.

Commercial emails/SMS to prospects require independently checked legal permission under applicable PKE art. 398 and GDPR; password reset/security notices are not marketing subscriptions. Do not invent an unsolicited outreach consent form or send campaigns.

## 7. Tests to CREATE/MODIFY

### Frontend

- **MODIFY** existing login page test at `FrontEnd/src/pages/login/ui/LoginPage.test.tsx` **only if the path exists**; otherwise **CREATE** it using existing Vitest/Testing Library patterns. Assert legal links exist/point to actual static files, link activation does not submit login, login flow and numeric keypad still operate.
- **MODIFY** layout tests `FrontEnd/src/app/layouts/overlay-sidebar-layout/OverlaySidebarLayout.test.tsx` and existing employee layout tests (if present) to verify links for manager/employee and accessibility at collapsed/mobile states; preserve existing admin visibility assertions.
- **MODIFY/CREATE** `FrontEnd/src/entities/regulations/ui/RegulationAcceptanceGate.test.tsx` as appropriate to verify checkbox starts unchecked, cannot accept until checked, PDF link, duplicate spinner behavior, failed download/POST feedback, legal hub link; do not test the B2B contract via the employee checkbox.

### Backend

- **CREATE** `GF3.Tests/RegulationAcceptanceGuardTests.cs` using existing Web API test-host/xUnit conventions. Scenarios: anonymous not blocked by guard (auth status preserved), authenticated manager/employee without pending proceeds, each role with pending receives HTTP 428 and safe ProblemDetails error code; unauthenticated/public auth and allowed regulation paths remain reachable; acceptance then proceeds; publishing a newer document re-blocks; non-system manager cannot use admin exception; system manager exception still goes through admin policy/middleware; DB failure blocks access/uses normal error; duplicate requests don't create double records.
- Reuse existing acceptance tests for immutability/hash/persistence and run all relevant regression suites. Do not modify the repository's existing acceptance schema or bypass authorization.

### Static legal document validation — CREATE `scripts/validate-legal-documents.mjs`

Node.js built-ins only; no new dependencies. Validate all seven public `.html` files and `legal.css` exist, link targets exist, UTF-8/lang/title/meta and version markers exist, and each has a link to legal index and the login page. Check `docs/legal/LEGAL_REVIEW.md` exists. `--release` additionally rejects any `DO UZUPEŁNIENIA`, `[PLACEHOLDER]`, `0.1.0-draft` or `WERSJA ROBOCZA` markers in public files and requires all public docs approved in `LEGAL_REVIEW.md` (including matching approved version/hash); fail with nonzero exit and names of offending documents, never print private data. Without `--release`, drafts pass syntax/coverage checks to keep developer builds functional. Do not give `--release` the power to approve documents automatically.

## 8. Exact API, persistence, DI and deployment deltas

- **Public routes:** `GET /legal/index.html`, `/legal/regulamin.html`, `/legal/polityka-prywatnosci.html`, `/legal/pliki-cookies.html`, `/legal/zasady-korzystania.html`, `/legal/podwykonawcy.html`, `/legal/bezpieczenstwo.html`, `/legal/legal.css` (static). Existing auth/session API contracts unchanged.
- **New API endpoints:** NONE.
- **Existing protected API behavior:** when a valid manager/employee has pending published regulations, most business API endpoints return HTTP 428 ProblemDetails instead of executing, with exempt paths defined in §5.3. All existing 401/403 and admin guard rules stay authoritative.
- **Persistence:** NONE. Reuse existing regulation documents, SHA-256 and acceptance timestamps. No EF migration, table, index or separate tenant ID. Corporate contracts/approval records are external signed artifacts retained outside source control.
- **DI:** NONE; request-scoped service resolution uses existing `IRegulationService`. If a shared `RegulationSubjectResolver` helper is introduced, implement as stateless static helper; no registration.
- **Environment configuration:** NONE added. Existing `GF3_BOOTSTRAP_MANAGER_PASSWORD`, `Jwt__SigningKey`, DB, SMTP, HTTPS and admin flags remain as-is. Operator-specific public legal facts are deliberately not sourced from secrets or customer data in `.env`; approved public text must match the deployed domain/operator per instance.
- **Privacy/security:** do not expose runtime secrets, real employee schedules or signed customer DPA in Git or static assets. Signed agreements and sensitive TOMs stay outside the public build artifact.
- **Storage/data flow:** signed B2B order + DPA (external) → approved public Terms before contract → operator provisions isolated instance → approved per-user PDF published by trusted system manager → manager/employee logs in → existing `RegulationAcceptanceGate` shows pending version/PDF → user checks acknowledgement → existing `POST /api/regulations/{id}/accept` stores immutable evidence → protected APIs become available. Newly published version → further acknowledgement before protected API access.

## 9. Non-code STOP-SHIP checklist — operator and lawyer/accountant must complete

Create the checklist from §3 C6 with checkboxes and named verification evidence. Do not let a green `dotnet test` alone mark these business steps complete.

1. **Legal identity:** CEIDG/KRS registration as appropriate, legal name/address, NIP, public contact, correct PKD and formal business tax status confirmed; contracts identify the actual registered operator rather than project brand.
2. **Legal review:** all public statements fit actual practices; company signs Order and DPA with verified authorized signatory, and provider has documented agreements with processors. Review mandatory protections for sole traders whose contracts are non-professional and any B2C sales; do not blindly assert all rights are excluded merely because the label says B2B.
3. **GDPR records:** categorize employer-vs-provider controllers/processors; Art. 13 notices, Art. 28 DPA and supplier agreements, Art. 30 records as applicable, security risks and DPIA screening, actual retention rules and employee subject request channel. Confirm EU/EEA hosting or lawful transfers with evidence. Employees' rights must not be waived by a checkbox.
4. **Security readiness:** strong production system account password (no fallback), no publicly guessable PINs, rate limiting/account lockout and monitoring assessed, JWT and backups protected, TLS-only internet endpoint, least-privilege admin, restore drill, patch/update ownership, tested isolation (one client/deployment), incident runbook, access/export logs. **The current six-digit login and localStorage bearer token are explicit security-review items and may require a separate hardening plan before workforce-data launch.**
5. **Browser privacy:** audit storage and trackers; if any optional trackers exist, disable them until prior consent mechanism; publish accurate PKE art. 399 notice. Do not use marketing email/SMS without valid separate permission under art. 398.
6. **Commercial/accounting:** signed prices/invoicing/termination terms, VAT/OSS/VAT-UE assessment if cross-border, KSeF 2026 obligations and any transitional threshold assessed with accountant; do not advertise automated billing unless implemented.
7. **Operational:** production hostname/SSL, support privacy contact, backups retention/restore/deletion capability verified, actual export window agreed, production availability promises no greater than achievable, risk allocation reviewed, incident communications contact tested.
8. **Regulatory applicability review:** DSA hosting/user-content classification (cloud storage/messaging might qualify depending on functionality), EAA/Polish accessibility e-commerce coverage (B2B-only vs consumer sales), NIS2/KSC relevance, Polish employment-record constraints and GDPR high-risk worker monitoring. Do not automatically claim all apply or none apply.
9. **Final publication:** fill placeholders, obtain legal approval/hash, publish pages and approved PDF (plus signed corporate agreement), run release validator, manually inspect mobile/desktop/readability/keyboard and public no-login access; store signed documents/version/hash/acceptance evidence. Keep the exact previously used legal versions archived outside the source repo when replacing terms.
10. **Tenant boundary:** separate company deployment per contract until a distinct complete tenancy architecture/security test plan is approved. Never place multiple unrelated employers' actual workforce data into this one SQLite data store under the assumption of data isolation.

## 10. Verification commands (run from pinned checkout)

Before any code change: `git rev-parse HEAD`, `git branch --show-current`, `git status --short`. Stop/report a baseline mismatch or conflicting local edits; do not reset or discard changes. Read only the target files and immediate dependencies referenced here.

```bash
# backend
 dotnet restore GF3.sln
 dotnet build GF3.sln
 dotnet test GF3.sln

# frontend
 cd FrontEnd
 npm ci
 npm run lint
 npm run build
 # use the project's existing test script for Vitest (inspect package.json script once)
 cd ..

# legal/static checks
 node scripts/validate-legal-documents.mjs
 # ONLY after real operator facts and lawyer approval are supplied:
 node scripts/validate-legal-documents.mjs --release
```

Manually smoke-test anonymous access to every `/legal/*.html` through Vite and the compiled ASP.NET production host. Validate no redirect to `/login`, 200/`text/html`, printable text, mobile layout, and working cross-links. Test manager and employee login plus existing schedules/swaps/exports before and after acknowledgement. Test admin publish with existing guard. Verify `curl` against private API fails with HTTP 428 when document pending; a normal authenticated account resumes immediately after accepted version is saved. Test that no B2B private contract/TOM document leaks into `FrontEnd/dist` / `wwwroot`.

## 11. Acceptance criteria

- [ ] All seven Polish public legal pages are readable anonymously, responsive, printable, linked from sign-in and relevant authenticated layouts, with actual operator identity or explicitly still flagged `DRAFT`.
- [ ] Public Terms cover service scope, technology, contracting, prohibited content, complaints, termination, IP, risk, worker schedule/swap limitations, and mandatory-law safeguards.
- [ ] Privacy notice explicitly distinguishes operator/controller vs employer/controller, GF3 processor functions and actual data/retention/suppliers; cookies notice includes exact verified browser storage.
- [ ] Private B2B order/DPA templates contain role/authorization and all Article 28 elements; employer worker notice template and internal compliance procedures exist.
- [ ] No fictitious payment system, SLA, SOC 2 certificate, cloud vendor, company identity, GDPR consent or tenant boundary appears in any draft.
- [ ] Existing published `Regulations` PDF/version/SHA-256/checkbox/acceptance persistence is reused, existing account roles and history unaffected.
- [ ] Direct protected API requests by a user with pending published regulation are blocked with 428; only narrow recovery/publishing/auth paths remain reachable; after acceptance normal behavior resumes. Existing authentication/authorization/admin flags unchanged.
- [ ] Backend and frontend tests pass; production static paths serve HTML, not the app shell; no unrelated modules are changed.
- [ ] Draft validator passes locally. `--release` MUST fail until real operator-specific facts + genuine legal approvals are supplied; do not defeat it by fake approval/checksums.
- [ ] The operator has a concrete STOP-SHIP list covering business registration, taxes/KSeF, DPA, employee privacy, incident response, backups, security/PIN review, individual contracts, and per-company isolation.

## 12. DO NOT TOUCH

- Do not rewrite scheduling, shift swap, roster algorithms, shop/employee entities, exported workbooks, SignalR business behavior, permission policies, JWT token format or password system.
- Do not turn `docs/design-system.json` into runtime config or change its existing snapshot semantics.
- Do not modify old repository `Plan.md` during execution unless the operator explicitly chooses this external new plan as the replacement. Do not commit legal customer contracts with genuine personal details or sensitive security annexes.
- Do not implement multi-tenancy, checkout/Stripe, automatic billing, consent-management platforms, marketing integrations or analytics in this plan.
- Do not assume worker acknowledgement constitutes an employment contract, GDPR consent, DPA acceptance, or an employer's legally effective B2B signature.

## 13. Primary research/legal references — check current law again before signing

1. Polish Act on services by electronic means (UŚUDE), Art. 5–8: https://eli.gov.pl/api/acts/DU/2024/1513/text.html
2. GDPR EU 2016/679, Arts. 12–14, 28, 30, 32–35: https://eur-lex.europa.eu/legal-content/EN-PL/TXT/?uri=CELEX%3A32016R0679
3. UODO controller/processor relationship and Art. 28: https://uodo.gov.pl/pl/675/4229 ; UODO processor obligations: https://uodo.gov.pl/pl/676/4258
4. Polish Electronic Communications Law (PKE), Art. 398–400, especially necessary browser storage exception Art. 399(3): https://eli.gov.pl/api/acts/DU/2024/1221/text.html
5. KSeF official 2026/2027 timetable and threshold: https://ksef.podatki.gov.pl/etapy-wdrozenia-ksef/ ; https://ksef.podatki.gov.pl/informacje-ogolne-ksef-20/zakres-obowiazkowego-ksef/
6. Business establishment/review of registration: https://biznes.gov.pl/pl/portal/00120
7. EU DSA, intermediary/hosting status decided case by case: https://eur-lex.europa.eu/eli/reg/2022/2065/oj/eng ; EC hosting explanation https://digital-strategy.ec.europa.eu/en/faqs/dsa-transparency-database-questions-and-answers
8. UOKiK consumer rights / digital service background (application to sole traders/B2B requires legal assessment): https://prawakonsumenta.uokik.gov.pl/
9. Pinned source references (verified, not a complete code archive):
   - https://raw.githubusercontent.com/OlehProtsun/GF3/5980123995199dc4b6d40d2e3b17ff49731a6a39/README.md
   - https://raw.githubusercontent.com/OlehProtsun/GF3/5980123995199dc4b6d40d2e3b17ff49731a6a39/docs/deploy.md
   - https://raw.githubusercontent.com/OlehProtsun/GF3/5980123995199dc4b6d40d2e3b17ff49731a6a39/GF3.WebApi/Controllers/RegulationsController.cs
   - https://raw.githubusercontent.com/OlehProtsun/GF3/5980123995199dc4b6d40d2e3b17ff49731a6a39/BusinessLogicLayer/Services/RegulationService.cs
   - https://raw.githubusercontent.com/OlehProtsun/GF3/5980123995199dc4b6d40d2e3b17ff49731a6a39/FrontEnd/src/entities/regulations/ui/RegulationAcceptanceGate.tsx

**Final execution report:** give changed-file list, tests/run results, drafts remaining for legal review, STOP-SHIP blockers not resolvable by code, and explicit statement that no legal compliance/certification is guaranteed. Do not claim commercial launch is approved while any critical operator-specific field remains unresolved.
