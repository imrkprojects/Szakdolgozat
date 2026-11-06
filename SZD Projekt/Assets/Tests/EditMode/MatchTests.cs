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

    // Starts the match and commits both players' units in the order they were added
    private void StartAndCommit()
    {
        _match.Start();
        _match.Turns.CommitPlan(PlayerID.Player1, _match.Turns.GetLivingUnits(PlayerID.Player1));
        _match.Turns.CommitPlan(PlayerID.Player2, _match.Turns.GetLivingUnits(PlayerID.Player2));
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

    // Adds a wall (blocks movement) to the match; ScriptableObjects are destroyed in TearDown
    private StructureState AddWall(int x, int y, bool destructible = false, int hp = 10)
    {
        var data = ScriptableObject.CreateInstance<StructureData>();
        _created.Add(data);
        data.destructible = destructible;
        data.maxHp = hp;
        StructureState wall = data.CreateState();
        _match.AddStructure(wall, new Vector2Int(x, y));
        return wall;
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
        StartAndCommit();

        p1B.TakeDamage(10);
        _match.EndTurn(); // P2's unit
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
        StartAndCommit();

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
    public void AddStructure_PlacesStructureOnBoard()
    {
        StructureState wall = AddWall(2, 3);

        Assert.AreSame(wall, _match.Board.GetStructureAt(new Vector2Int(2, 3)));
    }

    [Test]
    public void GetMoveTargets_ExcludesWalls()
    {
        UnitState unit = AddUnit(PlayerID.Player1, 0, 0);
        AddWall(1, 0);

        CollectionAssert.AreEquivalent(new[] { new Vector2Int(0, 1) }, _match.GetMoveTargets(unit));
    }

    [Test]
    public void TryMove_ActiveUnitToFreeNeighbour_MovesAndDoesNotEndTurn()
    {
        UnitState unit = AddUnit(PlayerID.Player1, 0, 0);
        AddUnit(PlayerID.Player2, 4, 4);
        StartAndCommit();

        Assert.IsTrue(_match.TryMove(unit, new Vector2Int(0, 1), out _));
        Assert.AreEqual(new Vector2Int(0, 1), unit.Position);
        Assert.AreSame(unit, _match.Turns.ActiveUnit);
    }

    [Test]
    public void TryMove_IllegalCell_Rejected()
    {
        UnitState unit = AddUnit(PlayerID.Player1, 0, 0);
        AddUnit(PlayerID.Player2, 4, 4);
        StartAndCommit();

        Assert.IsFalse(_match.TryMove(unit, new Vector2Int(1, 1), out _));
        Assert.AreEqual(Vector2Int.zero, unit.Position);
    }

    [Test]
    public void TryMove_WithinMoveRange_MovesAndReturnsPath()
    {
        var unit = new UnitState(PlayerID.Player1, 10, moveRange: 2);
        _match.AddUnit(unit, Vector2Int.zero);
        AddUnit(PlayerID.Player2, 4, 4);
        StartAndCommit();

        Assert.IsTrue(_match.TryMove(unit, new Vector2Int(1, 1), out List<Vector2Int> path));
        Assert.AreEqual(new Vector2Int(1, 1), unit.Position);
        Assert.AreEqual(2, path.Count);
        Assert.AreEqual(new Vector2Int(1, 1), path[1]);
    }

    [Test]
    public void TryMove_BeyondMoveRange_Rejected()
    {
        var unit = new UnitState(PlayerID.Player1, 10, moveRange: 2);
        _match.AddUnit(unit, Vector2Int.zero);
        AddUnit(PlayerID.Player2, 4, 4);
        StartAndCommit();

        Assert.IsFalse(_match.TryMove(unit, new Vector2Int(2, 1), out List<Vector2Int> path));
        Assert.IsNull(path);
        Assert.AreEqual(Vector2Int.zero, unit.Position);
    }

    [Test]
    public void GetMoveTargets_UsesUnitMoveRange()
    {
        var unit = new UnitState(PlayerID.Player1, 10, moveRange: 2);
        _match.AddUnit(unit, Vector2Int.zero);

        Assert.AreEqual(5, _match.GetMoveTargets(unit).Count);
    }

    [Test]
    public void TryMove_NotActiveUnit_Rejected()
    {
        AddUnit(PlayerID.Player1, 0, 0);
        UnitState enemy = AddUnit(PlayerID.Player2, 4, 4);
        StartAndCommit();

        Assert.IsFalse(_match.TryMove(enemy, new Vector2Int(4, 3), out _));
    }

    [Test]
    public void TryMove_SecondActionBeforeEndTurn_Rejected()
    {
        UnitState unit = AddUnit(PlayerID.Player1, 0, 0);
        UnitState enemy = AddUnit(PlayerID.Player2, 4, 4);
        StartAndCommit();

        Assert.IsTrue(_match.TryMove(unit, new Vector2Int(0, 1), out _));
        Assert.IsFalse(_match.TryMove(unit, new Vector2Int(0, 2), out _));
        Assert.AreEqual(new Vector2Int(0, 1), unit.Position);
    }

    [Test]
    public void TryMove_AfterGameOver_Rejected()
    {
        UnitState unit = AddUnit(PlayerID.Player1, 0, 0);
        UnitState enemy = AddUnit(PlayerID.Player2, 4, 4);
        StartAndCommit();
        enemy.TakeDamage(10);
        _match.EndTurn();

        Assert.IsFalse(_match.TryMove(unit, new Vector2Int(0, 1), out _));
    }

    // ─── Abilities ───────────────────────────────────────────────────────────

    [Test]
    public void TryUseAbility_EnemyInRange_AppliesEffects()
    {
        AbilityData strike = Strike();
        UnitState caster = AddUnit(PlayerID.Player1, 2, 2, strike);
        UnitState enemy = AddUnit(PlayerID.Player2, 2, 3);
        StartAndCommit();

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
        StartAndCommit();

        Assert.IsFalse(_match.TryUseAbility(caster, strike, ally.Position));
        Assert.IsFalse(_match.TryUseAbility(caster, strike, new Vector2Int(1, 2)));
        Assert.AreEqual(10, ally.Hp);
    }

    [Test]
    public void TryUseAbility_AllyFilter_AcceptsAllyAndRejectsEnemy()
    {
        AbilityData support = Strike();
        support.targetFilter = TargetFilter.Ally;
        UnitState caster = AddUnit(PlayerID.Player1, 2, 2, support);
        UnitState ally = AddUnit(PlayerID.Player1, 2, 3);
        UnitState enemy = AddUnit(PlayerID.Player2, 2, 1);
        StartAndCommit();

        Assert.IsFalse(_match.TryUseAbility(caster, support, enemy.Position));
        Assert.IsTrue(_match.TryUseAbility(caster, support, ally.Position));
    }

    [Test]
    public void TryUseAbility_EmptyCellFilter_AcceptsEmptyCell()
    {
        AbilityData blink = Strike();
        blink.targetFilter = TargetFilter.EmptyCell;
        UnitState caster = AddUnit(PlayerID.Player1, 2, 2, blink);
        AddUnit(PlayerID.Player2, 4, 4);
        StartAndCommit();

        Assert.IsTrue(_match.TryUseAbility(caster, blink, new Vector2Int(1, 2)));
    }

    [Test]
    public void TryUseAbility_DestructibleWall_LowersItsHp()
    {
        AbilityData strike = Strike();
        UnitState caster = AddUnit(PlayerID.Player1, 2, 2, strike);
        AddUnit(PlayerID.Player2, 4, 4);
        StructureState wall = AddWall(2, 3, destructible: true);
        StartAndCommit();

        Assert.IsTrue(_match.TryUseAbility(caster, strike, wall.Position));
        Assert.AreEqual(6, wall.Hp);
        Assert.AreSame(wall, _match.Board.GetStructureAt(wall.Position));
    }

    [Test]
    public void TryUseAbility_LethalOnWall_RemovesIt_AndCellBecomesWalkable()
    {
        AbilityData strike = Strike();
        UnitState caster = AddUnit(PlayerID.Player1, 2, 2, strike);
        AddUnit(PlayerID.Player2, 4, 4);
        StructureState wall = AddWall(2, 3, destructible: true, hp: 4);
        StartAndCommit();

        Assert.IsTrue(_match.TryUseAbility(caster, strike, wall.Position));
        Assert.IsTrue(wall.IsDestroyed);
        Assert.IsNull(_match.Board.GetStructureAt(new Vector2Int(2, 3)));
        Assert.IsTrue(_match.Board.IsWalkable(new Vector2Int(2, 3)));
    }

    [Test]
    public void TryUseAbility_IndestructibleWall_Rejected_AndTurnNotStarted()
    {
        AbilityData strike = Strike();
        UnitState caster = AddUnit(PlayerID.Player1, 2, 2, strike);
        AddUnit(PlayerID.Player2, 4, 4);
        StructureState wall = AddWall(2, 3);
        StartAndCommit();

        Assert.IsFalse(_match.TryUseAbility(caster, strike, wall.Position));
        Assert.IsFalse(_match.Turns.HasActiveUnitStarted);
        Assert.AreEqual(10, wall.Hp);
    }

    [Test]
    public void TryUseAbility_EnemyOutOfRange_Rejected()
    {
        AbilityData strike = Strike();
        UnitState caster = AddUnit(PlayerID.Player1, 0, 0, strike);
        UnitState enemy = AddUnit(PlayerID.Player2, 4, 4);
        StartAndCommit();

        Assert.IsFalse(_match.TryUseAbility(caster, strike, enemy.Position));
        Assert.AreEqual(10, enemy.Hp);
    }

    [Test]
    public void TryUseAbility_AbilityNotOwned_Rejected()
    {
        AbilityData strike = Strike();
        UnitState caster = AddUnit(PlayerID.Player1, 2, 2);
        UnitState enemy = AddUnit(PlayerID.Player2, 2, 3);
        StartAndCommit();

        Assert.IsFalse(_match.TryUseAbility(caster, strike, enemy.Position));
        Assert.AreEqual(10, enemy.Hp);
    }

    [Test]
    public void CanUseAbility_ActiveUnitOwnedAbility_True()
    {
        AbilityData strike = Strike();
        UnitState caster = AddUnit(PlayerID.Player1, 2, 2, strike);
        AddUnit(PlayerID.Player2, 4, 4);
        StartAndCommit();

        Assert.IsTrue(_match.CanUseAbility(caster, strike));
    }

    [Test]
    public void CanUseAbility_NotActiveNotOwnedOrNull_False()
    {
        AbilityData strike = Strike();
        UnitState caster = AddUnit(PlayerID.Player1, 2, 2);
        UnitState enemy = AddUnit(PlayerID.Player2, 4, 4, strike);
        StartAndCommit();

        Assert.IsFalse(_match.CanUseAbility(caster, strike), "ability not owned");
        Assert.IsFalse(_match.CanUseAbility(enemy, strike), "not the active unit");
        Assert.IsFalse(_match.CanUseAbility(caster, null), "no ability");
    }

    [Test]
    public void CanUseAbility_AfterActing_False()
    {
        AbilityData strike = Strike();
        UnitState caster = AddUnit(PlayerID.Player1, 0, 0, strike);
        AddUnit(PlayerID.Player2, 4, 4);
        StartAndCommit();

        Assert.IsTrue(_match.TryMove(caster, new Vector2Int(0, 1), out _));

        Assert.IsFalse(_match.CanUseAbility(caster, strike));
    }

    // ─── Cooldowns ───────────────────────────────────────────────────────────

    [Test]
    public void Cooldown_BlocksForNextNOwnTurns_ThenReady()
    {
        AbilityData strike = Strike();
        strike.cooldown = 2;
        UnitState caster = AddUnit(PlayerID.Player1, 2, 2, strike);
        UnitState enemy = AddUnit(PlayerID.Player2, 2, 3);
        StartAndCommit();

        Assert.IsTrue(_match.TryUseAbility(caster, strike, enemy.Position));
        _match.EndTurn(); // caster's turn
        _match.EndTurn(); // enemy's turn

        Assert.AreSame(caster, _match.Turns.ActiveUnit);
        Assert.AreEqual(2, caster.GetCooldown(strike));
        Assert.IsFalse(_match.CanUseAbility(caster, strike), "1st own turn after use");
        Assert.IsFalse(_match.TryUseAbility(caster, strike, enemy.Position));
        _match.EndTurn();
        _match.EndTurn();

        Assert.AreEqual(1, caster.GetCooldown(strike));
        Assert.IsFalse(_match.CanUseAbility(caster, strike), "2nd own turn after use");
        _match.EndTurn();
        _match.EndTurn();

        Assert.AreEqual(0, caster.GetCooldown(strike));
        Assert.IsTrue(_match.CanUseAbility(caster, strike), "3rd own turn after use");
    }

    [Test]
    public void Cooldown_OtherUnitsTurns_DoNotTickIt()
    {
        AbilityData strike = Strike();
        strike.cooldown = 2;
        UnitState caster = AddUnit(PlayerID.Player1, 2, 2, strike);
        UnitState ally = AddUnit(PlayerID.Player1, 0, 0);
        UnitState enemy = AddUnit(PlayerID.Player2, 2, 3);
        StartAndCommit();

        Assert.IsTrue(_match.TryUseAbility(caster, strike, enemy.Position));
        _match.EndTurn(); // caster
        _match.EndTurn(); // enemy
        Assert.AreSame(ally, _match.Turns.ActiveUnit);
        _match.EndTurn(); // ally
        _match.EndTurn(); // enemy

        Assert.AreSame(caster, _match.Turns.ActiveUnit);
        Assert.AreEqual(2, caster.GetCooldown(strike));
    }

    [Test]
    public void Cooldown_Zero_NeverBlocks()
    {
        AbilityData strike = Strike();
        UnitState caster = AddUnit(PlayerID.Player1, 2, 2, strike);
        UnitState enemy = AddUnit(PlayerID.Player2, 2, 3);
        StartAndCommit();

        Assert.IsTrue(_match.TryUseAbility(caster, strike, enemy.Position));
        _match.EndTurn();
        _match.EndTurn();

        Assert.AreEqual(0, caster.GetCooldown(strike));
        Assert.IsTrue(_match.TryUseAbility(caster, strike, enemy.Position));
    }

    [Test]
    public void Cooldown_ActiveUnitDiesDuringItsTurn_EndTurnStillWorks()
    {
        AbilityData strike = Strike();
        strike.cooldown = 1;
        UnitState caster = AddUnit(PlayerID.Player1, 2, 2, strike);
        AddUnit(PlayerID.Player1, 0, 0); // keeps Player 1 alive so the match goes on
        UnitState enemy = AddUnit(PlayerID.Player2, 2, 3);
        StartAndCommit();

        Assert.IsTrue(_match.TryUseAbility(caster, strike, enemy.Position));
        caster.TakeDamage(10);

        Assert.DoesNotThrow(() => _match.EndTurn());
        Assert.AreSame(enemy, _match.Turns.ActiveUnit);
    }

    [Test]
    public void TryUseAbility_NotActiveUnit_Rejected()
    {
        AbilityData strike = Strike();
        UnitState target = AddUnit(PlayerID.Player1, 2, 2);
        UnitState enemy = AddUnit(PlayerID.Player2, 2, 3, strike);
        StartAndCommit();

        Assert.IsFalse(_match.TryUseAbility(enemy, strike, target.Position));
        Assert.AreEqual(10, target.Hp);
    }
}