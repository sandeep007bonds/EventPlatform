#!/usr/bin/env python3
"""Catch the analyzer violations that keep reaching CI, without needing the SDK.

    ./scripts/check-csharp-style.py                 # whole repo
    ./scripts/check-csharp-style.py --staged        # only files staged for commit
    ./scripts/check-csharp-style.py path/to/File.cs # specific files

`dotnet build` remains the authority — StyleCop, Sonar and the .NET analyzers see
things a regex never will. This exists because the build is the *slow* authority:
it stops at the first failing project, so one run surfaces one project's errors,
you fix them, and the next run surfaces the next project's. This checks the whole
tree in under a second and front-loads the recurring offenders.

Every rule here has broken this build at least once, and every rule was calibrated
against the tree before being added: a rule that fires on code which currently
compiles is a wrong rule, not a finding. A checker that cries wolf gets ignored,
which is worse than no checker. Rules that cannot meet that bar are listed at the
bottom of this docstring rather than implemented badly.

Deliberately NOT checked, because it needs real parsing and a wrong guess would
rewrite correct code: SA1204/SA1201 member ordering, nullability, argument type
compatibility (CS1503 — whether a comparer suits a collection's element type is
inference, not text), and the semantic CA/S performance rules. Those stay the
compiler's job.

And the thing this script cannot tell you at all: whether the tree compiles. A
clean run means the recurring mistakes are absent, nothing more.

Every rule here has a row in docs/build-error-log.md saying what it cost us and
how it was calibrated, and so does every error we decided NOT to automate. Golden
rule 9: when the build fails, add the row, and add the rule in the same commit if
it can be detected without semantic analysis.

Exit code is 1 if anything is found, so it works as a pre-commit hook:
    git config core.hooksPath .githooks
"""
import argparse
import pathlib
import re
import subprocess
import sys

SKIP_DIRS = ('/Migrations/', '/Generated/', '/bin/', '/obj/', '/node_modules/')


# --------------------------------------------------------------- preprocessing

def blank_non_code(text):
    """Return a copy with comments and literals removed, ready for structural parsing.

    This is the foundation every structural rule depends on. Without it, ordinary
    prose corrupts the parse — "Confirmation (attaching a method — card, UPI, etc.)"
    reads as a multi-line argument list, and a comment's stray parenthesis throws
    the bracket depth off for the rest of the file.

    Comments become spaces: they must vanish entirely. String and char literals
    become underscores, NOT spaces — a string-only argument blanked to whitespace
    would be dropped as empty and silently miscount the list it belongs to.

    Length and newline positions are preserved exactly, so line and offset maths
    computed here stays valid against the original text.
    """
    out, index, length = [], 0, len(text)

    def fill(segment, char):
        return ''.join(c if c == '\n' else char for c in segment)

    while index < length:
        char = text[index]
        following = text[index + 1] if index + 1 < length else ''

        if char == '/' and following == '/':
            end = text.find('\n', index)
            end = length if end < 0 else end
            out.append(fill(text[index:end], ' '))
        elif char == '/' and following == '*':
            end = text.find('*/', index + 2)
            end = length if end < 0 else end + 2
            out.append(fill(text[index:end], ' '))
        elif char == '$' and text[index + 1:index + 2] in ('"', '@') :
            # An interpolated string, blanked whole — holes included. Scanning for the closing
            # quote naively stops at the first quote *inside* a hole, so
            # $"...{string.Join(", ", xs)}..." ends early and leaks that hole's comma into the
            # surrounding argument list, inflating every count taken from it. Brace depth is
            # what tells a hole's quote from the terminator.
            end = index + (2 if text[index + 1] == '"' else 3)
            braces = 0
            while end < length:
                here = text[end]
                if here == '{' and text[end:end + 2] != '{{':
                    braces += 1
                elif here == '}' and text[end:end + 2] != '}}':
                    braces = max(0, braces - 1)
                elif here == '"' and braces == 0 and text[end:end + 2] != '""':
                    break
                end += 2 if text[end:end + 2] in ('{{', '}}', '""') else 1
            end = min(end + 1, length)
            out.append(fill(text[index:end], '_'))
        elif char == '@' and following == '"':
            end = index + 2
            while end < length:
                if text[end] == '"' and text[end:end + 2] != '""':
                    break
                end += 2 if text[end:end + 2] == '""' else 1
            end = min(end + 1, length)
            out.append(fill(text[index:end], '_'))
        elif char in '"\'':
            end = index + 1
            while end < length and text[end] != char:
                end += 2 if text[end] == '\\' else 1
            end = min(end + 1, length)
            out.append(fill(text[index:end], '_'))
        else:
            out.append(char)
            index += 1
            continue
        index = end

    return ''.join(out)


