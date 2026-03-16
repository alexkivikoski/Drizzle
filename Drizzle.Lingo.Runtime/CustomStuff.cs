using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drizzle.Lingo.Runtime
{
    public static class HashColorize
    {
        public static LingoImage HashColorizedCopy(this LingoImage image, string fileName)
        {
            if (fileName.Contains("layer", StringComparison.InvariantCultureIgnoreCase)) throw new Exception("Not for layers");
            if (fileName.Contains("gradient", StringComparison.InvariantCultureIgnoreCase)) throw new Exception("Not for layers");
            var namepart = Path.GetFileNameWithoutExtension(fileName);
            var namehash = namepart.Substring(0, Math.Min(namepart.Length, 8)).GetHashCode(StringComparison.InvariantCultureIgnoreCase);

            var mask = image.makesilhouette(0).createmask();
            var hashCol = LingoColor.BitUnpack(namehash);
            var src = image.duplicate();
            src.fill(hashCol);

            var dst = image.duplicate();


            dst.copypixels(src, src.rect, src.rect, new LingoPropertyList { [new LingoSymbol("mask")] = mask });
            dst.IsHashColored = true;
            dst.HashColor = hashCol;
            return dst;
            
           
        }
    }
}
