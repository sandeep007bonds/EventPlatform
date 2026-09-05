# Build error log

Every analyzer or compiler error that has reached a build, what caused it, and whether
`scripts/check-csharp-style.py` now catches it. Golden rule 9: **add a row here every time the
build fails**, and add a checker rule in the same commit whenever the error is mechanically
detectable.

Warnings are errors in this repo, so everything below was a hard stop.

## Why a log and not just the checker

Two reasons. Some of these cannot be detected without real semantic analysis, and a regex that
approximates them fires on correct code — a checker that cries wolf gets ignored, which is worse
than no checker. Those rows exist so the mistake is at least *remembered*. And the rows that
**are** covered record the calibration, because more than one of these rules was wrong on its first
attempt and the wrong version looked plausible.

## Read CI. It was telling us for seven commits

The single most expensive mistake in this log is not any rule below. `.github/workflows/ci.yml`
runs `dotnet build --warnings-as-errors` on every push and had been **red since the Venue commit**
— seven consecutive runs, each naming its errors with file, line and rule id — and nobody opened
one. Work continued on top for two days, and the errors were eventually reported second-hand from
a local IDE with the file paths stripped, which is how three of them ended up "unlocatable" and
guessed at.

**When there is no SDK to hand, the CI log is the compiler.** Before starting work, and before
claiming anything builds, read the last run for the branch. Everything below was in that log
already.

## A green checker is not a green build

Worth saying plainly, because it has now cost a whole build. `check-csharp-style.py` and
`check-endpoint-conventions.py` both passed on the tree that produced the seven errors in the
*performance grain and envelope* block below. They are regex tools with no type information: they
cannot see that a `StringComparer` is being handed to a `List<Guid>`, and they knew nothing about
overload adjacency or unused fields until those errors arrived. **"Both checkers are green" means
the recurring mistakes are absent, not that it compiles.** Only `dotnet build` says that, and where
there is no SDK to hand the honest report is "the checkers pass, the build is unverified".

The same block is also a reminder about *reading* a failing build. Seven real errors produced
roughly fifty-six lines of output: a failed project makes every type it exports "could not be
found" downstream, and MSBuild then type-checks callers against the stale
`obj/Debug/net10.0/ref/*.dll` from before the change, so the errors describe **the old signatures**.
`Metadata file '...dll' could not be found`, `X does not implement interface member Y` naming a
method that no longer exists, and a constructor arity that was correct two commits ago are all that
same shadow. Fix the first error in each project, rebuild, and most of the list evaporates.

## Covered by `check-csharp-style.py`

