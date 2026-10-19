# feverTickets

WT - watch tickets. A small C# script, run by GitHub Actions, triggered every 10 minutes by [cron-job.org](https://cron-job.org). It checks the
[Fever Candlelight event page](https://feverup.com/m/436641/en) for Zone A / Zone B tickets on the
watched dates and sends a phone notification through [ntfy](https://ntfy.sh).

| What happens | ntfy priority |
| --- | --- |
| Zone A available on a watched date | 5 – urgent |
| Only Zone B available | 3 – default |
| The check failed (page changed, build error, network…) | 4 – high, tap to open the run logs |
| Daily heartbeat (07:00 Israel time) with the status of every watched zone | 2 – low |

## How it works

The event page has all its ticket selector data embedded as JSON in
`<script id="serverapp-state">`. The script downloads the page, reads that JSON
(dates → times → zones), and checks `has_available_tickets` for each watched session — the same
value that makes the selector show "Sold out" or "Get it!". No browser needed.

## Setup

1. Subscribe to your ntfy topic in the ntfy app (or at ntfy.sh in a browser with notifications allowed).
2. In the GitHub repo: **Settings → Secrets and variables → Actions → Secrets** → add `NTFY_TOPIC`.
3. Make the repo **public** — runs every 10 minutes would use up a private repo's free minutes.
4. In the **Actions** tab, enable workflows if asked, then open **Check tickets → Run workflow** to test.
5. Set up the two cron-job.org jobs below.

## Scheduling

GitHub's own `schedule:` trigger is best-effort and skipped runs for hours, so the workflow has no schedule.
[cron-job.org](https://cron-job.org) (free) calls the GitHub API instead — the same thing the "Run workflow" button does.

Both jobs: **POST** to
`https://api.github.com/repos/shira-vak/feverTickets/actions/workflows/check-tickets.yml/dispatches`
with headers

```
Authorization: Bearer <fine-grained token with Actions: Read and write on this repo>
Accept: application/vnd.github+json
X-GitHub-Api-Version: 2022-11-28
Content-Type: application/json
```

| Job | Schedule | Body |
| --- | --- | --- |
| Ticket check | every 10 minutes | `{"ref":"main"}` |
| Heartbeat | daily 07:00, time zone Asia/Jerusalem | `{"ref":"main","inputs":{"heartbeat":"true"}}` |

GitHub answers a successful trigger with **204 No Content**. To stop watching, disable the cron-job.org jobs.

## Changing what is watched (no code change needed)

Add any of these under **Settings → Secrets and variables → Actions → Variables**. Delete the variable
to go back to the default from `src/FeverTickets/Constants.cs`.

| Variable | Default | Format |
| --- | --- | --- |
| `TARGET_SESSIONS` | `2026-11-04 21:00, 2026-11-12 19:00` | comma separated, `yyyy-MM-dd HH:mm`, Israel time |
| `PRIMARY_ZONE` | `Zone A` | exact zone name from the page |
| `SECONDARY_ZONE` | `Zone B` | exact zone name from the page |
| `EVENT_URL` | `https://feverup.com/m/436641/en` | |
| `LOG_LEVEL` | `info` | `info` or `debug` – debug logs every session on the page and sends a "Debug: check result" ntfy message on **every** run, even when nothing is available |

**Testing that everything works:** set `TARGET_SESSIONS` to a session that has tickets
(e.g. `2026-12-01 19:00`), run the workflow manually, check you get the urgent message, then delete the variable.

## Local run

```bash
cp .env.example .env   # then put your topic in .env (git-ignored)
dotnet run --project src/FeverTickets
dotnet test
```

Override a single value for one run: `TARGET_SESSIONS="2026-12-01 19:00" HEARTBEAT=true dotnet run --project src/FeverTickets`.
