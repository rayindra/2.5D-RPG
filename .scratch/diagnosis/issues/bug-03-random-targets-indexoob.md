# BUG #3 — `GetRandomPartyMember` / `GetRandomEnemy` IndexOutOfRange

**Severity:** MEDIUM (crash kalau target list habis di tengah turn)
**File:** `Assets/Scripts/BattleSystem.cs:485-510`
**Blocks:** BUG #4 (AttackRoutine caller `currAttacker.SetTarget(GetRandomEnemy())` butuh -1 contract sebelum turn-flow fix)
**Test mode:** EditMode

## Root cause

```csharp
private int GetRandomPartyMember()
{
    List<int> partyMembers = new List<int>();
    // ... loop tambah yang IsPlayer & HP > 0
    return partyMembers[Random.Range(0, partyMembers.Count)];  // ← crash if empty
}
```

`Random.Range(0, 0)` = 0, akses `[0]` ke list kosong = `ArgumentOutOfRangeException`.

Skenario: 1-vs-1 final. Musuh baru saja mati (di-remove dari `enemyBattlers` di L147) → pada turn berikutnya, player mungkin targeting musuh habis (battle state sudah Won, tapi if easy race ada). Atau enemy turn call `GetRandomPartyMember()` setelah semua party mati tapi state belum Lost.

## Repro test

```csharp
[Test]
public void GetRandomPartyMember_WhenAllDead_ReturnsMinusOne()
{
    var go = new GameObject();
    var bs = go.AddComponent<BattleSystem>();
    var all = InjectField<List<BattleEntities>>(bs, "allBattlers");
    // 3 player, semua HP > 0 (alive) tapi akan kita set CurrHealth=0 via second loop using field
    var p1 = MakeBattler(0, isPlayer: true, name: "p1");
    var p2 = MakeBattler(0, isPlayer: true, name: "p2");
    var p3 = MakeBattler(0, isPlayer: true, name: "p3");
    all.Add(p1); all.Add(p2); all.Add(p3);
    // semua dead

    int result = InvokeMethod<int>(bs, "GetRandomPartyMember");
    Assert.AreEqual(-1, result);
}

[Test]
public void GetRandomEnemy_WhenAllDead_ReturnsMinusOne()
{
    // mirror versi GetRandomEnemy
}
```

## Plan

**Phase 3:** H1 (winner): missing empty guard + missing return contract untuk caller. Designers pilih: return `-1` atau throw. Saya pilih `-1` (caller sudah defensive `AttackRoutine:133 SetTarget(...)`, perlu handle supaya tidak crash juga).

**Phase 5:** Fix:

```csharp
private int GetRandomPartyMember()
{
    List<int> partyMembers = new List<int>();
    for (int i = 0; i < allBattlers.Count; i++)
        if (allBattlers[i].IsPlayer == true && allBattlers[i].CurrHealth > 0)
            partyMembers.Add(i);
    if (partyMembers.Count == 0) return -1;
    return partyMembers[Random.Range(0, partyMembers.Count)];
}

private int GetRandomEnemy()
{
    List<int> enemies = new List<int>();
    for (int i = 0; i < allBattlers.Count; i++)
        if (allBattlers[i].IsPlayer == false && allBattlers[i].CurrHealth > 0)
            enemies.Add(i);
    if (enemies.Count == 0) return -1;
    return enemies[Random.Range(0, enemies.Count)];
}
```

Caller audit (AttackRoutine):
- L131 `if (allBattlers[currAttacker.Target].CurrHealth <= 0) { currAttacker.SetTarget(GetRandomEnemy()); }` — kita bungkus: kalau `GetRandomEnemy()` return -1, skip attack (battle seharusnya sudah Won/Lost di branch lain).
- L168 `currAttacker.SetTarget(GetRandomPartyMember());` lalu langsung L169 `allBattlers[currAttacker.Target]` — kalau `-1`, crash. Guard: `if (target < 0) { ... }`.

Untuk fix minimalis yang aman: tambah guard di caller AttackRoutine:
```csharp
int targetIndex = GetRandomEnemy();
if (targetIndex < 0) yield break;
currAttacker.SetTarget(targetIndex);
```

**Phase 6:** Commit msg:
> `BattleSystem.GetRandom{PartyMember,Enemy}: return -1 on empty; AttackRoutine guards -1 target`.

Rekomendasi: extract rule "Battle entities target selection" ke helper class dengan pure function signature (return int atau throw known exception). EditMode testable.
