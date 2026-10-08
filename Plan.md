# Plan.md — GF3: legal onboarding, client meeting, employee documents and launch runbook

**Repository:** `https://github.com/OlehProtsun/GF3`  
**Branch / pinned commit:** `DEV2` / `37540cfffb9fd89bd2a22a54c00cf3f451ce58a3`  
**Prepared:** 2026-10-08 (Poland)  
**Status:** IMPLEMENTATION PLAN ONLY. No code or project files have been modified.  
**Audience:** CODEX (the implementation instructions in part A) and the GF3 operator (plain-language operating instructions in part B).  
**Important:** This is not an approved contract, a legal opinion, a release authorization or proof of production security. The existing legal texts are explicitly `DRAFT`; real identities, technical controls, agreements and lawyer/accountant approvals must be supplied by humans. Never fabricate them.

---

# PART A — EXACT CODEX EXECUTION PLAN

## 1. Objective and scope

Make the **already-existing legal documentation** coherent, practical and ready for *human review* rather than generating more disconnected legal drafts. Produce a single Ukrainian operator playbook and a clear document map. Eliminate contradictory instructions about which B2B document is primary and whether the current application records employee/manager PDF acceptance. Add only the two missing *draft operational templates* needed to make an onboarding handoff repeatable: an onboarding card and an activation/handover protocol. **Do not claim legal approval or implement a new onboarding feature**: the repository already has a regulation publication/acceptance mechanism.

**Boundary of scope:** document edits only. Do not alter .NET, React, DB, APIs, DI, auth, regulation persistence, deployment topology or UI. The actual contractual finalization, security remediation and client-specific data inputs are non-CODEX tasks and remain release blockers.

## 2. Pin to observed state (do not re-analyze the entire repository)

1. `docs/legal/README.md` describes the existing commercial flow: operator facts → review/STOP-SHIP → B2B order and DPA signed *outside the repository* → single-client isolated deployment → approved regulation PDF/version/hash → publication by system manager → manager/employee sees pending PDF → individual unticked checkbox → acceptance record → API access. Each changed version requires acknowledgement.
2. `GF3.WebApi/Controllers/RegulationsController.cs`: existing `GET /api/regulations/pending`, `GET /api/regulations/history/me`, `GET /api/regulations/history/employees/{employeeId}`, `GET /api/regulations/{documentId}/pdf`, and `POST /api/regulations/{documentId}/accept`.
3. `GF3.WebApi/Middleware/RegulationAcceptanceGuardMiddleware.cs`: guards manager/employee API operations when published required regulations are pending, returns **HTTP 428**; auth and regulation routes have exemptions. It is not a contract signature mechanism.
4. `FrontEnd/src/entities/regulations/ui/RegulationAcceptanceGate.tsx`: presents title/version, PDF download, unticked checkbox, acceptance action, and the explicit explanation that the acknowledgement is personal, not the employer's signature or GDPR consent.
5. `GF3.Tests/RegulationAcceptanceGuardTests.cs` covers the guarded workflow. `docs/legal/README.md` reports previously run test successes, but those historical reports are **not a fresh local test** for this plan.
6. `FrontEnd/public/legal/*.html` is the **public document set**. `docs/legal/templates/*` and `docs/legal/internal/*` contain private drafts/checklists and must not enter the public build or worker acceptance by accident.
7. `docs/legal/saas-agreement/umowa-saas-b2b-pl.md` is a **long, detailed pre-existing negotiated SaaS agreement draft**, with `zalacznik-1-zakres-uslug.md`, `checklista-przed-podpisaniem.md`, `generate-html.mjs`, and rendered `umowa-saas-b2b-pl.html`. Its draft contains a negotiated/example **500 PLN/month**, seven-day payment, and other terms: none are confirmed business terms merely because written in that template.
8. `docs/legal/templates/Zamowienie_Umowa_B2B.md` is a **second, shorter commercial draft**, not an additional contract employees must accept. Avoid two contradictory master agreements.
9. `docs/legal/electronic-services-regulations/README.md` currently contains stale pre-implementation text saying acceptance/version is not stored; that **conflicts** with the current controller, guard, UI, and tests. Its `regulamin-swiadczenia-uslug-elektronicznych-pl.md` is another editable working source, while `FrontEnd/public/legal/regulamin.html` is currently the canonical published-path HTML draft. They are not guaranteed to have identical wording.
10. `docs/legal/LEGAL_REVIEW.md` labels documents `DRAFT`. `docs/legal/OPERATOR_INPUTS.md` has unchecked operator identity, finance, privacy, subcontractors, retention and technical data. `docs/legal/internal/CHECKLIST_PRZED_STARTEM.md` is a real **STOP-SHIP checklist**, including PIN/token/auth, backups, TLS, isolation, browser storage and legal review.
11. `scripts/validate-legal-documents.mjs` checks the seven public pages, links and headings. With `--release`, it checks absence of draft markers and the actual approved document **version and SHA-256** in `docs/legal/LEGAL_REVIEW.md`; failure while files remain drafts is expected and must not be hidden.
12. Existing backend/UX allows PDF publication for the applicable roles; **no separate privacy-consent flow is established**. Do not repurpose the acknowledgement endpoint to claim workers consent to GDPR processing.

## 3. Exact file change matrix

