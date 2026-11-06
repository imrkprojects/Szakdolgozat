using NUnit.Framework;
using UnityEngine;

public class UnitDataTests
{
    private UnitData _data;
    private AbilityData _ability;

    [SetUp]
    public void SetUp()
    {
        _ability = ScriptableObject.CreateInstance<AbilityData>();
        _data = ScriptableObject.CreateInstance<UnitData>();
        _data.maxHp = 25;
        _data.moveRange = 3;
        _data.abilities = new[] { _ability };
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_data);
        Object.DestroyImmediate(_ability);
    }

    [Test]
    public void CreateState_CopiesOwnerStatsAndAbilities()
    {
        UnitState unit = _data.CreateState(PlayerID.Player2);

        Assert.AreEqual(PlayerID.Player2, unit.Owner);
        Assert.AreEqual(25, unit.MaxHp);
        Assert.AreEqual(25, unit.Hp);
        Assert.AreEqual(3, unit.MoveRange);
        CollectionAssert.AreEqual(new[] { _ability }, unit.Abilities);
    }

    [Test]
    public void CreateState_LaterAssetEdits_DoNotChangeExistingUnit()
    {
        UnitState unit = _data.CreateState(PlayerID.Player1);

        _data.abilities[0] = null;

        Assert.AreSame(_ability, unit.Abilities[0]);
    }

    [Test]
    public void CreateState_NullAbilities_GivesEmptyList()
    {
        _data.abilities = null;

        Assert.AreEqual(0, _data.CreateState(PlayerID.Player1).Abilities.Count);
    }
}
