using Drizzle.Lingo.Runtime;
using Drizzle.Lingo.Runtime.Cast;
using Drizzle.Ported;
using Microsoft.Scripting;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Intrinsics.Arm;
using static System.Net.Mime.MediaTypeNames;

namespace Drizzle.Logic.Rendering;

public sealed partial class LevelRenderer
{
    private static readonly string PngSoftwareName = $"Drizzle {Assembly.GetExecutingAssembly().GetName().Version}";

    // This partial contains core rendering logic.
    private int _cameraIndex;
    private int _countCamerasDone;

    private int _framesTotal;
    private int _currentFrame;


    private List<(string, LingoImage)> otherExportedImages = new List<(string, LingoImage)>();

    public RenderStage[] EnabledStages { get; set; } = Enum.GetValues<RenderStage>();


    public string OutputDir
    {
        get
        {
            if (Movie?.gLoadedName == null) return null;
            var levelsDir = Path.Combine(LingoRuntime.MovieBasePath, "Levels");
            var levelDir = Path.Combine(levelsDir, (string)Movie.gLoadedName);
            return levelDir;
        }
    }

    public void DoRender()
    {
        RenderStart();

        // Set up camera order.
        List<int> camOrder;
        if (_singleCamera is { } s)
        {
            camOrder = new List<int> { s + 1 };
        }
        else
        {
            camOrder = Enumerable.Range(1, (int)Movie.gCameraProps.cameras.count).ToList();

            if (Movie.gPrioCam is not (null or 0))
            {
                camOrder.Remove(Movie.gPrioCam);
                camOrder.Insert(0, Movie.gPrioCam);
            }
        }
        var levelsDir = Path.Combine(LingoRuntime.MovieBasePath, "Levels");
        var levelDir = Path.Combine(levelsDir, (string)Movie.gLoadedName);
        var layersDir = OutputDir;
        
        if (EnabledStages.Contains(RenderStage.SaveFile))
        {

            // Save image.


            if (!Directory.Exists(levelsDir))
            {
                Directory.CreateDirectory(levelsDir);
            }
            if (!Directory.Exists(levelDir))
            {
                Directory.CreateDirectory(levelDir);
            }
            if (!Directory.Exists(layersDir))
            {
                Directory.CreateDirectory(layersDir);
            }
        }

            foreach (var camIndex in camOrder)
        {
            try
            {
                if (EnabledStages.Contains(RenderStage.CameraSetup))
                    RenderSetupCamera(camIndex);

               


                if (EnabledStages.Contains(RenderStage.RenderLayers))
                    RenderLayers();

                if (EnabledStages.Contains(RenderStage.RenderPropsPreEffects))
                {
                    //ClearLayers();
                    RenderPropsPreEffects();
                   // OutputImages(layersDir, "layer{0}", "props_pre{0}");
                }

                if (EnabledStages.Contains(RenderStage.RenderEffects))
                {
                    //ClearLayers();
                    RenderEffects();
                    //OutputImages(layersDir, "layer{0}", "effect{0}");
                }

                if (EnabledStages.Contains(RenderStage.RenderPropsPostEffects))
                {
                  //  ClearLayers();
                    RenderPropsPostEffects();
                  //  OutputImages(layersDir, "layer{0}", "props_post{0}");
                }



                if (EnabledStages.Contains(RenderStage.RenderLight))
                    RenderLight();

                if (EnabledStages.Contains(RenderStage.Finalize))
                {
                    RenderFinalize();

                }

                if (EnabledStages.Contains(RenderStage.RenderColors))
                {
                    try
                    {
                        RenderColors();
                    } catch(Exception ex)
                    {

                    }
                    OutputImages(layersDir, "finalImage{0}");
                    OutputImages(layersDir, "rainBowMask{0}");
                    OutputImages(layersDir, "finalDecalImage{0}");
                    OutputImages(layersDir, "fogImage{0}");
                    OutputImages(layersDir, "dpImage{0}");
                    
                }
                if (EnabledStages.Contains(RenderStage.SaveFile))
                {
                    OutputImages(layersDir, "layer{0}");
                    OutputImages(layersDir, "rainBowMask{0}");
                    OutputImages(layersDir, "layer{0}dc");
                    OutputImages(layersDir, "gradientA{0}");
                    OutputImages(layersDir, "gradientB{0}");

                    Export2(layersDir, Movie.gExport_finalImage, "gfinalImage");
                    Export2(layersDir, Movie.gExport_finalDecalImage, "gfinalDecalImage");
                    Export2(layersDir, Movie.gExport_fogImage, "gfogImage");
                    Export2(layersDir, Movie.gExport_rainBowMask, "grainBowMask");
                    Export2(layersDir, Movie.gExport_flattenedGradientA, "gflattenedGradientA");
                    Export2(layersDir, Movie.gExport_flattenedGradientB, "gflattenedGradientB");
                    Export2(layersDir, Movie.gExport_dpImage, "gdpImage");
                }

                if (EnabledStages.Contains(RenderStage.RenderLayerMaterials) && false)
                {
                    this._runtime.UseHashColoredTextures = true;

                    RenderLayers();
                    
                    RenderPropsPreEffects();
                    RenderEffects();
                    RenderPropsPostEffects();
                    this._runtime.UseHashColoredTextures = false;
                    OutputImages(layersDir, "layer{0}","object{0}");
                    OutputImages(layersDir, "layer{0}dc", "object{0}dc");
                }

                if (EnabledStages.Contains(RenderStage.Unify) && false)
                    RenderUnify();

                if (EnabledStages.Contains(RenderStage.Finished))
                    RenderFinished();

                if (EnabledStages.Contains(RenderStage.SaveFile) )
                {



                    _framesTotal = 30;
                    RenderStartFrame(RenderStage.SaveFile);

                    var stat = new RenderStageStatus(RenderStage.SaveFile);

                    
                    OutputAllImages(layersDir);

                    NotifyCompleted(RenderStage.SaveFile);
                }



                OnScreenRenderCompleted?.Invoke(camIndex, _runtime.GetCastMember("finalImage")!.image!);
                _countCamerasDone += 1;
            }
            catch (RenderCancelledException)
            {
                // Pass through.
                throw;
            }
            catch (Exception e)
            {
                throw new RenderCameraException($"Exception on camera {camIndex}", e);
            }
        }

        // Output level data.
        //Movie.newmakelevel(Movie.gLoadedName);
    }
    private void OutputImages(string layersDir, string layerType, string outputName = null)
    {
        List<(string filename, LingoImage image)> images = new();
        for (int i = 0; i < 30; i++)
        {
            var withNum = string.Format(layerType, i);
            if (_runtime.GetCastMember(withNum)?.image is LingoImage img2)
            {
                if (img2.Height == 1) continue;
                if (outputName == null)
                {
                    images.Add((withNum, img2));
                }
                else
                {
                    var n = string.Format(outputName, i);
                    images.Add((n, img2));
                }
            }
        }
        foreach ((string filename, LingoImage image) in images)
        {
            var fileName2 = Path.Combine(layersDir,
                $"{filename}.png"
            );
            var imgSharp2 = image.GetImgSharpImage();
            if (File.Exists(fileName2))
            {
                File.Delete(fileName2);
            }
            imgSharp2.SaveAsPng(fileName2);
        }
    }
    private void OutputAllImages(string layersDir,params string[] excludedNames)
    {
        List<(string filename, LingoImage image)> images = new();

        for (int i = 0; i < 30; i++)
        {


            foreach (var layerType in ExtraLayers.LayerTypes)
            {
                var withNum = string.Format(layerType, i);

                if (_runtime.GetCastMember(withNum)?.image is LingoImage img2)
                {
                    if (img2.Height == 1) continue;
                    if (excludedNames?.Any(s=>withNum.Contains(s, StringComparison.OrdinalIgnoreCase)) == true)
                    {
                        // ignored
                        continue;
                    }
                    images.Add((withNum, img2));
                }
            }
            
        }
        foreach (var extra in otherExportedImages)
        {
            images.Add(extra);
        }
        int count = 0;
        _framesTotal = images.Count;

        RenderStartFrame(RenderStage.SaveFile);
        var stat = new RenderStageStatus(RenderStage.SaveFile);

        foreach ((string filename, LingoImage image) in images)
        {
            _currentFrame = count;
            SendUpdateStatus(stat);

            var fileName2 = Path.Combine(layersDir,
                $"{filename}.png"
            );


            var imgSharp2 = image.GetImgSharpImage();
            if (File.Exists(fileName2))
            {
                File.Delete(fileName2);
            }
            imgSharp2.SaveAsPng(fileName2);
            count++;
        }
    }
    private void RenderStart()
    {
        RenderStartFrame(RenderStage.Start);
        _runtime.CreateScript<renderStart>().exitframe();
        NotifyCompleted(RenderStage.Start);
    }

