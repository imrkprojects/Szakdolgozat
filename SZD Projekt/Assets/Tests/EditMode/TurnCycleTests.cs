using System.Collections.Generic;
using System.Linq;
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

    // Starts the match and commits both rosters in registration order
    private void StartActing()
    {
        _turns.Start();
        _turns.CommitPlan(PlayerID.Player1, _turns.GetLivingUnits(PlayerID.Player1));
        _turns.CommitPlan(PlayerID.Player2, _turns.GetLivingUnits(PlayerID.Player2));
    }

    private void Act(int times = 1)
    {
        for (int i = 0; i < times; i++)
        {
            _turns.BeginAction();
            _turns.EndTurn();
        }
    }

    private static UnitState[] Reversed(IEnumerable<UnitState> units) => units.Reverse().ToArray();

    // ─── Planning ────────────────────────────────────────────────────────────

    [Test]
    public void Start_EntersPlanning_WithNoActiveUnit()
    {
        _turns.Start();

        Assert.AreEqual(CyclePhase.Planning, _turns.Phase);
        Assert.IsNull(_turns.ActiveUnit);
        Assert.AreEqual(2, _turns.GetTokens(PlayerID.Player1));
        Assert.AreEqual(2, _turns.GetTokens(PlayerID.Player2));
    }

    [Test]
    public void CommitPlan_OnlyOnePlayerCommitted_StaysInPlanning()
    {
        _turns.Start();

        Assert.IsTrue(_turns.CommitPlan(PlayerID.Player2, _p2));

        Assert.AreEqual(CyclePhase.Planning, _turns.Phase);
        Assert.IsTrue(_turns.HasCommitted(PlayerID.Player2));
        Assert.IsFalse(_turns.HasCommitted(PlayerID.Player1));
        Assert.IsNull(_turns.ActiveUnit);
    }

    [Test]
    public void CommitPlan_BothCommitted_Player1ActsWithFirstPlannedUnit_NoReorderOffered()
    {
        var turns = new List<UnitState>();
        _turns.TurnChanged += (_, u) => turns.Add(u);
        _turns.Start();

        _turns.CommitPlan(PlayerID.Player2, _p2);
        _turns.CommitPlan(PlayerID.Player1, new[] { _p1[2], _p1[0], _p1[1] });

        Assert.AreEqual(CyclePhase.Acting, _turns.Phase);
        Assert.AreEqual(PlayerID.Player1, _turns.ActivePlayer);
        Assert.AreSame(_p1[2], _turns.ActiveUnit);
        CollectionAssert.AreEqual(new[] { _p1[2] }, turns);
        Assert.AreEqual(1, _turns.GetCycle(PlayerID.Player1));
        Assert.AreEqual(1, _turns.GetCycle(PlayerID.Player2));
        Assert.IsFalse(_turns.CanReorder(PlayerID.Player1), "the plan was just made");
    }

    [Test]
    public void CommitPlan_NotAPermutationOfLivingUnits_Rejected()
    {
        _turns.Start();
        _turns.UnregisterUnit(_p1[2]);

        Assert.IsFalse(_turns.CommitPlan(PlayerID.Player1, new[] { _p1[0] }), "missing unit");
        Assert.IsFalse(_turns.CommitPlan(PlayerID.Player1, new[] { _p1[0], _p1[0] }), "duplicate");
        Assert.IsFalse(_turns.CommitPlan(PlayerID.Player1, new[] { _p1[0], _p2[0] }), "enemy unit");
        Assert.IsFalse(_turns.CommitPlan(PlayerID.Player1, new[] { _p1[0], _p1[1], _p1[2] }), "dead unit");
        Assert.IsFalse(_turns.CommitPlan(PlayerID.Player1, null), "null");
        Assert.IsFalse(_turns.HasCommitted(PlayerID.Player1));
    }

    [Test]
    public void CommitPlan_TwiceOrOutsidePlanning_Rejected()
    {
        _turns.Start();
        _turns.CommitPlan(PlayerID.Player1, _p1);

        Assert.IsFalse(_turns.CommitPlan(PlayerID.Player1, _p1));
        _turns.CommitPlan(PlayerID.Player2, _p2);
        Assert.IsFalse(_turns.CommitPlan(PlayerID.Player2, _p2));
    }

    // ─── Turn flow ───────────────────────────────────────────────────────────

    [Test]
    public void Turns_AlternateStrictly_EachPlayerFollowsOwnOrder_AndOrderCarriesOver()
    {
        var order = new List<UnitState>();
        _turns.TurnChanged += (_, u) => order.Add(u);
        _turns.Start();
        _turns.CommitPlan(PlayerID.Player1, new[] { _p1[0], _p1[2], _p1[1] });
        _turns.CommitPlan(PlayerID.Player2, new[] { _p2[0], _p2[2], _p2[1] });

        Act(7);

        CollectionAssert.AreEqual(
            new[] { _p1[0], _p2[0], _p1[2], _p2[2], _p1[1], _p2[1], _p1[0], _p2[0] }, order);
        Assert.AreEqual(2, _turns.GetCycle(PlayerID.Player1));
        Assert.AreEqual(2, _turns.GetCycle(PlayerID.Player2));
    }

    [Test]
    public void BeginAction_TakesActiveUnitOutOfQueue()
    {
        StartActing();
        Assert.IsFalse(_turns.HasActiveUnitStarted);

        _turns.BeginAction();

        Assert.IsTrue(_turns.HasActiveUnitStarted);
        Assert.AreSame(_p1[0], _turns.ActiveUnit);
        CollectionAssert.AreEqual(new[] { _p1[1], _p1[2] }, _turns.GetQueue(PlayerID.Player1));
    }

    [Test]
    public void EndTurn_WithoutAction_PassesToOtherPlayer()
    {
        StartActing();

        _turns.EndTurn();

        Assert.AreSame(_p2[0], _turns.ActiveUnit);
        CollectionAssert.AreEqual(new[] { _p1[1], _p1[2] }, _turns.GetQueue(PlayerID.Player1));
    }

    [Test]
    public void EndTurn_OutsideActing_DoesNothing()
    {
        _turns.Start();

        _turns.EndTurn();

        Assert.AreEqual(CyclePhase.Planning, _turns.Phase);
    }

    [Test]
    public void ShorterCycle_PlayersStillAlternate_AndCyclesDesync()
    {
        var order = new List<UnitState>();
        StartActing();
        _turns.TurnChanged += (_, u) => order.Add(u);
        _turns.BeginAction(); // _p1[0] kills _p2[1]
        _turns.UnregisterUnit(_p2[1]);

        _turns.EndTurn();
        Act(4);

        CollectionAssert.AreEqual(new[] { _p2[0], _p1[1], _p2[2], _p1[2], _p2[0] }, order);
        Assert.AreEqual(2, _turns.GetCycle(PlayerID.Player2));
        Assert.AreEqual(1, _turns.GetCycle(PlayerID.Player1));
    }

    // ─── Token reorders ──────────────────────────────────────────────────────

    [Test]
    public void CycleStart_OffersTokenReorder_UntilTheUnitActs()
    {
        StartActing();
        Act(6);

        Assert.AreEqual(PlayerID.Player1, _turns.ActivePlayer);
        Assert.IsTrue(_turns.CanReorder(PlayerID.Player1));
        Assert.IsFalse(_turns.ReorderIsFree(PlayerID.Player1));
        Assert.IsFalse(_turns.CanReorder(PlayerID.Player2), "not their turn");

        _turns.BeginAction();

        Assert.IsFalse(_turns.CanReorder(PlayerID.Player1));
        Assert.IsFalse(_turns.TryReorder(PlayerID.Player1, Reversed(_p1)));
    }

    [Test]
    public void TokenReorder_SpendsToken_StartsCycleWithNewOrder()
    {
        StartActing();
        Act(6);

        Assert.IsTrue(_turns.TryReorder(PlayerID.Player1, Reversed(_p1)));

        Assert.AreEqual(1, _turns.GetTokens(PlayerID.Player1));
        Assert.AreSame(_p1[2], _turns.ActiveUnit);
        CollectionAssert.AreEqual(Reversed(_p1), _turns.GetQueue(PlayerID.Player1));
        CollectionAssert.AreEqual(Reversed(_p1), _turns.GetLivingUnits(PlayerID.Player1));
        Assert.IsFalse(_turns.CanReorder(PlayerID.Player1), "one reorder per turn");
    }

    [Test]
    public void TokenReorder_MidCycle_Rejected()
    {
        StartActing();
        Assert.IsFalse(_turns.TryReorder(PlayerID.Player1, Reversed(_p1)), "first turn");

        Act(2);

        Assert.IsFalse(_turns.CanReorder(PlayerID.Player1));
        Assert.IsFalse(_turns.TryReorder(PlayerID.Player1, Reversed(_p1)));
        Assert.AreEqual(2, _turns.GetTokens(PlayerID.Player1));
    }

    [Test]
    public void TokenReorder_NoTokensLeft_Rejected_AndTokensDoNotReset()
    {
        StartActing();
        for (int i = 0; i < 2; i++)
        {
            Act(6);
            Assert.IsTrue(_turns.TryReorder(PlayerID.Player1, Reversed(_turns.GetLivingUnits(PlayerID.Player1))));
        }

        Act(6);

        Assert.AreEqual(4, _turns.GetCycle(PlayerID.Player1));
        Assert.AreEqual(0, _turns.GetTokens(PlayerID.Player1));
        Assert.IsFalse(_turns.CanReorder(PlayerID.Player1));
        Assert.IsFalse(_turns.TryReorder(PlayerID.Player1, Reversed(_p1)));
        Assert.AreEqual(2, _turns.GetTokens(PlayerID.Player2));
    }

    // ─── Death reorders ──────────────────────────────────────────────────────

    // P1 and P2 plan U1, U3, U2; P1U1, P2U1 and P1U3 act, then P2U3 kills P1U1. It is P1's turn.
    private void PlayUntilP1U1Dies()
    {
        _turns.Start();
        _turns.CommitPlan(PlayerID.Player1, new[] { _p1[0], _p1[2], _p1[1] });
        _turns.CommitPlan(PlayerID.Player2, new[] { _p2[0], _p2[2], _p2[1] });
        Act(3);
        _turns.BeginAction();
        _turns.UnregisterUnit(_p1[0]);
        _turns.EndTurn();
    }

    [Test]
    public void DeathSkipped_OldQueueContinues_ThenNextCycleOffersToken()
    {
        PlayUntilP1U1Dies();

        Assert.AreSame(_p1[1], _turns.ActiveUnit, "P1U2 is next in the old queue");
        Assert.IsTrue(_turns.CanReorder(PlayerID.Player1));
        Assert.IsTrue(_turns.ReorderIsFree(PlayerID.Player1));

        Act(2); // P1U2 acts without reordering, then P2U2

        Assert.AreEqual(2, _turns.GetCycle(PlayerID.Player1));
        Assert.AreSame(_p1[2], _turns.ActiveUnit, "the old order minus the dead unit: U3, U2");
        Assert.IsTrue(_turns.CanReorder(PlayerID.Player1));
        Assert.IsFalse(_turns.ReorderIsFree(PlayerID.Player1), "a token offer, not a free one");
    }

    [Test]
    public void DeathReorderTaken_StartsNewCycleAtOnce_NoTokenOfferUntilItEnds()
    {
        PlayUntilP1U1Dies();

        Assert.IsTrue(_turns.TryReorder(PlayerID.Player1, new[] { _p1[1], _p1[2] }));

        Assert.AreEqual(2, _turns.GetTokens(PlayerID.Player1), "death reorders are free");
        Assert.AreEqual(2, _turns.GetCycle(PlayerID.Player1));
        Assert.AreSame(_p1[1], _turns.ActiveUnit);
        Act(2);
        Assert.AreSame(_p1[2], _turns.ActiveUnit);
        Assert.IsFalse(_turns.CanReorder(PlayerID.Player1), "the new cycle has not ended");
        Act(2);
        Assert.AreSame(_p1[1], _turns.ActiveUnit);
        Assert.AreEqual(3, _turns.GetCycle(PlayerID.Player1));
        Assert.IsTrue(_turns.CanReorder(PlayerID.Player1));
    }

    [Test]
    public void OwnUnitDiesDuringOwnAction_FreeReorderOnOwnNextTurn()
    {
        StartActing();
        _turns.BeginAction();
        _turns.UnregisterUnit(_p1[1]);
        _turns.EndTurn();

        Assert.IsFalse(_turns.CanReorder(PlayerID.Player1), "P2's turn");
        Assert.IsTrue(_turns.ReorderIsFree(PlayerID.Player1));
        Act();

        Assert.AreSame(_p1[2], _turns.ActiveUnit);
        Assert.IsTrue(_turns.CanReorder(PlayerID.Player1));
        Assert.IsTrue(_turns.ReorderIsFree(PlayerID.Player1));
    }

    [Test]
    public void DeathReorder_OneLivingUnitLeft_NotOffered()
    {
        StartActing();
        _turns.BeginAction();
        _turns.UnregisterUnit(_p2[1]);
        _turns.UnregisterUnit(_p2[2]);
        _turns.EndTurn();

        Assert.AreEqual(PlayerID.Player2, _turns.ActivePlayer);
        Assert.IsFalse(_turns.CanReorder(PlayerID.Player2));
    }

    [Test]
    public void TryReorder_BadOrder_Rejected()
    {
        StartActing();
        _turns.BeginAction();
        _turns.UnregisterUnit(_p2[0]);
        _turns.EndTurn();

        Assert.IsFalse(_turns.TryReorder(PlayerID.Player1, Reversed(_p1)), "not their turn");
        Assert.IsFalse(_turns.TryReorder(PlayerID.Player2, new[] { _p2[1] }), "missing unit");
        Assert.IsFalse(_turns.TryReorder(PlayerID.Player2, new[] { _p2[0], _p2[1], _p2[2] }), "dead unit");
        Assert.IsFalse(_turns.TryReorder(PlayerID.Player2, new[] { _p2[1], _p1[0] }), "enemy unit");
        Assert.IsFalse(_turns.TryReorder(PlayerID.Player2, null), "null");
        Assert.IsTrue(_turns.TryReorder(PlayerID.Player2, new[] { _p2[2], _p2[1] }));
    }

    // ─── Game over ───────────────────────────────────────────────────────────

    [Test]
    public void EndTurn_EnemyRosterEmpty_RaisesGameOverForActingPlayer()
    {
        PlayerID? winner = null;
        int turnChanges = 0;
        _turns.GameOver += w => winner = w;
        StartActing();
        _turns.TurnChanged += (_, _) => turnChanges++;
        _turns.BeginAction();
        foreach (UnitState unit in _p2) _turns.UnregisterUnit(unit);

        _turns.EndTurn();

        Assert.AreEqual(PlayerID.Player1, winner);
        Assert.IsTrue(_turns.IsGameOver);
        Assert.AreEqual(0, turnChanges);
        Assert.IsNull(_turns.ActiveUnit);
    }

    [Test]
    public void EndTurn_BothRostersEmpty_IsDraw()
    {
        PlayerID? winner = null;
        _turns.GameOver += w => winner = w;
        StartActing();
        _turns.BeginAction();
        foreach (UnitState unit in _p1) _turns.UnregisterUnit(unit);
        foreach (UnitState unit in _p2) _turns.UnregisterUnit(unit);

        _turns.EndTurn();

        Assert.AreEqual(PlayerID.None, winner);
    }

    [Test]
    public void EndTurn_AfterGameOver_DoesNothing()
    {
        int gameOvers = 0;
        _turns.GameOver += _ => gameOvers++;
        StartActing();
        foreach (UnitState unit in _p2) _turns.UnregisterUnit(unit);
        _turns.EndTurn();

        _turns.EndTurn();

        Assert.AreEqual(1, gameOvers);
        Assert.IsFalse(_turns.TryReorder(PlayerID.Player1, Reversed(_turns.GetLivingUnits(PlayerID.Player1))));
    }

    [Test]
    public void UnregisterUnit_UnknownUnit_IsIgnored()
    {
        StartActing();
        Assert.DoesNotThrow(() => _turns.UnregisterUnit(new UnitState(PlayerID.Player1, 10)));
        Assert.AreSame(_p1[0], _turns.ActiveUnit);
    }
}