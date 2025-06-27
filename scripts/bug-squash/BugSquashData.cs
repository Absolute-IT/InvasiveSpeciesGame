using Godot;
using System.Collections.Generic;

namespace InvasiveSpeciesAustralia
{
    public class BugSquashStage
    {
        public string Id { get; set; }
        public string BackgroundImage { get; set; }
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
        public float Speed { get; set; }
        public string Description { get; set; }
        public int StartingNumber { get; set; }
    }

    public enum EntityBehavior
    {
        Predator,
        Prey,
        Food
    }
} 