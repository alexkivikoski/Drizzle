using Drizzle.Lingo.Runtime;
using Drizzle.Ported;
using Serilog;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;




namespace Drizzle.Logic.Rendering;

public  partial class LevelRenderer
{
    private static readonly string PngSoftwareName = $"Drizzle {Assembly.GetExecutingAssembly().GetName().Version}";

    // This partial contains core rendering logic.
    private int _cameraIndex;
    private int _countCamerasDone;

    private int _framesTotal;
    private int _currentFrame;
    public int CompletedLayers { get; private set; }
    public List<(RenderStage, DateTime?)> CompletedTimes { get; } = new List<(RenderStage, DateTime?)>();
    public DateTime StartTime { get; } = DateTime.Now;
    public DateTime? EndTime { get; private set; }
    public RenderStage CurrentStage { get; private set; }
    public int ProgressMax { get; private set; }
    public int ProgressCurrent { get; private set; }
    public string LevelName => (string)Movie?.gLoadedName;
    public bool Overwrite { get; set; }
    public Exception Exception { get; private set; }

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

    public void DoRender(IList<RenderStage> stages)
    {
        this.EnabledStages = stages.ToArray();
        
        this.CompletedTimes.Add((RenderStage.RenderLayers, null));
        this.CompletedTimes.Add((RenderStage.RenderPropsPreEffects, null));
        this.CompletedTimes.Add((RenderStage.RenderPropsPreEffects, null));
        this.CompletedTimes.Add((RenderStage.RenderEffects, null));
        this.CompletedTimes.Add((RenderStage.RenderPropsPostEffects, null));
        this.CompletedTimes.Add((RenderStage.RenderLight, null));
        this.CompletedTimes.Add((RenderStage.Finalize, null));

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

        

        //if (EnabledStages.Contains(RenderStage.SaveFile))
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
                {
                    RenderSetupCamera(camIndex);
                }




                if (EnabledStages.Contains(RenderStage.RenderLayers))
                {
                    RenderLayers();
                   // OutputImages(layersDir, "layer{0}", "layer{0}prefx");
                }
#if DEBUG
                RenderFinalize();
#endif

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
                {
                    RenderLight();
                }

                if (EnabledStages.Contains(RenderStage.Finalize))
                {
                    RenderFinalize();
                    //SaveImageIfExists(layersDir, "blackOutImg1");
                    //SaveImageIfExists(layersDir, "blackOutImg2");
                    //SaveImageIfExists(layersDir, "GradientOutput");
                }
#if false
                if (EnabledStages.Contains(RenderStage.RenderColors) && false)
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
                if (EnabledStages.Contains(RenderStage.SaveFile) && false)
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

                if (EnabledStages.Contains(RenderStage.Finished) && false)
                    RenderFinished();

                if (EnabledStages.Contains(RenderStage.SaveFile) && false )
                {



                    _framesTotal = 30;
                    RenderStartFrame(RenderStage.SaveFile);

                    var stat = new RenderStageStatus(RenderStage.SaveFile);

                    
                    OutputAllImages(layersDir);

                    NotifyCompleted(RenderStage.SaveFile);
                }

#endif

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
                this.Exception = e;
                throw; //new RenderCameraException($"Exception on camera {camIndex}", e);
            }
        }
        EndTime = DateTime.Now;
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
            using var imgSharp2 = image.GetImgSharpImage();
            if (File.Exists(fileName2))
            {
                File.Delete(fileName2);
            }
            imgSharp2.SaveAsPng(fileName2);
        }
    }
    private void SaveImageIfExists(string layersDir, string membername)
    {
        if (this._runtime.GetCastMember(membername)?.image is LingoImage l)
        {
            SaveImage(layersDir, membername, l);
        }
    }
    void SaveImage(string layersDir, string filename, LingoImage image)
    {
        var fileName2 = Path.Combine(layersDir,
            $"{filename}.png"
        );


        using var imgSharp2 = image.GetImgSharpImage();
        if (File.Exists(fileName2))
        {
            File.Delete(fileName2);
        }
        imgSharp2.SaveAsPng(fileName2);
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
                    SaveImage(layersDir, withNum, img2);
                }
            }
            
        }
        foreach (var extra in otherExportedImages)
        {
            SaveImage(layersDir, extra.Item1,extra.Item2);
            
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
        CurrentStage = RenderStage.RenderLayers;
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
        CurrentStage = RenderStage.RenderPropsPreEffects;
        RenderStartFrame(RenderStage.RenderPropsPreEffects);
        Movie.afterEffects = new LingoNumber(0);
        _runtime.CreateScript<renderPropsStart>().exitframe();
        _framesTotal = Movie.propsToRender.count.IntValue;

        var script = _runtime.CreateScript<renderProps>();
        ProgressMax = Movie.propsToRender.count.IntValue;
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
                // StatusChanged?.Invoke(new RenderStatus(Movie.col.IntValue / 20, Movie.propsToRender.count.IntValue, false, new(RenderStage.RenderPropsPreEffects)));
            }
            RenderStartFrame(RenderStage.RenderPropsPreEffects);
            script.newframe();
            _currentFrame = Movie.c.IntValue;
            ProgressCurrent = Movie.c.IntValue;
        }
        NotifyCompleted(RenderStage.RenderPropsPreEffects);
    }
    private void RenderEffects2()
    {
        CurrentStage = RenderStage.RenderEffects;
        var script = _runtime.CreateScript<renderEffects>();
        var effectsList = (LingoList)Movie.gEEprops.effects;
       // var effectNames = effectsList.List.Select(e => (string)((dynamic)e!).nm).ToArray();
        var totalCount = effectsList.List.Count;
        var cols = (int)Movie.gLOprops.size.loch;
        var rows = (int)Movie.gLOprops.size.locv;

        ProgressMax = totalCount * cols * rows;
        ProgressCurrent = 0;
        List<(int col, int row)> blocks = new List<(int col, int row)>();
        for (int col = 0; col < cols-1; col++)
        {
            for (int row = 0; row < rows-1; row++)
            {
                blocks.Add((col, row));
            }
        }
        for (int r = 0; r < totalCount; r++)
        {
            Parallel.ForEach(blocks, (block) =>
            {
                //-- q2 and c2 are the positions of the tile in global-space ( equivalent to (x, z) )
                int q2 = block.col + 1;
                int c2 = block.row + 1;
                try
                {
                    if ((q2 > 0)  && (q2 <= Movie.gLOprops.size.locH) && (c2 > 0) && (c2 <= Movie.gLOprops.size.locV)) {
                        script.effectontile(q2, c2, q2, c2, effectsList[r + 1]);
                    }
                    else
                    {

                    }
                } catch (Exception ex)
                {
                    throw;
                }
                ProgressCurrent += 1;
            });
        }

    }
    private void RenderEffects()
    {

        CurrentStage = RenderStage.RenderEffects;
        ProgressMax = ((LingoList)Movie.gEEprops.effects).count.IntValue;
        RenderStartFrame(RenderStage.RenderEffects);
        _runtime.CreateScript<renderEffectsStart>().exitframe();

        var script = _runtime.CreateScript<renderEffects>();

        while (Movie.keepLooping == 1)
        {
            ProgressCurrent = Movie.r.IntValue;
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
        CurrentStage = RenderStage.RenderPropsPostEffects;
        RenderStartFrame(RenderStage.RenderPropsPostEffects);
        Movie.afterEffects = new LingoNumber(1);
        _runtime.CreateScript<renderPropsStart>().exitframe();

        var script = _runtime.CreateScript<renderProps>();
        _framesTotal = Movie.propsToRender.count.IntValue;
        ProgressMax = Movie.propsToRender.count.IntValue;
        while (Movie.keepLooping == 1)
        {
            ProgressCurrent = Movie.c.IntValue;
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
    private void RenderFinal()
    {
        var unify = _runtime.CreateScript<unify>();
        var finalize = _runtime.CreateScript<finalize>();
        var renderColors = _runtime.CreateScript<renderColors>();
        for (var j = 0; j < 30; j++)
        {
            Movie.c = j;
            //finalize.exitframe();
            unify.exitframe();
            renderColors.newframe();
        }
    }
    private void RenderLight()
    {
        CurrentStage = RenderStage.RenderLight;
        RenderStartFrame(RenderStage.RenderLight);
        _runtime.CreateScript<renderLightStart>().exitframe();

        if (Movie.gLOprops.light == new LingoNumber(0))
            return;

        var script = _runtime.CreateScript<renderLight>();
        LingoImage[] images = null;
        ProgressMax = 30;
        while (Movie.keepLooping == 1)
        {
            ProgressCurrent = Movie.c.IntValue;
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
                using var sh = img.GetImgSharpImage();
                sh.SaveAsPng(fileName2);
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
        CurrentStage = RenderStage.Finalize;
        RenderStartFrame(RenderStage.Finalize);
        var script = _runtime.CreateScript<finalize>();
        script.startframe();
        Movie.c = 1;
        Movie.keepLooping = 1;
        var width = Movie.gLOprops.size.loch.IntValue * 20;
        ProgressMax = width-1;

        for (int i = 0; i < 30; i++)
        {
            //ProgressCurrent = i*width;
            int layerNum = 29 - i;
            CurrentStage = RenderStage.Finalize;
            script.finalizelayer(i+1);
            var fileName2 = Path.Combine(OutputDir, $"finalImage{layerNum}.png");
            if (File.Exists(fileName2) && !Overwrite) continue;
            for (int x = 0; x < width; x++)
            {
                ProgressCurrent = x;
                for (int y = 0; y < Movie.gLOprops.size.locv.IntValue * 20; y++)
                {
                    script.processcolors(y, x);
                }
                
            }
            if (layerNum == 0) {
                try
                {
                    script.paintdecalcolors();
                } catch (Exception ex)
                {
                    this.Exception = ex;
                }
            }
#if false
            if (EnabledStages.Contains(RenderStage.SaveFile))
            {
                CurrentStage = RenderStage.SaveFile;
                string[] fils = ["finalImage", "fogImage", "dpImage", "shadowImage", "rainBowMask", "flattenedGradientA", "flattenedGradientB", "finalDecalImage"];
                foreach (string fil in fils) {
                    if (this._runtime.GetCastMember(fil)?.image is LingoImage img)
                    {
                        var fileName2 = Path.Combine(OutputDir, $"{fil}{layerNum}.png");
                        using var sh = img.GetImgSharpImage();
                        sh.SaveAsPng(fileName2);
                    }
                }
            } else
            {
#endif
                // just finalimage
                //SaveImageIfExists(OutputDir, $"finalImage{layerNum}");
                if (this._runtime.GetCastMember($"finalImage")?.image is LingoImage img)
                {
                    
                    using var sh = img.GetImgSharpImage();
                    sh.SaveAsPng(fileName2);
                }
           // }
            CompletedLayers++;
        }
            //_runtime.CreateScript<unify>().exitframe();
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
                using var imgSharp2 = img2.GetImgSharpImage();
                if (File.Exists(fileName2))
                {
                    File.Delete(fileName2);
                }
                imgSharp2.SaveAsPng(fileName2);

            }
        }
    }
    private void RenderColors(string exportPath = null)
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
                    //StatusChanged?.Invoke(new RenderStatus(Movie.col.IntValue + (i+1) * 800, 800*30, false, new RenderStageStatus(RenderStage.RenderColors)));
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
                //StatusChanged?.Invoke(new RenderStatus(Movie.col.IntValue, 800, false, new RenderStageStatus(RenderStage.RenderColors)));
                Log.Debug("RenderColors: " +  _currentFrame);
                script.newframe();
                if (exportPath != null)
                {
                    var thisLayer = this.Movie.DRFinalImage;
                     

                }
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
