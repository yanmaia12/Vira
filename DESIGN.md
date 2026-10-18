# Vira Design System

## Purpose

Use this document as the visual source of truth for Vira's Windows desktop interface. Keep the product task-focused, calm, and easy to understand. Vira is a file utility, not a marketing site or analytics dashboard.

The interface language is Portuguese (Portugal). Preserve the existing conversion and PDF-signing workflows while improving their hierarchy and visual consistency. Do not copy another product's logo, name, or exact branded composition.

## Visual direction

- Use a light, Windows-friendly canvas with clear white work surfaces.
- Use Vira blue for the main action and focused controls; reserve it for meaningful actions and selection.
- Keep text contrast strong and secondary instructions quiet but readable.
- Prefer simple shapes, subtle borders, and restrained shadows over gradients, glass effects, or decorative illustrations.
- Give the selected file, conversion target, PDF page, and signature placement the most visual attention in their respective workflows.

## Color tokens

| Token | Value | Use |
| --- | --- | --- |
| `Canvas` | `#F4F6FB` | Main window background |
| `Surface` | `#FFFFFF` | Cards, dialogs, document preview surface |
| `SurfaceSubtle` | `#F8F9FC` | File list and quiet inset areas |
| `TextPrimary` | `#172033` | Headings and primary content |
| `TextSecondary` | `#53617A` | Explanatory text and labels |
| `TextMuted` | `#667085` | Supporting hints and metadata |
| `Border` | `#E2E7F0` | Card outlines and separators |
| `Accent` | `#315FEA` | Primary buttons, focus, selection, signature bounds |
| `AccentSoft` | `#EAF0FF` | Low-emphasis accent surfaces |
| `Success` | `#16834A` | Completed conversions and saved files |
| `Warning` | `#A65B00` | Caution and recoverable issues |
| `Error` | `#B42318` | Failed actions and validation errors |

Use semantic tokens in WPF resources instead of introducing new hard-coded color values in individual views. Maintain readable text contrast on every surface.

## Typography

- Use `Segoe UI` or the system's `Segoe UI Variable` where available. Do not require remote fonts.
- Window title: 28–30 px, semibold or bold.
- Section heading: 18–20 px, semibold.
- Body and control labels: 14–16 px, regular.
- Helper text and metadata: 12–13 px; never use muted text for essential instructions.
- Keep labels concise and use sentence case in Portuguese.

## Spacing, shape, and depth

- Use a 4 px base spacing scale: 4, 8, 12, 16, 24, and 32 px.
- Keep 20–24 px inside primary cards and 12–16 px between related controls.
- Use 8 px corners for controls, 12–16 px for cards, and consistent radii within a view.
- Use a 1 px `Border` outline to define surfaces. Shadows should be subtle and reserved for floating or modal surfaces.
- Keep primary buttons at least 38 px high; provide comfortable hit targets for resize handles and other pointer controls.

## Application layout

### Main window

- Preserve the current two-workflow focus: file conversion and PDF signing.
- Make the conversion area the primary work surface. The user should see the drop target or file list, output format, and primary **Converter** action without hunting through the window.
- Keep QR signing and File Explorer registration discoverable, but visually secondary to the current task.
- Maintain a clear status area for ready, working, completed, and failed states. Give the status a short message and an optional next action.
- When the window narrows, let secondary content move below the primary work surface rather than squeezing controls until labels are unreadable.

### Conversion workflow

- Make drag-and-drop and **Adicionar ficheiros** clear ways to start.
- Each queued row should show the file name, detected type, and a clear remove action. Do not use color alone to indicate a file's state.
- Keep output format selection near the file queue and the **Converter** action at the end of that flow.
- Provide distinct empty, converting, completed, and failed states. Preserve enough detail in an error to tell the user what to try next.
- Avoid implying that every file format converts to every other format. Show only supported targets for the selected inputs.

### PDF signature workflow

- Keep the rendered PDF page as the visual center of the placement window.
- Keep page selection and signature size easy to find without covering the page preview.
- Show the signature outline and resize handle clearly against both light and dark parts of a PDF page.
- Keep **Guardar PDF assinado** as the primary action and **Cancelar** as a quieter secondary action.
- State which page will receive the signature and reassure the user that the original PDF is preserved.
- In the QR flow, explain the network requirement before the user scans the code and show clear waiting, received, expired, and failed states.

## Component rules

- Primary button: `Accent` fill, white text, clear hover, pressed, disabled, and keyboard-focus states.
- Secondary button: `Surface` fill with `Border`; use `TextPrimary` or `Accent` text according to importance.
- Inputs and selectors: white surface, visible border, consistent height, and an obvious keyboard-focus outline.
- Cards: `Surface` with a subtle `Border`; do not nest bordered cards without a clear grouping reason.
- File rows: consistent alignment and spacing, with file identity before metadata and actions.
- Notifications and status text: pair color with an icon or label so meaning does not depend on color alone.

## Accessibility and interaction

- Support keyboard navigation, visible focus, and Enter/Escape behavior for primary and cancel actions.
- Keep text readable at common Windows display scaling levels and when the window is resized.
- Do not communicate success, failure, or selection through color alone.
- Use descriptive accessible names for icon-only controls.
- Avoid unnecessary animation; any transition must not delay completion or hide status changes.

## WPF implementation guidance

- Put shared brushes, text styles, spacing, and control styles in reusable WPF `ResourceDictionary` files.
- Reference those resources from `MainWindow.xaml` and `SignaturePlacementWindow.xaml` rather than duplicating style values.
- Keep view-specific layout in the view; keep conversion, PDF rendering, networking, and signing logic out of visual resources.
- Preserve existing control names and event bindings unless a behavior change requires an intentional code update.
- When adding a new visual state, define its normal, hover, pressed, focused, disabled, and error appearance where applicable.

## Review checklist

Before calling a UI change complete:

1. Check the main window and signature-placement window at their minimum sizes and at a larger size.
2. Confirm queued, empty, busy, success, and failure states remain understandable.
3. Check keyboard focus and control contrast.
4. Verify that the change did not alter conversion, QR signing, page selection, signature sizing, or File Explorer registration behavior.
5. Reuse existing tokens or update this document before introducing a new visual rule.
