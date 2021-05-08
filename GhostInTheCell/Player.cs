using System;
using System.Collections.Generic;
using System.Linq;

// To debug: Console.Error.WriteLine("Debug messages...");
internal class Player
{
    private static void Main(string[] args)
    {
        string[] inputs;
        int factoryCount = int.Parse(Console.ReadLine()); // the number of factories
        int linkCount = int.Parse(Console.ReadLine()); // the number of links between factories
        List<Link> map = new List<Link>();
        bool isbombingAvailable = true;
        for (int i = 0; i < linkCount; i++)
        {
            inputs = Console.ReadLine().Split(' ');
            int factory1 = int.Parse(inputs[0]);
            int factory2 = int.Parse(inputs[1]);
            int distance = int.Parse(inputs[2]);
            map.Add(new Link(factory1, factory2, distance));
        }

        List<string> commands = new List<string>();

        // game loop
        while (true)
        {
            commands.Clear(); // Reset commands each turn.
            List<Entity> entities = new List<Entity>();
            int entityCount = int.Parse(Console.ReadLine()); // the number of entities (e.g. factories and troops)
            for (int i = 0; i < entityCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                ParseEntity(inputs, entities);
            }

            List<Factory> factories = entities.Where(e => e.GetType() == typeof(Factory)).Select(x => (Factory)x).ToList();
            List<Factory> friendlyFactories = factories.Where(e => e.IsFriendly).ToList();
            List<Factory> enemyFactories = factories.Where(e => !e.IsFriendly).ToList();
            List<Troop> enemyTroops = entities.Where(e => e.IsHostile && e.GetType() == typeof(Troop))
                .Select(t => (Troop)t).ToList();
            List<Bomb> bombs = entities.Where(e => e.GetType() == typeof(Bomb)).Select(b => (Bomb)b)
                .ToList();

            if (isbombingAvailable)
            {
                Factory enemyHq = (Factory)entities.First(e => e.IsHostile && e.GetType() == typeof(Factory));
                List<int> linkedFactories = GetClosestXLinkedFactories(enemyHq, map, 3);
                int bombTarget = enemyFactories.Where(t => linkedFactories.Contains(t.Id))
                    .OrderByDescending(t => t.Production).First().Id;
                commands.Add($"BOMB {friendlyFactories.First().Id} {enemyHq.Id}");
                commands.Add($"BOMB {friendlyFactories.First().Id} {bombTarget}");
                isbombingAvailable = false;
            }

            foreach (Factory factory in friendlyFactories)
            {
                int availableCyborgs = CalculateDefenses(factory, enemyTroops);
                availableCyborgs = DefendFactories(factory, availableCyborgs, friendlyFactories, enemyTroops, map, commands);
                if (ShouldIncreaseProduction(factory, friendlyFactories.Count, availableCyborgs, bombs.Any(b => b.IsHostile)))
                {
                    availableCyborgs -= 10;
                    commands.Add($"INC {factory.Id}");
                }

                int target = FindTarget(factory, enemyFactories, bombs, map);
                if (target == factory.Id)
                {
                    continue;
                }
                int path = FindPath(factory, target, map, factories);
                if (availableCyborgs > 0)
                {
                    commands.Add($"MOVE {factory.Id} {path} {availableCyborgs}");
                }
            }

            if (commands.Count == 0)
            {
                commands.Add("WAIT");
            }
            Console.WriteLine(string.Join(';', commands));
            // Any valid action, such as "WAIT" or "MOVE source destination cyborgs"
        }
    }

    private static int DefendFactories(Factory source, int availableCyborgs, List<Factory> friendlyFactories, List<Troop> enemyTroops, List<Link> map, List<string> commands)
    {
        var defenseCandidates = friendlyFactories.Where(f => f.Id != source.Id).Select(f => GetLinkBetween(source, f, map)).Where(f => f.Distance <= 5).OrderBy(f => f.Distance)
            .Select(f => new { f.Distance, Id = f.Factory1 == source.Id ? f.Factory2 : f.Factory1 }).Take(2);
        foreach (var candidate in defenseCandidates)
        {
            Factory targetFactory = friendlyFactories.First(f => f.Id == candidate.Id);
            List<Troop> attackers = enemyTroops.Where(t => t.Target == targetFactory.Id).ToList();
            if (!attackers.Any())
            {
                continue;
            }

            int attackingTroops = attackers.Where(t => t.ETA <= candidate.Distance).Sum(t => t.Strength);
            int defendingTroops = targetFactory.Defense + (targetFactory.Production * candidate.Distance);
            if (attackingTroops <= defendingTroops)
            {
                continue;
            }

            int backupRequired = attackingTroops - defendingTroops;
            int sentBackup = backupRequired > availableCyborgs ? availableCyborgs : backupRequired;
            commands.Add($"MOVE {source.Id} {candidate.Id} {sentBackup}");
            return availableCyborgs - sentBackup;
        }

        return availableCyborgs;
    }

    // Calculate required defenses five turns ahead, considering already sent enemy troops.
    // Does not evaluate enemy troops not sent yet by close enemy factories.
    private static int CalculateDefenses(Factory source, List<Troop> enemyTroops)
    {
        List<Troop> attackers = enemyTroops.Where(t => t.Target == source.Id && t.ETA <= 5).ToList();
        if (attackers.Count == 0)
        {
            return source.Defense;
        }
        int incomingAttackers = attackers.Sum(t => t.Strength);
        if (source.Defense > incomingAttackers)
        {
            return source.Defense - incomingAttackers;
        }

        return 0;
    }