def bracket_body(text, open_index):
    """Contents of the bracket group opening at open_index, or None if unbalanced.

    The '>' of a lambda arrow is not a closing bracket. Missing that silently
    mis-counts every argument list containing a lambda.
    """
    depth, out, previous = 0, [], ''
    for char in text[open_index:]:
        arrow = char == '>' and previous == '='
        if char in '([{<' and not (char == '<' and previous == '='):
            depth += 1
            if depth == 1 and char == '(':
                previous = char
                continue
        elif char in ')]}>' and not arrow:
            depth -= 1
            if depth == 0:
                return ''.join(out)
        out.append(char)
        previous = char
    return None


def split_top_level(raw):
    """Split on commas at nesting depth zero. Empty entries are kept, not dropped."""
    parts, depth, current = [], 0, ''
    for index, char in enumerate(raw):
        previous = raw[index - 1] if index else ''
        arrow = char == '>' and previous == '='
        if char in '([{<' and not (char == '<' and previous == '='):
            depth += 1
        elif char in ')]}>' and not arrow:
            depth -= 1
        elif char == ',' and depth == 0:
            parts.append(current)
            current = ''
            continue
        current += char
    parts.append(current)
    return parts


def argument_lines(code, match_end, body):
    """The line number each top-level argument starts on."""
    base, offset, lines = code[:match_end].count('\n'), 0, []
    for argument in [a for a in split_top_level(body) if a.strip()]:
        leading = len(argument) - len(argument.lstrip('\n\r \t'))
        lines.append(base + body[:offset + leading].count('\n'))
        offset += len(argument) + 1
    return base, lines


# ---------------------------------------------------------------------- rules

def check_sa1117(path, code, findings):
    """SA1117 — parameters all on one line, or each on its own line.

    All arguments on a single line is legal even when the opening parenthesis sits
    on the line above, so the trigger is not "the list spans lines". It is "the
    arguments occupy more than one line, but fewer lines than there are arguments".
    """
    for match in re.finditer(r'\w\s*\(', code):
        body = bracket_body(code, match.end() - 1)
        if body is None or '\n' not in body:
            continue
        count = len([a for a in split_top_level(body) if a.strip()])
        if count < 2:
            continue
        base, lines = argument_lines(code, match.end(), body)
        if 1 < len(set(lines)) < count:
            findings.append((path, base + 1, 'SA1117',
                             f'{count} arguments over {len(set(lines))} lines — '
                             f'put each on its own line, or all on one'))


def check_s125(path, raw, findings):
    """S125 — Sonar reads a comment as commented-out code.

    The trigger is a comment line that *ends* with a semicolon, which reads as a
    complete statement. Merely containing one is ordinary English punctuation and
    fires on dozens of files that compile perfectly.
    """
    for number, line in enumerate(raw.split('\n'), start=1):
        stripped = line.strip()
        if stripped.startswith('//') and not stripped.startswith('///') and stripped.endswith(';'):
            findings.append((path, number, 'S125',
                             'comment line ends with ";" — reads as commented-out code'))


def check_sa1506(path, raw, findings):
    """SA1506 — a doc-comment block separated from its member by a blank line."""
    lines = raw.split('\n')
    index = 0
    while index < len(lines):
        if not lines[index].strip().startswith('///'):
            index += 1
            continue
        start = index
        while index < len(lines) and lines[index].strip().startswith('///'):
            index += 1
        after, blanks = index, 0
        while after < len(lines) and lines[after].strip() == '':
            blanks += 1
            after += 1
        if blanks and after < len(lines) and not re.match(r'^\s*[}\])]', lines[after]):
            findings.append((path, start + 1, 'SA1506',
                             f'doc header followed by {blanks} blank line(s)'))


