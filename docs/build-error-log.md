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
