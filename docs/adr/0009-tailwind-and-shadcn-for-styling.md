# ADR-0009: Tailwind CSS and shadcn/ui for styling

- Status: Accepted
- Date: 2026-11-27

## Context

- The frontend has no styles. Story #30 gives it a minimalist, consistent design with a dark palette, so that attention stays on the data.
- Buttons, fields and tables are repeated across the pages and must look the same.
- A light theme is planned as a later story, and the palette is expected to be adjusted after seeing it in use.
- Tailwind CSS and shadcn/ui were already part of the planned stack.

## Decision

- **Styling:** Tailwind CSS v4, through its Vite plugin.
- **Components:** shadcn/ui on Base UI (its current default). Its components are copied into `src/components/ui`, and only the ones in use are added.
- **Palette:** every color is a CSS variable in `src/index.css`, exposed to Tailwind with `@theme`. Components and pages use the variable names, never color values.
- **One theme:** the app is dark only. The `dark:` variants are bound to a `.dark` class that is not used, so the look does not depend on the theme of the operating system.
- **Configuration:** `components.json` was written by hand instead of running `shadcn init`, which would have added a theme of its own. The `@/` alias points to `src/`.

## Alternatives considered

1. **Plain CSS with no dependencies.** Rejected because buttons, fields and tables, with their focus and disabled states, would have to be designed and maintained by hand.
2. **A component library such as MUI or Mantine.** Not adopted: they bring their own theming system and more components than the app needs, while shadcn/ui leaves the code in the project and was already the planned choice.

## Consequences

- **Positive:** changing the palette means editing one file; a light theme only needs another set of values for the same variables; the component code can be changed freely.
- **Negative:** several new dependencies (`tailwindcss`, `@tailwindcss/vite`, `@base-ui/react`, `class-variance-authority` and `cn`), and long class lists in the markup.
- Updates to the copied components are not automatic: they have to be applied by hand.
