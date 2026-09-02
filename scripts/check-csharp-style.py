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
rewrite correct code: SA1204/SA1201 member ordering, nullability, and the
semantic CA/S performance rules. Those stay the compiler's job.

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
        while after < len(code_lines) and code_lines[after].strip() == '':
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
            declared[match.group(1)] = (required, len(params), path)

    for path, code in sources.items():
        for match in re.finditer(r'\bnew\s+(\w+)\s*\(', code):
            name = match.group(1)
            if name not in declared:
                continue
            body = bracket_body(code, match.end() - 1)
            if body is None:
                continue
            args = [a for a in split_top_level(body) if a.strip()]
            if any(re.match(r'^\s*\w+\s*:(?!:)', a) for a in args):
                continue  # named arguments — positional counting says nothing
            low, high, where = declared[name]
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


def check_pinned_versions(path, raw, findings):
    """Central Package Management — versions belong in Directory.Packages.props."""
    for number, line in enumerate(raw.split('\n'), start=1):
        if '<PackageReference' in line and re.search(r'\bVersion\s*=\s*"', line):
            findings.append((path, number, 'NU1008',
                             'Version= on a PackageReference — pin it in Directory.Packages.props'))


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
        check_param_tags(normalized, raw, code, findings)
        check_local_usings(normalized, code, findings)

    # Arity needs every declaration in view, so it only runs on a full-tree pass.
    if not scoped:
        check_record_arity(sources, findings)

    if not findings:
        print(f'No style violations found ({len(sources)} C# files checked).')
        return 0

    for path, line, rule, detail in sorted(findings):
        print(f'{path}:{line}: {rule}: {detail}')
    print(f'\n{len(findings)} violation(s). Warnings are errors here, so these fail the build.')
    return 1


if __name__ == '__main__':
    sys.exit(main())
