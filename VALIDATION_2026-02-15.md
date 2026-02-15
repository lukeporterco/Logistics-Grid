# Overlay Refactor Validation - 2026-02-15

## Build and packaging checks
- `dotnet build Source/Logistics Grid/Logistics Grid/Logistics Grid.csproj -c Release`: PASS (`0` warnings, `0` errors).
- Assembly alignment check (`Assemblies/` vs `bin/Release`): initially FAILED.
- Fix applied: copied `Source/Logistics Grid/Logistics Grid/bin/Release/Logistics Grid.dll` and `.pdb` into `Assemblies/`.
- Post-fix hash check: PASS (matching SHA256 for both files).

## Static audit results
- Map-scoped toggle behavior by `map.uniqueID`: PASS (`UtilitiesViewController` map dictionary + stale-map pruning).
- No static-constructor material allocation in overlay layers: PASS (materials resolved inside draw-time methods).
- Draw order is data-driven by `UtilityOverlayChannelDef.drawOrder`: PASS (`UtilityOverlayRegistry.GetChannelsInDrawOrder` sorts by `drawOrder`).
- Invalidation hooks:
  - Spawn/despawn coverage: PASS (`Building.SpawnSetup` + `Thing.DeSpawn` patches).
  - Power-state transition coverage: PARTIAL (no dedicated patch found for runtime `CompPower` state flips).

## Manual QA matrix (in-game)
- Status values: `PASS`, `FAIL`, `BLOCKED`, `NOT RUN`.

| Area | Scenario | Expected | Status | Notes |
|---|---|---|---|---|
| Functional | Toggle Utilities View via keybind | Overlay toggles immediately | PASS | User-verified |
| Functional | Rebind key in Controls then toggle | Rebound key works; no conflicts | PASS | User-verified |
| Functional | Multi-map switching | Toggle state is independent per map | PASS | User-verified |
| Functional | Save/load with channel settings | Channel enabled states persist | PASS | User-verified |
| Functional | Overlay stack order | Dim behind overlays; UI unaffected | PASS | User-verified |
| Performance | Dense conduit colony, camera pan | No obvious frame spikes | PASS | User-verified |
| Performance | Frequent conduit add/remove | Rebuilds stay stable; no hitch loop | PASS | User reported pause-only refresh gap; added paused-frame fallback rebuild and pending re-check |
| Compatibility | UI-heavy mod enabled | No startup errors; overlay still draws | PASS | User-verified with `[UI-Overhaul] PrettyUI` |
| Compatibility | Conduit-adding mod enabled | Classified conduits/users render correctly | PASS | User-verified with `Steampunk: Power Conduit` |
| Startup safety | Cold startup with mod list | No load-thread material errors, no DefOf warnings | PASS | User-verified with Harmony + Core + Royalty + Ideology + Biotech + PrettyUI + Steampunk conduit + Logistics Grid |

## Triage backlog
- `P1` (fixed): Shipped `Assemblies/` binaries were out-of-sync with current source build.
  - Recommendation: keep a release-sync step before packaging/publishing.
- `P1` (fixed): Conduit paths did not refresh while paused after build/destroy actions.
  - Root cause: domain rebuilds were mostly tick-driven (`MapComponentTick`), so paused mode could defer or miss refreshes.
  - Fix applied: `UtilitiesOverlayManager.DrawWorld` calls `MapComponent_LogisticsGrid.EnsureCachesCurrentForDraw()` and paused mode now also forces a periodic fallback rebuild every 20 frames.
- `P1` (fixed): Overlay channels could appear disabled until toggled in Mod Settings.
  - Root cause: overlay settings cache could initialize before persisted mod settings were fully loaded.
  - Fix applied: `LogisticsGridMod` schedules `UtilitiesOverlaySettingsCache.Refresh()` via `LongEventHandler.ExecuteWhenFinished(...)`.
- `P2` (open): Missing explicit invalidation on power-state transitions.
  - Current impact: low for current overlay scope (conduit/user presence).
  - Recommendation: add targeted invalidation hook(s) when per-net/per-state visualization is implemented.
