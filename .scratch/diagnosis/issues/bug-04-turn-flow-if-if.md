# BUG #4 — AttackRoutine `if` / `if` terpisah bukan `else if`

**Severity:** LOW-MEDIUM (logika fragile, saat ini coincidence-safe)
**File:** `Assets/Scripts/BattleSystem.cs:128-165`
**Blocks-by:** BUG #1, #2, #3 (perlu fixes lebih dulu supaya repro flow terkontrol)
**Test mode:** HITL Play-mode (tidak ada seam EditMode untuk battle coroutine + UI)

## Root cause

```csharp
private IEnumerator AttackRoutine(int i)
{
    if (allBattlers[i].IsPlayer == true) { ... }                  // L128
    // tidak ada `else`
    if (i < allBattlers.Count && allBattlers[i].IsPlayer == false) { ... } // L165
}
```

Buka, bukan `else if`. Saat ini "coincidence-safe" karena untuk player IsPlayer==true, blok kedua tidak akan tereksekusi (cek kedua `IsPlayer != false`).

**Interaksi dengan BUG #1/2:**
- L147 `enemyBattlers.Remove(currTarget)` — `enemyBattlers` dan `allBattlers` adalah 3 list berbeda (deklarasi L20-22). Kalau remove di `enemyBattlers` saja, `allBattlers.Count` TIDAK ikut berubah. Tapi `RemoveDeadBattlers` di L110 nanti akan clean-up `allBattlers`. **Setelah fix BUG #2**, ini aman.
- L165 `i < allBattlers.Count` — defensive check aman tapi redundant kalau parameter `i` selalu valid.

## Test seam

Tidak ada seam EditMode bagus. Pakai HITL Play-mode:
1. User buka Unity Editor, Play scene BattleScene.
2. Tambahkan log instrumentation tagged `[DEBUG-BUG04]` di:
   - L128 (entering player block): `Debug.Log($"[DEBUG-BUG04] i={i} attacker={allBattlers[i].Name} IsPlayer=true");`
   - L147 (pre-remove dead enemy): `Debug.Log($"[DEBUG-BUG04] removed {currTarget.Name} from enemyBattlers; allBattlers.Count={allBattlers.Count}");`
   - L165 (entering enemy block): `Debug.Log($"[DEBUG-BUG04] i={i} attacker={allBattlers[i]?.Name} IsPlayer=false");`
3. Repro: pilih attack, kill enemy. Lihat console.
4. Assert: setiap turn = 1 player block entry atau 1 enemy block entry (tidak ada yang skip).
5. Cleanup: grep `[DEBUG-BUG04]` → hapus 3 log lines.

## Plan

**Phase 3:** Hypothesis (ranked):
1. H1: Ganti `if (IsPlayer == false)` jadi `else if (IsPlayer == false)` → konsisten.
2. H2: Hapus `enemyBattlers.Remove(currTarget)` di L147 (sudah handle oleh `RemoveDeadBattlers()` di akhir turn) → kurangi side-effect tangle.
3. H3 (low): Fix `i < allBattlers.Count` di L165 jadi `assertion` kalau hardening layak.

**Phase 5:** Fix:

```csharp
private IEnumerator AttackRoutine(int i)
{
    if (allBattlers[i].IsPlayer == true)
    {
        // players turn  -- (konten sama, tapi hapus enemyBattlers.Remove di L147 -- biarkan RemoveDeadBattlers yang handle)
        // ...
        enemyBattlers.Remove(currTarget);   // opsional bisa tetap, tapi sekarang race dengan RemoveDeadBattlers => double-remove safe (Contains check) tapi redundant.
        // Untuk minimal-impact: HAPUS baris ini; RemoveDeadBattlers udah handle.
    }
    else if (!allBattlers[i].IsPlayer)
    {
        // enemies turn  (konten sama)
    }
}
```

**Phase 6:** Cleanup instrumentation. Commit msg:
> `BattleSystem.AttackRoutine: use else-if branch; defer dead-enemy removal to RemoveDeadBattlers for single cleanup pass`.

Rekomendasi arsitektur (gating ini ke arsitektur-review):
- Extract turn-flow state machine ke file terpisah (testable).
- Tidak ada seam unit test untuk battle loop coroutine → decline `/tdd` di area ini.
- `BattleSystem.cs` 593 LOC god-class → layak dipecah jadi BattleFlowState + BattleEntityFactory + BattleReward.