    private void RenderSetupCamera(int camIndex)
    {
        RenderStartFrame(RenderStage.CameraSetup);

        var camera = (LingoPoint)Movie.gCameraProps.cameras[camIndex];
        _cameraIndex = camIndex;
        Movie.gCurrentRenderCamera = new LingoNumber(camIndex);


        Movie.gRenderCameraTilePos = new LingoPoint((LingoNumber)0, (LingoNumber)0);
        Movie.gRenderCameraPixelPos = (Movie.gRenderCameraTilePos * 20); //camera - (Movie.gRenderCameraTilePos * 20);
        Movie.gRenderCameraPixelPos.loch = Movie.gRenderCameraPixelPos.loch.integer;
        Movie.gRenderCameraPixelPos.locv = Movie.gRenderCameraPixelPos.locv.integer;
        Movie.gPrioCam = camera;
        NotifyCompleted(RenderStage.CameraSetup);
    }
    private void StoreHashColorLayers()
    {
        int cols = (int)Movie.gLOprops.size.loch;
        int rows = (int)Movie.gLOprops.size.locv;
        for (var i = 0; i < 30; i++)
        {
            var orig = _runtime.GetCastMember($"layer{i}")!;
            otherExportedImages.Add(($"object{i}", orig.image));
            orig.image = new LingoImage(cols * 20, rows * 20, 32);
            //_runtime.GetCastMember($"object{i}")!.CloneFrom(orig);
        }
    }
    private void ClearLayers()
    {
        int cols = (int)Movie.gLOprops.size.loch;
        int rows = (int)Movie.gLOprops.size.locv;
        for (var i = 0; i < 30; i++)
        {
            _runtime.GetCastMember($"layer{i}")!.image = new LingoImage(cols * 20, rows * 20, 32);
            _runtime.GetCastMember($"gradientA{i}")!.image = new LingoImage(cols * 20, rows * 20, ImageType.L8);
            _runtime.GetCastMember($"gradientB{i}")!.image = new LingoImage(cols * 20, rows * 20, ImageType.L8);
            _runtime.GetCastMember($"layer{i}dc")!.image = new LingoImage(cols * 20, rows * 20, 32);
        }
    }
    private void RenderLayers()
    {
        
        int cols = (int)Movie.gLOprops.size.loch;
        int rows = (int)Movie.gLOprops.size.locv;

        RenderStartFrame(RenderStage.RenderLayers);

        ClearLayers();

        _runtime.GetCastMember("rainBowMask")!.image = new LingoImage(cols * 20, rows * 20, 32);

        var sw = Stopwatch.StartNew();
        // Movie.gSkyColor = new LingoColor(0, 0, 0);
        Movie.gTinySignsDrawn = new LingoNumber(0);
        Movie.gRenderTrashProps = new LingoList();
        _runtime.GetCastMember(@"finalImage")!.image = new LingoImage(cols * 20, rows * 20, 32);
        _runtime.Global.the_randomSeed = Movie.gLOprops.tileseed;
        _framesTotal = 3;
        for (var i = 3; i > 0; i--)
        {
            _currentFrame = 3 - i;
            // Don't measure pauses as part of the stopwatch.
            sw.Stop();
            RenderStartFrame(new RenderStageStatusLayers(i));
            sw.Start();

            if (ShouldSendPreview())
            {
                var images = new LingoImage[30];
                for (var j = 0; j < 30; j++)
                {
                    images[j] = _runtime.GetCastMember($"layer{j}")!.image!.DuplicateShared();
                }

                SendPreview(new RenderPreviewProps(images));
            }

            Movie.setuplayer(new LingoNumber(i));
        }

        Movie.gLastImported = "";
        Log.Information("{LevelName} rendered layers in {ElapsedMilliseconds} ms",
            Movie.gLoadedName, sw.ElapsedMilliseconds);

        Movie.c = new LingoNumber(1);
        NotifyCompleted(RenderStage.RenderLayers);
    }

