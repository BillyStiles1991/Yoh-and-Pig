# Yoh and Pig · Dialogue and Reviews · v1.9.0

Full-source update from the unchanged v1.8.0 Activities release. 87 C# files; six additions and thirteen replacements. Includes all previous supplied source, updated PlayerActions.inputactions, complete integration, inventory integration, focused dialogue guide and development work log. Unchanged v1.8 activity and pickup guides are retained.

## Install

1. Back up/commit the project and save files; exit Play Mode.
2. Replace matching source in its existing location, preserving .meta files. Add only new files from SCRIPT_MANIFEST.md. Keep Editor scripts in an Editor folder. Do not leave duplicate classes in Assets.
3. Replace PlayerActions.inputactions in place, preserving .meta. Existing 114 IDs and existing maps are unchanged; Dialogue adds rebindable Enter/Cross/A advance and right-stick scrolling.
4. After Unity compiles, open the configured gameplay scene and run Tools → Freezer Game → Configure Dialogue v1.9 In Active Scene.
5. Assign the shared IntroContent and portrait/font assets. Configure each selected NPC's head anchor and interaction references as described in the focused guide. Save scenes/assets.
6. Run the documented Editor checks, then the Play Mode test matrix. Existing managers must remain outside deactivated panels.

## Read first

- Documentation/Yoh_and_Pig_Dialogue_Review_Integration_v1.9.0.md — new NPC setup, Reviews, Jereremy tutorial and testing.
- Documentation/Yoh_and_Pig_Complete_Integration_v1.9.0.md — comprehensive installation with previous systems retained and obsolete dialogue instructions replaced.
- Documentation/Yoh_and_Pig_Inventory_Integration_v1.9.0.md — inventory and updated NPC review integration.
- Documentation/Yoh_and_Pig_Development_Work_Log.md — task, changes, date, validation limits and follow-up.
- SCRIPT_MANIFEST.md and VALIDATION.md — exact file changes and verification status.

## Compatibility

Existing schema-2 freezer saves remain in FreezerSaves_v2. Legacy bond points and gift limits migrate into Review records. Unlocks are permanent per save. Old schema-1 saves and saveData.json still have no supplied migration. Keep backups and do not save a migrated slot with an older build.

This package includes code and documentation, not finished portrait art, animation clips, fonts, scenes or authored database assets. The optional tutorial builder adds placeholder text to your existing database without overwriting matching IDs.

External syntax and structural checks passed. Unity compilation, the included Unity Editor data checks, controller input and visual Play Mode checks have not been run here. See VALIDATION.md before marking the installation complete.