| Action | Exact repository path | Responsibility |
|---|---|---|
| CREATE | `docs/legal/OPERATOR_ONBOARDING_PLAYBOOK_UA.md` | One practical human-readable guide: before meeting, negotiation, signatures, instance setup, worker notification and acknowledgement, operations, incidents, offboarding |
| CREATE | `docs/legal/templates/Karta_Wdrozenia_Klienta_Wzor.md` | Client-by-client intake form; only placeholder fields, no real customer information |
| CREATE | `docs/legal/templates/Protokol_Uruchomienia_i_Przekazania_Wzor.md` | Evidence of handed-over instance/access/training/testing and start date; not a replacement for signed B2B/DPA |
| MODIFY | `docs/legal/README.md` | Canonical navigation and flow, links to the new files, distinction between app PDFs and contractual files, STOP-SHIP status |
| MODIFY | `docs/legal/saas-agreement/README.md` | Mark the longer SaaS agreement as the primary **working basis**, not a legally approved master; explain short order template may be used only as a cover/order after legal reconciliation, not as a competing second signed agreement |
| MODIFY | `docs/legal/electronic-services-regulations/README.md` | Correct obsolete assertion that no acceptance is saved; distinguish historical intent from 37540cf implementation; show canonical production URL and review requirement |
| MODIFY | `docs/legal/internal/CHECKLIST_PRZED_STARTEM.md` | Add procedural gates for meeting, approved signatures, correct PDF categorization, worker information evidence, tenant isolation, dry-run and offboarding; preserve original risk list |
| MODIFY | `docs/legal/LEGAL_REVIEW.md` | Add a *DRAFT* tracking row for each new private template/guide as appropriate; do not alter status of existing rows or invent hashes, reviewer names or signatures |
| REUSE | `docs/legal/templates/Umowa_Powierzenia_Danych_DPA.md` | Processor agreement framework under GDPR Article 28; requires customer-specific finalization |
| REUSE | `docs/legal/templates/Zalacznik_TOMs_i_Retencja.md` | Security measures, backups, retention, deletion and evidence |
| REUSE | `docs/legal/templates/Zalacznik_SLA_i_Wsparcie.md` | Optional contractual support commitments; never silently promise a performance guarantee |
| REUSE | `docs/legal/templates/Klauzula_Informacyjna_Dla_Pracownikow_Wzor.md` | Employer-customized GDPR notice for employees; informational delivery, not consent |
| REUSE | `FrontEnd/public/legal/{index,regulamin,polityka-prywatnosci,pliki-cookies,zasady-korzystania,podwykonawcy,bezpieczenstwo}.html` | Public web documents; no change until *real operator data and lawyer approval* exist |
| REUSE | `docs/legal/internal/{Rejestr_Czynnosci_Przetwarzania,Rejestr_Podwykonawcow_i_Transferow,Procedura_Incydentow_i_Naruszen,Procedura_DSAR_Retencji_Usuwania,Ocena_Ryzyka_i_DPIA_Screening}.md` | Private compliance records and procedures, not worker acceptance PDFs |

Do **not** create a fake new mandatory employee `regulamin pracy` as an operator document. Such employment regulations belong to the employer, are subject to labor-law thresholds and must be written/approved by that employer. The existing electronic-services/usage rules can be the basis of a **separately approved app-usage PDF**; prepare it through human legal review, not auto-conversion of draft boilerplate.

## 4. Ordered deterministic implementation steps

### Step 1 — CREATE operator playbook

**File:** `docs/legal/OPERATOR_ONBOARDING_PLAYBOOK_UA.md`.

Create these named, numbered sections, with concrete instructions and no fabricated data:

1. Purpose, roles, distinction between provider/operator, customer/employer, and user/employee; when provider is processor vs separate controller for own activities; mark latter for privacy-law review.
2. Readiness traffic light: `DEMO ONLY`, `LEGAL REVIEW PENDING`, `READY TO SIGN`, `READY FOR PRODUCTION`, with **preconditions**; don't automatically infer status from passing tests.
3. Document inventory with exact source paths and columns: document, purpose, actor completing, actor signing/accepting, where stored, whether uploaded to regulation gate. Include long agreement, short order, SaaS scope annex, DPA, TOMs, optional SLA, public terms, privacy/cookies, worker information, app usage PDF, employer HR work rules, internal records.
4. Meeting flow: what to take/ask/demo; verify customer identity and representation; collect contacts, staff counts, locations, job/scheduling process, scope of swap approval, price, term, invoicing, service region, use of work/personal phone and e-mail, onboarding limits, permitted data categories; never collect real staff spreadsheets before DPA and secure provision.
5. Signature flow: identify parties, negotiate a **single** commercial agreement (long SaaS draft as primary base), scope annex, DPA and TOMs; optional SLA only when promised; obtain representative authority and retain signature/e-delivery evidence *outside Git and public hosting*. Make approval by Polish lawyer and finance advisor a gate.
6. Technical onboarding: one client → isolated instance, domain, DB/volume, secrets, backup and restoration, TLS; customer manager account; publish only approved, correctly scoped/versioned PDF; verify each role sees only applicable documents; download/history and HTTP 428; train manager to create/manage user accounts and control swaps; never claim automatic payroll/working-time legal compliance.
7. Employee journey: employer provides GDPR notice; operator makes site usage terms accessible; **only** relevant versioned platform/app-usage regulation gets affirmative acceptance in the current gate; no DPA, SaaS, invoices, internal controller audit docs, or GDPR 'consent' checkbox to be pushed as a regulation.
8. Operations: invoices, contract contacts, support/escalation, change/new versions, employee departure, audit trail, incident reporting, data subject access request, backup, cancellation/export/delete, record retention.
9. Go/no-go checklist, unknowns, source references and precise operational ownership.

