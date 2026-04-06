using System.Diagnostics;
using System.Linq;

namespace Drizzle.Lingo.Runtime.Cast;

public partial class CastMember
{
    private LingoImage? _image;
    private LingoImage? _hashColoredImage;
    public LingoImage? HashColoredImage {
        get => _hashColoredImage;
        set => _hashColoredImage = value;
    } 


    public LingoImage? image
    {
        get
        {
            AssertType(CastMemberType.Bitmap);
            if (Runtime.UseHashColoredTextures )
            {
                return _hashColoredImage ?? _image;
            }
            else
            {
                return _image;
            }
        }
        set
        {
            AssertType(CastMemberType.Bitmap);
            _image = value;
        }
    }

    public LingoRect rect
    {
        get
        {
            AssertType(CastMemberType.Bitmap);
            return _image!.rect;
        }
    }

    public LingoNumber width
    {
        get
        {
            AssertType(CastMemberType.Bitmap);
            return image!.width;
        }
    }

    public LingoNumber height
    {
        get
        {
            AssertType(CastMemberType.Bitmap);
            return image!.height;
        }
    }

    public LingoColor getpixel(int x, int y)
    {
        AssertType(CastMemberType.Bitmap);
        return image!.getpixel(x, y);
    }

    public LingoColor getpixel(LingoNumber x, LingoNumber y) => getpixel((int)x, (int)y);

    public LingoPoint regpoint { get; set; }

    private static string[] ExcludedFromHashColor = ["ayer", "radient"];
    private void ImportFileImplBitmap(string path)
    {
        FullPath = path;
        _image = LingoImage.LoadFromPath(path).Trimmed();
        if (Runtime.UseHashColoredTextures && !ExcludedFromHashColor.Any(s => path.Contains(s)))
        {
            _hashColoredImage = _image.duplicate();
            _hashColoredImage = _hashColoredImage.HashColorizedCopy(path); //LingoImage.LoadFromPathHashColorized(path).Trimmed();
            Debug.Assert(_image != _hashColoredImage);
        }
        
#if false
        if (Runtime.UseHashColoredTextures)
        {
            image = LingoImage.LoadFromPathHashColorized(path).Trimmed();
            IsHashColorized = true;
        }
        else
        {
            
            IsHashColorized = false;
        }
#endif
    }
}
