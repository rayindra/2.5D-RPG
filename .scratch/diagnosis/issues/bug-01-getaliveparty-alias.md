# BUG #1 — `PartyManager.GetAliveParty` alias + off-by-one

**Severity:** HIGH (CRITICAL — bisa menghilangkan party member permanen)
**File:** `Assets/Scripts/PartyManager.cs:79-87`
**Blocks:** BUG #4 (BattleSystem.CreatePartyEntities:354 pakai corrupted roster)
**Test mode:** EditMode (pure-logic, reflection-inject)

## Root cause

Dua bug dalam satu method:

```csharp
public List<PartyMember> GetAliveParty()
{
    List<PartyMember> aliveParty = new List<PartyMember>();
    aliveParty = currentParty;                              // ← ALIAS, bukan copy
    for (int i = 0; i < aliveParty.Count; i++)
        if (aliveParty[i].CurrHealth <= 0)
            aliveParty.RemoveAt(i);                         // ← forward-iter + RemoveAt = skip
    return aliveParty;
}
```

1. **`aliveParty = currentParty` = alias.** `List<T>` reference type. `RemoveAt` memodifikasi `currentParty` permanen. Member HP<=0 hilang selamanya dari roster.
2. **Forward loop + `RemoveAt(i)`** = classic skip. Saat elemen di-remove, elemen berikutnya turun ke `i`, lalu `i++` loncat ke `i+2`. 2 dead berturut-turut → salah satu lolos.

Repro: party [A=10, B=0, C=-5]. Expected `GetAliveParty()=[A]` DAN `currentParty.Count` tetap 3 (B, C masih ada, HP 0 / negative). Actual: `GetAliveParty()=[A]` dan `currentParty.Count=1` (C hilang dari alias, B mungkin lolos karena off-by-one).

## Repro test (akan FAIL sebelum fix)

```csharp
[Test]
public void GetAliveParty_AliasedRemove_CorruptsPermanentRoster()
{
    var mgr = new GameObject().AddComponent<PartyManager>();
    var party = InjectField<List<PartyMember>>(mgr, "currentParty");
    var dead1 = MakeMember("dead1", 0);
    var dead2 = MakeMember("dead2", -5);
    var alive = MakeMember("alive", 10);
    party.Add(dead1); party.Add(dead2); party.Add(alive);

    var result = mgr.GetAliveParty();

    Assert.AreEqual(3, party.Count, "currentParty harus tetap 3 setelah GetAliveParty (alias bug)");
    Assert.AreEqual(1, result.Count, "hanya 'alive' yang harus kembali");
    Assert.AreEqual("alive", result[0].MemberName);
}
```

## Plan

**Phase 1:** Test di atas. EditMode Unity Test Runner. Red-capable (assert specific corruption).
**Phase 3:** Hypothesis:
- H1 (winner): `aliveParty = currentParty` adalah alias assignment.
- H2 (winner): Forward loop skip.
**Phase 5:** Fix — copy + iter mundur:

```csharp
public List<PartyMember> GetAliveParty()
{
    List<PartyMember> aliveParty = new List<PartyMember>(currentParty);
    for (int i = aliveParty.Count - 1; i >= 0; i--)
        if (aliveParty[i].CurrHealth <= 0)
            aliveParty.RemoveAt(i);
    return aliveParty;
}
```

**Phase 6:** Cleanup instrumentation, commit msg:
> `PartyManager.GetAliveParty: copy list before filter; iterate backward to avoid RemoveAt skip` — root cause: List<T> alias assignment mutates source.

Rekomendasi: `BattleSystem.CreatePartyEntities:354` masih aman setelah fix (no alias contamination). Tapi check `BattleSystem.CreatePartyEntities:354` — kalau call lain di masa depan, dokumentasikan rule "GetAliveParty() tidak mengubah currentParty" di comment method.
