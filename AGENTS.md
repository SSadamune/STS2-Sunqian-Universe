# Repository instructions

## Player-facing text

- Keep card, power, and keyword descriptions concise and focused on gameplay.
- Omit implementation details that do not affect player decisions or understanding. Keep technical explanations in developer documentation.
- Preserve user-provided wording and formatting exactly when specified; do not add unsolicited caveats or explanations.
- Card changes implicitly last for the current combat. Omit that duration from player-facing text; explicitly label exceptions such as permanent or this-turn changes.

## Unreleased renames

- Renames introduced on this branch have not shipped. Rename code and localization identifiers directly; do not add old-save compatibility aliases or migration patches for these renames.

## Delegating to DeepSeek Harness or Cursor

These rules apply when the user requests delegation to DeepSeek Harness / DSH or Cursor / Grok, or explicitly selects dsh-baton or worker-router. They do not automatically delegate ordinary tasks.

- For DSH, use the separate dsh-baton MCP directly. For Cursor, use worker-router with worker="cursor-grok". In both cases, use this repository as the working directory. If the selected executor is unavailable, report the blocker; do not silently switch executors or implement the task in Codex. Do not run both executors concurrently against this repository.
- Codex defines the specification, permitted scope, and acceptance criteria, then performs the final review. Include these requirements and the following testing responsibilities in the delegated task.
- Delegate implementation, automated test authoring, test execution, failure analysis, and fixes to the selected executor (DSH or Cursor). Explicitly instruct that executor to run the relevant tests and provide results, rather than leaving test execution to Codex. Use the repository's actual build/test workflow; do not assume an npm-based project. Do not skip tests, weaken assertions, or relax acceptance criteria to obtain a passing result.
- Where supported, supply the agreed acceptance commands through the selected MCP's `checks` (dsh-baton or worker-router) so they run independently on the execution side. Do not add redundant repeated test runs without a concrete verification reason.
- Require evidence: exact commands, working directory, exit codes, test counts when available, failure summaries, and paths to full logs. Clearly distinguish passed, failed, blocked, and not-run checks. Request a requirement-by-requirement completion report with file references.
- To conserve Codex usage, review the returned evidence, change summary, and requirement report first. Do not routinely rerun the same tests in Codex or read complete logs. Prefer asking the selected executor to supply missing evidence or run targeted follow-up checks. Inspect relevant code or independently verify specific concerns when evidence is inconsistent, incomplete, or the change warrants it.
- Use the selected MCP's long-wait mechanism instead of frequent status checks or progress summaries, unless the user requests progress reports. Send corrections back to the same Harness or Cursor conversation when appropriate.
- An execution-completed status or the worker's own success claim is not acceptance. Report the final outcome, test evidence, and remaining limitations honestly; ask the user about unresolved decisions rather than answering on their behalf without authorization.

## Git commits

- Write commit subjects and bodies in concise English and follow the existing commit style.
- Every commit created or amended by Codex, including merge commits, must include this trailer after a blank line:

  Co-authored-by: Codex <codex@openai.com>

- Preserve existing authors and co-author trailers. Do not add the Codex trailer more than once.
- After committing, inspect the final commit message with `git log -1 --format=%B` and verify the trailer is present before reporting completion.

## This branch's new cards

- On `feature/new-character`, new cards belong to the Reincarnation Prisoner (`轮回囚徒`), implemented as `LiubeiCharacter` / `LiubeiCardPool`, unless a card explicitly specifies another pool.
- Do not draw new card illustrations on this branch. Use the existing `res://images/cards/PrisonerPlaceholder.png` placeholder unless explicitly told otherwise.

## Card timing wording

- "When played" effects resolve before the card's effects; "after played" effects resolve after the card's effects.