MEMBER_START = re.compile(
    r'^    (?:\[|(?:public|private|protected|internal|static|readonly|async|override|sealed'
    r'|virtual|const|new)\b)')

# A line that closes a *multi-line* element: a brace at member indentation, or a continuation line
# ending an initializer that was split across lines.
ENDS_MULTILINE_ELEMENT = re.compile(r'^    \}\s*$|^\s{8,}.*;\s*$')


def check_sa1516(path, raw, findings):
    """SA1516 — elements should be separated by a blank line.

    Narrow on purpose. StyleCop is happy with consecutive *single-line* fields, which is why the
    obvious reading of this rule ("any two adjacent members need a blank line") reports 72
    violations on a tree that compiles. What it actually wants is a blank line after an element
    that spanned multiple lines — which is exactly what breaks when a one-line field is expanded
    into a chained initializer and the field below it is left flush against the closing call.
    """
    lines = raw.split('\n')
    for index in range(len(lines) - 1):
        if ENDS_MULTILINE_ELEMENT.match(lines[index]) and MEMBER_START.match(lines[index + 1]):
            findings.append((path, index + 2, 'SA1516',
                             'element follows a multi-line element with no blank line between'))


def check_param_tags(path, raw, code, findings):
    """CS1573 / SA1611 / SA1612 — <param> tags must cover the signature, in order."""
    raw_lines, code_lines = raw.split('\n'), code.split('\n')
    index = 0
    while index < len(raw_lines):
        if not raw_lines[index].strip().startswith('///'):
            index += 1
            continue
        start = index
        while index < len(raw_lines) and raw_lines[index].strip().startswith('///'):
            index += 1
        documented = [d.lstrip('@') for d in
                      re.findall(r'<param\s+name="([^"]+)"', '\n'.join(raw_lines[start:index]))]
        if not documented:
            continue

        after = index
        # Blank lines *and attributes* sit between a doc block and the thing it documents. An
        # attribute is itself a call with arguments, so leaving it in makes it look like the
        # signature: `[AttributeUsage(AttributeTargets.Class, Inherited = false)]` reads as a
        # two-parameter `AttributeUsage()` whose <param> docs are all missing.
        while after < len(code_lines) and (
                code_lines[after].strip() == '' or code_lines[after].lstrip().startswith('[')):
            after += 1
        signature, cursor = [], after
        while cursor < len(code_lines) and cursor < after + 60:
            signature.append(code_lines[cursor])
            joined = ' '.join(signature)
            if joined.count('(') and joined.count('(') == joined.count(')'):
                break
            cursor += 1
        signature = ' '.join(signature)

        opener = re.search(r'\b(\w+)\s*\(', signature)
        if not opener:
            continue
        body = bracket_body(signature, opener.end() - 1)
        if body is None:
            continue

        actual = []
        for part in split_top_level(body):
            part = re.sub(r'=.*$', '', part.strip())
            part = re.sub(r'^\[[^\]]*\]\s*', '', part).strip()
            tokens = part.split()
            if tokens:
                actual.append(tokens[-1].lstrip('@_'))
        actual = [a for a in actual if re.fullmatch(r'\w+', a or '')]
        if not actual or documented == actual:
            continue

        missing = [a for a in actual if a not in documented]
        stale = [d for d in documented if d not in actual]
        if missing:
            detail = f'{opener.group(1)}(): no <param> for {missing}'
        elif stale:
            detail = f'{opener.group(1)}(): <param> for non-existent {stale}'
        else:
            detail = f'{opener.group(1)}(): <param> order {documented} != signature {actual}'
        findings.append((path, start + 1, 'CS1573/SA1611/SA1612', detail))


SHARED_SCOPE = 'building-blocks'


