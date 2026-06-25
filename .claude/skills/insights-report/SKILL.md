---
name: insights-report
description: >
  Use to generate Ankhora's project insights / progress report for an Epitech follow-up or
  any review — a dated, self-contained HTML file with charts (KPIs, velocity, contribution,
  board, quality, decisions & discoveries, feature-flow diagrams, what worked / didn't,
  improvement ideas), AND to derive a horizontal slideshow deck from it for live
  presentation. Both share Ankhora's branded "spatial control deck" design (dark
  visionOS/Meta-Spatial glass, anchor logo, Bricolage Grotesque display). Pulls from the
  repo docs/ADRs, git history (all branches), the PRs (CodeRabbit + Claude summaries) and
  the GitHub Project board. Triggers: "rapport d'insights", "rapport de suivi / follow-up",
  "progress report", "KPI report", "rapport pour la présentation", "génère le diaporama /
  la présentation / les slides", "slideshow / deck".
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

## 3. Design system — "spatial control deck" (brand-coherent, non-negotiable)

The report **and** the slideshow share one branded identity so they read as Ankhora, not a
generic dashboard. Hold to it:

- **Theme**: dark, atmospheric — a visionOS / Meta-Spatial *spatial control deck*. Deep
  near-black base (`#06070f`) with layered radial gradient meshes (royal-blue + cyan) for
  depth; glass cards (`backdrop-filter: blur`, hairline white strokes, subtle inner gradient).
  Never light-mode, never violet-on-white (that is exactly the generic look to avoid).
- **Logo**: the Ankhora anchor (`reports/assets/ankhora-logo.png`, royal-blue glossy 3D
  anchor — "Ankhora" ≈ *anchor*). Base64-inlined; in the hero with a blue glow
  (`drop-shadow`). It is the brand anchor of every artifact.
- **Palette** (`:root`): `--blue:#5350ff; --blue-l:#7e7bff; --cyan:#38d6ff; --teal:#2ee6b6;
  --gold:#ffce6b; --red:#ff6478` on ink `#eef1ff` / muted `#9aa3c7`. Blue→cyan gradients for
  the wordmark and accents; teal = "worked", red = "didn't".
- **Typography**: **Bricolage Grotesque** (800) for display/headings/KPI numerals — base64
  woff2 inlined via `@font-face`; system sans for body; monospace for eyebrows/labels. No
  Inter / Roboto / Space Groteske.
- **Charts**: Chart.js, dark-tuned (`Chart.defaults.color='#9aa3c7'`, faint grid
  `rgba(255,255,255,.06)`, blue/cyan datasets, gradient fills on area charts).
- **Feature-flow diagrams**: hand-authored inline `<svg>` (no lib) in the same palette —
  the report ships two showpieces to *explain the big features on D-day*: (1) the
  **record → replay loop** (Instructor → Record → Masterclass → Player → Learner, with the
  Passthrough VR⇄MR annotation), and (2) the **S1→S8 build pipeline** with an M0–M5
  milestone band. Keep/adapt these; they are the most presentable assets.

**Brand assets are inlined via three markers**, kept out of context until render:
`__LOGO_B64__` (anchor PNG, may appear several times), `__FONT_B64__` (Bricolage woff2, once),
`/*__CHARTJS__*/` (Chart.js, once). Stage the asset files once:

```bash
base64 -i reports/assets/ankhora-logo.png  -o /tmp/logo.b64     # may already exist
# Bricolage Grotesque woff2 → /tmp/font.b64 (base64), Chart.js → /tmp/chart.min.js
curl -fsSL "https://cdn.jsdelivr.net/npm/chart.js@4.4.4/dist/chart.umd.min.js" -o /tmp/chart.min.js
```

## 4. Render the report HTML

Copy the latest `reports/insights-*.html` to `reports/insights-<today>.html` — it is the
clean branded template (palette, KPI grid, chart `<canvas>`, the two flow `<svg>`, narrative
sections, and a `<script>` with the `DATA = {...}` blob). Update **only**: header dates, KPI
values, the `DATA` blob, the decisions cards, the flow-diagram labels if a feature changed,
and the worked/didn't/next-steps prose. If the latest report already has assets inlined (no
markers left), just edit the data/prose. If you start from a marker skeleton, inject assets:

