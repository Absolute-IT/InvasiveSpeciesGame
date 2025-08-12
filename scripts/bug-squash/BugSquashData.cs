using Godot;
using System.Collections.Generic;

namespace InvasiveSpeciesAustralia
{
    public class BugSquashStage
    {
        public string Id { get; set; }
        public string BackgroundImage { get; set; }
        public string AmbienceSound { get; set; }
        public string InteractionDescription { get; set; }
        public List<BugSquashSpecies> Species { get; set; }
    }

    public class BugSquashSpecies
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public string Color { get; set; }
        public string Behavior { get; set; }
        public List<BugSquashGoal> Goals { get; set; } = new List<BugSquashGoal>();
        public float Speed { get; set; }
        public float Size { get; set; } = 100f; // Default to 100%
        public string Description { get; set; }
        public int StartingNumber { get; set; }
        public int Health { get; set; } = 1; // Default to 1
        public float SpawnRate { get; set; } = 0f; // Default to 0 (no spawning)
        public string CreatesOnEaten { get; set; } // Entity ID to create when this food is eaten
    }

    public class BugSquashGoal
    {
        public string Type { get; set; }
        public string Target { get; set; }
        public float Value { get; set; }
    }

    public enum EntityBehavior
    {
        Predator,
        Prey,
        Food,
        Nest,
        Weed
    }

    public enum GoalType
    {
        Eat,
        Breed,
        Spawn,
        Kill
    }
} 