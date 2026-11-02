# Repository instructions

## Player-facing text

- Keep card, power, and keyword descriptions concise and focused on gameplay.
- Omit implementation details that do not affect player decisions or understanding. Keep technical explanations in developer documentation.
- Preserve user-provided wording and formatting exactly when specified; do not add unsolicited caveats or explanations.
- Card changes implicitly last for the current combat. Omit that duration from player-facing text; explicitly label exceptions such as permanent or this-turn changes.

## Git commits

- Write commit subjects and bodies in concise English and follow the existing commit style.
- Every commit created or amended by Codex, including merge commits, must include this trailer after a blank line:

  Co-authored-by: Codex <codex@openai.com>

- Preserve existing authors and co-author trailers. Do not add the Codex trailer more than once.
- After committing, inspect the final commit message with `git log -1 --format=%B` and verify the trailer is present before reporting completion.
