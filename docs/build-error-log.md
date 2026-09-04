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
| **`.WithIntegrationEnvelope()`** on every `.WithTopic(...)` | Nothing. The message is handled correctly — it just starts a fresh correlation chain, so the trail from the buyer's click ends there and no test notices (ADR-0040) | A `.WithTopic(` statement whose chain does not mention `WithIntegrationEnvelope` |

The second rule was calibrated the same way as the rest: zero findings across the eleven
subscriptions once they were all annotated, and verified to catch a real omission by removing the
call from Queue's subscriber and watching it fail.

## Not detectable without semantic analysis

Left to `dotnet build` deliberately. A regex here would rewrite correct code.

| Rule | Cause when it bit us |
|---|---|
| **SA1204** | A `static` helper placed after instance methods in `AuditFieldsInterceptor` |
| **SA1201** | Field declared after a constructor in `OrderingEndpoints` |
| **SA1515** | A comment inserted directly beneath code with no blank line above it |
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
