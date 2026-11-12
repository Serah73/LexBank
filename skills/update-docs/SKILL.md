---
name: update-docs
description: Closing step of /pair. After a finished story, updates the READMEs and CLAUDE.md files only with what the story made relevant. Academic, professional tone for the READMEs (TFM); short operational notes for CLAUDE.md. Never commits.
disable-model-invocation: true
---

# Update docs

A story has just been finished. Bring the documentation up to date with it. **The default is to change nothing**: a file is only touched when the story made something in it false, or added something a reader of that file needs.

## Files

| File | Reader | Purpose |
|---|---|---|
| `README.md` | Academic committee, any visitor | What LexBank is, why, how it is built, what it does today |
| `src/backend/README.md`, `src/frontend/README.md` | Someone who runs or evaluates that part | Architecture, API or screens, how to run, test and inspect it |
| `CLAUDE.md` | The AI agent, every session | Project-wide context, decisions and conventions |
| `src/backend/CLAUDE.md`, `src/frontend/CLAUDE.md` | The AI agent working in that area | Commands, where things go, conventions, pitfalls |

## Steps

1. **Read what changed.** Use the story block and the story's diff (`git status`, `git diff`, `git log`: read-only commands). Don't rely on memory of the session.
2. **Read the six files** as they are now.
3. **Find what is stale first**, then what is missing. Apply the filters below.
4. **Propose the changes** as a short list and wait for my go-ahead:

   ```
   <file> — <change in one line>. Why: <what the story changed>
   ```

   If nothing is worth changing, say "No documentation changes needed" and stop. That is a valid result.
5. **Apply the approved changes** and suggest a `docs:` commit message. I make the commit.

## What belongs in a README

Only if the story changed it:

- A capability in the project status table (mark it "Implemented" only when I confirmed the story is done).
- An endpoint, an error code or the shape of a response; a screen or a user-visible flow.
- A layer, project or component a reader needs to understand the architecture.
- A technology that is now really installed (move it out of "planned").
- A requirement, a command, or a step to run, test or inspect the system.
- A new ADR (add it to the index).
- A scope or design decision that changes what the thesis claims.

Does not belong: implementation details, class-by-class descriptions, test counts, changelogs, story numbers, how the session went, or anything from local working files (`Backlog.md`, `plan.md`, `pendiente.md`).

## What belongs in a CLAUDE.md

Only what would save time or prevent a mistake in a future session:

- A command that is new or changed.
- A convention the story established and the next story must follow.
- A pitfall we hit that is not obvious from the code.
- A decision that applies to the whole project (root file) or the whole area (area file).
- A line that is now false: fix or delete it.

Does not belong: what the code or the README already says, history of what was done, project status, one-off facts about this story. Put each note in the most specific file that covers it, and never repeat the root file in an area file.

## Rules

- **Don't invent.** No facts I have not given you (supervisor, dates, results, figures). If a README needs one, ask.
- **READMEs: academic and professional.** Third person, present tense, precise terms, no marketing language, no first person, no emojis. Describe what exists, and label what is planned as planned.
- **CLAUDE.md: short.** One line per note, imperative. Prefer replacing or deleting a line over adding one; if a file grows, something else should leave.
- **English**, as all repository text. Spanish only for proper names and Spanish-language sources.
- **Keep the structure** of each file. Don't add sections or reorganize unless the story requires it; if you think a restructure is due, suggest it separately.
- **Never commit** or run any command that modifies git.