    const int EntitiesPerUpdate = 20;

    private void RenderPropsPreEffects()
    {
        RenderStartFrame(RenderStage.RenderPropsPreEffects);
        Movie.afterEffects = new LingoNumber(0);
        _runtime.CreateScript<renderPropsStart>().exitframe();
        _framesTotal = Movie.propsToRender.count.IntValue;

        var script = _runtime.CreateScript<renderProps>();
        while (Movie.keepLooping == 1)
        {
            if (ShouldSendPreview())
            {
                var images = new LingoImage[30];
                for (var j = 0; j < 30; j++)
                {
                    images[j] = _runtime.GetCastMember($"layer{j}")!.image!.DuplicateShared();
                }

                SendPreview(new RenderPreviewProps(images));
            }
            if (Movie.c.IntValue % EntitiesPerUpdate == 0)
            {
                // StatusChanged?.Invoke(new RenderStatus(Movie.c.IntValue / 20, Movie.propsToRender.count.IntValue, false, new(RenderStage.RenderPropsPreEffects)));
            }
            RenderStartFrame(RenderStage.RenderPropsPreEffects);
            script.newframe();
            _currentFrame = Movie.c.IntValue;
        }
        NotifyCompleted(RenderStage.RenderPropsPreEffects);
    }

    private void RenderEffects()
    {
        RenderStartFrame(RenderStage.RenderEffects);
        _runtime.CreateScript<renderEffectsStart>().exitframe();

        var script = _runtime.CreateScript<renderEffects>();

        while (Movie.keepLooping == 1)
        {
            if (ShouldSendPreview())
            {
                var images = new LingoImage[30];
                for (var j = 0; j < 30; j++)
                {
                    images[j] = _runtime.GetCastMember($"layer{j}")!.image!.DuplicateShared();
                }

                SendPreview(new RenderPreviewEffects(
                    images,
                    _runtime.GetCastMember("blackOutImg1")!.image!,
                    _runtime.GetCastMember("blackOutImg2")!.image!));
            }

            var effectsList = (LingoList)Movie.gEEprops.effects;
            var effectNames = effectsList.List.Select(e => (string)((dynamic)e!).nm).ToArray();
            var totalCount = effectsList.List.Count;
            var curr = (int)Movie.r;
            var vert = (int)Movie.vertRepeater;
            RenderStartFrame(new RenderStageStatusEffects(totalCount, curr, vert, effectNames));
            script.newframe();
        }
        NotifyCompleted(RenderStage.RenderEffects);
    }