```bash
python3 - <<'PY'
r="reports/insights-<today>.html"
h=open(r,encoding="utf-8").read()
for m,p,n in [("__LOGO_B64__","/tmp/logo.b64",-1),("__FONT_B64__","/tmp/font.b64",1),
              ("/*__CHARTJS__*/","/tmp/chart.min.js",1)]:
    v=open(p,encoding="utf-8").read().strip()
    h=h.replace(m,v) if n<0 else h.replace(m,v,n)
open(r,"w",encoding="utf-8").write(h)
PY
```

## 5. Render the slideshow deck (presentation mode)

On a "slideshow / diaporama / présentation / deck" request, derive
`reports/slides-<today>.html` from the **same data and brand** as the report — it is the
live-presentation form of the same source of truth (don't invent new numbers; reuse the
report's `DATA` and narrative). It is a separate self-contained file, **horizontal**:

- **Layout**: a flex `.deck` of full-viewport `.slide`s, `scroll-snap-type:x mandatory`,
  each `min-width:100vw;height:100vh`. One idea per slide, big Bricolage headings, generous
  space — slides, not a dense page.
- **Navigation**: `←/→` (+ Space / PageUp-Down) scroll slide-to-slide, `Home`/`End` jump,
  `F` toggles fullscreen; clickable progress **dots** + an `n / total` counter; a fixed
  brandmark (logo + ANKHORA) top-right and a key-hint bottom-left.
- **~9 slides** mirroring the report: ① title (logo + gradient wordmark + team/date),
  ② problem/vision statement, ③ KPI grid, ④ velocity (one burn-up chart), ⑤ the
  **record→replay flow SVG** (full-bleed), ⑥ the **S1→S8 pipeline SVG** + milestone band,
  ⑦ decisions & discoveries cards, ⑧ worked / didn't (teal / red columns), ⑨ next-steps +
  closing. The two flow SVGs are reused verbatim from the report — they are the slides that
  let you *explain the big features on D-day*.
- Reuse the **same markers** (`__LOGO_B64__`, `__FONT_B64__`, `/*__CHARTJS__*/`) and the same
  injection snippet from §4 (point `r` at the slides file). Keep only the 1–2 charts a deck
  needs; lean on big numbers + the SVGs.

The latest `reports/slides-*.html` is the template for the next run — copy it, swap data/prose.

## 6. Verify before claiming done

```bash
for f in reports/insights-<today>.html reports/slides-<today>.html; do
  echo "== $f =="
  grep -c '__LOGO_B64__\|__FONT_B64__\|/\*__CHARTJS__\*/' "$f"   # must be 0 (all injected)
  grep -c '<canvas' "$f"                                        # charts present
  python3 -c "import html.parser as p;p.HTMLParser().feed(open('$f',encoding='utf-8').read());print('parse OK')"
done
```

Then confirm each **renders** (Playwright blocks `file://`, so serve it):

```bash
( cd reports && python3 -m http.server 8765 --bind 127.0.0.1 >/tmp/httpd.log 2>&1 & echo $! >/tmp/httpd.pid )
```

Navigate Playwright to `http://127.0.0.1:8765/insights-<today>.html` (screenshot full page)
and `…/slides-<today>.html` (screenshot slide 1, then `←/→` through a chart slide and a flow
slide to confirm nav + Chart.js + SVGs). Console must be clean apart from a `favicon.ico`
404. Then `kill "$(cat /tmp/httpd.pid)"`, remove screenshots, and delete any
`.playwright-mcp/` artifacts (`find … -type f -delete` then remove the empty dir — never
`rm -rf`; the hook blocks it, and compound `find -delete … rmdir` chains also trip it, so run
them as separate commands).

## 7. Output

Report the paths `reports/insights-<today>.html` and (if generated)
`reports/slides-<today>.html`. Commit only the report/slideshow + skill/spec changes — never
the user's uncommitted Unity WIP (stage files explicitly). Follow the team flow (Conventional
Commits; the work lives on a `feat/tooling-*` branch + PR, see the spec).
