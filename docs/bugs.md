# 2.5D-RPG — Bug Post-Mortem

Katalog hasil diagnosis 5 bug kritis (Proses diagnosis mengikuti skill `diagnosing-bugs`).

## Status

| # | Bug | Status | File | Test |
|---|-----|--------|------|------|
| 1 | `PartyManager.GetAliveParty` alias + off-by-one | POSTED | `Assets/Scripts/PartyManager.cs` | EditMode |
| 2 | `RemoveDeadBattlers` off-by-one | POSTED | `Assets/Scripts/BattleSystem.cs` | EditMode |
| 3 | `GetRandom*` IndexOutOfRange | POSTED | `Assets/Scripts/BattleSystem.cs` | EditMode |
| 4 | Turn-flow `if`/`if` terpisah | POSTED | `Assets/Scripts/BattleSystem.cs` | HITL Play-mode |
| 5 | SettingsManager PlayerPrefs no clamp | POSTED | `Assets/Scripts/SettingsManager.cs` | EditMode |

Detail per-bug di-update setelah fix selesai (root cause → fix → regression test result).
