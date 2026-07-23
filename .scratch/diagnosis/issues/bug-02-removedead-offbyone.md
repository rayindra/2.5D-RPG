# BUG #2 — `BattleSystem.RemoveDeadBattlers` off-by-one

**Severity:** HIGH
**File:** `Assets/Scripts/BattleSystem.cs:342-348`
**Blocks:** BUG #4 (turn flow bersih butuh cleanup daftar dead yang konsisten)
**Test mode:** EditMode (private method → reflection invoke)

## Root cause

```csharp
private void RemoveDeadBattlers()
{
    for (int i = 0; i < allBattlers.Count; i++)
    {
        if (allBattlers[i].CurrHealth <= 0)
            allBattlers.RemoveAt(i);   // turun ke i, i++ → skip elem berikutnya
    }
}
```

Forward iteration dengan `RemoveAt(i)` saat ini = skip elemen yang turun ke indeks `i`. Kalau ada 2 dead berturut-turut, yang kedua tidak ter-remove.

## Repro test

```csharp
[Test]
public void RemoveDeadBattlers_DoubleDead_SkipsSecond()
{
    var go = new GameObject();
    var bs = go.AddComponent<BattleSystem>();
    var battlers = InjectField<List<BattleEntities>>(bs, "allBattlers");
    battlers.Add(MakeBattler(10));
    battlers.Add(MakeBattler(0));
    battlers.Add(MakeBattler(0));   // 2 dead berturut-turut

    InvokeMethod(bs, "RemoveDeadBattlers");

    Assert.AreEqual(1, battlers.Count, "hanya elem pertama yg hidup yg harus tersisa; off-by-one membuat 1 dead lolos");
}
```

## Plan

**Phase 3:** H1 (winner) iterasi mundur adalah fix standard.
**Phase 5:** Fix:

```csharp
for (int i = allBattlers.Count - 1; i >= 0; i--)
    if (allBattlers[i].CurrHealth <= 0)
        allBattlers.RemoveAt(i);
```

**Phase 6:** Commit msg:
> `BattleSystem.RemoveDeadBattlers: iterate backward to avoid RemoveAt skip`.

Rekomendasi: audit `PartyMenuController.cs:147, :218` — kalau juga forward-loop-remove, apply pattern yang sama.