def compilation_scope(path):
    """Which set of projects a file can see types from.

    Two services may both declare a `GetSeatMapQuery` with different arities and both
    compile: they are separate assemblies with no reference between them. Matching
    record names across the whole tree makes the second one a false CS7036 against
    every caller of the first, which is exactly the cry-wolf failure this checker is
    supposed to avoid. Everything can see `building-blocks/`; nothing else crosses.
    """
    parts = pathlib.PurePath(path).parts
    if not parts:
        return SHARED_SCOPE
    if parts[0] == 'building-blocks':
        return SHARED_SCOPE
    if parts[0] in ('services', 'gateways') and len(parts) > 1:
        return f'{parts[0]}/{parts[1]}'
    return parts[0]


def check_record_arity(sources, findings):
    """CS7036 / CS1729 — a positional record gained a parameter, a caller did not."""
    declared = {}
    pattern = re.compile(r'\b(?:public|internal)\s+(?:sealed\s+|abstract\s+)*record\s+(\w+)\s*\(')
    for path, code in sources.items():
        for match in pattern.finditer(code):
            body = bracket_body(code, match.end() - 1)
            if body is None:
                continue
            params = [p for p in split_top_level(body) if p.strip()]
            required = sum(1 for p in params if '=' not in p)
            key = (compilation_scope(path), match.group(1))
            declared.setdefault(key, []).append((required, len(params), path))

    for path, code in sources.items():
        scope = compilation_scope(path)
        for match in re.finditer(r'\bnew\s+(\w+)\s*\(', code):
            name = match.group(1)
            candidates = declared.get((scope, name), []) + declared.get((SHARED_SCOPE, name), [])
            # More than one arity in view means the name alone does not identify the type —
            # say nothing rather than pick one and be confidently wrong.
            if len({(low, high) for low, high, _ in candidates}) != 1:
                continue
            body = bracket_body(code, match.end() - 1)
            if body is None:
                continue
            args = [a for a in split_top_level(body) if a.strip()]
            if any(re.match(r'^\s*\w+\s*:(?!:)', a) for a in args):
                continue  # named arguments — positional counting says nothing
            low, high, where = candidates[0]
            if not low <= len(args) <= high:
                findings.append((path, code[:match.start()].count('\n') + 1, 'CS7036',
                                 f'new {name}(...) passes {len(args)}, declaration takes '
                                 f'{low}..{high} (declared in {where})'))


def check_local_usings(path, code, findings):
    """Golden rule 'global usings only' — no using directives in individual files."""
    if pathlib.PurePath(path).name == 'GlobalUsings.cs':
        return
    for number, line in enumerate(code.split('\n'), start=1):
        if re.match(r'^\s*using\s+[\w.]+\s*;', line):
            findings.append((path, number, 'convention',
                             'using directive in a file — add it to GlobalUsings.cs instead'))


DOC_VOID_TAGS = ('see', 'seealso', 'paramref', 'typeparamref', 'inheritdoc', 'br')


def check_doc_xml(path, raw, findings):
    """CS1570 — a doc comment block must be well-formed XML.

    The cheap half of well-formedness only: every opening tag is closed by the
    matching name, in order. That is the mistake people actually make — editing a
    <summary> into a <remarks> and leaving the old closer behind, or closing an
    outer tag before an inner one. Attribute syntax and entity escaping are left to
    the compiler; guessing at those would fire on prose that compiles fine.
    """
    lines = raw.split('\n')
    index = 0
    while index < len(lines):
        if not lines[index].strip().startswith('///'):
            index += 1
            continue
        start = index
        block = []
        while index < len(lines) and lines[index].strip().startswith('///'):
            block.append(lines[index].strip()[3:])
            index += 1

        stack = []
        for closing, name, self_closing in re.findall(
                r'<(/?)([A-Za-z][\w.-]*)(?:\s[^<>]*?)?(/?)>', '\n'.join(block)):
            if self_closing or name.lower() in DOC_VOID_TAGS:
                continue
            if not closing:
                stack.append(name)
            elif not stack:
                findings.append((path, start + 1, 'CS1570',
                                 f'</{name}> with no matching opening tag'))
                break
            elif stack[-1] != name:
                findings.append((path, start + 1, 'CS1570',
                                 f'</{name}> closes an open <{stack[-1]}>'))
                break
            else:
                stack.pop()
        else:
            if stack:
                findings.append((path, start + 1, 'CS1570',
                                 f'unclosed <{stack[-1]}> in a doc comment'))


