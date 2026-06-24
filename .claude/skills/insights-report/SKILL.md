---
name: insights-report
description: >
  Use to generate Ankhora's project insights / progress report for an Epitech follow-up or
  any review — a dated, self-contained HTML file with charts (KPIs, velocity, contribution,
  board, quality, decisions & discoveries, what worked / didn't, improvement ideas). Pulls
  from the repo docs/ADRs, git history (all branches), the PRs (CodeRabbit + Claude
  summaries) and the GitHub Project board. Triggers: "rapport d'insights", "rapport de
  suivi / follow-up", "progress report", "KPI report", "rapport pour la présentation".
---

# Ankhora insights report

Produces `reports/insights-YYYY-MM-DD.html` — a **single self-contained file** (Chart.js
vendored inline, data inlined) that opens offline and renders all charts. It is a
**source of truth** to prove and analyse the team's work. Spec:
[`docs/05-operations/insights-report.md`](../../../docs/05-operations/insights-report.md).

Each run = a fresh dated report. Use the **most recent** `reports/insights-*.html` as the
visual template; only the data blob and the narrative change.

## 0. Preconditions

- Run from the repo root.
- `gh` needs the `read:project` scope for the board. If `gh project view 3 --owner
  LenySauzet` errors on scopes, tell the user to run (in a real terminal, not the `!`
  prefix): `gh auth refresh -s read:project --hostname github.com`.

## 1. Gather data

```bash
# PRs (work unit) — counts, dates, size, authors
gh pr list --repo LenySauzet/Ankhora --state all --limit 200 \
  --json number,title,author,state,createdAt,mergedAt,additions,deletions > /tmp/prs.json
jq -r 'group_by(.state)[]|"\(.[0].state): \(length)"' /tmp/prs.json
jq -r '"add:\([.[].additions]|add) del:\([.[].deletions]|add)"' /tmp/prs.json
jq -r '[.[]|select(.mergedAt!=null)|((.mergedAt|fromdate)-(.createdAt|fromdate))/3600]|add/length' /tmp/prs.json  # avg time-to-merge (h)
jq -r '.[]|select(.mergedAt!=null)|.mergedAt[0:10]' /tmp/prs.json | sort | uniq -c   # PRs merged / day
jq -r '.[].author.login' /tmp/prs.json | sort | uniq -c | sort -rn                   # PR authors

# Git — contribution across ALL branches (honest + broadened), activity, active days
git fetch origin --quiet
git log --all --pretty='%an' | sort | uniq -c | sort -rn
git log --all --pretty='%ad' --date=short | sort | uniq -c        # commits / day
git log --all --pretty='%ad' --date=short | sort -u | wc -l        # active days

# Quality — CodeRabbit actionable findings per PR
for n in $(jq -r '.[].number' /tmp/prs.json); do
  c=$(gh pr view "$n" --repo LenySauzet/Ankhora --json reviews \
        --jq '[.reviews[]|select(.author.login=="coderabbitai")|.body]|join("\n")' 2>/dev/null \
        | grep -oE 'Actionable comments posted: [0-9]+' | grep -oE '[0-9]+' | head -1)
  echo "#$n: ${c:-0}"
done

# Board (Projects v2) — id is stable: PVT_kwHOBk5vnc4BZNux ; status options:
# Backlog, Spec / Research, Ready, In progress, In review, Done
gh api graphql -f id="PVT_kwHOBk5vnc4BZNux" -f query='
query($id:ID!){node(id:$id){... on ProjectV2{items(first:100){totalCount nodes{
  isArchived content{__typename ... on Issue{number title state} ... on PullRequest{number title state} ... on DraftIssue{title}}
  status:fieldValueByName(name:"Status"){... on ProjectV2ItemFieldSingleSelectValue{name}}}}}}}'

# Decisions & discoveries — for the narrative
ls docs/02-architecture/adr/*.md | grep -v 0000-adr-template      # ADRs
find docs/superpowers -name '*.md'                                 # spikes / plans
ls docs/01-product/ docs/07-milestones.md                          # scope / milestones / roadmap
```

> **Data-source reality (don't fight it):** the board is sparse and GitHub does **not**
> expose **archived** Projects v2 items via the API — so **merged PRs are the primary work
> unit**, the board is a light supplement. State this in the report's provenance footer.

## 2. Compute KPIs & synthesise

From the gathered numbers, fill the KPI cards and the four data series (commits/day,
PRs/day, contribution, board status, CodeRabbit/PR). Then **reason over the artifacts** to
write the qualitative sections: executive summary, decisions & discoveries (read the ADRs
and spike notes), what worked / what didn't, improvement ideas. Be honest and evidence-led;
for contribution, broaden to all branches and add the bootstrap-phase context note (the
team iterates device-side on Windows/Quest Link; not yet merged).

## 3. Render the HTML

Copy the latest `reports/insights-*.html` to `reports/insights-<today>.html`. It is a clean
template: a `:root` CSS palette, KPI grid, chart `<canvas>` elements, narrative sections,
and a `<script>` with a `DATA = {...}` blob feeding Chart.js via a small `mk(id,cfg)` helper.
Update **only**: the header dates, the KPI card values, the `DATA` blob, the decisions table,
and the worked/didn't/improvement prose.

Chart.js is vendored inline. The template ships with it already injected. If you start from a
fresh skeleton instead, leave the marker `<script>/*__CHARTJS__*/</script>` in `<head>` and
inject the library (keeps the 205 KB out of context):

```bash
curl -fsSL "https://cdn.jsdelivr.net/npm/chart.js@4.4.4/dist/chart.umd.min.js" -o /tmp/chart.min.js
python3 - <<'PY'
r="reports/insights-<today>.html"
h=open(r,encoding="utf-8").read().replace("/*__CHARTJS__*/", open("/tmp/chart.min.js",encoding="utf-8").read(), 1)
open(r,"w",encoding="utf-8").write(h)
PY
```

## 4. Verify before claiming done

```bash
f=reports/insights-<today>.html
grep -c '/\*__CHARTJS__\*/' "$f"   # must be 0 (placeholder injected)
grep -c '<canvas' "$f"             # number of charts
grep -oE 'Chart\.js v[0-9.]+' "$f" | head -1
python3 -c "import html.parser as p;p.HTMLParser().feed(open('$f',encoding='utf-8').read());print('parse OK')"
```

Then confirm it **renders** (Playwright blocks `file://`, so serve it):

```bash
( cd reports && python3 -m http.server 8765 --bind 127.0.0.1 >/tmp/httpd.log 2>&1 & echo $! >/tmp/httpd.pid )
```

Navigate Playwright to `http://127.0.0.1:8765/insights-<today>.html`, screenshot full page,
check console (a `favicon.ico` 404 is the only acceptable message — no JS errors), then
`kill "$(cat /tmp/httpd.pid)"` and delete any `.playwright-mcp/` artifacts (do **not** commit
them; never use `rm -rf` — the hook blocks it).

## 5. Output

Report the path `reports/insights-<today>.html`. Commit only the report (and skill/spec
changes) — never the user's uncommitted Unity WIP. Follow the team flow (Conventional
Commits; the work lives on a `feat/tooling-*` branch + PR, see the spec).