**Requirement:** The playbook must be Ukrainian in very plain language, with an exact illustrative timeline (e.g. Friday 2026-10-09 discovery meeting), an example of which party signs what, and a small FAQ about mandatory regulations and worker acceptance. Use the longer usable instructions provided in **Part B of this plan** as the content contract, but reformulate cleanly for a standalone document.

### Step 2 — CREATE customer intake card

**File:** `docs/legal/templates/Karta_Wdrozenia_Klienta_Wzor.md`.

Polish-language private **draft** form with explicit placeholders:

- Client legal name, KRS/CEIDG, NIP, address, authorized representative and authority verification, emails for billing/support/privacy/incidents; owner/date/source of verification.
- Provider legal identity/business status to be separately validated (no assumption of registered company or automatically valid nonregistered activity).
- Contract version, order/reference, signed date, start date, subscription amount and currency, tax treatment **pending accountant**, duration/notice/first invoice, agreed optional SLA.
- Locations, expected number of users, role mapping manager/employee, needed functions, who approves swaps, what does **not** constitute legally valid working time approval.
- Data matrix: employee names/identifiers, contact type, schedules, availability, swap records, manager messages, account/access logs, storage region, transfer/subprocessor review, lawful basis decided by employer, retention, exports/deletion.
- Secure delivery contact/method (do not paste personal data or credentials into the template); DPA and privacy notice delivery/approval evidence.
- Deployment checklist: instance ID (nonsecret), domain, isolated DB/volume, TLS, backups/restore, least privilege, test accounts, regulation PDF ID/version/hash, acceptance evidence; date and tester.
- Signatures/approval status, link to *external protected repository* containing final signed PDFs. Never store real customer data in Git.

### Step 3 — CREATE activation/handover record

**File:** `docs/legal/templates/Protokol_Uruchomienia_i_Przekazania_Wzor.md`.

Polish draft capturing client/provider identity, signed agreement reference, isolated instance URL, activation datetime, actual delivered features, account handover done securely, basic training provided, customer receipt, smoke test of management/employee roles and swaps, backup/restore test evidence, approved regulations, contact/support details, unresolved issues and acceptance of delivery by authorized representatives. Explicitly say: does not replace DPA, employment contracts, GDPR notices or the SaaS agreement. Never auto-certify testing.

### Step 4 — MODIFY legal navigation

**File:** `docs/legal/README.md`.

Keep currently documented 37540cf behavior and test history. Add a top-of-file mini-directory linking to the new playbook, the two new templates, existing SaaS and DPA, employee notice and internal STOP-SHIP. Define **three separate tracks**:

- **Commercial:** provider ↔ company: one SaaS agreement + scope, DPA/TOMs, optional negotiated SLA.
- **Public site:** legal pages accessible without login, after real operator identity and approval.
- **Individual user:** approved app regulation PDF acknowledged by each relevant manager/employee, while employer delivers GDPR information separately.

Add a note: a worker's checkbox never signs company agreements, grants generalized GDPR consent or replaces employment documents.

### Step 5 — MODIFY SaaS source selection

**File:** `docs/legal/saas-agreement/README.md`.

Add a section `Which document to give the client?` which states:

- use `docs/legal/saas-agreement/umowa-saas-b2b-pl.md` (generated `.html` for print-to-PDF) as **primary longer negotiation draft**;
- `docs/legal/templates/Zamowienie_Umowa_B2B.md` is an alternative short order/cover concept **not an independently approved second contract**. If a finalized SaaS agreement already includes the full order terms, do not hand over a contradictory second master agreement; an order cover must explicitly incorporate the finalized master and be vetted;
- `zalacznik-1-zakres-uslug.md` specifies delivered functions;
- attach negotiated DPA plus its security and subprocessors instructions; optional SLA only where agreed;
- 500 PLN, billing direction and termination provisions are **draft default/example terms, not fixed by verified sales agreement**;
- export the edited HTML to PDF (Chrome/Edge Ctrl+P A4) and keep the human-signed output privately; never imply Node generation signs or approves it.

Do not rewrite the underlying commercial clauses, tax status or worker privacy bases.

### Step 6 — MODIFY stale acceptance description

**File:** `docs/legal/electronic-services-regulations/README.md`.

Replace the stale language in the `Rejestrowanie akceptacji` and `Funkcje wyłączone` sections that wrongly claims the current code does not store acceptance with a precise dated statement:

- As of pinned commit, manager and employee regulation acceptances already have published-PDF pending/list/history/accept endpoints and a 428 guard.
- `FrontEnd/src/entities/regulations/ui/RegulationAcceptanceGate.tsx` requires a deliberately checked box; it offers PDF download; this proves a submitted acknowledgement, **not** that the person read every word and not consent to personal-data processing.
- An approved and uploaded PDF is separate from the web `regulamin.html` URL; keep textual versions aligned by an explicit human review/checklist, rather than assume one is generated automatically from another.
- Keep other historical sections if they are still relevant, but label historical audit findings `as of before 37540cf` if superseded; don't imply changes to unrelated account features.

### Step 7 — MODIFY pre-launch checklist and legal review register

