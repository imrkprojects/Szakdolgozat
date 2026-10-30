using System;
using NUnit.Framework;

public class UnitStateTests
{
    [Test]
    public void New_StartsAtFullHealth()
    {
        var unit = new UnitState(PlayerID.Player1, 30);

        Assert.AreEqual(30, unit.Hp);
        Assert.IsTrue(unit.IsAlive);
        Assert.AreEqual(PlayerID.Player1, unit.Owner);
    }

    [TestCase(0)]
    [TestCase(-5)]
    public void New_NonPositiveMaxHp_Throws(int maxHp)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnitState(PlayerID.Player1, maxHp));
    }

    [Test]
    public void TakeDamage_ReducesHp_AndRaisesHealthChanged()
    {
        var unit = new UnitState(PlayerID.Player1, 30);
        int changes = 0;
        unit.HealthChanged += _ => changes++;

        unit.TakeDamage(10);

        Assert.AreEqual(20, unit.Hp);
        Assert.AreEqual(1, changes);
    }

    [Test]
    public void TakeDamage_Lethal_ClampsToZero_AndRaisesDiedOnce()
    {
        var unit = new UnitState(PlayerID.Player1, 30);
        int deaths = 0;
        unit.Died += _ => deaths++;

        unit.TakeDamage(50);
        unit.TakeDamage(10);

        Assert.AreEqual(0, unit.Hp);
        Assert.IsFalse(unit.IsAlive);
        Assert.AreEqual(1, deaths);
    }

    [Test]
    public void TakeDamage_RaisesHealthChangedBeforeDied()
    {
        var unit = new UnitState(PlayerID.Player1, 10);
        string order = "";
        unit.HealthChanged += _ => order += "H";
        unit.Died += _ => order += "D";

        unit.TakeDamage(10);

        Assert.AreEqual("HD", order);
    }

    [TestCase(0)]
    [TestCase(-3)]
    public void TakeDamage_NonPositive_IsIgnored(int amount)
    {
        var unit = new UnitState(PlayerID.Player1, 30);
        int changes = 0;
        unit.HealthChanged += _ => changes++;

        unit.TakeDamage(amount);

        Assert.AreEqual(30, unit.Hp);
        Assert.AreEqual(0, changes);
    }

    [Test]
    public void Heal_ClampsToMaxHp()
    {
        var unit = new UnitState(PlayerID.Player1, 30);
        unit.TakeDamage(5);

        unit.Heal(100);

        Assert.AreEqual(30, unit.Hp);
    }

    [Test]
    public void Heal_DeadUnit_IsIgnored()
    {
        var unit = new UnitState(PlayerID.Player1, 10);
        unit.TakeDamage(10);

        unit.Heal(5);

        Assert.AreEqual(0, unit.Hp);
        Assert.IsFalse(unit.IsAlive);
    }
}
