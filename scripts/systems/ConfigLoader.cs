using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using InvasiveSpeciesAustralia.Systems;

namespace InvasiveSpeciesAustralia
{
    /// <summary>
    /// Handles loading and merging configuration files from both internal and user directories
    /// </summary>
    public partial class ConfigLoader : Node
    {
        private static ConfigLoader _instance;
        
        public static ConfigLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ConfigLoader();
                }
                return _instance;
            }
        }

        // Configuration data storage
        private Dictionary<string, Species> _speciesData = new Dictionary<string, Species>();
        private List<string> _menuBackgrounds = new List<string>();
        
        // Story data
        private List<StoryInfo> _stories = new List<StoryInfo>();
        
        // JSON serialization options
        private JsonSerializerOptions _jsonOptions;

        public ConfigLoader()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };
        }
        
        public override void _Ready()
        {
            // Ensure singleton instance is set
            _instance = this;
        }

        /// <summary>
        /// Gets all loaded species data
        /// </summary>
        public Dictionary<string, Species> GetSpeciesData()
        {
            return new Dictionary<string, Species>(_speciesData);
        }

        /// <summary>
        /// Gets a specific species by ID
        /// </summary>
        public Species GetSpecies(string id)
        {
            return _speciesData.TryGetValue(id, out var species) ? species : null;
        }

        /// <summary>
        /// Gets all species of a specific type
        /// </summary>
        public List<Species> GetSpeciesByType(string type)
        {
            return _speciesData.Values.Where(s => s.Type == type).ToList();
        }

        /// <summary>
        /// Gets all species
        /// </summary>
        public List<Species> GetAllSpecies()
        {
            return _speciesData.Values.ToList();
        }

        /// <summary>
        /// Gets all enabled species
        /// </summary>
        public List<Species> GetAllEnabledSpecies()
        {
            return _speciesData.Values.Where(s => s.Enabled).ToList();
        }

        /// <summary>
        /// Gets enabled species of a specific type
        /// </summary>
        public List<Species> GetEnabledSpeciesByType(string type)
        {
            return _speciesData.Values.Where(s => s.Type == type && s.Enabled).ToList();
        }

        /// <summary>
        /// Gets all menu background paths
        /// </summary>
        public List<string> GetMenuBackgrounds()
        {
            return new List<string>(_menuBackgrounds);
        }

        /// <summary>
        /// Loads all configuration files
        /// </summary>
        public void LoadAllConfigs()
        {
            GD.Print("ConfigLoader: Starting configuration load...");
            
            // Load species configuration
            LoadSpeciesConfig();
            
            // Load menu backgrounds configuration
            LoadMenuBackgroundsConfig();
            
            // Load story data
            LoadStories();
            
            // Load bug squash levels (static method, already loaded on demand)
            
            // Start LibreOffice/Poppler slide generation for stories
            StartStorySlideGeneration();
            
            // Future config files can be loaded here
            
            GD.Print("ConfigLoader: Configuration load complete.");
        }
        
        /// <summary>
        /// Start LibreOffice/Poppler slide generation for loaded stories
        /// </summary>
        private void StartStorySlideGeneration()
        {
            if (!IsInsideTree())
            {
                CallDeferred(nameof(StartStorySlideGeneration));
                return;
            }

            var generator = StorySlideGenerator.Instance;
            if (generator == null)
            {
                generator = new StorySlideGenerator();
                generator.Name = "StorySlideGenerator";
                GetTree().Root.AddChild(generator);
            }

            generator.StartGeneration(_stories);
        }

        /// <summary>
        /// Loads bug squash stages configuration
        /// </summary>
        public static List<BugSquashStage> LoadBugSquashStages()
        {
            const string fileName = "bug-squash.json";
            string path = $"res://config/{fileName}";
            
            var stages = new List<BugSquashStage>();
            
            if (!FileAccess.FileExists(path))
            {
                GD.PrintErr($"ConfigLoader: Bug squash config not found at {path}");
                return stages;
            }
            
            try
            {
                using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
                if (file == null)
                {
                    GD.PrintErr($"ConfigLoader: Failed to open file {path}");
                    return stages;
                }

                string jsonContent = file.GetAsText();
                file.Close();

                // Parse JSON
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    Converters = { new JsonStringEnumConverter() }
                };
                
                stages = JsonSerializer.Deserialize<List<BugSquashStage>>(jsonContent, options);
                
                if (stages != null)
                {
                    GD.Print($"ConfigLoader: Loaded {stages.Count} bug squash stages");
                }
            }
            catch (Exception e)
            {
                GD.PrintErr($"ConfigLoader: Error loading bug squash file {path}: {e.Message}");
            }
            
            return stages ?? new List<BugSquashStage>();
        }

        /// <summary>
        /// Loads species configuration from internal and user directories
        /// </summary>
        private void LoadSpeciesConfig()
        {
            const string fileName = "species.json";
            
            // Clear existing data
            _speciesData.Clear();
            
            // Load internal configuration first
            string internalPath = $"res://config/{fileName}";
            if (FileAccess.FileExists(internalPath))
            {
                LoadSpeciesFile(internalPath, false);
            }
            else
            {
                GD.PrintErr($"ConfigLoader: Internal species config not found at {internalPath}");
            }
            
            // Load user configuration to override/extend
            string userPath = $"user://config/{fileName}";
            if (FileAccess.FileExists(userPath))
            {
                GD.Print($"ConfigLoader: Loading user species config from {userPath}");
                LoadSpeciesFile(userPath, true);
            }
            else
            {
                GD.Print($"ConfigLoader: No user species config found at {userPath}");
            }
        }

        /// <summary>
        /// Loads a species JSON file and merges it with existing data
        /// </summary>
        private void LoadSpeciesFile(string path, bool isUserFile)
        {
            try
            {
                using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
                if (file == null)
                {
                    GD.PrintErr($"ConfigLoader: Failed to open file {path}");
                    return;
                }

                string jsonContent = file.GetAsText();
                file.Close();

                // Parse JSON into temporary structure for manual mapping
                using var doc = JsonDocument.Parse(jsonContent);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Array)
                {
                    GD.PrintErr($"ConfigLoader: Expected array in {path}");
                    return;
                }

                int loadedCount = 0;
                int updatedCount = 0;

                foreach (var element in root.EnumerateArray())
                {
                    var species = ParseSpeciesFromJson(element);
                    if (species != null && !string.IsNullOrEmpty(species.Id))
                    {
                        if (_speciesData.ContainsKey(species.Id))
                        {
                            _speciesData[species.Id] = species;
                            updatedCount++;
                        }
                        else
                        {
                            _speciesData[species.Id] = species;
                            loadedCount++;
                        }
                    }
                }

                string source = isUserFile ? "user" : "internal";
                GD.Print($"ConfigLoader: Loaded {loadedCount} new and updated {updatedCount} existing species from {source} config");
            }
            catch (Exception e)
            {
                GD.PrintErr($"ConfigLoader: Error loading species file {path}: {e.Message}");
            }
        }

        /// <summary>
        /// Parses a single species from JSON element
        /// </summary>
        private Species ParseSpeciesFromJson(JsonElement element)
        {
            try
            {
                var species = new Species();

                // Parse basic string properties
                if (element.TryGetProperty("id", out var id))
                    species.Id = id.GetString();
                if (element.TryGetProperty("enabled", out var enabled))
                    species.Enabled = enabled.GetBoolean();
                if (element.TryGetProperty("name", out var name))
                    species.Name = name.GetString();
                if (element.TryGetProperty("scientific_name", out var scientificName))
                    species.ScientificName = scientificName.GetString();
                if (element.TryGetProperty("type", out var type))
                    species.Type = type.GetString();
                if (element.TryGetProperty("history", out var history))
                    species.History = history.GetString();
                if (element.TryGetProperty("habitat", out var habitat))
                    species.Habitat = habitat.GetString();
                if (element.TryGetProperty("diet", out var diet))
                    species.Diet = diet.GetString();
                if (element.TryGetProperty("image", out var image))
                    species.Image = image.GetString();
                if (element.TryGetProperty("image_scale", out var imageScale))
                    species.ImageScale = (float)imageScale.GetDouble();
                if (element.TryGetProperty("environment_image", out var environmentImage))
                    species.EnvironmentImage = environmentImage.GetString();
                if (element.TryGetProperty("card_image", out var cardImage))
                    species.CardImage = cardImage.GetString();
                if (element.TryGetProperty("ambience_sound", out var ambienceSound))
                    species.AmbienceSound = ambienceSound.GetString();
                if (element.TryGetProperty("wikipedia", out var wikipedia))
                    species.Wikipedia = wikipedia.GetString();
                if (element.TryGetProperty("australian_museum", out var australianMuseum))
                    species.AustralianMuseum = australianMuseum.GetString();

                // Parse identification array
                if (element.TryGetProperty("identification", out var identification) && 
                    identification.ValueKind == JsonValueKind.Array)
                {
                    species.Identification.Clear();
                    foreach (var item in identification.EnumerateArray())
                    {
                        var text = item.GetString();
                        if (!string.IsNullOrEmpty(text))
                            species.Identification.Add(text);
                    }
                }

                // Parse identification_images array
                if (element.TryGetProperty("identification_images", out var identificationImages) && 
                    identificationImages.ValueKind == JsonValueKind.Array)
                {
                    species.IdentificationImages.Clear();
                    foreach (var item in identificationImages.EnumerateArray())
                    {
                        var imagePath = item.GetString();
                        if (!string.IsNullOrEmpty(imagePath))
                            species.IdentificationImages.Add(imagePath);
                    }
                }

                // Parse references array
                if (element.TryGetProperty("references", out var references) && 
                    references.ValueKind == JsonValueKind.Array)
                {
                    species.References.Clear();
                    foreach (var refElement in references.EnumerateArray())
                    {
                        var reference = new SpeciesReference();
                        if (refElement.TryGetProperty("field", out var field))
                            reference.Field = field.GetString();
                        if (refElement.TryGetProperty("reference", out var referenceText))
                            reference.ReferenceText = referenceText.GetString();
                        
                        if (!string.IsNullOrEmpty(reference.Field) && !string.IsNullOrEmpty(reference.ReferenceText))
                            species.References.Add(reference);
                    }
                }

                return species;
            }
            catch (Exception e)
            {
                GD.PrintErr($"ConfigLoader: Error parsing species: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates the user config directory if it doesn't exist
        /// </summary>
        public void EnsureUserConfigDirectory()
        {
            var dir = DirAccess.Open("user://");
            if (dir != null && !dir.DirExists("config"))
            {
                dir.MakeDir("config");
                GD.Print("ConfigLoader: Created user config directory");
            }
        }

        /// <summary>
        /// Copies the default config to user directory (useful for initial setup)
        /// </summary>
        public void CopyDefaultConfigToUser(string fileName)
        {
            EnsureUserConfigDirectory();
            
            string sourcePath = $"res://config/{fileName}";
            string destPath = $"user://config/{fileName}";
            
            if (!FileAccess.FileExists(sourcePath))
            {
                GD.PrintErr($"ConfigLoader: Source file not found: {sourcePath}");
                return;
            }
            
            if (FileAccess.FileExists(destPath))
            {
                GD.Print($"ConfigLoader: User config already exists: {destPath}");
                return;
            }
            
            using var sourceFile = FileAccess.Open(sourcePath, FileAccess.ModeFlags.Read);
            if (sourceFile == null)
            {
                GD.PrintErr($"ConfigLoader: Failed to open source file: {sourcePath}");
                return;
            }
            
            string content = sourceFile.GetAsText();
            sourceFile.Close();
            
            using var destFile = FileAccess.Open(destPath, FileAccess.ModeFlags.Write);
            if (destFile == null)
            {
                GD.PrintErr($"ConfigLoader: Failed to create destination file: {destPath}");
                return;
            }
            
            destFile.StoreString(content);
            destFile.Close();
            
            GD.Print($"ConfigLoader: Copied default config to user directory: {fileName}");
        }

        /// <summary>
        /// Loads menu backgrounds configuration from internal and user directories
        /// </summary>
        private void LoadMenuBackgroundsConfig()
        {
            const string fileName = "menu-backgrounds.json";
            
            // Clear existing data
            _menuBackgrounds.Clear();
            
            // Load internal configuration first
            string internalPath = $"res://config/{fileName}";
            if (FileAccess.FileExists(internalPath))
            {
                LoadMenuBackgroundsFile(internalPath, false);
            }
            else
            {
                GD.PrintErr($"ConfigLoader: Internal menu backgrounds config not found at {internalPath}");
            }
            
            // Load user configuration to override/extend
            string userPath = $"user://config/{fileName}";
            if (FileAccess.FileExists(userPath))
            {
                GD.Print($"ConfigLoader: Loading user menu backgrounds config from {userPath}");
                LoadMenuBackgroundsFile(userPath, true);
            }
            else
            {
                GD.Print($"ConfigLoader: No user menu backgrounds config found at {userPath}");
            }
        }

        /// <summary>
        /// Loads a menu backgrounds JSON file and merges it with existing data
        /// </summary>
        private void LoadMenuBackgroundsFile(string path, bool isUserFile)
        {
            try
            {
                using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
                if (file == null)
                {
                    GD.PrintErr($"ConfigLoader: Failed to open file {path}");
                    return;
                }

                string jsonContent = file.GetAsText();
                file.Close();

                // Parse JSON into temporary structure for manual mapping
                using var doc = JsonDocument.Parse(jsonContent);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Array)
                {
                    GD.PrintErr($"ConfigLoader: Expected array in {path}");
                    return;
                }

                // Clear existing data if loading from user file (complete override)
                if (isUserFile)
                {
                    _menuBackgrounds.Clear();
                }

                int loadedCount = 0;

                foreach (var element in root.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        var backgroundPath = element.GetString();
                        if (!string.IsNullOrEmpty(backgroundPath) && !_menuBackgrounds.Contains(backgroundPath))
                        {
                            _menuBackgrounds.Add(backgroundPath);
                            loadedCount++;
                        }
                    }
                }

                string source = isUserFile ? "user" : "internal";
                GD.Print($"ConfigLoader: Loaded {loadedCount} menu backgrounds from {source} config");
            }
            catch (Exception e)
            {
                GD.PrintErr($"ConfigLoader: Error loading menu backgrounds file {path}: {e.Message}");
            }
        }

        /// <summary>
        /// Loads story data from stories.json
        /// </summary>
        public void LoadStories()
        {
            _stories.Clear();
            string configPath = "res://config/stories.json";
            
            try
            {
                if (FileAccess.FileExists(configPath))
                {
                    using var file = FileAccess.Open(configPath, FileAccess.ModeFlags.Read);
                    string jsonString = file.GetAsText();
                    
                    // Parse the JSON
                    var json = new Json();
                    var parseResult = json.Parse(jsonString);
                    
                    if (parseResult == Error.Ok)
                    {
                        var data = json.Data;
                        if (data.VariantType == Variant.Type.Array)
                        {
                            var storyArray = data.AsGodotArray();
                            foreach (var item in storyArray)
                            {
                                if (item.VariantType == Variant.Type.Dictionary)
                                {
                                    var storyDict = item.AsGodotDictionary();
                                    var story = ParseStoryInfo(storyDict);
                                    if (story != null)
                                    {
                                        _stories.Add(story);
                                    }
                                }
                            }
                            
                            GD.Print($"ConfigLoader: Loaded {_stories.Count} stories");
                        }
                    }
                    else
                    {
                        GD.PrintErr($"ConfigLoader: Failed to parse stories.json: {parseResult}");
                    }
                }
                else
                {
                    GD.PrintErr($"ConfigLoader: stories.json not found at {configPath}");
                }
            }
            catch (System.Exception e)
            {
                GD.PrintErr($"ConfigLoader: Error loading stories: {e.Message}");
            }
        }
        
        /// <summary>
        /// Parses a story dictionary into StoryInfo object
        /// </summary>
        private StoryInfo ParseStoryInfo(Godot.Collections.Dictionary dict)
        {
            var story = new StoryInfo();

            // Required fields
            if (dict.ContainsKey("title"))
                story.Title = dict["title"].ToString();
            else
                return null;

            // Optional fields
            if (dict.ContainsKey("id"))
                story.Id = dict["id"].ToString();
            else
                story.Id = story.Title.ToLower().Replace(" ", "-");

            if (dict.ContainsKey("description"))
                story.Description = dict["description"].ToString();

            if (dict.ContainsKey("file"))
                story.File = dict["file"].ToString();

            if (dict.ContainsKey("thumbnail"))
                story.Thumbnail = dict["thumbnail"].ToString();

            if (dict.ContainsKey("visible"))
                story.Visible = (bool)dict["visible"];

            return story;
        }
        
        /// <summary>
        /// Gets all loaded stories
        /// </summary>
        public List<StoryInfo> GetStories()
        {
            return new List<StoryInfo>(_stories);
        }
        
        /// <summary>
        /// Gets a specific story by ID
        /// </summary>
        public StoryInfo GetStoryById(string id)
        {
            return _stories.Find(s => s.Id == id);
        }
    }
} 