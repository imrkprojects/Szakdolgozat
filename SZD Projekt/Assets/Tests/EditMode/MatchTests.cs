using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class MatchTests
{
    private readonly List<Object> _created = new List<Object>();
    private Match _match;

    [SetUp]
    public void SetUp() => _match = new Match(5, 5);

    [TearDown]
    public void TearDown()
    {
        foreach (Object obj in _created) Object.DestroyImmediate(obj);
        _created.Clear();
    }

    private UnitState AddUnit(PlayerID owner, int x, int y, params AbilityData[] abilities)
    {
        var unit = new UnitState(owner, 10, abilities);
        _match.AddUnit(unit, new Vector2Int(x, y));
        return unit;
    }

    // Range-1 diamond ability that deals 4 damage; ScriptableObjects are destroyed in TearDown
    private AbilityData Strike()
    {
        var rule = ScriptableObject.CreateInstance<DiamondTargetingRule>();
        var damage = ScriptableObject.CreateInstance<DamageEffect>();
        var ability = ScriptableObject.CreateInstance<AbilityData>();
        _created.AddRange(new Object[] { rule, damage, ability });

        var ruleSo = new SerializedObject(rule);
        ruleSo.FindProperty("range").intValue = 1;
        ruleSo.ApplyModifiedPropertiesWithoutUndo();
        var damageSo = new SerializedObject(damage);
        damageSo.FindProperty("damage").intValue = 4;
        damageSo.ApplyModifiedPropertiesWithoutUndo();

        ability.targetingRule = rule;
        ability.effects = new AbilityEffect[] { damage };
        return ability;
    }

    // ─── Setup and death ─────────────────────────────────────────────────────

    [Test]
    public void New_CreatesBoardOfGivenSize()
    {
        Assert.AreEqual(5, _match.Board.Width);
        Assert.AreEqual(5, _match.Board.Height);
    }

    [Test]
    public void New_CreatesBothPlayersWithZeroScore()
    {
        Assert.AreEqual(PlayerID.Player1, _match.Player1.ID);
        Assert.AreEqual(PlayerID.Player2, _match.Player2.ID);
        Assert.AreEqual(0, _match.Player1.Score);
        Assert.AreEqual(0, _match.Player2.Score);
    }

    [Test]
    public void GetPlayer_ReturnsPlayerById_AndNullForNone()
    {
        Assert.AreSame(_match.Player1, _match.GetPlayer(PlayerID.Player1));
        Assert.AreSame(_match.Player2, _match.GetPlayer(PlayerID.Player2));
        Assert.IsNull(_match.GetPlayer(PlayerID.None));
    }

    [Test]
    public void AddUnit_PlacesUnitOnBoard()
    {
        UnitState unit = AddUnit(PlayerID.Player1, 2, 3);

        Assert.AreSame(unit, _match.Board.GetUnitAt(new Vector2Int(2, 3)));
    }

    [Test]
    public void UnitDies_IsRemovedFromBoard()
    {
        UnitState unit = AddUnit(PlayerID.Player1, 1, 1);

        unit.TakeDamage(10);

        Assert.IsNull(_match.Board.GetUnitAt(new Vector2Int(1, 1)));
    }

    [Test]
    public void UnitDies_IsSkippedInTurnOrder()
    {
        AddUnit(PlayerID.Player1, 0, 0);
        UnitState p1B = AddUnit(PlayerID.Player1, 1, 0);
        UnitState p1C = AddUnit(PlayerID.Player1, 2, 0);
        AddUnit(PlayerID.Player2, 0, 4);
        _match.Start();

        p1B.TakeDamage(10);
        _match.EndTurn(); // P2
        _match.EndTurn();

        Assert.AreSame(p1C, _match.Turns.ActiveUnit);
    }

    [Test]
    public void LastEnemyDies_EndTurnEndsMatchWithActingPlayerAsWinner()
    {
        AddUnit(PlayerID.Player1, 0, 0);
        UnitState enemy = AddUnit(PlayerID.Player2, 0, 4);
        PlayerID? winner = null;
        _match.Turns.GameOver += w => winner = w;
        _match.Start();

        enemy.TakeDamage(10);
        _match.EndTurn();

        Assert.IsTrue(_match.Turns.IsGameOver);
        Assert.AreEqual(PlayerID.Player1, winner);
    }

    // ─── Movement ────────────────────────────────────────────────────────────

    [Test]
    public void GetMoveTargets_ReturnsFreeOrthogonalNeighbours()
    {
        UnitState unit = AddUnit(PlayerID.Player1, 0, 0);
        AddUnit(PlayerID.Player2, 1, 0);

        CollectionAssert.AreEquivalent(new[] { new Vector2Int(0, 1) }, _match.GetMoveTargets(unit));
    }

    [Test]
    public void TryMove_ActiveUnitToFreeNeighbour_MovesAndDoesNotEndTurn()
    {
        UnitState unit = AddUnit(PlayerID.Player1, 0, 0);
        AddUnit(PlayerID.Player2, 4, 4);
        _match.Start();

        Assert.IsTrue(_match.TryMove(unit, new Vector2Int(0, 1)));
        Assert.AreEqual(new Vector2Int(0, 1), unit.Position);
        Assert.AreSame(unit, _match.Turns.ActiveUnit);
    }

    [Test]
    public void TryMove_IllegalCell_Rejected()
    {
        UnitState unit = AddUnit(PlayerID.Player1, 0, 0);
        AddUnit(PlayerID.Player2, 4, 4);
        _match.Start();

        Assert.IsFalse(_match.TryMove(unit, new Vector2Int(1, 1)));
        Assert.AreEqual(Vector2Int.zero, unit.Position);
    }

    [Test]
    public void TryMove_NotActiveUnit_Rejected()
    {
        AddUnit(PlayerID.Player1, 0, 0);
        UnitState enemy = AddUnit(PlayerID.Player2, 4, 4);
        _match.Start();

        Assert.IsFalse(_match.TryMove(enemy, new Vector2Int(4, 3)));
    }

    [Test]
    public void TryMove_AfterGameOver_Rejected()
    {
        UnitState unit = AddUnit(PlayerID.Player1, 0, 0);
        UnitState enemy = AddUnit(PlayerID.Player2, 4, 4);
        _match.Start();
        enemy.TakeDamage(10);
        _match.EndTurn();

        Assert.IsFalse(_match.TryMove(unit, new Vector2Int(0, 1)));
    }

    // ─── Abilities ───────────────────────────────────────────────────────────

    [Test]
    public void TryUseAbility_EnemyInRange_AppliesEffects()
    {
        AbilityData strike = Strike();
        UnitState caster = AddUnit(PlayerID.Player1, 2, 2, strike);
        UnitState enemy = AddUnit(PlayerID.Player2, 2, 3);
        _match.Start();

        Assert.IsTrue(_match.TryUseAbility(caster, strike, enemy.Position));
        Assert.AreEqual(6, enemy.Hp);
        Assert.AreSame(caster, _match.Turns.ActiveUnit);
    }

    [Test]
    public void TryUseAbility_EmptyOrFriendlyCellInRange_Rejected()
    {
        AbilityData strike = Strike();
        UnitState caster = AddUnit(PlayerID.Player1, 2, 2, strike);
        UnitState ally = AddUnit(PlayerID.Player1, 2, 3);
        AddUnit(PlayerID.Player2, 4, 4);
        _match.Start();

        Assert.IsFalse(_match.TryUseAbility(caster, strike, ally.Position));
        Assert.IsFalse(_match.TryUseAbility(caster, strike, new Vector2Int(1, 2)));
        Assert.AreEqual(10, ally.Hp);
    }

    [Test]
    public void TryUseAbility_EnemyOutOfRange_Rejected()
    {
        AbilityData strike = Strike();
        UnitState caster = AddUnit(PlayerID.Player1, 0, 0, strike);
        UnitState enemy = AddUnit(PlayerID.Player2, 4, 4);
        _match.Start();

        Assert.IsFalse(_match.TryUseAbility(caster, strike, enemy.Position));
        Assert.AreEqual(10, enemy.Hp);
    }

    [Test]
    public void TryUseAbility_AbilityNotOwned_Rejected()
    {
        AbilityData strike = Strike();
        UnitState caster = AddUnit(PlayerID.Player1, 2, 2);
        UnitState enemy = AddUnit(PlayerID.Player2, 2, 3);
        _match.Start();

        Assert.IsFalse(_match.TryUseAbility(caster, strike, enemy.Position));
        Assert.AreEqual(10, enemy.Hp);
    }

    [Test]
    public void TryUseAbility_NotActiveUnit_Rejected()
    {
        AbilityData strike = Strike();
        UnitState target = AddUnit(PlayerID.Player1, 2, 2);
        UnitState enemy = AddUnit(PlayerID.Player2, 2, 3, strike);
        _match.Start();

        Assert.IsFalse(_match.TryUseAbility(enemy, strike, target.Position));
        Assert.AreEqual(10, target.Hp);
    }
}