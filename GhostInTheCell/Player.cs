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
        for (int i = 0; i < linkCount; i++)
        {
            inputs = Console.ReadLine().Split(' ');
            int factory1 = int.Parse(inputs[0]);
            int factory2 = int.Parse(inputs[1]);
            int distance = int.Parse(inputs[2]);
            map.Add(new Link(factory1, factory2, distance));
        }

        // game loop
        while (true)
        {
            List<Entity> entities = new List<Entity>();
            int entityCount = int.Parse(Console.ReadLine()); // the number of entities (e.g. factories and troops)
            for (int i = 0; i < entityCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                ParseEntity(inputs, entities);
            }

            List<Entity> sources = entities.Where(e => e.IsFriendly && e.GetType() == typeof(Factory)).ToList();
            List<Entity> targets = entities.Where(e => !e.IsFriendly && e.GetType() == typeof(Factory)).ToList();
            List<Entity> bombTargets = entities.Where(e => e.IsHostile && e.GetType() == typeof(Factory)).ToList();
            List<string> commands = new List<string>();
            foreach (Entity entity in sources)
            {
                Factory source = (Factory)entity;
                if (source.Production < 3 && source.Defense > 10)
                {
                    commands.Add($"INC {source.Id}");
                }
                if (!targets.Any()) // This is not good enough anymore. Must be able to defend friendly factories.
                {
                    commands.Add("WAIT");
                    continue;
                }
                string destination = GetDestination(source, map.Where(m => m.Factory1 == source.Id || m.Factory2 == source.Id), targets);
                if (bombTargets.Any()) // This is not good enough. Need better bomb logic.
                {
                    commands.Add($"BOMB {source.Id} {bombTargets.First().Id}");
                }
                if (source.Defense <= 4 && source.Production > 0)
                {
                    commands.Add("WAIT");
                }
                else
                {
                    int count = source.Defense / 2;
                    commands.Add($"MOVE {source.Id} {destination} {count}");
                }
            }

            Console.WriteLine(string.Join(';', commands));
            // Any valid action, such as "WAIT" or "MOVE source destination cyborgs"
        }
    }

    private static string GetDestination(Factory source, IEnumerable<Link> proximityFactories, List<Entity> targets)
    {
        List<Target> closestTargets = new List<Target>();
        foreach (Link factory in proximityFactories.OrderBy(p => p.Distance))
        {
            if (factory.Factory1 == source.Id && targets.Any(t => t.Id == factory.Factory2))
            {
                Factory candidate = (Factory)targets.First(t => t.Id == factory.Factory2);
                closestTargets.Add(new Target(candidate.Id, factory.Distance, candidate.Production, candidate.Defense));
            }
            else if (factory.Factory2 == source.Id && targets.Any(t => t.Id == factory.Factory1))
            {
                Factory candidate = (Factory)targets.First(t => t.Id == factory.Factory1);
                closestTargets.Add(new Target(candidate.Id, factory.Distance, candidate.Production, candidate.Defense));
            }
        }
        if (closestTargets.Any())
        {
            return closestTargets.OrderBy(t => t.Priority).ThenBy(t => t.Defense).First().Id.ToString();
        }
        return targets.OrderByDescending(t => ((Factory)t).Production).ThenBy(t => ((Factory)t).Defense).First().Id.ToString();
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
        public int Priority { get; }
        public int Defense { get; }

        public Target(int id, int distance, int production, int defense)
        {
            Id = id;
            Defense = defense;
            Priority = production == 0 ? 10 * distance : distance / production;
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
    }

    public class Bomb : Entity
    {
        public int Source { get; set; }
        public int Target { get; set; }
        public int ETA { get; set; }
    }
}