**Files:** `docs/legal/internal/CHECKLIST_PRZED_STARTEM.md` and `docs/legal/LEGAL_REVIEW.md`.

Append independently testable, human-owned gates: `operator verified`, `client representative verified`, `single executed SaaS contract`, `executed DPA/TOMs`, `support and finance ready`, `subprocessors accounted for`, `employee notice approved by employer and delivered`, `separate production instance verified`, `published PDF approved/scope and history checked`, `test user acceptance/428/manager employee history`, `exit/export/delete process validated`, `incident contact tested`. Each gate needs `owner / evidence location / test date / status` placeholders. Preserve all present STOP-SHIP items and historical draft rows. Add DRAFT rows for new private documents only; **do not set `APPROVED`**, create false SHA hashes, or silently turn release validator green.

### Step 8 — Documentation consistency and static validation

1. In repo root, `git diff --check` — no whitespace defects in changes made by Codex.
2. `node scripts/validate-legal-documents.mjs` — expected to pass existing **draft mode**, assuming untouched public HTML.
3. `node scripts/validate-legal-documents.mjs --release` — **expected to fail until a real reviewer has approved the final public texts**; a failure is a valid STOP-SHIP, do not bypass or fake its inputs.
4. Search *only changed legal docs* for unqualified statements `no acceptance exists` and `500 PLN is obligatory for employees`: there must be none.
5. Validate every new relative markdown link resolves to a real path in the pinned repo.
6. Verify no generated documents with actual customer information, secrets or signed contracts are added to Git.
7. No need for `dotnet build`/`dotnet test` or `npm build` to validate documentation-only changes; no runtime artifacts touched. Existing tests should be run before actual release after any other implementation/security changes.

## 5. API, data, dependency and configuration contract

**API:** NONE. Preserve `GET /api/regulations/pending`, `GET /api/regulations/history/me`, `GET /api/regulations/history/employees/{employeeId}`, `GET /api/regulations/{documentId}/pdf`, `POST /api/regulations/{documentId}/accept` and existing publication/upload flows without modification.  
**Persistence / migration:** NONE. Preserve existing version/history/acceptance persistence.  
**Dependency injection:** NONE.  
**Configuration / `.env`:** NONE. Do not make up production secrets or client identities.  
**UI:** NONE. Do not build a new checkbox, duplicate gate or a new signup funnel.  
**Operational release:** Outside Codex; requires signed contracts, verified controls and legal approvals.  
**Error behavior:** Do not mask existing `428`, errors, unpublished PDF checks or validator failures. A document link missing or inconsistent is a documentation verification failure; don't publish it.

## 6. Acceptance checklist for CODEX

- [ ] One coherent Ukrainian operator playbook exists at exact path, describing every phase from client discovery to data deletion.
- [ ] Polish intake card + activation protocol are marked **templates/drafts** and contain no real client information.
- [ ] Navigation points to all relevant existing and new files.
- [ ] The long SaaS agreement is clearly identified as primary *working draft*, not an approved agreement; shorter document is explicitly non-competing.
- [ ] The historical "no acceptance stored" assertion is corrected using the pinned version's code evidence.
- [ ] Clear distinction among operator site regulation, employer labor regulations and employer GDPR notice.
- [ ] No employee is described as signing B2B/DPA or "consenting to RODO" via the regulation checkbox.
- [ ] STOP-SHIP is retained; none of the DRAFT statuses, missing operator facts, unresolved security warnings or release checks are falsified.
- [ ] Validator draft mode passes (where tool runtime is available); release mode remains blocked if still genuinely unapproved.
- [ ] No production code, packages, schema, infrastructure or unrelated files changed.

## 7. DO NOT TOUCH

`GF3.WebApi/**`, `GF3.Tests/**`, `FrontEnd/src/**`, `FrontEnd/public/**`, `BusinessLogicLayer/**`, `DataAccessLayer/**`, `scripts/**`, deployment/CI files, account/swap/schedule services and existing real DB. Preserve the originally pinned commit as the analysis baseline and implement only the documentation changes above.

---

# PART B — ПРАКТИЧНА ІНСТРУКЦІЯ ДЛЯ ВЛАСНИКА GF3 (користуватися вже зараз)

> **Статус на 8 жовтня 2026 року:** у репозиторії є хороші чернетки, але НЕ готовий юридично затверджений пакет. Нижче — робочий сценарій і карта документів. Це не замінює перевірку польського юриста, бухгалтера чи аудит конкретного сервера. Не підписуй незаповнену версію з `[DO UZUPEŁNIENIA]`.

## Б1. Хто є ким і чому це важливо

**Ти / оператор GF3:** надаєш компанії доступ до вебзастосунку для графіків, доступності, замін, сповіщень. Для даних персоналу, які обробляєш за дорученням роботодавця, зазвичай ти — `podmiot przetwarzający` (процесор). За власні дані щодо рахунків, продажів і ділового зв'язку можеш мати окрему роль адміністратора; це потрібно визначити за фактичними цілями обробки.

**Клієнт / компанія-роботодавець:** купує сервіс, призначає менеджерів, вводить працівників, визначає, навіщо обробляються їхні дані. Для кадрових даних зазвичай є `administrator danych` — відповідає за законну підставу, інформування працівників і законність трудових графіків.