    private static int FindPath(Factory source, int targetFactoryId, List<Link> map, List<Factory> factories)
    {
        Link directPath = map.First(l =>
            (l.Factory1 == source.Id && l.Factory2 == targetFactoryId) ||
            l.Factory1 == targetFactoryId && l.Factory2 == source.Id);
        int directDistance = directPath.Distance;
        IEnumerable<Link> alternativePaths = map.Where(l => l.Factory1 == source.Id || l.Factory2 == source.Id).Where(l =>
            !(l.Factory1 == source.Id && l.Factory2 == targetFactoryId) &&
            !(l.Factory1 == targetFactoryId && l.Factory2 == source.Id));
        List<Factory> candidates = new List<Factory>();
        foreach (Link alternativePath in alternativePaths)
        {
            int intermediateDistance = alternativePath.Distance;
            int intermediateFactoryId = alternativePath.Factory1 == source.Id
                ? alternativePath.Factory2
                : alternativePath.Factory1;
            Link directPathFromIntermediate =
                map.First(l =>
                    (l.Factory1 == intermediateFactoryId && l.Factory2 == targetFactoryId) ||
                    l.Factory2 == intermediateFactoryId && l.Factory1 == targetFactoryId);
            if (intermediateDistance + directPathFromIntermediate.Distance <= directDistance)
            {
                Factory intermediateFactory = factories.First(f => f.Id == intermediateFactoryId);
                intermediateFactory.DistanceTo = intermediateDistance;
                candidates.Add(intermediateFactory);
            }
        }

        if (candidates.Any())
        {
            return candidates.OrderBy(c => c.DistanceTo).ThenByDescending(c => c.Production).First().Id;
        }
        return targetFactoryId;
    }

    private static int FindTarget(Factory source, List<Factory> targets, List<Bomb> friendlyBombs, List<Link> map)
    {
        if (targets.Count == 0)
        {
            return source.Id;
        }

        IOrderedEnumerable<Factory> prioritizedTargets = targets.OrderByDescending(t => t.Production).ThenBy(t => t.Defense);
        foreach (Factory candidate in prioritizedTargets)
        {
            Link link = GetLinkBetween(source, candidate, map);
            if (friendlyBombs.All(b => b.ETA != link.Distance && b.ETA != link.Distance + 1))
            {
                return candidate.Id;
            }
        }
        return source.Id;
    }

    private static Link GetLinkBetween(Factory source, Factory target, List<Link> map)
    {
        return map.First(l =>
            (l.Factory1 == source.Id && l.Factory2 == target.Id) ||
            (l.Factory1 == target.Id && l.Factory2 == source.Id));
    }

    private static List<int> GetClosestXLinkedFactories(Factory enemyHq, List<Link> map, int take)
    {
        IEnumerable<Link> links = map.Where(m => m.Factory1 == enemyHq.Id || m.Factory2 == enemyHq.Id).OrderBy(l => l.Distance)
            .Take(take);
        return links.Select(link => link.Factory1 == enemyHq.Id ? link.Factory2 : link.Factory1).ToList();
    }

    private static bool ShouldIncreaseProduction(Factory source, int factoryCount, int availableCyborgs, bool bombPresent)
    {
        return source.Production != 3 && factoryCount >= 3 && availableCyborgs >= 10 && !bombPresent;
    }

    private static void ParseEntity(string[] inputs, List<Entity> entities)
    {
        int entityId = int.Parse(inputs[0]);
        string entityType = inputs[1];
        int entityTeam = int.Parse(inputs[2]);
        int arg2 = int.Parse(inputs[3]);
        int arg3 = int.Parse(inputs[4]);
        int arg4 = int.Parse(inputs[5]);
        int arg5 = int.Parse(inputs[6]);
        switch (entityType)
        {
            case "BOMB":
                entities.Add(new Bomb
                {
                    Id = entityId,
                    Type = entityType,
                    Team = entityTeam,
                    Source = arg2,
                    Target = arg3,
                    ETA = arg4
                });
                break;
            case "TROOP":
                entities.Add(new Troop
                {
                    Id = entityId,
                    Type = entityType,
                    Team = entityTeam,
                    Source = arg2,
                    Target = arg3,
                    Strength = arg4,
                    ETA = arg5
                });
                break;
            default:
                entities.Add(new Factory
                {
                    Id = entityId,
                    Type = entityType,
                    Team = entityTeam,
                    Defense = arg2,
                    Production = arg3,
                    Cooldown = arg4
                });
                break;
        }
    }

    public class Link
    {
        public int Factory1 { get; }
        public int Factory2 { get; }
        public int Distance { get; }

        public Link(int factory1, int factory2, int distance)
        {
            Factory1 = factory1;
            Factory2 = factory2;
            Distance = distance;
        }
    }

    public class Target
    {
        public int Id { get; }
        public int Defense { get; }
        public int Distance { get; }
        public int Production { get; }

        public Target(int id, int distance, int production, int defense)
        {
            Id = id;
            Distance = distance;
            Production = production;
            Defense = defense;
        }
    }
    public class Entity
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public int Team { get; set; }
        public bool IsFriendly => Team == 1;
        public bool IsNeutral => Team == 0;
        public bool IsHostile => Team == -1;
    }

    private class Troop : Entity
    {
        public int Source { get; set; }
        public int Target { get; set; }
        public int Strength { get; set; }
        public int ETA { get; set; }
    }

    public class Factory : Entity
    {
        public int Defense { get; set; }
        public int Production { get; set; }
        public int Cooldown { get; set; }
        public int DistanceTo { get; set; }
    }

    public class Bomb : Entity
    {
        public int Source { get; set; }
        public int Target { get; set; }
        public int ETA { get; set; }
    }
}

