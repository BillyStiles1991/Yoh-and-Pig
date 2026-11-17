# Validation · v1.9.0

## Performed outside Unity

- Parsed all 87 C# files with tree-sitter-c-sharp: no syntax error nodes.
- Checked qualified declared types across the package: no duplicate types.
- Compared against the published v1.8.0 ZIP: six new C# files, thirteen updated, sixty-eight unchanged; no baseline source file removed.
- Parsed PlayerActions.inputactions as JSON: all 114 baseline IDs preserved, no duplicate IDs, existing maps unchanged, expected Dialogue defaults present.
- Reconstructed the current complete/inventory/work-log source documents with matching original UTF-8 byte counts before editing. Updated current instructions and retained prior unrelated integration/history.
- Generated the full path/change manifest and per-source SHA-256 report in STATIC_VALIDATION.json.
- Checked the release archive opens without corrupt entries.

## Included but NOT executed in Unity

Tools → Freezer Game → Run Dialogue Review Data Checks v1.9 includes 14 assertions for migration, duplicate prevention, unlock permanence, point floors, quest promotion and save state. Validate Dialogue and Reviews v1.9 checks authored definitions and runs as a build preprocess check. Existing inventory checks were updated to inspect migrated Review records.

No Unity Editor/compiler, game scenes, art assets or input devices were available for execution here. Syntax parsing does not prove Unity compilation or runtime behaviour. Run the checks and Play Mode matrix in the focused guide before accepting the release. In particular verify old database serialization, normal/Key bubble placement, UI portrait clips, TMP glyphs, long-line scrolling, rebindings, review quotas, save/load, menu locks and T1/T2 transitions.

No final visual assets, scene prefabs or Unity-generated .meta files are supplied. Preserve the existing project's metadata during replacement.