**Менеджер клієнта:** користувач з рольовим доступом, керує графіками, працівниками і swap. Зазвичай не підписує від імені компанії B2B, якщо немає окремих повноважень.

**Працівник клієнта:** звичайний користувач GF3. Не платить тобі за підписку; не укладає B2B і DPA; може персонально підтвердити правила користування застосунком. Його особистий checkbox **не є** юридичною згодою на всі операції з персональними даними і не замінює трудові документи.

## Б2. Які бувають документи: точно кому і куди

| Документ | Де вже є заготовка | Що заповнити | Хто підписує / отримує | В `RegulationAcceptanceGate`? |
|---|---|---|---|---|
| Основний платний **договір SaaS B2B** | `docs/legal/saas-agreement/umowa-saas-b2b-pl.md` та `.html` | Твої правдиві реквізити/статус; юридичні дані клієнта; ціну; VAT; оплату; строк; припинення; підтримку | Уповноважений оператор та уповноважений представник компанії | **НІ** |
| Додаток із конкретними функціями | `docs/legal/saas-agreement/zalacznik-1-zakres-uslug.md` | Кількість акаунтів, об'єкти, включені функції, запуск, межі послуги | Компанія + оператор разом із договором | **НІ** |
| Коротке `Zamówienie / Umowa B2B` | `docs/legal/templates/Zamowienie_Umowa_B2B.md` | Комерційні параметри за однією узгодженою схемою | Лише якщо юрист узгодить із основним договором; **не підписуй два суперечливі договори** | **НІ** |
| **DPA / umowa powierzenia** (ст. 28 GDPR/RODO) | `docs/legal/templates/Umowa_Powierzenia_Danych_DPA.md` | Сторони, дані/категорії людей, мета, строки, інструкції, субпроцесори, ризики, допомога, видалення | Компанія-адміністратор і оператор-процесор | **НІ** |
| **TOMs/retencja** — засоби захисту, строки зберігання | `docs/legal/templates/Zalacznik_TOMs_i_Retencja.md` | РЕАЛЬНІ HTTPS, backup, відновлення, доступи, зберігання, експорт, видалення | Як погоджений додаток до DPA | **НІ** |
| **SLA / підтримка** | `docs/legal/templates/Zalacznik_SLA_i_Wsparcie.md` | Контакт, дні/години, час реагування, обмеження, backup, RPO/RTO лише підтверджені | Тільки якщо сторони справді погодили | **НІ** |
| **Regulamin świadczenia usług drogą elektroniczną / правила платформи** | `docs/legal/electronic-services-regulations/regulamin-swiadczenia-uslug-elektronicznych-pl.md` + **публічний** `FrontEnd/public/legal/regulamin.html` | Дані оператора, функції, техвимоги, обмеження, скарги, припинення, дата й версія; узгодити обидва тексти | Оператор публікує ДО користування/контракту; користувачам забезпечити доступ | **Може бути** погоджена, релевантна версія PDF для персонального підтвердження; не публікувати чернетку |
| **Zasady korzystania** / практичні правила користувачів | `FrontEnd/public/legal/zasady-korzystania.html` | Особисті акаунти, безпека, заборонені дії, графіки/swap; після review може бути єдиний узгоджений PDF | Користувачі бачать / за потреби підтверджують | **ТАК**, якщо людино- та рольово-доречний затверджений PDF |
| **Політика приватності / cookies** | `FrontEnd/public/legal/polityka-prywatnosci.html`, `pliki-cookies.html` | Реальні ролі й цілі, дані, основи, строки, cookies, контакти, провайдери | Оператор публікує, користувачі читають; додаткові cookie-згоди лише якщо застосовні | **НІ як універсальна GDPR-згода** |
| **Інформаційне повідомлення для працівника (GDPR art. 13/14)** | `docs/legal/templates/Klauzula_Informacyjna_Dla_Pracownikow_Wzor.md` | Реальні дані **роботодавця**, мета/основи, види даних, GF3 як процесор, строк/права/контакт/одержувачі | Роботодавець надає працівникам та фіксує факт інформування | **Не через кнопку «zgadzam się na RODO»**. Передати окремо, можливе підтвердження отримання |
| **Regulamin pracy / правила роботи компанії** | У GF3 немає універсального затвердженого регламенту роботодавця | Створює сам роботодавець за польським трудовим правом, якщо застосовно | Роботодавець ознайомлює працівників | **Не замінювати правилом оператора**. Не завантажувати автоматично |
| **Внутрішні RODO-реєстри, DPIA, інциденти** | `docs/legal/internal/*.md` | Фактичні процеси, відповідальні, докази | Оператор/клієнт всередині своїх структур | **НІ**, внутрішні документи |

**Принцип:** якщо ти не можеш пояснити, *чому кожен працівник має саме це підтверджувати*, не публікуй цей файл як обов'язкову регуляцію через gate. Комерційні й кадрові папери — інша юридична взаємодія.

## Б3. Завтра, п'ятниця 9 жовтня 2026: зустріч із потенційним клієнтом

### Що підготувати ДО зустрічі

