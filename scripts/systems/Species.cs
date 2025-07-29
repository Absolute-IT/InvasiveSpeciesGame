using Godot;
using System.Collections.Generic;

namespace InvasiveSpeciesAustralia
{
    /// <summary>
    /// Represents a single species entry from the configuration
    /// </summary>
    public partial class Species : Resource
    {
        public string Id { get; set; }
        public bool Enabled { get; set; } = true;
        public string Name { get; set; }
        public string ScientificName { get; set; }
        public string Type { get; set; } // "animals" or "plants"
        public string History { get; set; }
        public string Habitat { get; set; }
        public string Diet { get; set; }
        public List<string> Identification { get; set; } = new List<string>();
        public List<string> IdentificationImages { get; set; } = new List<string>();
        public string Image { get; set; }
        public float ImageScale { get; set; } = 1.0f;
        public string EnvironmentImage { get; set; }
        public string CardImage { get; set; }
        public string AmbienceSound { get; set; }
        public string Wikipedia { get; set; }
        public string AustralianMuseum { get; set; }
        public List<SpeciesReference> References { get; set; } = new List<SpeciesReference>();

        /// <summary>
        /// Creates a deep copy of the species data
        /// </summary>
        public Species Clone()
        {
            var clone = new Species
            {
                Id = Id,
                Enabled = Enabled,
                Name = Name,
                ScientificName = ScientificName,
                Type = Type,
                History = History,
                Habitat = Habitat,
                Diet = Diet,
                Image = Image,
                ImageScale = ImageScale,
                EnvironmentImage = EnvironmentImage,
                CardImage = CardImage,
                AmbienceSound = AmbienceSound,
                Wikipedia = Wikipedia,
                AustralianMuseum = AustralianMuseum,
                Identification = new List<string>(Identification),
                IdentificationImages = new List<string>(IdentificationImages),
                References = new List<SpeciesReference>()
            };

            foreach (var reference in References)
            {
                clone.References.Add(reference.Clone());
            }

            return clone;
        }
    }

    /// <summary>
    /// Represents a reference entry for a species
    /// </summary>
    public partial class SpeciesReference : Resource
    {
        public string Field { get; set; }
        public string ReferenceText { get; set; }

        public SpeciesReference Clone()
        {
            return new SpeciesReference
            {
                Field = Field,
                ReferenceText = ReferenceText
            };
        }
    }
} 