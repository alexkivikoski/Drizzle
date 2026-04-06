using Drizzle.Logic.Rendering;
using Drizzle.Ported;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Meow.MeowHash;

namespace Drizzle.ConsoleApp
{
    public class LevelRendererConsoleOutput
    {
        const char ProgressSymbol = '█';
        const char EmptySymbol = '░';
        const int ProgressBarWidth = 20;
        public bool Active { get; set; }
        public int TotalCount { get; }
        public int CompletedCount { get; private set; }
        Dictionary<string, LevelRenderer> LevelRenderers { get; } = new Dictionary<string, LevelRenderer>();
        DateTime startTime;
        DateTime? endTime;
        public LevelRendererConsoleOutput(IList<string> levelnames)
        {
            startTime = DateTime.Now;
            TotalCount = levelnames.Count;
            foreach (var name in levelnames)
            {
                LevelRenderers.Add(name, null);
            }
        }
        string formatTimespan(TimeSpan timespan)
        {
            return timespan.ToString(@"mm\:ss").PadRight(6);
        }
        public void SetRenderer(string levelname, LevelRenderer renderer)
        {
            LevelRenderers[levelname] = renderer;
        }
        public void CompleteRendederer(string levelname)
        {
            //LevelRenderers.Remove(levelname);
            LevelRenderers[levelname] = null;
            CompletedCount++;
        }
        public void PrintStatus()
        {

            var currentY = Console.CursorTop;
            var neededRows = LevelRenderers.Count + 2;
            var completeds = LevelRenderers.Values.OfType<LevelRenderer>().Count(l => l.EndTime.HasValue);
            Console.SetCursorPosition(0, Math.Max(0, currentY - neededRows));
            var dur = " " + formatTimespan((endTime ?? DateTime.Now) - startTime);
            var line1 = $"─ RENDERED {CompletedCount} of {TotalCount} ROOMS ".PadRight(Console.WindowWidth - dur.Length, '─');
            line1 += dur;
            Console.WriteLine(line1);
            
                    
            var longestStageName = Enum.GetValues<RenderStage>().Max(s => s.ToString().Length);
            foreach (var pair in LevelRenderers.Where(l=>l.Value != null).ToList())
            {
                try
                {
                    if (pair.Value is LevelRenderer renderer)
                    {
                        var name = renderer!.LevelName.PadRight(16);
                        if (renderer.EndTime.HasValue)
                        {
                            var timeDiff1 = renderer.EndTime.Value - renderer!.StartTime;
                            var timespent = formatTimespan(timeDiff1);
                            var right = $"completed in {timespent}";
                            var line = $" {name}".PadRight(Console.WindowWidth - right.Length);
                            line += right;
                            Console.WriteLine(line);
                        }
                        else if (renderer.Exception != null)
                        {
                            var line = $" {name} failed: {renderer.Exception.Message}";
                            Console.WriteLine(line);
                        }
                        else
                        {

                            var state = renderer!.CurrentStage.ToString().PadRight(longestStageName);
                            int emptys = 20;
                            int filleds = 0;
                            int progressPercentage = 0;

                            if (renderer!.ProgressMax > 0)
                            {
                                var progress = Math.Min(1, (float)renderer!.ProgressCurrent / (float)renderer.ProgressMax);
                                filleds = (int)Math.Floor(progress * ProgressBarWidth);
                                emptys = ProgressBarWidth - filleds;
                                progressPercentage = (int)Math.Floor(progress * 100);
                            }
                            var barLeft = new string(ProgressSymbol, filleds);
                            var barRight = new string(EmptySymbol, emptys);
                            var progressbar = filleds * ProgressSymbol + emptys * EmptySymbol;
                            string progressText = $"{progressPercentage} %".PadLeft(5);
                            var timeDiff = DateTime.Now - renderer!.StartTime;
                            var timespent = formatTimespan(timeDiff);
                            var line = $" {name} {progressText} {state} {barLeft}{barRight}";

                            if (renderer.CurrentStage == RenderStage.Finalize)
                            {

                                var layersLeft = new string(ProgressSymbol, renderer.CompletedLayers);
                                var layersRight = new string(EmptySymbol, 29 - renderer.CompletedLayers);
                                line += " Layer ";
                                line += renderer.CompletedLayers.ToString().PadLeft(2);
                                line += $" {layersLeft}{layersRight} ";
                            }

                            line = line.PadRight(Console.WindowWidth - timespent.Length);
                            line = line + timespent;
                            Console.WriteLine(line);
                        }
                    }
                    else
                    {
                        var right = "pending ";
                        var line = $" {pair.Key}".PadRight(Console.WindowWidth - right.Length);
                        line += right;
                        Console.WriteLine(line);
                    }
                } catch (Exception ex)
                {
                    var msg = ex.Message;
                    Console.WriteLine(msg.Substring(0,Math.Min(Console.WindowWidth,msg.Length)));
                }
            }
        }
    }
}
