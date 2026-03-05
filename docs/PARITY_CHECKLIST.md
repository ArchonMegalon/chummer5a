# UI Parity Checklist

Generated automatically from the legacy shell contract and current contracts catalogs.

- Generated UTC: `2026-03-05T20:58:07+00:00`
- Legacy shell source: `Chummer.Web/wwwroot/index.html`
- Tab catalog source: `Chummer.Contracts/Presentation/NavigationTabCatalog.cs`
- Action catalog source: `Chummer.Contracts/Presentation/WorkspaceSurfaceActionCatalog.cs`
- Control catalog source: `Chummer.Contracts/Presentation/DesktopUiControlCatalog.cs`
- Workspace Actions coverage compares legacy `data-action` IDs to action `TargetId` values.

## Summary

| Surface | Legacy IDs | Covered | Missing In Catalog | Catalog Only |
| --- | ---: | ---: | ---: | ---: |
| Tabs | 17 | 17 | 0 | 1 |
| Workspace Actions | 47 | 47 | 0 | 1 |
| Desktop Controls | 29 | 29 | 0 | 0 |

## Tabs Coverage

| ID | Status |
| --- | --- |
| `tab-adept` | covered |
| `tab-armor` | covered |
| `tab-attributes` | covered |
| `tab-calendar` | covered |
| `tab-combat` | covered |
| `tab-contacts` | covered |
| `tab-cyberware` | covered |
| `tab-gear` | covered |
| `tab-improvements` | covered |
| `tab-info` | covered |
| `tab-lifestyle` | covered |
| `tab-magician` | covered |
| `tab-notes` | covered |
| `tab-qualities` | covered |
| `tab-skills` | covered |
| `tab-technomancer` | covered |
| `tab-vehicles` | covered |
| `tab-rules` | catalog_only |

## Workspace Actions Coverage

| ID | Status |
| --- | --- |
| `aiprograms` | covered |
| `armorlocations` | covered |
| `armormods` | covered |
| `armors` | covered |
| `arts` | covered |
| `attributedetails` | covered |
| `attributes` | covered |
| `awakening` | covered |
| `build` | covered |
| `calendar` | covered |
| `complexforms` | covered |
| `contacts` | covered |
| `critterpowers` | covered |
| `customdatadirectorynames` | covered |
| `cyberwares` | covered |
| `drugs` | covered |
| `expenses` | covered |
| `foci` | covered |
| `gear` | covered |
| `gearlocations` | covered |
| `improvements` | covered |
| `initiationgrades` | covered |
| `inventory` | covered |
| `lifestyles` | covered |
| `limitmodifiers` | covered |
| `martialarts` | covered |
| `mentorspirits` | covered |
| `metadata` | covered |
| `metamagics` | covered |
| `movement` | covered |
| `powers` | covered |
| `profile` | covered |
| `progress` | covered |
| `qualities` | covered |
| `rules` | covered |
| `skills` | covered |
| `sources` | covered |
| `spells` | covered |
| `spirits` | covered |
| `summary` | covered |
| `validate` | covered |
| `vehiclelocations` | covered |
| `vehiclemods` | covered |
| `vehicles` | covered |
| `weaponaccessories` | covered |
| `weaponlocations` | covered |
| `weapons` | covered |
| `data_exporter` | catalog_only |

## Desktop Controls Coverage

| ID | Status |
| --- | --- |
| `combat_add_armor` | covered |
| `combat_add_weapon` | covered |
| `combat_damage_track` | covered |
| `combat_reload` | covered |
| `contact_add` | covered |
| `contact_connection` | covered |
| `contact_edit` | covered |
| `contact_remove` | covered |
| `create_entry` | covered |
| `delete_entry` | covered |
| `edit_entry` | covered |
| `gear_add` | covered |
| `gear_delete` | covered |
| `gear_edit` | covered |
| `gear_mount` | covered |
| `gear_source` | covered |
| `magic_add` | covered |
| `magic_bind` | covered |
| `magic_delete` | covered |
| `magic_source` | covered |
| `move_down` | covered |
| `move_up` | covered |
| `open_notes` | covered |
| `show_source` | covered |
| `skill_add` | covered |
| `skill_group` | covered |
| `skill_remove` | covered |
| `skill_specialize` | covered |
| `toggle_free_paid` | covered |
