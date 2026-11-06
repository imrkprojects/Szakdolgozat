using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class StructureTests
{
    private StructureData _data;

    [SetUp]
    public void SetUp()
    {
        _data = ScriptableObject.CreateInstance<StructureData>();
        _data.destructible = true;
        _data.maxHp = 10;
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_data);

    [Test]
    public void CreateState_CopiesDataAndHp()
    {
        StructureState structure = _data.CreateState();

        Assert.AreSame(_data, structure.Data);
        Assert.AreEqual(10, structure.Hp);
        Assert.IsFalse(structure.IsDestroyed);
    }

    [Test]
    public void TakeDamage_LowersHp_AndRaisesHealthChanged()
    {
        StructureState structure = _data.CreateState();
        int changed = 0;
        structure.HealthChanged += _ => changed++;

        structure.TakeDamage(4);

        Assert.AreEqual(6, structure.Hp);
        Assert.AreEqual(1, changed);
    }

    [Test]
    public void TakeDamage_Lethal_RaisesDestroyedOnce_AfterHealthChanged()
    {
        StructureState structure = _data.CreateState();
        var events = new List<string>();
        structure.HealthChanged += _ => events.Add("health");
        structure.Destroyed += _ => events.Add("destroyed");

        structure.TakeDamage(25);
        structure.TakeDamage(5);

        Assert.AreEqual(0, structure.Hp);
        Assert.IsTrue(structure.IsDestroyed);
        CollectionAssert.AreEqual(new[] { "health", "destroyed" }, events);
    }

    [Test]
    public void TakeDamage_Indestructible_Ignored()
    {
        _data.destructible = false;
        StructureState structure = _data.CreateState();
        int changed = 0;
        structure.HealthChanged += _ => changed++;

        structure.TakeDamage(25);

        Assert.AreEqual(10, structure.Hp);
        Assert.AreEqual(0, changed);
    }

    [TestCase(0)]
    [TestCase(-3)]
    public void TakeDamage_NonPositiveAmount_Ignored(int amount)
    {
        StructureState structure = _data.CreateState();

        structure.TakeDamage(amount);

        Assert.AreEqual(10, structure.Hp);
    }
}
