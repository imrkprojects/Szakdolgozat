using System.Collections.Generic;
using NUnit.Framework;

public class TurnCycleTests
{
    private TurnCycle _turns;
    private UnitState[] _p1;
    private UnitState[] _p2;

    [SetUp]
    public void SetUp()
    {
        _turns = new TurnCycle();
        _p1 = Register(PlayerID.Player1, 3);
        _p2 = Register(PlayerID.Player2, 3);
    }

    private UnitState[] Register(PlayerID owner, int count)
    {
        var units = new UnitState[count];
        for (int i = 0; i < count; i++)
        {
            units[i] = new UnitState(owner, 10);
            _turns.RegisterUnit(units[i]);
        }

        return units;
    }

    [Test]
    public void Start_GivesPlayer1FirstUnit_AndRaisesTurnChanged()
    {
        PlayerID? player = null;
        UnitState unit = null;
        _turns.TurnChanged += (p, u) => { player = p; unit = u; };

        _turns.Start();

        Assert.AreEqual(PlayerID.Player1, player);
        Assert.AreSame(_p1[0], unit);
        Assert.AreSame(_p1[0], _turns.ActiveUnit);
    }

    [Test]
    public void EndTurn_AlternatesPlayers_AndEachPlayerCyclesThroughOwnUnits()
    {
        var order = new List<UnitState>();
        _turns.TurnChanged += (_, u) => order.Add(u);

        _turns.Start();
        for (int i = 0; i < 6; i++) _turns.EndTurn();

        CollectionAssert.AreEqual(new[] { _p1[0], _p2[0], _p1[1], _p2[1], _p1[2], _p2[2], _p1[0] }, order);
    }

    [Test]
    public void EndTurn_SkipsUnregisteredUnit()
    {
        _turns.Start();
        _turns.EndTurn(); // P2 unit 0
        _turns.UnregisterUnit(_p1[1]);

        _turns.EndTurn();

        Assert.AreSame(_p1[2], _turns.ActiveUnit);
    }

    [Test]
    public void EndTurn_ActiveUnitDiedDuringOwnTurn_OwnerContinuesWithNextUnit()
    {
        _turns.Start(); // P1 unit 0 acts and dies
        _turns.UnregisterUnit(_p1[0]);

        _turns.EndTurn(); // P2 unit 0
        _turns.EndTurn();

        Assert.AreSame(_p1[1], _turns.ActiveUnit);
    }

    [Test]
    public void EndTurn_NextPlayerHasNoUnits_RaisesGameOverForActingPlayer()
    {
        PlayerID? winner = null;
        int turnChanges = 0;
        _turns.GameOver += w => winner = w;
        _turns.Start();
        _turns.TurnChanged += (_, _) => turnChanges++;
        foreach (UnitState unit in _p2) _turns.UnregisterUnit(unit);

        _turns.EndTurn();

        Assert.IsTrue(_turns.IsGameOver);
        Assert.AreEqual(PlayerID.Player1, winner);
        Assert.AreEqual(0, turnChanges);
        Assert.IsNull(_turns.ActiveUnit);
    }

    [Test]
    public void EndTurn_AfterGameOver_DoesNothing()
    {
        int gameOvers = 0;
        _turns.GameOver += _ => gameOvers++;
        _turns.Start();
        foreach (UnitState unit in _p2) _turns.UnregisterUnit(unit);
        _turns.EndTurn();

        _turns.EndTurn();

        Assert.AreEqual(1, gameOvers);
    }

    [Test]
    public void UnregisterUnit_UnknownUnit_IsIgnored()
    {
        _turns.Start();
        Assert.DoesNotThrow(() => _turns.UnregisterUnit(new UnitState(PlayerID.Player1, 10)));
        Assert.AreSame(_p1[0], _turns.ActiveUnit);
    }
}