    private void RenderPropsPostEffects()
    {
        RenderStartFrame(RenderStage.RenderPropsPostEffects);
        Movie.afterEffects = new LingoNumber(1);
        _runtime.CreateScript<renderPropsStart>().exitframe();

        var script = _runtime.CreateScript<renderProps>();
        _framesTotal = Movie.propsToRender.count.IntValue;
        while (Movie.keepLooping == 1)
        {
            if (ShouldSendPreview())
            {
                var images = new LingoImage[30];
                for (var j = 0; j < 30; j++)
                {
                    images[j] = _runtime.GetCastMember($"layer{j}")!.image!.DuplicateShared();
                }

                SendPreview(new RenderPreviewProps(images));
            }

            RenderStartFrame(RenderStage.RenderPropsPostEffects);
            script.newframe();
        }

        // Can clear prop/tile LRU cache now since we won't use it from here on.
        Movie.ImageCacheClear();
    }

    private void RenderLight()
    {
        RenderStartFrame(RenderStage.RenderLight);
        _runtime.CreateScript<renderLightStart>().exitframe();

        if (Movie.gLOprops.light == new LingoNumber(0))
            return;

        var script = _runtime.CreateScript<renderLight>();
        LingoImage[] images = null;
        while (Movie.keepLooping == 1)
        {
            if (ShouldSendPreview() || EnabledStages.Contains(RenderStage.SaveFile))
            {
                images = new LingoImage[30];
                for (var j = 0; j < 30; j++)
                {
                    images[j] = _runtime.GetCastMember($"layer{j}sh")!.image!.DuplicateShared();
                }

                SendPreview(new RenderPreviewLights(images));
            }

            var curr = (int)Movie.c;
            RenderStartFrame(new RenderStageStatusLight(curr));

            script.newframe();
            if (EnabledStages.Contains(RenderStage.SaveFile) && images?.ElementAtOrDefault(curr) is LingoImage img)
            {
                var fileName2 = Path.Combine(OutputDir, $"layer{curr}sh.png");
                img.GetImgSharpImage().SaveAsPng(fileName2);
            }
        }
        NotifyCompleted(RenderStage.RenderLight);
    }
    private void RenderUnify()
    {
        Movie.lvlPropOutput = LingoGlobal.TRUE;
        _runtime.CreateScript<unify>().exitframe();
    }
    private void RenderFinalize()
    {
        RenderStartFrame(RenderStage.Finalize);
        _runtime.CreateScript<finalize>().exitframe();
        _runtime.CreateScript<unify>().exitframe();
        NotifyCompleted(RenderStage.Finalize);
    }
    private void Export2(string layersDir, LingoList gExport, string outputName)
    {
        for (int i = 0; i < gExport.List.Count; i++)
        {
            
            if (gExport.List[i] is LingoImage img2)
            {
                if (img2.Height == 1) continue;
                
                var n = string.Format(outputName, i);
                var fileName2 = Path.Combine(layersDir,
                 $"{outputName}{i}.png"
                );
                var imgSharp2 = img2.GetImgSharpImage();
                if (File.Exists(fileName2))
                {
                    File.Delete(fileName2);
                }
                imgSharp2.SaveAsPng(fileName2);

            }
        }
    }
    private void RenderColors()
    {
        var oldRenderColors = Environment.GetEnvironmentVariable("DRIZZLE_OLD_RENDER_COLORS") is not (null or "0");
        oldRenderColors = true;
        if (!oldRenderColors)
        {
            Log.Debug("Using new RenderColors");
            _framesTotal = 31 * Movie.gLOprops.size.locv.IntValue;
            for (int i = -1; i < 30; i++)
            {
                _currentFrame = i + 1;
                Movie.c = 1;
                Movie.keepLooping = 1;
                while (Movie.keepLooping == 1)
                {
                    RenderStartFrame(RenderStage.RenderColors);
                    //StatusChanged?.Invoke(new RenderStatus(Movie.c.IntValue + (i+1) * 800, 800*30, false, new RenderStageStatus(RenderStage.RenderColors)));
                    RenderColorsNewFramee(i);
                }
            }
        }
        else
        {
            Log.Debug("Using old RenderColors");
            var script = _runtime.CreateScript<renderColors>();
            Movie.c = 0;

            Movie.keepLooping = 1;
            _framesTotal = 30 * Movie.gLOprops.size.locv.IntValue; ;
            while (Movie.keepLooping == 1)
            {
                _currentFrame = Movie.c.IntValue;
                RenderStartFrame(RenderStage.RenderColors);
                //StatusChanged?.Invoke(new RenderStatus(Movie.c.IntValue, 800, false, new RenderStageStatus(RenderStage.RenderColors)));
                Log.Debug("RenderColors: " +  _currentFrame);
                script.newframe();
            }
        }
        NotifyCompleted(RenderStage.RenderColors);
    }

    private void RenderFinished()
    {
        RenderStartFrame(RenderStage.Finished);
        _runtime.CreateScript<finished>().exitframe();
        NotifyCompleted(RenderStage.Finished);
    }
}