1. **Демо на синтетичних даних**, без справжніх прізвищ/номерів телефону співробітників. Показати manager → створення графіка → публікацію → працівник бачить графік → доступність → swap та підтвердження → історію; визнати, що це не кадрова система, не зарплатний калькулятор і не юридична перевірка графіка.
2. **Односторінкову комерційну пропозицію**: що саме отримує компанія, приблизна кількість співробітників/локацій, ціна й умови як *пропозиція*, не неперевірений рахунок чи підписаний договір. 500 PLN/місяць — лише число, закладене в чинному **чернетковому** SaaS договорі; його треба підтвердити/узгодити.
3. **Чернетку основного договору**: `docs/legal/saas-agreement/umowa-saas-b2b-pl.html`. Можна показати як матеріал для переговорів. HTML → Chrome/Edge → `Ctrl+P` → Save as PDF, A4; не прибирати юридичні caveats до погодження з юристом.
4. **Чорнові DPA/TOMs** (для розмови про дані), **scope annex**, окремо **список запитань** з `docs/legal/saas-agreement/checklista-przed-podpisaniem.md`.
5. **Свою перевірену особу й комерційний статус**. Якщо ти ще не маєш зареєстрованої діяльності, НЕ заявляй автоматично, що можеш підписати як компанія без CEIDG або що немає ZUS/VAT. Чинний шаблон прямо передбачає можливу `działalność nierejestrowana`; для безперервного SaaS і контрактної класифікації потрібна перевірка бухгалтером/юристом (див. також нормативні джерела в Б9).
6. **Картку питань клієнту**: повна назва/NIP/KRS або CEIDG, хто має право підписати, юридична адреса, контакт ІТ/HR/RODO, адреса для рахунків, кількість робітників і локацій, чи потрібні окремі ролі, як схвалюється swap, строки договору, види даних, чи будуть особисті номери/пошти, вимоги до експорту/видалення, бажана дата запуску.

### Як провести зустріч — репліки простими словами

«GF3 — вебсервіс для створення та публікації графіків, доступності й замін змін. Ви як роботодавець залишаєтеся відповідальними за своїх працівників і законність графіків. Ми надаємо інструмент, а не беремо на себе payroll, трудове право чи кадрову документацію. Якщо домовимося, фіксуємо точний обсяг і ціну в договорі SaaS, окремо підписуємо угоду про обробку персональних даних DPA, готуємо вашу ізольовану інстанцію і тільки після перевірки вносимо дані співробітників. Працівники не підписують з нами ваш корпоративний договір: вони отримують інформацію від роботодавця і бачать затверджені правила користування системою».

**Не роби завтра:** не вимагай підписів під `DRAFT`; не проси Excel із реальними працівниками на незахищену пошту; не обіцяй 24/7, SOC2, ISO, 99,9%, автоматичну відповідність трудовому праву, юридичну сертифікацію; не вписуй вигадані строки видалення; не підписуй від імені компанії без повноважень.

### ПІСЛЯ зустрічі

Занеси результати у приватну картку клієнта. Уточни оферту/ціну, звір повноваження підписанта, передай юридичні питання адвокату/radca prawny, облік/VAT/ZUS — бухгалтеру. Тільки після погодження сторони підписують фінальну комерційну угоду + scope + DPA/узгоджені додатки і отримують копії. Підписані PDF зберігай у захищеному сховищі з контрольованим доступом, **не в GitHub і не в публічному `public/legal`**.

## Б4. Як заповнити найважливіші поля договору

**У SaaS договорі:**

- `Usługodawca` — **справжня юридична особа/фізична особа у законній формі**; ПІБ/назва, адреса та відповідні ідентифікатори. Якщо форма діяльності неясна — STOP, фінансово-правова перевірка.
- `Klient` — точна назва й організаційна форма, KRS/CEIDG, NIP, адреса, хто представляє та за яким правом (не просто менеджер із графіків).
- `Platforma` — домен конкретного клієнта/інстанції, реальні доступні функції, число користувачів, дата старту.
- `Opłata` — узгоджена сума/валюта та спосіб податкового опису, перший період, оплата наперед/після, термін платежу, форма бухгалтерського документа.
- `Okres / wypowiedzenie` — строк дії, повідомлення про припинення, коли зупиняється доступ, як клієнт експортує дані й коли вони видаляються.
- `Odpowiedzialność / SLA` — тільки умови, на які погодився юрист і які реально можеш виконати. Межі відповідальності не встановлювати навмання.

**У DPA:** компанія = адміністратор даних; оператор GF3 = процесор на стороні обслуговування персоналу. Зазначити категорії співробітників, які поля обробляються, задачі (графіки, swaps, повідомлення, права доступу), дозволених менеджерів, місця хостингу, субпроцесорів, процедури інциденту, строки, видалення та допомогу із запитами людей. Додаток TOMs повинен описувати **дійсно наявний** захист, не плани.

**У повідомленні працівнику:** роботодавець вказує себе як адміністратора, зв'язок/відповідального, цілі та підстави для кожної категорії обробки, одержувачів, строки, права та шлях звернення. Зазвичай правова підстава у кадровому процесі **не** зводиться до «натиснув кнопку — дав згоду». Не збирай добровільну GDPR-згоду там, де обробка об'єктивно потрібна для виконання трудових обов'язків/правових зобов'язань.

## Б5. Працівник зайшов на сайт: що він реально побачить у нинішньому GF3

