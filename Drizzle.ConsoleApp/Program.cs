using Drizzle.ConsoleApp;
using Drizzle.Lingo.Runtime;
using Drizzle.Lingo.Runtime.Utils;
using Drizzle.Logic;
using Drizzle.Logic.Rendering;
using Drizzle.Ported;
using Meow;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
//"commandLineArgs": "render D:\\source\\drizzle3\\Drizzle\\Data\\LevelEditorProjects\\World\\SU --region --rooms C04 A41 A42 A43 A44 S01 A22 A23 A24 A25"
//"commandLineArgs": "render D:\\source\\drizzle3\\Drizzle\\Data\\LevelEditorProjects\\World\\SU --region --rooms S04 A37 A63 A39 B04 A63 B12 A53 A06 A38 A36 A34 A33 A17 A40"
//"commandLineArgs": "render D:\\source\\drizzle3\\Drizzle\\Data\\LevelEditorProjects\\World\\SU --region --limit 5"
CultureFix.FixCulture();

if (!CommandLineArgs.TryParse(args, out var parsedArgs))
    return 1;

var isCi = Environment.GetEnvironmentVariable("CI") == "true";
var checksumErrors = 0;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Error()
    .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
    .CreateLogger();

return parsedArgs.Verb switch
{
    CommandLineArgs.VerbRender render => DoCmdRender(render),
    _ => throw new ArgumentOutOfRangeException()
};

int DoCmdRender(CommandLineArgs.VerbRender options)
{
    Configuration.Default.PreferContiguousImageBuffers = true;

   //Console.WriteLine("Initializing Zygote runtime");

    var zygote = MakeZygoteRuntime();

   // Console.WriteLine($"Starting render of {options.Levels.Count} levels");
    var sw = Stopwatch.StartNew();


    var errors = 0;
    var success = 0;

    var parallelOptions = new ParallelOptions
    {
        MaxDegreeOfParallelism = options.MaxParallelism == 0 ? -1 : options.MaxParallelism
    };

    var doChecksums = options.Checksums;
    Dictionary<string, Dictionary<string, string>>? checksums = null;
    if (options.CompareChecksums is { } chkFileName)
    {
        doChecksums = true;
        using var chkFile = File.OpenRead(chkFileName);
        checksums = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(chkFile);
    }

    //Shuffle(options.Levels, new Random());

    var output = new LevelRendererConsoleOutput(options.Levels);
    output.Active = true;
    Task.Factory.StartNew(async () =>
    {
        try
        {
            while (output.Active)
            {

                output.PrintStatus();

                await Task.Delay(300);
            }
        } catch (Exception ex)
        {
            //Console.WriteLine(ex.ToString());
        }
    });
    Console.Clear();

    Parallel.ForEach(options.Levels, parallelOptions, s =>
    {
        var levelName = Path.GetFileNameWithoutExtension(s);
        //output.SetRenderer(levelName, null);
        var renderRuntime = zygote.Clone();
        output.Active = true;
        

        var levelSw = Stopwatch.StartNew();
        try
        {
            EditorRuntimeHelpers.RunLoadLevel(renderRuntime, s);

            var renderer = new LevelRenderer(renderRuntime, null,0);
            renderer.Overwrite = options.overwrite;
            output.SetRenderer(levelName, renderer);
            
            //renderer.EnabledStages = Enum.GetValues<RenderStage>().Except([ RenderStage.RenderColors, RenderStage.RenderLight,  RenderStage.Finished]).ToArray();
            var stages = Enum.GetValues<RenderStage>().Except([RenderStage.Finished, RenderStage.RenderColors, RenderStage.Unify, RenderStage.SaveFile]).ToList();
            //renderer.EnabledStages = Enum.GetValues<RenderStage>().Except([RenderStage.Finished, RenderStage.RenderColors, RenderStage.Unify]).ToArray();
            if (doChecksums)
                renderer.OnScreenRenderCompleted += (cam, img) => HandleChecksum(levelName, cam, img, checksums);

            renderer.DoRender(stages);
            output.CompleteRendederer(levelName);
            renderer.Dispose();
        }
        catch (Exception e)
        {
            // Fancy error output for Actions CI.
            if (isCi)
                Console.WriteLine($"::error::{levelName}: Rendering failed");

            Console.WriteLine($"{levelName}: Exception while rendering!");
            Console.WriteLine(e);
            Interlocked.Increment(ref errors);
            return;
        }
        
        //Console.WriteLine($"{levelName}: Render succeeded in {levelSw.Elapsed}");
        Interlocked.Increment(ref success);
    });
    output.Active = false;
    Console.WriteLine($"Finished rendering in {sw.Elapsed}. {errors} errored, {success} succeeded");
    if (checksums != null)
        Console.WriteLine($"{checksumErrors} checksum failures.");

    return errors != 0 || checksumErrors != 0 ? 1 : 0;
}

void HandleChecksum(string name, int cameraIndex, LingoImage finalImg,
    Dictionary<string, Dictionary<string, string>>? checksums)
{
    Span<byte> hash = stackalloc byte[16];
    CalcChecksum(finalImg, hash);
    var hashHex = Convert.ToHexString(hash);

    Console.WriteLine($"checksum {name} cam {cameraIndex}: {hashHex}");
    if (checksums == null)
        return;

    if (!checksums.TryGetValue(name, out var cameras) ||
        !cameras.TryGetValue(cameraIndex.ToString(), out var checksum))
    {
        Console.WriteLine(
            $"{(isCi ? "::notice::" : "")}{name}#{cameraIndex} not found in checksum manifest.");
        return;
    }

    if (checksum != hashHex)
    {
        Console.WriteLine(
            $"{(isCi ? "::error::" : "")}{name}#{cameraIndex} mismatches checksum! New: {hashHex} old: {checksum}.");

        Interlocked.Increment(ref checksumErrors);
    }
    else
    {
        Console.WriteLine($"{name}#{cameraIndex}: Checksums passed");
    }
}

static void CalcChecksum(LingoImage img, Span<byte> outData)
{
    unsafe
    {
        Debug.Assert(sizeof(Vector128<byte>) == outData.Length);
    }

    if (img.depth.IntValue == 1)
    {
        // 1-bit images have padding bytes to make certain ops easier.
        // These bytes can contain undefined garbage,
        // and I can't be bothered to clear them to make sure the hash is consistent.
        // Just make it not supported for now, it's fine for the final images (those are 32bpp).
        throw new NotSupportedException();
    }

    var hash = MeowHash.Hash(MeowHash.MeowDefaultSeed, img.ImageBufferNoPadding);
    Unsafe.WriteUnaligned(ref outData[0], hash);
}

static LingoRuntime MakeZygoteRuntime()
{
    var runtime = new LingoRuntime(typeof(MovieScript).Assembly);
    runtime.Init();

    EditorRuntimeHelpers.RunStartup(runtime);

    return runtime;
}

static void Shuffle<T>(List<T> array, System.Random random)
{
    var n = array.Count;
    while (n > 1)
    {
        n--;
        var k = random.Next(n + 1);
        (array[k], array[n]) =
            (array[n], array[k]);
    }
}
