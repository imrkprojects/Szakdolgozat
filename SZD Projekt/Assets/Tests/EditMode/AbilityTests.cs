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
        _match.Turns.CommitPlan(PlayerID.Player1, new[] { caster });
        _match.Turns.CommitPlan(PlayerID.Player2, new[] { enemy });

        Ability(null, Damage(10)).Execute(_match, caster, new[] { enemy.Position });
        _match.EndTurn();

        Assert.IsFalse(enemy.IsAlive);
        Assert.IsNull(_match.Board.GetUnitAt(new Vector2Int(4, 5)));
        Assert.IsTrue(_match.Turns.IsGameOver);
    }

    // ─── Target filters ──────────────────────────────────────────────────────

    // Caster at (4,4) with an ally, an enemy and an empty cell around it
    [TestCase(TargetFilter.Enemy, false, true, false, false)]
    [TestCase(TargetFilter.Ally, true, false, false, true)]
    [TestCase(TargetFilter.Self, false, false, false, true)]
    [TestCase(TargetFilter.AnyUnit, true, true, false, true)]
    [TestCase(TargetFilter.EmptyCell, false, false, true, false)]
    public void IsValidTarget_MatchesFilter(TargetFilter filter, bool ally, bool enemy, bool empty, bool self)
    {
        UnitState caster = AddUnit(PlayerID.Player1, 4, 4);
        AddUnit(PlayerID.Player1, 4, 5);
        AddUnit(PlayerID.Player2, 5, 4);
        AbilityData ability = Ability(null);
        ability.targetFilter = filter;

        Assert.AreEqual(ally, ability.IsValidTarget(_match.Board, caster, new Vector2Int(4, 5)), "ally");
        Assert.AreEqual(enemy, ability.IsValidTarget(_match.Board, caster, new Vector2Int(5, 4)), "enemy");
        Assert.AreEqual(empty, ability.IsValidTarget(_match.Board, caster, new Vector2Int(3, 4)), "empty");
        Assert.AreEqual(self, ability.IsValidTarget(_match.Board, caster, new Vector2Int(4, 4)), "self");
    }

    private StructureState AddStructure(int x, int y, bool blocksMovement, bool destructible)
    {
        var data = Create<StructureData>();
        data.blocksMovement = blocksMovement;
        data.destructible = destructible;
        StructureState structure = data.CreateState();
        _match.AddStructure(structure, new Vector2Int(x, y));
        return structure;
    }

    [Test]
    public void IsValidTarget_Enemy_AcceptsDestructibleWall_RejectsIndestructible()
    {
        UnitState caster = AddUnit(PlayerID.Player1, 4, 4);
        AddStructure(4, 5, blocksMovement: true, destructible: true);
        AddStructure(5, 4, blocksMovement: true, destructible: false);
        AbilityData ability = Ability(null);

        Assert.IsTrue(ability.IsValidTarget(_match.Board, caster, new Vector2Int(4, 5)), "destructible");
        Assert.IsFalse(ability.IsValidTarget(_match.Board, caster, new Vector2Int(5, 4)), "indestructible");
    }

    [Test]
    public void IsValidTarget_EmptyCell_RejectsWall_AcceptsEmptyBush()
    {
        UnitState caster = AddUnit(PlayerID.Player1, 4, 4);
        AddStructure(4, 5, blocksMovement: true, destructible: false);
        AddStructure(5, 4, blocksMovement: false, destructible: false);
        AbilityData ability = Ability(null);
        ability.targetFilter = TargetFilter.EmptyCell;

        Assert.IsFalse(ability.IsValidTarget(_match.Board, caster, new Vector2Int(4, 5)), "wall");
        Assert.IsTrue(ability.IsValidTarget(_match.Board, caster, new Vector2Int(5, 4)), "bush");
    }

    [Test]
    public void Damage_HitsDestructibleWall_LeavesIndestructibleAtFullHp()
    {
        UnitState caster = AddUnit(PlayerID.Player1, 4, 4);
        StructureState destructible = AddStructure(4, 5, blocksMovement: true, destructible: true);
        StructureState indestructible = AddStructure(5, 4, blocksMovement: true, destructible: false);

        Ability(null, Damage(3)).Execute(_match, caster, new[] { destructible.Position, indestructible.Position });

        Assert.AreEqual(7, destructible.Hp);
        Assert.AreEqual(10, indestructible.Hp);
    }

    [Test]
    public void TargetFilter_DefaultsToEnemy()
    {
        Assert.AreEqual(TargetFilter.Enemy, Create<AbilityData>().targetFilter);
    }

    // ─── Heal ────────────────────────────────────────────────────────────────

    private HealEffect Heal(int amount)
    {
        var effect = Create<HealEffect>();
        SetField(effect, "amount", amount);
        return effect;
    }

    [Test]
    public void Heal_RestoresAlliesAndCaster_UpToMaxHp()
    {
        UnitState caster = AddUnit(PlayerID.Player1, 4, 4);
        UnitState ally = AddUnit(PlayerID.Player1, 4, 5);
        caster.TakeDamage(5);
        ally.TakeDamage(20);

        Heal(8).Apply(_match, caster, new[] { new Vector2Int(4, 4), new Vector2Int(4, 5) });

        Assert.AreEqual(30, caster.Hp, "clamped to MaxHp");
        Assert.AreEqual(18, ally.Hp);
    }

    [Test]
    public void Heal_IgnoresEnemiesAndEmptyCells()
    {
        UnitState caster = AddUnit(PlayerID.Player1, 4, 4);
        UnitState enemy = AddUnit(PlayerID.Player2, 5, 4);
        enemy.TakeDamage(10);

        Assert.DoesNotThrow(() =>
            Heal(8).Apply(_match, caster, new[] { new Vector2Int(5, 4), new Vector2Int(3, 4) }));
        Assert.AreEqual(20, enemy.Hp);
    }
}