| Rule | Cause when it bit us | Detection |
|---|---|---|
| **SA1117** | `taxRatePercent: null, bookingFeePerTicketMinor: 0` shared a line while other arguments were one-per-line | Arguments spread over >1 line but fewer lines than there are arguments. All on one line is legal *even when the `(` is on the line above* — the naive "the list spans lines" reading flags correct code |
| **S125** | A comment line ending in `;` — Sonar reads it as a statement | Comment line whose content **ends** with `;`. "Contains a semicolon" is ordinary English punctuation and fired on ~25 compiling files |
| **SA1506** | A blank line between a `</remarks>` and the property it documents | Doc block followed by blank line(s) then a member |
| **SA1516** | Expanding a one-line field into a chained initializer left the next field flush against `.Build();` | Element following a **multi-line** element with no blank line. StyleCop allows consecutive single-line fields, so the naive reading reports 72 violations on a passing tree |
| **CS1573 / SA1611 / SA1612** | `amountMinor` added to `RefundAsync` without a `<param>`; `QueueStatusResponse` documented its parameters out of order | `<param>` names and order compared against the signature |
| **CS7036 / CS1729** | `EventPricing` gained `BookingFeePerTicketMinor`; three construction sites were not updated | `new X(...)` argument count against the positional record declaration, **scoped to one compilation** — see below |
| **CS1570** | `SessionScanContext`'s summary was split into a `<summary>` and a `<remarks>` when it was re-keyed to the performance, and the old `</summary>` closer was left on the end of the new block | Doc-comment tags matched as a stack: a closer that names a different tag than the one still open, a closer with nothing open, or a block that ends with a tag unclosed |
| **CS0102** | `SessionPublishReadiness` gained a positional `Problem` parameter while keeping a static factory called `Problem` | A positional record's parameter names against the member declarations in its own body |
| **CS1061** | `MessagingExtensions` called `TryAddScoped`, which lives in `…DependencyInjection.**Extensions**` — a different namespace from `AddScoped`, and absent from that project's `GlobalUsings.cs` | `TryAdd{Scoped,Singleton,Transient,Enumerable}` used in a project whose `GlobalUsings.cs` lacks that namespace |
| **S1144** | Rewriting `OutboxRelay.WithEnvelope` to work on `JsonNode` removed the last reader of its `SerializerOptions` field | A `private` field whose identifier occurs exactly once in its file |
| **S4136** | `SeatMapMapping` grew a fourth `ToResponse` overload with `ToSummary` sitting between them | Two declarations of one method name with a differently-named member in between |
| **SA1515** | A comment explaining the per-buyer cap was placed flush against the statement above it | A `//` line whose predecessor is a finished statement — see below |
| **SA1208 / SA1210** | `Catalog.Application/GlobalUsings.cs` grew `System.Globalization` and `System.Reflection` at the bottom, where the diff was smallest | `GlobalUsings.cs` compared against its sorted order — see below for what "sorted" means |
| **CS7036** (own constructor) | `Ticket.Create` threaded `eventSessionId` into the constructor's parameter list and the `new Ticket(...)` eight lines below it was not updated | `new <TypeName>(...)` against every constructor arity declared in that same file |
| **CS7036** (static factory) | `Hold.Create` gained `catalogEventId`; its one caller, in another project, still passed six | `Type.Method(...)` against a `public static` declaration of that name in the same compilation scope |
| **SA1115** | A comment explaining `causationId` was given air with a blank line above it, inside the argument list | A blank line between two arguments |
| **SA1515 (again)** | Removing that blank line to satisfy SA1115 produced SA1515 on the very next run — a comment flush under `correlationId: …,`. **The two rules together forbid a comment inside an argument list in either form**; the only fix is to move it above the whole statement. The first version of the SA1515 rule treated a trailing comma as "mid-expression, flush is fine", which is exactly backwards | A `//` line whose predecessor ends in `,` is now a violation like any other statement |
| **CS0246 / CS0103** | `Queue.Infrastructure` and `Communication.Infrastructure` gained `IDeadLetterDbContext` with no `ProjectReference` to `EventPlatform.Messaging` and no global using — Communication had deliberately never referenced it, since it publishes nothing. `CultureInfo` went the same way in `Ticketing.Infrastructure` | A curated map of names to the namespace each needs, against that project's `GlobalUsings.cs` |
| **SA1512** | The dead-letter handler was inserted between the poll-budget comment and the fields it describes, leaving the comment above a blank line explaining nothing | A `//` line followed by a blank line and then real code |
| **NU1008** | — | `Version=` on a `PackageReference` instead of `Directory.Packages.props` |
| **Global usings** | — | A `using` directive outside `GlobalUsings.cs` |

Two parser details these rules depend on, both learned by getting them wrong: **comments and string
literals must be blanked before any structural parsing** (prose like `(attaching a method — card,
UPI, etc.)` otherwise parses as an argument list), and **the `>` of a lambda arrow is not a closing
bracket** (it silently miscounts every argument list containing a lambda).

A third, learned when the Venue service landed: **a type name only identifies a type within one
compilation**. The arity rule keyed declarations by bare name across the whole tree, so the moment
a second service declared its own `GetSeatMapQuery`/`SeatResponse`/`SeatMapResponse`, three
correct call sites in Catalog were reported as CS7036 against Venue's unrelated records. Two
services are separate assemblies with no reference between them and both compile fine. The rule now
scopes lookups to `services/<name>` or `gateways/<name>` plus `building-blocks/` (which everything
references), and stays silent when a name resolves to more than one arity in view rather than
picking one. Verified both ways: zero findings on the passing tree, and still catches a genuine
three-argument call to a six-parameter record in the same project.

A fourth, learned when `EventVersionAttribute` landed: **an attribute sits between a doc block and
the thing it documents, and an attribute is itself a call with arguments**. The `<param>` rule
skipped blank lines when looking for the signature after a doc block but not attribute lines, so
`[AttributeUsage(AttributeTargets.Class, Inherited = false)]` was read as a two-parameter
`AttributeUsage()` whose `<param>` docs were all missing — on a file that compiles. The rule now
skips leading `[` lines too. Nothing in the tree had a documented member behind an attribute before,
which is why it took this long to surface; verified zero findings across 856 files afterwards, and
still catching a genuinely missing `<param>` on a method that carries an `[Obsolete]`.

