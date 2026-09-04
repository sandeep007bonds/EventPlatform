#!/usr/bin/env python3
"""Fails on an endpoint registration that is missing a convention which fails *silently* at runtime.

Everything here shares one shape: forget it and nothing throws, nothing logs, and the damage shows
up somewhere else entirely — a probe 401ing, a subscriber going quiet, a correlation chain
restarting. That is precisely the class of mistake a build-time check is for.

Three sweeps:

1. Minimal-API handlers under `services/*/​*.Api/Endpoints/`. Every `Map{Get,Post,Put,Delete,Patch}`
   must carry RequireOrganizer(), RequireBuyer(), RequireAuthenticatedCaller() or AllowAnonymous() —
   chained on the call itself or on the MapGroup it belongs to.

2. Infrastructure endpoints registered outside those files — health probes, the OpenAPI document,
   the Scalar UI, the Dapr subscribe manifest. These are the ones nobody thinks of as "endpoints",
   which is exactly why the deny-by-default fallback policy would have 401'd Kubernetes probes and
   silently killed pub/sub had they not been annotated.

3. Dapr pub/sub subscribers. A subscription must go through `.SubscribesTo(topic, deadLetterTopic)`
   rather than a bare `.WithTopic(...)`. That one call applies both conventions a subscriber needs
   (ADR-0040): it adopts the incoming message's correlation id, and it names a dead-letter topic.
   Skip either and nothing breaks loudly — the message is handled perfectly and only the trail goes
   quiet, or a poison message is redelivered forever. The one legitimate bare `.WithTopic(...)` is
   the dead-letter drain itself, which subscribes via `.DrainsDeadLetters(...)`; a drain with its
   own dead-letter topic would only move the question.

Sweeps 1 and 2 exist because the platform shipped with no authorization enforcement at all and three
endpoints leaking payment and ticket credentials (ADR-0035). The fallback policy now denies
unannotated endpoints, so a missing decision fails closed — but it fails at runtime, and a probe or
a subscriber failing closed is its own outage.
"""
import re
import sys
from pathlib import Path

HANDLER_MAP = re.compile(r'\.Map(?:Get|Post|Put|Delete|Patch)\s*\(')
INFRA_MAP = re.compile(r'\.Map(?:HealthChecks|OpenApi|ScalarApiReference|SubscribeHandler)\s*\(')
AUTH = ('RequireOrganizer', 'RequireBuyer', 'RequireAuthenticatedCaller', 'AllowAnonymous')

SUBSCRIBE = re.compile(r'\.WithTopic\s*\(')
SUBSCRIBE_OK = ('SubscribesTo', 'DrainsDeadLetters')

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


def scan_subscriptions():
    """Bare .WithTopic(...) outside the two sanctioned wrappers.

    Matched on the *statement*, so a subscription is only reported when neither wrapper appears
    anywhere in its chain — the wrappers call .WithTopic themselves, one level down in a building
    block the handler globs never reach.
    """
    found = []
    for root, glob in HANDLER_GLOBS:
        for path in sorted(Path(root).glob(glob)):
            text = path.read_text(encoding='utf-8')
            for line_no, stmt in registrations(text, SUBSCRIBE):
                if not any(ok in stmt for ok in SUBSCRIBE_OK):
                    found.append(f"{path}:{line_no}: {stmt.strip()[:100]}")
    return found


auth_failures = scan(HANDLER_GLOBS, HANDLER_MAP, use_groups=True)
auth_failures += scan(INFRA_GLOBS, INFRA_MAP, use_groups=False)
subscription_failures = scan_subscriptions()

if auth_failures:
    print("Endpoints with no explicit authorization decision:\n")
    for f in auth_failures:
        print("  " + f)
    print(f"\n{len(auth_failures)} endpoint(s). Add RequireOrganizer/RequireBuyer/"
          "RequireAuthenticatedCaller/AllowAnonymous — see ADR-0035.\n")

if subscription_failures:
    print("Subscriptions bypassing the shared conventions:\n")
    for f in subscription_failures:
        print("  " + f)
    print(f"\n{len(subscription_failures)} subscription(s). Use .SubscribesTo(topic, deadLetterTopic) "
          "— or .DrainsDeadLetters(...) for a drain — see ADR-0040.\n")

if auth_failures or subscription_failures:
    sys.exit(1)

print("All endpoints carry an explicit authorization decision.")
print("All pub/sub subscriptions carry a correlation chain and a dead-letter topic.")