def check_pinned_versions(path, raw, findings):
    """Central Package Management — versions belong in Directory.Packages.props."""
    for number, line in enumerate(raw.split('\n'), start=1):
        if '<PackageReference' in line and re.search(r'\bVersion\s*=\s*"', line):
            findings.append((path, number, 'NU1008',
                             'Version= on a PackageReference — pin it in Directory.Packages.props'))


def check_record_member_clash(path, code, findings):
    """CS0102 — a positional record parameter and a member of the same name.

    `SessionPublishReadiness(string? Problem, ...)` generates a `Problem` property, so a static
    factory also called `Problem` is a second declaration the record cannot hold. Easy to write,
    because the factory reads as a different kind of thing from the property it collides with,
    and the compiler names only the type.
    """
    pattern = re.compile(r'\b(?:public|internal)\s+(?:sealed\s+|abstract\s+)*record\s+(\w+)\s*\(')
    for match in pattern.finditer(code):
        params = bracket_body(code, match.end() - 1)
        if params is None:
            continue
        names = set()
        for part in split_top_level(params):
            tokens = re.findall(r'\w+', part.split('=')[0])
            if tokens:
                names.add(tokens[-1])
        after = match.end() + len(params)
        brace = code.find('{', after)
        # A base list or another declaration between the ')' and the '{' means this brace is not
        # this record's body — say nothing rather than parse the wrong block.
        if brace == -1 or code[after:brace].strip(') \n\r\t'):
            continue
        body = bracket_body(code, brace)
        if body is None:
            continue
        member = re.compile(
            r'^[ \t]+(?:public|private|internal|protected)[\w\s<>,\[\]?.]*?\b(\w+)\s*(?:\(|=>|\{|;|=[^=>])',
            re.M)
        for declaration in member.finditer(body):
            if declaration.group(1) in names:
                line = code[:brace].count('\n') + body[:declaration.start()].count('\n') + 2
                findings.append((path, line, 'CS0102',
                                 f'{match.group(1)} declares {declaration.group(1)}, which its '
                                 f'positional parameter of that name already generates'))


DI_TRY_ADD = re.compile(r'\.TryAdd(?:Scoped|Singleton|Transient|Enumerable)\s*[(<]')
DI_EXTENSIONS_NAMESPACE = 'Microsoft.Extensions.DependencyInjection.Extensions'


def check_di_extensions_using(path, code, findings):
    """CS1061 — `TryAddScoped` without the namespace it lives in.

    `TryAdd*` sits on `ServiceCollectionDescriptorExtensions`, in a *different* namespace from
    `AddScoped`. The error reads "IServiceCollection does not contain a definition for
    TryAddScoped", which points at the type rather than at the missing using — and the project
    next door already having the line makes it look like it must be there already.
    """
    match = DI_TRY_ADD.search(code)
    if not match:
        return
    directory = pathlib.Path(path).parent
    while directory != pathlib.Path('.') and not any(directory.glob('*.csproj')):
        directory = directory.parent
    usings = directory / 'GlobalUsings.cs'
    if not usings.is_file():
        return
    if DI_EXTENSIONS_NAMESPACE not in usings.read_text(encoding='utf-8', errors='replace'):
        findings.append((path, code[:match.start()].count('\n') + 1, 'CS1061',
                         f'TryAdd* needs `global using {DI_EXTENSIONS_NAMESPACE};` in {usings}'))


def check_unused_private_field(path, code, findings):
    """S1144 — a private field nothing reads.

    Left behind when the last reader is refactored away, which is exactly when nobody is looking
    at the declaration. One textual occurrence in the file means the declaration is the only one;
    `nameof(x)` and any other mention still counts as a use, so this cannot fire on a field that
    is referred to at all.
    """
    declaration = re.compile(
        r'^[ \t]+private\s+(?:static\s+)?(?:readonly\s+)?[\w<>,\[\]?.]+\s+(\w+)\s*(?:=|;)', re.M)
    for match in declaration.finditer(code):
        name = match.group(1)
        if len(re.findall(rf'\b{re.escape(name)}\b', code)) == 1:
            findings.append((path, code[:match.start()].count('\n') + 1, 'S1144',
                             f"private field '{name}' is never read"))