1. Компанія **попередньо законно** включила потрібні дані, забезпечила GDPR-повідомлення і створила обліковий запис через менеджера. Самореєстрація кожної незалежної фірми в цій версії не підтверджена.
2. Застосунок перевіряє автентифікацію, роль, опубліковані обов'язкові документи **для цього користувача** і вже підтверджені версії.
3. За наявності pending PDF користувач бачить назву і версію, може **завантажити PDF**, ставить **порожній за замовчуванням checkbox** «ознайомився і приймаю версію…», тисне «Accept and continue».
4. Сервер записує прийняття за користувачем/роллю/документом/версією в наявному механізмі; у разі незавершеного прийняття звичайні захищені API відповідають **HTTP 428**. Після прийняття доступ поновлюється.
5. Коли опублікована **нова обов'язкова версія**, потрібно підтвердити її окремо; старі записи й історію зберегти. Менеджер має endpoint перегляду історії конкретного працівника, у межах поточних дозволів.
6. **Важливе обмеження:** чекбокс доводить факт заявленого підтвердження, **не доводить фактичного уважного читання** й не є підписом роботодавця. Не можна змушувати працівника підтверджувати DPA/рахунок або видавати це за згоду на будь-яку RODO-обробку.

### Який саме PDF ставити на acceptance?

**Перший кандидат:** один перевірений юристом, датований, версійний **Regulamin korzystania z platformy GF3 dla upoważnionych użytkowników**, погоджений з опублікованими операторськими `Regulamin`/`Zasady korzystania`. Зміст: призначення GF3; хто створює акаунти; особисте користування; захист пароля/PIN; кому звертатись щодо доступу; що графіки і swaps потребують організаційного/юридичного підтвердження роботодавцем; заборона стороннього використання, контакт підтримки і процедура скарг; дата/номер версії. Не писати в ньому «працівник дає добровільну згоду на всю обробку RODO».

**Що НЕ прикріплювати як acceptance:** SaaS/B2B; DPA; конфіденційні TOMs; SLA; рахунок; внутрішній реєстр RODO; інформаційну клаузулу роботодавця у вигляді «zgadzam się na przetwarzanie»; трудовий контракт; неузгоджений «regulamin pracy» іншої компанії.

**Якщо компанія хоче підтвердження власних HR-правил:** спочатку обговорити зі своїм HR/польським трудовим юристом, чи саме цей документ належить працівникам і чи належним способом він доводиться до відома. Поточний загальний gate не слід автоматично вважати повноцінним кадровим документообігом; для цього потрібне окреме погоджене призначення й аудит.

## Б6. Чи взагалі потрібен «regulamin»? Не плутай три різні речі

**1) `Regulamin świadczenia usług drogą elektroniczną` оператора GF3:** загалом потрібен для дистанційної електронної послуги; польський закон вимагає визначити та **безоплатно надати його до укладання договору**, щоб можна було зберегти/відтворити; описати функції, технічні умови, заборони, укладення/розірвання, рекламації (art. 8 UŚUDE). Це НЕ «regulamin pracy». Наявні draft HTML ще треба завершити.

**2) `Zasady korzystania z aplikacji` працівника/менеджера:** зрозуміла інструкція та правила користування сервісом. Її доцільно показувати й документувати персональне ознайомлення/прийняття, якщо текст коректний і його застосування узгоджене з юридичною моделлю договору. Не кожна політика за законом вимагає окремого checkbox; документ має бути наданий належним чином.

**3) `Regulamin pracy` роботодавця:** це кадровий акт **клієнта, не власника GF3**. За польським Кодексом праці загалом обов'язковий від **50 працівників**, за **20–49** — якщо за умовами закону звернулася профспілкова організація; для менших компаній можливий добровільний; є винятки для колективних договорів. Роботодавець повинен ознайомити працівника з чинними правилами перед початком роботи. Це не означає, що ти маєш генерувати його клієнтам або додавати до кожного GF3-логіну.

## Б7. Як запускати реального клієнта без хаосу

**Фаза 0 — Legal & security gate:** підтверджена законна форма продажу послуги, рахунки, VAT/ZUS/KSeF за конкретними обставинами; юрист затвердив публічний regulamin, політики, SaaS, DPA; перевірені безпечні PIN/auth/token, HTTPS, доступи, ізоляція, копії і restore, субпроцесори, cookie/storage, персональна інформація. Поточний `CHECKLIST_PRZED_STARTEM.md` окремо позначає ризики 6-значних паролів, localStorage JWT тощо — не вважати проєкт готовим тільки через успішні unit tests.

**Фаза 1 — Угода:** фінальні, узгоджені й підписані SaaS + scope та DPA/TOMs; погоджене SLA лише коли справді є. Контакти юридичні/фінансові/інцидентні з обох сторін і зовнішній захищений архів підписів.

**Фаза 2 — Ізольована інстанція:** окрема організація → свій домен, база/volume, конфігурація і секрети, резервні копії; окремо перевірити реальне серверне оточення. **Не** розміщати двох незалежних роботодавців у спільній інстанції, бо поточний пакунок не підтверджує відповідну ізоляцію.

**Фаза 3 — Налаштування:** створити уповноваженого менеджера, налаштувати локації/акаунти, перевірити безпечний канал початкових доступів, не завантажувати більше персональних даних, ніж потрібно, і переконатися у можливості відкликати доступ.

**Фаза 4 — Документи для людей:** опублікований та затверджений публічний `regulamin.html` доступний до підпису, інформаційні повідомлення від роботодавця доставлені; **окремий придатний PDF** — через існуючий системний інтерфейс upload/publish відповідальним системним менеджером, в правильній інстанції. Не заливати PDF із написом DRAFT.

