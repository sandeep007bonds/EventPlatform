#!/usr/bin/env python3
"""Fails if any endpoint is registered without an explicit authorization decision.

Two sweeps:

1. Minimal-API handlers under `services/*/​*.Api/Endpoints/`. Every `Map{Get,Post,Put,Delete,Patch}`
   must carry RequireOrganizer(), RequireBuyer(), RequireAuthenticatedCaller() or AllowAnonymous() —
   chained on the call itself or on the MapGroup it belongs to.

2. Infrastructure endpoints registered outside those files — health probes, the OpenAPI document,
   the Scalar UI, the Dapr subscribe manifest. These are the ones nobody thinks of as "endpoints",
   which is exactly why the deny-by-default fallback policy would have 401'd Kubernetes probes and
   silently killed pub/sub had they not been annotated.

This exists because the platform shipped with no authorization enforcement at all and three
endpoints leaking payment and ticket credentials (ADR-0035). The fallback policy now denies
unannotated endpoints, so a missing decision fails closed — but it fails at runtime, and a probe or
a subscriber failing closed is its own outage. This check moves that discovery to build time.
"""
import re
import sys
from pathlib import Path

HANDLER_MAP = re.compile(r'\.Map(?:Get|Post|Put|Delete|Patch)\s*\(')
INFRA_MAP = re.compile(r'\.Map(?:HealthChecks|OpenApi|ScalarApiReference|SubscribeHandler)\s*\(')
AUTH = ('RequireOrganizer', 'RequireBuyer', 'RequireAuthenticatedCaller', 'AllowAnonymous')

HANDLER_GLOBS = [('services', '*/*.Api/Endpoints/*.cs')]
INFRA_GLOBS = [
    ('services', '*/*.Api/Program.cs'),
    ('building-blocks/EventPlatform.Hosting', '*.cs'),
]


def strip_comment(line):
    """Drop a `//` line comment, leaving `//` inside a string literal alone.

    Comments matter twice here: one describing a call must not be mistaken for the call
    (Identity's Program.cs explains that it has *no* MapSubscribeHandler), and one reading
    "AllowAnonymous deliberately" must not be mistaken for the decision itself.
    """
    in_string = False
    i = 0
    while i < len(line):
        c = line[i]
        if c == '\\' and in_string:
            i += 2
            continue
        if c == '"':
            in_string = not in_string
        elif c == '/' and not in_string and line[i + 1:i + 2] == '/':
            return line[:i]
        i += 1
    return line


def registrations(text, pattern):
    """Yield (line_no, statement) for each match, joining continuation lines up to the `;`."""
    lines = [strip_comment(line) for line in text.split('\n')]
    for i, line in enumerate(lines):
        if not pattern.search(line):
            continue
        stmt, j = line, i
        while ';' not in stmt and j + 1 < len(lines):
            j += 1
            stmt += ' ' + lines[j].strip()
        yield i + 1, stmt


def group_is_authed(text, var):
    """True when the MapGroup assigned to `var` carries an auth call at declaration."""
    m = re.search(rf'var\s+{re.escape(var)}\s*=\s*[^;]*?MapGroup[^;]*;', text, re.S)
    return bool(m) and any(a in m.group(0) for a in AUTH)


def scan(globs, pattern, use_groups):
    found = []
    for root, glob in globs:
        for path in sorted(Path(root).glob(glob)):
            text = path.read_text(encoding='utf-8')
            for line_no, stmt in registrations(text, pattern):
                if any(a in stmt for a in AUTH):
                    continue
                if use_groups:
                    receiver = stmt.strip().split('.Map')[0].strip()
                    if receiver and group_is_authed(text, receiver):
                        continue
                found.append(f"{path}:{line_no}: {stmt.strip()[:100]}")
    return found


failures = scan(HANDLER_GLOBS, HANDLER_MAP, use_groups=True)
failures += scan(INFRA_GLOBS, INFRA_MAP, use_groups=False)

if failures:
    print("Endpoints with no explicit authorization decision:\n")
    for f in failures:
        print("  " + f)
    print(f"\n{len(failures)} endpoint(s). Add RequireOrganizer/RequireBuyer/"
          "RequireAuthenticatedCaller/AllowAnonymous — see ADR-0035.")
    sys.exit(1)

print("All endpoints carry an explicit authorization decision.")