def check_overload_adjacency(path, code, findings):
    """S4136 — overloads of one method separated by another member.

    One type per file (SA1402) is what makes this safe to read positionally: the members at one
    indent level all belong to one type, so "a different name in between" really is a different
    member of the same type rather than a neighbour's.
    """
    # The name is the identifier immediately before the parameter list, not the first thing that
    # looks like one: `async Task<HoldView> GetHoldAsync(` must yield GetHoldAsync, and a run that
    # can cross '(' yields `Task` on every async method in the repo. Stopping at '=' and '{' too
    # is what keeps `DefaultOptions { get; } = Create();` from reading as a method called Create.
    method = re.compile(
        r'^    (?:public|private|internal|protected)[^\n(={]*?\b(\w+)\s*(?:<[^()<>]*>)?\s*\(', re.M)
    order = [(match.group(1), match.start()) for match in method.finditer(code)]
    seen = {}
    for index, (name, start) in enumerate(order):
        if name in seen and any(other != name for other, _ in order[seen[name] + 1:index]):
            findings.append((path, code[:start].count('\n') + 1, 'S4136',
                             f"'{name}' overloads are not adjacent — another member sits between them"))
            break
        seen[name] = index


def using_sort_key(namespace):
    """StyleCop's order: System.* first (dotnet_sort_system_directives_first), then
    case-INSENSITIVE ordinal. The case-insensitive half is not a guess — eight GlobalUsings.cs
    in this repo put `NetArchTest.Rules` before `NSubstitute` and compile, which only an
    ordinal-ignore-case comparison allows.
    """
    system_first = 0 if namespace == 'System' or namespace.startswith('System.') else 1
    return (system_first, namespace.lower())


def check_using_order(path, raw, findings):
    """SA1208 / SA1210 — global usings out of order.

    Cost a whole CI run, because the natural way to add one is to put it where it reads best or
    where the diff is smallest. Aliases and `using static` sort under separate StyleCop rules,
    so a file containing either is left alone rather than guessed at.
    """
    if pathlib.PurePath(path).name != 'GlobalUsings.cs':
        return
    directives = [(number, line[len('global using '):].rstrip().rstrip(';').strip())
                  for number, line in enumerate(raw.split('\n'), start=1)
                  if line.startswith('global using ')]
    names = [name for _, name in directives]
    if any(name.startswith('static ') or '=' in name for name in names):
        return
    ordered = sorted(names, key=using_sort_key)
    if names == ordered:
        return
    first = next(i for i, (a, b) in enumerate(zip(names, ordered)) if a != b)
    findings.append((path, directives[first][0], 'SA1208/SA1210',
                     f'global usings are out of order — expected {ordered[first]} '
                     f'before {names[first]}'))


def check_factory_arity(sources, findings):
    """CS7036 on a hand-written type — a static factory or constructor gained a parameter and a
    caller did not.

    The record-arity rule next door cannot see these: `Ticket` and `Hold` are classes with
    private constructors and a public `Create`, so nothing about them is positional. Both broke
    the same way in the same change — a field was threaded through the parameter list and the
    call one line below it was missed.

    Only same-file calls are checked. That is the whole point: a factory calling its own
    constructor is where the mistake happens, and it needs no cross-assembly resolution to see.
    """
    signature = re.compile(
        r'^    (?:public|private|internal|protected)[^\n(={]*?\b(\w+)\s*(?:<[^()<>]*>)?\s*\(', re.M)
    for path, code in sources.items():
        type_name = pathlib.PurePath(path).stem

        # Every arity the name is declared with, not one. Overloads are the normal case here,
        # not the exception: an EF aggregate always has a private parameterless constructor
        # beside its real one, so treating "more than one" as unknowable would switch this rule
        # off for precisely the types it exists to check.
        arities = []
        for match in signature.finditer(code):
            if match.group(1) != type_name:
                continue
            body = bracket_body(code, code.index('(', match.end() - 1))
            if body is None:
                continue
            params = [p for p in split_top_level(body) if p.strip()]
            arities.append((sum(1 for p in params if '=' not in p), len(params)))

        if not arities:
            continue

        for match in re.finditer(r'\bnew\s+(\w+)\s*\(', code):
            if match.group(1) != type_name:
                continue
            body = bracket_body(code, match.end() - 1)
            if body is None:
                continue
            args = [a for a in split_top_level(body) if a.strip()]
            if any(re.match(r'^\s*\w+\s*:(?!:)', a) for a in args):
                continue
            # A call has to match *some* overload; matching none is the error.
            if not any(low <= len(args) <= high for low, high in arities):
                accepted = ', '.join(str(low) if low == high else f'{low}..{high}'
                                     for low, high in sorted(set(arities)))
                findings.append((path, code[:match.start()].count('\n') + 1, 'CS7036',
                                 f'new {type_name}(...) passes {len(args)}, but the constructors '
                                 f'in this file take {accepted}'))