A fifth, sixth and seventh, all learned in the same block, all about *where a name ends*. The
S4136 rule first read `async Task<HoldView> GetHoldAsync(` as a method called **`Task`**, because a
non-greedy run that may cross `(` stops at the first identifier rather than the last — seven async
methods reported across a passing tree. Excluding `(` from that run fixed it and immediately
introduced the opposite error: `DefaultOptions { get; } = Create();` read as a method called
`Create`, because a *field initializer* also has an identifier before a parenthesis. Excluding `=`
and `{` as well leaves only real parameter lists. Verified zero across 867 files and still catching
the genuine four-overload split in `SeatMapMapping`.

SA1515 moves **out** of the not-detectable list below, where it had been sitting on the assumption
that "comment beneath code" could not be told from legitimate flush comments. It can, and the
distinction is narrow: the line above must be a *finished statement*. A comment opening a block,
continuing an argument list, or labelling a `case` is allowed to sit flush — the naive reading
reported ~25 sites on a compiling tree, and excluding predecessors ending in `{ ( [ , : && || => +`
plus `case` labels takes that to zero while still catching the real one in `HoldService`.

**What "sorted" means for global usings**, since getting it wrong is what made the first attempt at
this rule report nine files: `System.*` first (`dotnet_sort_system_directives_first = true` in
`.editorconfig`), then **case-insensitive** ordinal. The case-insensitivity is not a guess — eight
`GlobalUsings.cs` in this tree put `NetArchTest.Rules` before `NSubstitute` and compile, which a
plain ordinal comparison forbids (`S` < `e`). Aliases and `using static` sort under separate
StyleCop rules, so a file containing either is skipped rather than guessed at.

**The two arity rules exist because one type name is not one arity.** The first attempt keyed each
name to a single declaration and treated a second as "unknowable", which switched the rule off for
every EF aggregate — they all carry a private parameterless constructor beside the real one. A call
has to match *some* overload, so the rule now collects every arity and reports only a call matching
none.

Finding these also exposed a real bug in `blank_non_code` that had been there from the start:
**an interpolated string was scanned as an ordinary one**, so `$"…{string.Join(", ", xs)}…"`
terminated at the quote *inside* the hole and leaked that hole's comma into the enclosing argument
list. It inflated the count for one legitimate call to `SessionPublishReadiness.Blocked` — a
one-argument call read as two. Interpolated strings are now blanked whole, holes included, with
brace depth telling a hole's quote from the terminator. Every structural rule depends on that
function, so this was quietly wrong for all of them.

**The namespace and SA1512 rules were both wrong on their first run — 42 findings on a tree that
mostly compiled — and what they got wrong is worth keeping.** The map had `IEventPublisher` under
`EventPlatform.Messaging` when it lives in `EventPlatform.Contracts`, which alone accounted for
eighteen passing files: *check where a type actually lives before asserting it*. The rule also
demanded a direct `ProjectReference`, which is unsound — references are transitive, so every `.Api`
reaches `EventPlatform.Messaging` through its `.Infrastructure` without naming it. And SA1512 does
not fire when the blank line is followed by **another comment or a doc block**; five such sites
compile here, so only real code following the gap counts.

That work also exposed the cost of the interpolated-string fix above: blanking a hole's contents
hides real code, and `CultureInfo` was invisible because it sits inside
`$"…{n.ToString(CultureInfo.InvariantCulture)}"`. Structural rules still want the strict pass;
"is this name used anywhere" wants `blank_comments_only`, which strips comments and keeps
everything else.

The CS1570 rule checks **only tag balance**, and that limit is the calibration. Attribute syntax and
entity escaping are the compiler's job: `<`/`&` appear in doc prose that compiles, and a rule that
guessed at them would fire on correct code. Tags that never close (`<see>`, `<paramref>`,
`<inheritdoc>`, `<br/>`, and anything self-closing) are skipped rather than pushed. Verified on the
passing tree (zero findings across 850 files) and against three constructed failures — the closer
naming the wrong tag, a block ending with `<summary>` still open, and crossed nesting
(`<b>…<i>…</b></i>`) — all three caught, and a file exercising `<para>`, `<list>`, `<see langword>`
and `<br/>` stayed silent.

