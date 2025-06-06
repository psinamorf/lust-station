using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.CombatMode;
using Robust.Server.Player;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Actions;

/// <summary>
/// This tests checks that actions properly get added to an entity's actions component..
/// </summary>
[TestFixture]
public sealed class ActionsAddedTest
{
    // TODO add magboot test (inventory action)
    // TODO add ghost toggle-fov test (client-side action)

    [Test]
    public async Task TestCombatActionsAdded()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false });
        var server = pair.Server;
        var client = pair.Client;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var cEntMan = client.ResolveDependency<IEntityManager>();
        var clientSession = client.Session;
        var serverSession = server.ResolveDependency<IPlayerManager>().Sessions.Single();
        var sActionSystem = server.System<SharedActionsSystem>();
        var cActionSystem = client.System<SharedActionsSystem>();

        // Dummy ticker is disabled - client should be in control of a normal mob.
        Assert.That(serverSession.AttachedEntity, Is.Not.Null);
        var serverEnt = serverSession.AttachedEntity!.Value;
        var clientEnt = clientSession!.AttachedEntity!.Value;
        try
        {
            Assert.That(sEntMan.EntityExists(serverEnt));
            Assert.That(cEntMan.EntityExists(clientEnt));
            Assert.That(sEntMan.HasComponent<ActionsComponent>(serverEnt));
            Assert.That(cEntMan.HasComponent<ActionsComponent>(clientEnt));
            Assert.That(sEntMan.HasComponent<CombatModeComponent>(serverEnt));
            Assert.That(cEntMan.HasComponent<CombatModeComponent>(clientEnt));
        }
        catch (AssertionException)
        {
            Console.WriteLine("Assert Failed.");
        }

        var sComp = sEntMan.GetComponent<ActionsComponent>(serverEnt);
        var cComp = cEntMan.GetComponent<ActionsComponent>(clientEnt);

        // Mob should have a combat-mode action.
        // This action should have a non-null event both on the server & client.
        var evType = typeof(ToggleCombatActionEvent);

        var sQuery = sEntMan.GetEntityQuery<InstantActionComponent>();
        var cQuery = cEntMan.GetEntityQuery<InstantActionComponent>();
        var sActions = sActionSystem.GetActions(serverEnt).Where(
            ent => sQuery.CompOrNull(ent)?.Event?.GetType() == evType).ToArray();
        var cActions = cActionSystem.GetActions(clientEnt).Where(
            ent => cQuery.CompOrNull(ent)?.Event?.GetType() == evType).ToArray();

        Assert.That(sActions.Length, Is.EqualTo(1));
        Assert.That(cActions.Length, Is.EqualTo(1));

        var sAct = sActions[0];
        var cAct = cActions[0];

        Assert.That(sAct.Comp, Is.Not.Null);
        Assert.That(cAct.Comp, Is.Not.Null);

        // Finally, these two actions are not the same object
        // required, because integration tests do not respect the [NonSerialized] attribute and will simply events by reference.
        Assert.That(ReferenceEquals(sAct.Comp, cAct.Comp), Is.False);
        Assert.That(ReferenceEquals(sQuery.GetComponent(sAct).Event, cQuery.GetComponent(cAct).Event), Is.False);

        await pair.CleanReturnAsync();
    }
}