def check_static_factory_arity(sources, findings):
    """CS7036 across files — `Hold.Create(...)` after `Hold.Create` gained a parameter.

    The sibling rule above only sees a type calling its own constructor. This one covers the
    other half of the same mistake, and it is the half that actually shipped: a field was
    threaded into `Hold.Create`'s parameter list and the one caller, in another project, was
    missed. Keyed on `Type.Method` inside one compilation scope, so two services' unrelated
    `Create`s never collide.
    """
    declaration = re.compile(
        r'^    public static\s+[\w<>,\[\]?.]+\s+(\w+)\s*(?:<[^()<>]*>)?\s*\(', re.M)
    declared = {}
    for path, code in sources.items():
        type_name = pathlib.PurePath(path).stem
        for match in declaration.finditer(code):
            body = bracket_body(code, code.index('(', match.end() - 1))
            if body is None:
                continue
            params = [p for p in split_top_level(body) if p.strip()]
            # An extension method's receiver is not passed at the call site.
            offset = 1 if params and params[0].strip().startswith('this ') else 0
            key = (compilation_scope(path), type_name, match.group(1))
            declared.setdefault(key, []).append(
                (sum(1 for p in params if '=' not in p) - offset, len(params) - offset))

    for path, code in sources.items():
        scope = compilation_scope(path)
        for match in re.finditer(r'\b([A-Z]\w*)\.(\w+)\s*\(', code):
            arities = (declared.get((scope, match.group(1), match.group(2)))
                       or declared.get((SHARED_SCOPE, match.group(1), match.group(2))))
            if not arities:
                continue
            body = bracket_body(code, match.end() - 1)
            if body is None:
                continue
            args = [a for a in split_top_level(body) if a.strip()]
            if any(re.match(r'^\s*\w+\s*:(?!:)', a) for a in args):
                continue
            if not any(low <= len(args) <= high for low, high in arities):
                accepted = ', '.join(str(low) if low == high else f'{low}..{high}'
                                     for low, high in sorted(set(arities)))
                findings.append((path, code[:match.start()].count('\n') + 1, 'CS7036',
                                 f'{match.group(1)}.{match.group(2)}(...) passes {len(args)}, '
                                 f'the declaration takes {accepted}'))


def check_sa1115(path, raw, code, findings):
    """SA1115 — a blank line between two arguments.

    A comment explaining one argument wants air around it, and a blank line above the comment is
    the natural way to give it. StyleCop reads that as the parameter not beginning on the line
    after the previous one.

    Structure comes from the blanked code — a blank line inside a verbatim string must not count
    — but *blankness* is judged on the raw line, because `blank_non_code` turns every comment
    into spaces and a documented argument would otherwise look like a gap. Line positions are
    preserved between the two, which is what makes reading them together safe.
    """
    raw_lines = raw.split('\n')
    for match in re.finditer(r'\(\s*\n', code):
        body = bracket_body(code, match.start())
        if body is None or ',' not in body:
            continue
        base = code[:match.start()].count('\n')
        lines = body.split('\n')
        for offset in range(1, len(lines) - 1):
            number = base + offset
            if number >= len(raw_lines) or raw_lines[number].strip():
                continue
            if any(l.strip() for l in lines[offset + 1:]):
                findings.append((path, number + 1, 'SA1115',
                                 'blank line between arguments — the next parameter must begin '
                                 'on the line after the previous one'))
                break