## Covered by `check-endpoint-conventions.py`

Not build errors at all — conventions whose absence compiles perfectly and fails *silently* at
runtime, which is why they need a check rather than a rule review.

| Convention | What breaks without it | Detection |
|---|---|---|
| **Explicit auth decision** on every endpoint | The deny-by-default fallback 401s it — including health probes and Dapr subscribers, where failing closed is its own outage (ADR-0035) | `Map{Get,Post,…}` without `RequireOrganizer`/`RequireBuyer`/`RequireAuthenticatedCaller`/`AllowAnonymous`, on the call or its `MapGroup` |
| **`.SubscribesTo(...)`** rather than a bare `.WithTopic(...)` | Nothing, twice over. Without the envelope the message is handled correctly and only the correlation chain restarts; without a dead-letter topic a poison message is redelivered forever. Neither throws, neither shows up in a test (ADR-0040) | A `.WithTopic(` statement mentioning neither `SubscribesTo` nor `DrainsDeadLetters` |

The second rule was calibrated the same way as the rest: zero findings across the eleven
subscriptions and the five drains, and verified to catch a real omission by rewriting Queue's
subscriber to call `.WithTopic(...)` directly and watching it fail.

## Not detectable without semantic analysis

Left to `dotnet build` deliberately. A regex here would rewrite correct code.

| Rule | Cause when it bit us |
|---|---|
| **SA1204** | A `static` helper after instance methods in `AuditFieldsInterceptor`; later `OutboxRelay.WithEnvelope`/`TenantOf` after `RelayPendingAsync`. The rule orders static before instance **within one access level**, which is why dozens of files put a `private static` helper below `public` methods and compile — a naive "static after any instance member" reading reports ~70 of them. Telling those apart needs the member's own modifiers *and* its neighbours', which is parsing, not matching |
| **S3453** | Reported on `Ticket` — "make at least one constructor public" — while both its constructors were private and it had a perfectly good `public static Create`. It was a **symptom, not a cause**: the `new Ticket(...)` inside `Create` did not resolve (CS7036 above), so Sonar saw no constructor invocation anywhere and concluded the type was uninstantiable. Fixing the arity cleared it. Do not "fix" this one by widening a constructor — look for a broken `new` first |
| **SA1201** | Field declared after a constructor in `OrderingEndpoints` |
| **CS1503 / CS1929** | `OrderPricingCalculator.AppliesTo` still passed `StringComparer.OrdinalIgnoreCase` to `Contains` after `PromoCodeTerms.TicketTypeIds` was re-keyed from tier *names* to `Guid`. Deciding whether a comparer matches a collection's element type is exactly the type inference a regex does not have; a rule that flagged every `StringComparer` would fire on the twenty-odd legitimate ones in this repo. The general lesson is cheaper than a rule: **when a field changes type, grep for its name and read every call site**, because the ones that still compile are the dangerous half |
| **S6667** | `logger.LogInformation` inside a `catch` without passing the caught exception |
| **CA1859** | A private helper taking `IReadOnlyDictionary` where every caller passes a `Dictionary` |
| **S1450 / CA1001** | A field only ever used in one method; a disposable field on a non-disposable type |
| **NU1902** | A package version with a known advisory — needs a restore against the advisory database |
| **NU1510** | An explicit `PackageReference` to a framework-provided package on .NET 10 |

## Frontend

| Check | Cause when it bit us | Covered |
|---|---|---|
| `format:check` | Unformatted TSX committed; later, `*emphasis*` in `frontend/CLAUDE.md` where Prettier wants `_emphasis_` | Yes — `.githooks/pre-commit` runs Prettier on every staged file type it owns, not just `.ts`/`.tsx` |
| `tsc` unused import | Imports added ahead of the code that would use them | Yes — `npm run typecheck` |

## How to add a rule

1. Reproduce: run the candidate detection across the whole tree. **If it reports anything on code
   that currently compiles, the rule is wrong** — not the code.
2. Prove it catches the real thing: run it against the pre-fix file out of git history
   (`git show <commit>~1:<path>`) and confirm the line numbers match what the build reported.
3. Only then add it to `scripts/check-csharp-style.py`, and add a row above.
