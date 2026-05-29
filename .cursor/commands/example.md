# Cursor Command Template (`/example`)

> **Purpose**: Reference for creating other commands in `.cursor/commands/`.
> The filename (`example.md`) becomes the slash command: `/example`.
> When the user invokes `/example`, briefly explain the structure below
> or help them draft a new command from this template — do not run a fictional business workflow.
>
> **Language**: Write all project commands and agent output in **English**.

---

## What a Cursor command is

| Aspect | Detail |
|--------|--------|
| **Project location** | `.cursor/commands/*.md` (versioned, shared with the team) |
| **Global location** | `~/.cursor/commands/*.md` (personal, outside the repo) |
| **Invocation** | Type `/` in chat or Composer, then the name (e.g. `/example`) |
| **Name** | Derived from the file: `my-command.md` → `/my-command` (kebab-case recommended) |
| **User arguments** | Any text **after** the command name is passed as extra context |
| **Format** | Markdown **without YAML frontmatter** (unlike *rules* in `.cursor/rules/`) |

**Do not confuse**:

- **Rules** (`.cursor/rules/*.mdc`) — persistent context, `alwaysApply`, `globs`, `@rule`
- **Commands** (this folder) — user-triggered actions via `/`
- **Skills** (`.cursor/skills/` or plugins) — reusable know-how, separate mechanism
- **Agents** (`.cursor/agents/` if present) — specialized personas

---

## Recommended command structure

Copy the sections you need; delete the rest. Keep each file **short and actionable** (< ~200 lines ideally).

### 1. Title and summary (required)

```markdown
# Clear command title

One sentence: what the command does and the expected outcome.
```

### 2. Project context (optional)

Point to repo docs and conventions:

- Read `@AGENTS.md` and `@README.md` before acting.
- Follow active rules (`@.cursor/rules/general.mdc`).

### 3. Required inputs (when the command needs information)

List what to ask **before** acting, with defaults:

```markdown
## Required inputs

1. **Name**: …
2. **Description**: …
3. **Options**: … (default: …)
```

Useful patterns:

- "If the user did not provide X in their message after `/command`, ask for X."
- "Do not re-ask for what is already in the message or @mentioned files."
- "Use today's date if not specified."

### 4. Step-by-step instructions (core of the command)

```markdown
## Instructions

When the user invokes `/my-command`, do the following in order:

1. …
2. …
3. …
```

Best practices:

- Imperative verbs, numbered steps, unambiguous wording
- Explicit branches: "If … then … else …"
- Scope limits: "Only modify files under …"
- Prohibitions: "Do not commit unless explicitly asked"

### 5. Slash arguments (text after the command)

Document how to handle the user suffix:

```markdown
## Arguments

- `/my-command` — default behavior (context = conversation + open files)
- `/my-command fix-login` — target the "fix-login" task
- `/my-command @src/foo.ts` — prioritize this file
```

### 6. `@` references (files and folders)

The command body can include mentions to anchor context:

```markdown
Follow patterns in `@src/ExampleComponent.tsx`.
Check config in `@package.json` or `@ProjectSettings/`.
```

### 7. Output templates (generated files or text)

```markdown
## Template

Create the file `path/{placeholder}.ext`:

\`\`\`lang
// content with {placeholders}
\`\`\`
```

Replacement rules: describe how to derive `{slug}`, `{date}`, etc.

### 8. Key rules / constraints

```markdown
## Key rules

- …
- …
```

Examples: date formats, naming conventions, "no frontmatter", "no secrets in the diff".

### 9. Expected output format

```markdown
## Expected output

Respond with:
1. List of files created/modified
2. Commands run (if applicable)
3. Next steps for the user
```

### 10. Full example (strongly recommended)

An end-to-end scenario avoids divergent interpretations:

```markdown
## Example

**User input**: `/my-command My Title`

**Result**:
- File: `…`
- Content: …
```

### 11. Verification before finishing

```markdown
## Verification

- [ ] …
- [ ] Run `…` and confirm it passes
```

### 12. Chaining (optional)

```markdown
## Chaining

After this command, suggest `/other-command` if …
```

### 13. Tools and permissions (optional)

Specify when the agent should use a particular tool:

```markdown
- Use `gh` for GitHub PRs/issues
- Ask for confirmation before `git push`
- Do not start a dev server unless requested
```

---

## Mini-example: generic "create an artifact" command

Below is a **reusable excerpt** (not meant to be executed as-is for `/example`):

```markdown
# Create an artifact

Create a new file from user inputs.

## Required inputs

1. **Title**
2. **Short description**
3. **Tags** (comma-separated → lowercase array)
4. **Date** (YYYY-MM-DD, default: today)

## Instructions

1. Collect any missing inputs.
2. Generate a slug: lowercase, spaces → `-`, strip special characters.
3. Create `artifacts/{slug}.md` using the template below.

## Template

\`\`\`markdown
# {title}

> {description}

- Date: {date}
- Tags: {tags}
\`\`\`

## Key rules

- No `slug` field in the content — it comes from the filename.
- Date format: `YYYY-MM-DD`.

## Example

Title "My Feature", description "…", tags `unity, editor`, date `2026-05-29`
→ file `artifacts/my-feature.md`
```

---

## Checklist before adding a new command

- [ ] Filename in kebab-case with a clear verb or goal (`review-pr.md`, not `cmd1.md`)
- [ ] No YAML frontmatter (reserved for rules)
- [ ] One-sentence goal at the top of the file
- [ ] Numbered, testable steps
- [ ] At least one input → output example
- [ ] Project constraints (`@AGENTS.md`, rules) referenced when relevant
- [ ] No massive duplication of a rule — the command *does*, the rule *guides*
- [ ] Written in **English**

---

## Behavior for `/example`

If the user invokes `/example` **with no extra detail**:

1. Summarize the useful sections above in 5–8 bullets.
2. Offer to help draft a new command: ask for **name**, **goal**, **inputs**, and **expected outcome**.
3. Generate a draft `.cursor/commands/<name>.md` only if they explicitly ask.

If the user adds a topic after `/example` (e.g. `/example command for PR review`), draft that command directly using this template.