# A comment may sit flush under a line that opens a block or is mid-expression — but NOT under a
# comma. CI proved that one: a comment placed against `correlationId: ...,` to satisfy SA1115 was
# then reported as SA1515. The two rules together mean a comment cannot live inside an argument
# list at all, in either form, so it has to move above the whole statement.
CONTINUES_STATEMENT = ('{', '(', '[', ':', '&&', '||', '=>', '+')


def check_sa1515(path, raw, findings):
    """SA1515 — a single-line comment needs a blank line above it.

    Only when the line above is a finished *statement*. A comment opening a block, continuing an
    argument list, or labelling a `case` is allowed to sit flush against it, and counting those
    reported ~25 sites on a tree that compiles.
    """
    lines = raw.split('\n')
    for number in range(1, len(lines)):
        stripped = lines[number].strip()
        if not stripped.startswith('//') or stripped.startswith('///'):
            continue
        previous = lines[number - 1].strip()
        if not previous or previous.startswith('//') or previous.startswith('#'):
            continue
        if previous.endswith(CONTINUES_STATEMENT) or previous.startswith('case '):
            continue
        findings.append((path, number + 1, 'SA1515',
                         'single-line comment directly beneath code — add a blank line above it'))


# --------------------------------------------------------------------- driver

def staged_files():
    result = subprocess.run(['git', 'diff', '--cached', '--name-only', '--diff-filter=ACM'],
                            capture_output=True, text=True, check=False)
    return [f for f in result.stdout.split('\n') if f.endswith(('.cs', '.csproj'))]


def main():
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('paths', nargs='*', help='files to check (default: whole repo)')
    parser.add_argument('--staged', action='store_true', help='check only staged files')
    args = parser.parse_args()

    scoped = args.staged or bool(args.paths)
    if args.staged:
        candidates = [pathlib.Path(p) for p in staged_files()]
    elif args.paths:
        candidates = [pathlib.Path(p) for p in args.paths]
    else:
        candidates = list(pathlib.Path('.').rglob('*.cs')) + list(pathlib.Path('.').rglob('*.csproj'))

    findings, sources = [], {}
    for path in candidates:
        normalized = str(path).replace('\\', '/')
        if any(skip in normalized for skip in SKIP_DIRS) or not path.is_file():
            continue
        raw = path.read_text(encoding='utf-8', errors='replace')

        if path.suffix == '.csproj':
            check_pinned_versions(normalized, raw, findings)
            continue

        code = blank_non_code(raw)
        sources[normalized] = code
        check_sa1117(normalized, code, findings)
        check_s125(normalized, raw, findings)
        check_sa1506(normalized, raw, findings)
        check_sa1516(normalized, raw, findings)
        check_sa1515(normalized, raw, findings)
        check_sa1115(normalized, raw, code, findings)
        check_using_order(normalized, raw, findings)
        check_param_tags(normalized, raw, code, findings)
        check_doc_xml(normalized, raw, findings)
        check_local_usings(normalized, code, findings)
        check_record_member_clash(normalized, code, findings)
        check_di_extensions_using(normalized, code, findings)
        check_unused_private_field(normalized, code, findings)
        check_overload_adjacency(normalized, code, findings)

    # Arity needs every declaration in view, so it only runs on a full-tree pass.
    if not scoped:
        check_record_arity(sources, findings)

    # Same-file only, so it is correct on a partial pass too.
    check_factory_arity(sources, findings)
    if not scoped:
        check_static_factory_arity(sources, findings)

    if not findings:
        print(f'No style violations found ({len(sources)} C# files checked).')
        return 0

    for path, line, rule, detail in sorted(findings):
        print(f'{path}:{line}: {rule}: {detail}')
    print(f'\n{len(findings)} violation(s). Warnings are errors here, so these fail the build.')
    return 1


if __name__ == '__main__':
    sys.exit(main())
