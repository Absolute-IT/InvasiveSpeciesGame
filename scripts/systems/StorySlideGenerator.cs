using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace InvasiveSpeciesAustralia.Systems
{
    /// <summary>
    /// Generates story slide PNGs using LibreOffice (PPTX->PDF) and Poppler pdftoppm (PDF->PNG), outputting to user://stories/<id>/
    /// Assumes LibreOffice and Poppler are installed on the OS.
    /// </summary>
    public partial class StorySlideGenerator : Node
    {
        private static StorySlideGenerator _instance;
        public static StorySlideGenerator Instance => _instance;

        private const string GeneratingMarkerName = ".generating";

        public override void _Ready()
        {
            _instance = this;
            ProcessMode = ProcessModeEnum.Always;
        }

        public async void StartGeneration(List<StoryInfo> stories)
        {
            if (stories == null || stories.Count == 0) return;

            foreach (var story in stories)
            {
                if (story == null || string.IsNullOrEmpty(story.File) || string.IsNullOrEmpty(story.Id)) continue;
                try
                {
                    await GenerateSlidesForStory(story);
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"StorySlideGenerator: Failed to generate slides for {story.Id}: {ex.Message}");
                }
            }
        }

        private async System.Threading.Tasks.Task GenerateSlidesForStory(StoryInfo story)
        {
            // Resolve absolute path to PPTX
            string pptxAbsolute = ResolvePptxAbsolutePath(story.File);
            if (string.IsNullOrEmpty(pptxAbsolute) || !File.Exists(pptxAbsolute))
            {
                GD.PrintErr($"StorySlideGenerator: PPTX not found for story '{story.Id}': {story.File}");
                return;
            }

            // Prepare user output dir: user://stories/<id>/
            string userStoryDir = EnsureStoryDir(story.Id);

            // If slides exist and PPTX older than first slide, skip
            if (SlidesUpToDate(userStoryDir, pptxAbsolute))
            {
                return;
            }

            // Convert PPTX -> PDF
            string pdfOsOutDir = ProjectSettings.GlobalizePath(userStoryDir);
            Directory.CreateDirectory(pdfOsOutDir);

            // Create ".generating" marker
            try
            {
                var markerPath = Path.Combine(pdfOsOutDir, GeneratingMarkerName);
                if (!File.Exists(markerPath)) File.WriteAllText(markerPath, DateTime.UtcNow.ToString("O"));
            }
            catch { }

            string soffice = FindSofficeCommand();
            if (string.IsNullOrEmpty(soffice))
            {
                GD.PrintErr("StorySlideGenerator: Could not locate LibreOffice/soffice binary in PATH or known locations.");
                return;
            }

            var pdfProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = soffice,
                    Arguments = $"--headless --convert-to pdf --outdir \"{pdfOsOutDir}\" \"{pptxAbsolute}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };

            pdfProcess.Start();
            await pdfProcess.WaitForExitAsync();
            if (pdfProcess.ExitCode != 0)
            {
                string err = await pdfProcess.StandardError.ReadToEndAsync();
                GD.PrintErr($"StorySlideGenerator: LibreOffice conversion failed for {story.Id}: {err}");
                return;
            }

            // Determine actual pdf name produced (LibreOffice keeps original file name base)
            string producedPdf = Directory
                .GetFiles(pdfOsOutDir, "*.pdf")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .FirstOrDefault();
            if (string.IsNullOrEmpty(producedPdf) || !File.Exists(producedPdf))
            {
                GD.PrintErr($"StorySlideGenerator: PDF not found after conversion for {story.Id}");
                return;
            }

            // Clear existing slide-*.png files in user dir
            DeleteExistingSlides(userStoryDir);

            // Convert PDF -> PNGs using pdftoppm
            string pdftoppm = FindPdftoppmCommand();
            if (string.IsNullOrEmpty(pdftoppm))
            {
                GD.PrintErr("StorySlideGenerator: Could not locate pdftoppm in PATH.");
                return;
            }

            // Output prefix path in OS form, but final files will be re-read via user://
            string outputPrefix = Path.Combine(pdfOsOutDir, "slide");
            var imgProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pdftoppm,
                    Arguments = $"-png -rx 300 -ry 300 \"{producedPdf}\" \"{outputPrefix}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            imgProcess.Start();
            await imgProcess.WaitForExitAsync();
            if (imgProcess.ExitCode != 0)
            {
                string err = await imgProcess.StandardError.ReadToEndAsync();
                GD.PrintErr($"StorySlideGenerator: pdftoppm failed for {story.Id}: {err}");
                return;
            }

            // Rename generated files slide-1.png, slide-2.png ...
            RenumberSlidePngs(userStoryDir);

            // Create thumbnail from slide-1 if not present
            CreateThumbnailIfMissing(userStoryDir);

            // Remove ".generating" marker
            try
            {
                var markerPath = Path.Combine(pdfOsOutDir, GeneratingMarkerName);
                if (File.Exists(markerPath)) File.Delete(markerPath);
            }
            catch { }
        }

        private static string ResolvePptxAbsolutePath(string storyFile)
        {
            if (Path.IsPathRooted(storyFile)) return storyFile;
            // First try project res path
            var projectRoot = ProjectSettings.GlobalizePath("res://").TrimEnd(Path.DirectorySeparatorChar, '/');
            var candidate = Path.Combine(projectRoot, storyFile);
            if (File.Exists(candidate)) return candidate;

            // In exported build, try alongside executable
            var exeDir = Path.GetDirectoryName(OS.GetExecutablePath());
            var candidate2 = Path.Combine(exeDir ?? string.Empty, storyFile);
            if (File.Exists(candidate2)) return candidate2;
            return null;
        }

        private static string EnsureStoryDir(string storyId)
        {
            var dir = DirAccess.Open("user://");
            if (dir == null) return $"user://stories/{storyId}"; // fallback; Godot will create on write
            if (!dir.DirExists("stories")) dir.MakeDir("stories");
            var sub = $"stories/{storyId}";
            if (!dir.DirExists(sub)) dir.MakeDir(sub);
            return $"user://{sub}";
        }

        private static bool SlidesUpToDate(string userStoryDir, string pptxAbsolute)
        {
            var dir = DirAccess.Open(userStoryDir);
            if (dir == null) return false;
            var files = dir.GetFiles();
            string firstSlide = null;
            if (files != null)
            {
                foreach (var f in files)
                {
                    var fs = f.ToString();
                    if (fs.StartsWith("slide-") && fs.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        firstSlide = fs;
                        break;
                    }
                }
            }
            if (string.IsNullOrEmpty(firstSlide)) return false;

            var firstSlideAbs = ProjectSettings.GlobalizePath($"{userStoryDir}/{firstSlide}");
            if (!File.Exists(firstSlideAbs)) return false;

            var pptxTime = File.GetLastWriteTime(pptxAbsolute);
            var slideTime = File.GetLastWriteTime(firstSlideAbs);
            return slideTime >= pptxTime;
        }

        private static void DeleteExistingSlides(string userStoryDir)
        {
            var dir = DirAccess.Open(userStoryDir);
            if (dir == null) return;
            foreach (var f in dir.GetFiles())
            {
                if (f.StartsWith("slide-") && f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    var p = $"{userStoryDir}/{f}";
                    if (Godot.FileAccess.FileExists(p))
                    {
                        Godot.FileAccess.Open(p, Godot.FileAccess.ModeFlags.Write).Close();
                        // Use OS path for deletion reliability
                        var abs = ProjectSettings.GlobalizePath(p);
                        if (File.Exists(abs))
                        {
                            try { File.Delete(abs); } catch { }
                        }
                    }
                }
            }
        }

        private static void RenumberSlidePngs(string userStoryDir)
        {
            var osDir = ProjectSettings.GlobalizePath(userStoryDir);
            var generated = Directory.GetFiles(osDir, "slide-*.png").OrderBy(p => p).ToList();
            if (generated.Count == 0)
            {
                // Some pdftoppm versions produce slide-1.png, slide-2.png directly; also try prefix without dash
                generated = Directory.GetFiles(osDir, "slide*.png").OrderBy(p => p).ToList();
            }

            // Sort by numeric suffix if possible
            int IndexOf(string path)
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var parts = name.Split('-');
                if (parts.Length > 1 && int.TryParse(parts[^1], out var n)) return n;
                // Try slideNN
                var digits = new string(name.Where(char.IsDigit).ToArray());
                if (int.TryParse(digits, out var m)) return m;
                return 0;
            }
            generated = generated.OrderBy(IndexOf).ToList();

            // Rename atomically to slide-1.png, slide-2.png
            for (int i = 0; i < generated.Count; i++)
            {
                var dest = Path.Combine(osDir, $"slide-{i + 1}.png");
                if (!string.Equals(generated[i], dest, StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(dest)) File.Delete(dest);
                    File.Move(generated[i], dest);
                }
            }
        }

        private static void CreateThumbnailIfMissing(string userStoryDir)
        {
            var osDir = ProjectSettings.GlobalizePath(userStoryDir);
            var first = Path.Combine(osDir, "slide-1.png");
            var thumb = Path.Combine(osDir, "thumbnail.png");
            if (!File.Exists(first)) return;

            if (File.Exists(thumb)) return;

            try
            {
                // If ImageMagick is available, generate a proper thumbnail, else copy
                var magick = FindInPath("magick") ?? FindInPath("convert");
                if (!string.IsNullOrEmpty(magick))
                {
                    var p = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = magick,
                            Arguments = $"\"{first}\" -resize 500x300 \"{thumb}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    p.Start();
                    p.WaitForExit();
                    if (p.ExitCode == 0) return;
                }
            }
            catch { }

            try { File.Copy(first, thumb, true); } catch { }
        }

        private static string FindSofficeCommand()
        {
            // macOS common path
            if (OS.GetName() == "macOS")
            {
                var macPath = "/Applications/LibreOffice.app/Contents/MacOS/soffice";
                if (File.Exists(macPath)) return macPath;
            }

            // PATH lookup
            var inPath = FindInPath("soffice") ?? FindInPath("libreoffice");
            return inPath;
        }

        private static string FindPdftoppmCommand()
        {
            // Try PATH first
            var inPath = FindInPath("pdftoppm");
            if (!string.IsNullOrEmpty(inPath)) return inPath;

            // Probe common install locations per platform
            if (OS.GetName() == "macOS")
            {
                string[] macPaths =
                {
                    "/opt/homebrew/bin/pdftoppm",   // Homebrew (Apple Silicon)
                    "/usr/local/bin/pdftoppm",      // Homebrew (Intel)
                    "/opt/local/bin/pdftoppm"       // MacPorts
                };
                foreach (var p in macPaths)
                {
                    if (File.Exists(p)) return p;
                }
            }
            else if (OS.GetName() == "Windows")
            {
                // Common Poppler paths on Windows (adjust as needed)
                var programFiles = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
                var programFilesX86 = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86);
                string[] winPatterns =
                {
                    Path.Combine(programFiles, "poppler-0.68.0", "bin", "pdftoppm.exe"),
                    Path.Combine(programFiles, "poppler-23.11.0", "Library", "bin", "pdftoppm.exe"),
                    Path.Combine(programFilesX86, "poppler-0.68.0", "bin", "pdftoppm.exe")
                };
                foreach (var p in winPatterns)
                {
                    if (File.Exists(p)) return p;
                }
            }
            else
            {
                // Linux
                string[] linuxPaths =
                {
                    "/usr/bin/pdftoppm",
                    "/usr/local/bin/pdftoppm",
                    "/snap/bin/pdftoppm"
                };
                foreach (var p in linuxPaths)
                {
                    if (File.Exists(p)) return p;
                }
            }

            // As a last resort, log PATH for debugging
            GD.PrintErr($"StorySlideGenerator: PATH seen by process: {System.Environment.GetEnvironmentVariable("PATH")}");
            return null;
        }

        /// <summary>
        /// Returns true if a story is currently generating (presence of a .generating marker file)
        /// </summary>
        public static bool IsStoryGenerating(string storyId)
        {
            var userDir = $"user://stories/{storyId}";
            var abs = ProjectSettings.GlobalizePath(userDir);
            var marker = Path.Combine(abs, GeneratingMarkerName);
            return File.Exists(marker);
        }

        /// <summary>
        /// Returns true if the story has at least slide-1.png with non-zero size
        /// </summary>
        public static bool IsStoryReady(string storyId)
        {
            var userDir = $"user://stories/{storyId}";
            var abs = ProjectSettings.GlobalizePath(userDir);
            var first = Path.Combine(abs, "slide-1.png");
            if (!File.Exists(first)) return false;
            try
            {
                var info = new FileInfo(first);
                if (info.Length <= 0) return false;
            }
            catch { return false; }
            return true;
        }

        private static string FindInPath(string cmd)
        {
            try
            {
                var which = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = OS.GetName() == "Windows" ? "where" : "which",
                        Arguments = cmd,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                which.Start();
                which.WaitForExit();
                if (which.ExitCode == 0)
                {
                    var output = which.StandardOutput.ReadToEnd().Trim();
                    var first = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    if (!string.IsNullOrEmpty(first)) return first;
                }
            }
            catch { }
            return null;
        }
    }
}


