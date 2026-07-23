# Diagnosa 5 Bug Kritis — 2.5D-RPG

**Tanggal:** 2026-07-22
**Codebase:** Unity 6 (6000.3.10f1) + C# + URP, ~22 file C# di `Assets/Scripts/`
**Plan:** skill `diagnosing-bugs` — Phase 1 (loop merah) → Phase 6 (post-mortem) per bug
**Test mode:** Unity Editor Test Runner → EditMode (kecuali BUG #4 yg Play-mode)

---

## Lokasi exact bugs (verified via verbatim read)

| # | Label | File:Line | Tipe |
|---|-------|-----------|------|
| 1 | BUG #1 alias+off-by-one | `Assets/Scripts/PartyManager.cs:79-87` | Logic bug (CRITICAL) |
| 2 | BUG #2 off-by-one | `Assets/Scripts/BattleSystem.cs:342-348` | Logic bug |
| 3 | BUG #5 PlayerPrefs clamp | `Assets/Scripts/SettingsManager.cs:27-66` | Security/crash bug |
| 4 | BUG #3 IndexOOB random | `Assets/Scripts/BattleSystem.cs:485-510` | Crash bug |
| 5 | BUG #4 turn-flow if/if | `Assets/Scripts/BattleSystem.cs:128-165` | Logic bug (subtle) |

## Dependency graph

```
BUG #1 ─┬─> BUG #2 ─┐
BUG #5 ─┤           ├─> BUG #3 ─> BUG #4
        └───────────┘
```

**Urutan eksekusi:** #1 → #2 → #5 → #3 → #4 (dari paling fundamental ke paling dependent).

## Test feedback loop strategy

- **EditMode tests** untuk #1, #2, #3, #5: pure-logic, bisa dijalankan via `Window → General → Test Runner → Run All`. Pure C# dengan reflection untuk access private field/method.
- **HITL Play-mode** untuk #4: butuh coroutine + battle UI → tidak ada seam EditMode yang baik. Pakai bash script template [`hitl-loop.template.sh`](file:C:/Users/LENOVO/.agents/skills/diagnosing-bugs/scripts/hitl-loop.template.sh) style, ngajak user klik Play.
- **Reflection strategy:** field `currentParty` di PartyManager adalah `[SerializeField] private List<PartyMember>`. Test akan inject via `typeof(PartyManager).GetField("currentParty", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(...)`.

---

## Post-mortem (diisi per-bug saat Phase 6 selesai)
Lihat `docs/bugs.md` untuk entri individual.

## Rekomendasi arsitektur (post-fix)
Setelah semua 5 bug selesai, hand-off ke `/improve-codebase-architecture` untuk:

1. Bug #4 tidak punya seam EditMode yang baik → perlu extract pure turn-flow logic jadi service inject-able.
2. Audit semua `RemoveAt` callsites: `PartyMenuController.cs:147,:218` juga forward-loop-remove pattern.
3. Audit semua `PlayerPrefs` callsites (cuman SettingsManager sekarang, tapi seed pattern penting).
4. `BattleSystem.cs` 593 LOC god-class → extract state-machine + entity-spawner + reward-distribution ke file masing-masing.
