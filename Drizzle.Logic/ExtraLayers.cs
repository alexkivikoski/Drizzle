using Drizzle.Lingo.Runtime;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Drizzle.Logic
{
    public static class ExtraLayers
    {
        public static string[] LayerTypes = [
        "layer{0}",
        "layer{0}dc",
        "layer{0}sh",
        "finalImage{0}",
        "dpImage{0}",
        "fogImage{0}",
        "shadowImage{0}",
        "rainBowMask{0}",
        "gradientA{0}",
        "gradientB{0}",
        "flattenedGradientA{0}",
        "flattenedGradientB{0}",
        "finalDecalImage{0}"
            ];
        public static void InitializeExtraCast()
        {
            var dir = "D:\\source\\drizzle3\\Drizzle\\Data\\Cast";
            var editorCast = "levelEditor";
            // find cast for level editor
            var levelEditorFiles = Directory.EnumerateFiles(dir).Where(s => s.Contains(editorCast));

            int GetIndexPart(string path)
            {
                var fileName = Path.GetFileName(path);
                var match = LingoRuntime.CastPathRegex.Match(fileName);
                if (!match.Success)
                {
                    Log.Warning("Warning: Unable to parse {CastFileName} for cast file name", fileName);
                    return 0;
                }

                var cast = match.Groups[1].Value;
                var number = int.Parse(match.Groups[2].Value);
                return number;
            }
            var lastIndex = levelEditorFiles.Max(GetIndexPart);
            

            string templateFile = "layer0.png";

            if (levelEditorFiles.FirstOrDefault(s => s.Contains(templateFile)) is string clonedTemplate)
            {

                for (int i = 0; i < 30; i++)
                {
                    foreach (var layerType in LayerTypes)
                    {
                        var withNum = string.Format(layerType, i);
                        var name = $"{withNum}.png";
                        if (!levelEditorFiles.Any(s => s.EndsWith(name)))
                        {
                            lastIndex++;
                            var fullname = Path.Combine(dir, $"{editorCast}_{lastIndex}_{name}");
                            File.Copy(clonedTemplate, fullname);
                        }
                    }

                }
            }


        }

    }
}
