using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class AbilityTests
{
    private readonly List<Object> _created = new List<Object>();
    private Match _match;

    [SetUp]
    public void SetUp() => _match = new Match(9, 9);

    [TearDown]
    public void TearDown()
    {
        foreach (Object obj in _created) Object.DestroyImmediate(obj);
        _created.Clear();
    }

    // ScriptableObjects must be destroyed by hand in EditMode, so every one goes through here
    private T Create<T>() where T : ScriptableObject
    {
        var obj = ScriptableObject.CreateInstance<T>();
        _created.Add(obj);
        return obj;
    }

    // Sets a private [SerializeField] the way the Inspector would
    private static void SetField(Object target, string field, int value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private UnitState AddUnit(PlayerID owner, int x, int y, int hp = 30)
    {
        var unit = new UnitState(owner, hp);
        _match.AddUnit(unit, new Vector2Int(x, y));
        return unit;
    }

    private DamageEffect Damage(int amount)
    {
        var effect = Create<DamageEffect>();
        SetField(effect, "damage", amount);
        return effect;
    }

    private AbilityData Ability(TargetingRule rule, params AbilityEffect[] effects)
    {
        var ability = Create<AbilityData>();
        ability.targetingRule = rule;
        ability.effects = effects;
        return ability;
    }

    // ─── Targeting ───────────────────────────────────────────────────────────

    [Test]
    public void DiamondTargeting_ReturnsDiamondAroundCaster_WithoutCasterCell()
    {
        UnitState caster = AddUnit(PlayerID.Player1, 4, 4);
        var rule = Create<DiamondTargetingRule>();
        SetField(rule, "range", 1);

        CollectionAssert.AreEquivalent(
            new[] { new Vector2Int(4, 5), new Vector2Int(4, 3), new Vector2Int(3, 4), new Vector2Int(5, 4) },
            rule.GetTargetablePositions(_match.Board, caster));
    }

    [Test]
    public void DiamondTargeting_FollowsCasterAfterMove()
    {
        UnitState caster = AddUnit(PlayerID.Player1, 4, 4);
        var rule = Create<DiamondTargetingRule>();
        SetField(rule, "range", 1);

        _match.Board.Place(caster, new Vector2Int(0, 0));

        CollectionAssert.AreEquivalent(
            new[] { new Vector2Int(1, 0), new Vector2Int(0, 1) },
            rule.GetTargetablePositions(_match.Board, caster));
    }

    [Test]
    public void GetTargetablePositions_NoRule_ReturnsEmpty()
    {
        UnitState caster = AddUnit(PlayerID.Player1, 4, 4);
        AbilityData ability = Ability(null);

        UnityEngine.TestTools.LogAssert.Expect(LogType.Warning,
            "Ability 'Unnamed Ability' has no TargetingRule assigned.");
        Assert.IsEmpty(ability.GetTargetablePositions(_match.Board, caster));
    }

    // ─── Damage ──────────────────────────────────────────────────────────────

    [Test]
    public void Damage_HitsEnemyOnTargetCell()
    {
        UnitState caster = AddUnit(PlayerID.Player1, 4, 4);
        UnitState enemy = AddUnit(PlayerID.Player2, 4, 5);

        Ability(null, Damage(10)).Execute(_match, caster, new[] { enemy.Position });

        Assert.AreEqual(20, enemy.Hp);
    }

    [Test]
    public void Damage_IgnoresAlliesAndEmptyCells()
    {
        UnitState caster = AddUnit(PlayerID.Player1, 4, 4);
        UnitState ally = AddUnit(PlayerID.Player1, 4, 5);

        Ability(null, Damage(10)).Execute(_match, caster,
            new[] { caster.Position, ally.Position, new Vector2Int(0, 0) });

        Assert.AreEqual(30, caster.Hp);
        Assert.AreEqual(30, ally.Hp);
    }

    [Test]
    public void Damage_OnlyHitsGivenTargets()
    {
        UnitState caster = AddUnit(PlayerID.Player1, 4, 4);
        UnitState hit = AddUnit(PlayerID.Player2, 4, 5);
        UnitState missed = AddUnit(PlayerID.Player2, 5, 4);

        Ability(null, Damage(10)).Execute(_match, caster, new[] { hit.Position });

        Assert.AreEqual(20, hit.Hp);
        Assert.AreEqual(30, missed.Hp);
    }

    [Test]
    public void Execute_AppliesEffectsInOrder_AndSkipsNullEffects()
    {
        UnitState caster = AddUnit(PlayerID.Player1, 4, 4);
        UnitState enemy = AddUnit(PlayerID.Player2, 4, 5);

        Ability(null, Damage(5), null, Damage(7)).Execute(_match, caster, new[] { enemy.Position });

        Assert.AreEqual(18, enemy.Hp);
    }

    [Test]
    public void LethalDamage_RemovesEnemyFromBoard_AndEndsMatch()
    {
        UnitState caster = AddUnit(PlayerID.Player1, 4, 4);
        UnitState enemy = AddUnit(PlayerID.Player2, 4, 5, hp: 10);
        _match.Start();

        Ability(null, Damage(10)).Execute(_match, caster, new[] { enemy.Position });
        _match.EndTurn();

        Assert.IsFalse(enemy.IsAlive);
        Assert.IsNull(_match.Board.GetUnitAt(new Vector2Int(4, 5)));
        Assert.IsTrue(_match.Turns.IsGameOver);
    }
}