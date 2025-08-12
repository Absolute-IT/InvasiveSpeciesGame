using System.Collections.Generic;

namespace InvasiveSpeciesAustralia.Systems
{
    /// <summary>
    /// Minimal story metadata loaded from config/stories.json
    /// </summary>
    public class StoryInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string File { get; set; } // Path to .pptx (relative to project root or absolute)
        public string Thumbnail { get; set; } // Optional static thumbnail path
        public bool Visible { get; set; } = true;

        // Runtime: populated by generator for convenience (not serialized)
        public List<string> GeneratedSlides { get; set; } = new List<string>(); // user:// paths
    }
}