**Фаза 5 — Контрольний тест:** свіжий тестовий manager/employee, pending → PDF → checkbox → accept → API працює; без підтвердження — HTTP 428; нова версія знову просить підтвердження; відновлюються графік, swaps і відображається історія. Перевірити доступність старого PDF/історії в погодженому архіві.

**Фаза 6 — Акт введення:** заповнити протокол запуску/передачі, хто отримав доступ, проведено навчання, що протестовано та які проблеми залишилися. Він не замінює підписаний договір.

## Б8. Що робити після запуску — повсякденний цикл

**Кожного місяця / розрахунковий період:** перевірка вартості договору, виставлення правильного документа й отримання оплати, запис платежів, підтримка й обробка звернень, перевірка резервних копій. GF3 не має підтвердженого автоматичного білінгу — не покладайся на відсутню функцію.

**Коли додають працівника:** компанія забезпечує інформацію та підставу обробки, менеджер створює акаунт, працівник має доступ до правильних правил; лише за необхідності окремо підтверджує approved PDF.

**Коли змінюється правило:** юрист/відповідальна особа затверджує нову редакцію, зазначається версія і дата, публікується новий PDF, зберігаються попередні версії і підтвердження. Не змінюй існуючий файл 'тихо' без версіонування й доказу.

**Коли працівник звільняється:** клієнт відкликає доступ; за погодженими політиками зберігає/видаляє відповідні кадрові дані, графіки й слід аудиту. Не знищувати все відразу без перевірки трудових строків і DPA; не залишати активний доступ.

**Якщо витік чи підозра на витік:** зареєструвати інцидент, локалізувати, зберегти докази, без зволікання повідомити клієнта-адміністратора через контакт DPA. **72 години** стосуються оцінки повідомлення наглядового органу адміністратором за відповідних умов GDPR, а не універсального права процесора чекати 72 години, перш ніж повідомити клієнта.

**Коли працівник просить видалити дані / доступ:** не видаляй самовільно дані роботодавця; верифікуй запит, передай адміністратору-роботодавцю за DPA, допоможи виконати його рішення, збережи журнал дій.

**Коли клієнт припиняє підписку:** за підписаними SaaS/DPA надай погоджений експорт у доступному форматі, закрий доступ, поверни/видали дані згідно з інструкцією і обов'язковими винятками, перевір бекапи/ретенцію, збережи протокол. Не обіцяй PDF-експорт із поточного UI або спеціальну міграцію, якщо це не наявна функція.

## Б9. Що не можна вважати вирішеним (реальні блокери)

- У `docs/legal/LEGAL_REVIEW.md` документи все ще **DRAFT**, без реальних затверджень і SHA-256. Публічні HTML містять `[DO UZUPEŁNIENIA]`.
- Треба підтвердити, чи маєш **законну форму надання постійного SaaS**, які ZUS/VAT/податки та потрібні рахунки. Польська `działalność nierejestrowana` має специфічні вимоги та навіть за такого статусу договори послуг можуть створювати обов'язки платника внесків у клієнта. Не вважати, що просто слово "B2B" обходить ці правила.
- Не визначені фактичні субпроцесори/хостинг, терміни зберігання, тест restore і політика cookie/storage на реальній доменній конфігурації.
- У STOP-SHIP є окремі питання реальної безпеки акаунтів; їх треба вирішити **до** реальних даних персоналу, незалежно від цього документаційного плану.
- Розрізнення старих `docs/legal/electronic-services-regulations/*` та нового `FrontEnd/public/legal/*` потребує узгодження остаточного затвердженого тексту; не вважати будь-який markdown автоматично опублікованим.
- Докази підписання/повноважень та передачі GDPR-повідомлень не з'являються від того, що тест чекбокса зелений.

## Б10. Джерела для юриста та фінансової перевірки (станом на 2026-10-08)

- GF3 pinned legal README: https://github.com/OlehProtsun/GF3/blob/37540cfffb9fd89bd2a22a54c00cf3f451ce58a3/docs/legal/README.md
- GF3 launch blocker list: https://github.com/OlehProtsun/GF3/blob/37540cfffb9fd89bd2a22a54c00cf3f451ce58a3/docs/legal/internal/CHECKLIST_PRZED_STARTEM.md
- EU GDPR/RODO arts. 13, 28, 30, 32–34: https://eur-lex.europa.eu/legal-content/PL/TXT/?uri=CELEX:32016R0679
- Polish electronic services law, art. 8: https://eli.gov.pl/api/acts/DU/2024/1513/text.html
- Polish UODO — controllers vs processors: https://www.uodo.gov.pl/pl/675/4234
- Polish PIP — employer duties and workplace regulations: https://www.pip.gov.pl/dla-pracodawcow/niezbednik-pracodawcy/jak-zatrudnic-pracownika-w-ramach-umowy-o-prace
- Polish government — nonregistered activity 2026, conditions and services/ZUS: https://biznes.gov.pl/pl/firma/zakladanie-firmy/chce-wiedziec-jak-zalozyc-wlasna-firme/dzialalnosc-nierejestrowa-oraz-inne-sytuacje-w-ktorych-nie-trzeba-rejestrowac-firmy

**Final operational rule:** For the meeting you may bring *drafts* and present the product. For **paid production with real employees**, first close the legal, contractual, fiscal, security and GDPR release gates. CODEX can improve documentation, but cannot certify those gates or sign anything for the parties.
