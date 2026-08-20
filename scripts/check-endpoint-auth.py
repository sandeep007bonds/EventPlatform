#!/usr/bin/env python3
"""Fails if any minimal-API endpoint is registered without an explicit authorization decision.

Every `Map{Get,Post,Put,Delete,Patch}` registration must carry one of RequireOrganizer(),
RequireBuyer(), RequireAuthenticatedCaller() or AllowAnonymous() — chained on the call itself
or on the MapGroup it belongs to.

This exists because the platform shipped with no authorization enforcement at all and three
endpoints leaking payment and ticket credentials (ADR-0035). An endpoint carrying no metadata is
anonymous; silence is not a deny. Until the deny-by-default fallback policy lands, this script is
the only thing standing between a newly-added handler and the same exposure.
"""
import re
import sys
from pathlib import Path

MAP = re.compile(r'\.Map(?:Get|Post|Put|Delete|Patch)\s*\(')
AUTH = ('RequireOrganizer', 'RequireBuyer', 'RequireAuthenticatedCaller', 'AllowAnonymous')

def registrations(text):
    """Yield (line_no, statement) for each Map* call, joining continuation lines."""
    lines = text.split('\n')
    for i, line in enumerate(lines):
        if not MAP.search(line):
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

failures = []
for path in sorted(Path('services').glob('*/*.Api/Endpoints/*.cs')):
    text = path.read_text(encoding='utf-8')
    for line_no, stmt in registrations(text):
        if any(a in stmt for a in AUTH):
            continue
        receiver = stmt.strip().split('.Map')[0].strip()
        if receiver and group_is_authed(text, receiver):
            continue
        failures.append(f"{path}:{line_no}: {stmt.strip()[:100]}")

if failures:
    print("Endpoints with no explicit authorization decision:\n")
    for f in failures:
        print("  " + f)
    print(f"\n{len(failures)} endpoint(s). Add RequireOrganizer/RequireBuyer/"
          "RequireAuthenticatedCaller/AllowAnonymous — see ADR-0035.")
    sys.exit(1)

print("All endpoints carry an explicit authorization decision.")
