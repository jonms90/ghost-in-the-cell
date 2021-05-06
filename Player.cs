using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
class Player
{
    static void Main(string[] args)
    {
        string[] inputs;
        int factoryCount = int.Parse(Console.ReadLine()); // the number of factories
        int linkCount = int.Parse(Console.ReadLine()); // the number of links between factories
        var map = new List<Link>();
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
            var sources = new List<Factory>();
            var targets = new List<Factory>();
            var bombTargets = new List<Factory>();
            int entityCount = int.Parse(Console.ReadLine()); // the number of entities (e.g. factories and troops)
            for (int i = 0; i < entityCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int entityId = int.Parse(inputs[0]);
                string entityType = inputs[1];
                int arg1 = int.Parse(inputs[2]);
                int arg2 = int.Parse(inputs[3]);
                int arg3 = int.Parse(inputs[4]);
                int arg4 = int.Parse(inputs[5]);
                int arg5 = int.Parse(inputs[6]);
                if(entityType == "FACTORY"){
                    if(arg1 == 1){
                        sources.Add(new Factory(entityId, arg2, arg3));
                    }
                    else{
                        targets.Add(new Factory(entityId, arg2, arg3));
                        if(arg1 == -1){
                            bombTargets.Add(new Factory(entityId, arg2 , arg3));
                        }
                    }
                }
            }

            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");
            var commands = new List<string>();
            foreach(var source in sources){
                if(!targets.Any()){
                    commands.Add("WAIT");
                    continue;
                }
                var destination = GetDestination(source, map.Where(m => m.Factory1 == source.Id || m.Factory2 == source.Id), targets);
                if(bombTargets.Any()){
                    var bombTarget = bombTargets.First();
                    commands.Add($"BOMB {source.Id} {bombTarget.Id}");
                }
                if(source.Defense <= 4 && source.Production > 0){
                    commands.Add("WAIT");
                }
                else{
                    var count = source.Defense / 2;
                    commands.Add($"MOVE {source.Id} {destination} {count}");
                }
            }

            Console.WriteLine(string.Join(';', commands));
            // Any valid action, such as "WAIT" or "MOVE source destination cyborgs"
        }
    }

    private static string GetDestination(Factory source, IEnumerable<Link> proximityFactories, List<Factory> targets){
        var closestTargets = new List<Target>();
        foreach(var factory in proximityFactories.OrderBy(p => p.Distance)){
            if(factory.Factory1 == source.Id && targets.Any(t => t.Id == factory.Factory2)){
                var candidate = targets.First(t => t.Id == factory.Factory2);
                closestTargets.Add(new Target(candidate.Id, factory.Distance, candidate.Production, candidate.Defense));
            }
            else if(factory.Factory2 == source.Id && targets.Any(t => t.Id == factory.Factory1)){
                var candidate = targets.First(t => t.Id == factory.Factory1);
                closestTargets.Add(new Target(candidate.Id, factory.Distance, candidate.Production, candidate.Defense));
            }
        }
        if(closestTargets.Any()){
            return closestTargets.OrderBy(t => t.Priority).ThenBy(t => t.Defense).First().Id.ToString();
        }
        return targets.OrderByDescending(t => t.Production).ThenBy(t => t.Defense).First().Id.ToString();
    }

    private class Link{
        public int Factory1 {get;}
        public int Factory2 {get;}
        public int Distance {get;}

        public Link(int factory1, int factory2, int distance){
            Factory1 = factory1;
            Factory2 = factory2;
            Distance = distance;
        }
    }

    private class Target{
        public int Id{get;}
        public int Priority{get;}
        public int Defense{get;}
        
        public Target(int id, int distance, int production, int defense){
            Id = id;
            Defense = defense;
            Priority = production == 0 ? 10 * distance : distance / production;
        }
    }

    private class Factory{
        public int Id{get;}
        public int Defense{get;}
        public int Production{get;}
        
        public Factory(int id, int defense, int production){
            Id = id;
            Defense = defense;
            Production = production;
        }
    }
}