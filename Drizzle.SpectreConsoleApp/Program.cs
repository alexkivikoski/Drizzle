using Avalonia.Rendering;
using Avalonia.X11;
using Drizzle.ConsoleApp;
using Drizzle.Editor.ViewModels.Render;
using Drizzle.Lingo.Runtime;
using Drizzle.Lingo.Runtime.Utils;
using Drizzle.Logic;
using Drizzle.Logic.Rendering;
using Drizzle.Ported;
using ReactiveUI;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using SixLabors.ImageSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Drizzle.SpectreConsoleApp
{
    internal class Program
    {
        private static readonly Subject<RenderStatus> _statusObservable = new();
        static async Task Main(string[] args)
        {
            ExtraLayers.InitializeExtraCast();
            CultureFix.FixCulture();

            var levelname = "DM_A15";
            var region = levelname.Split('_')[0];
            var levelpath = $"D:\\source\\drizzle3\\Drizzle\\Data\\LevelEditorProjects\\World\\{region}\\{levelname}.txt";
            
            var options = new CommandLineArgs.VerbRender(0, [levelpath], false, null);
            Configuration.Default.PreferContiguousImageBuffers = true;



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
            
            int RenderProgressMax = 0;
            int RenderStageProgressMax = 0;
            var prog = AnsiConsole.Progress();
            LingoRuntime zygote = null;
            

            AnsiConsole.Clear();
            AnsiConsole.MarkupLine($"Rendering: [bold]{levelname}[/]");

            // Synchronous
            AnsiConsole.Status()
                .Start("Initializing Zygote...", ctx =>
                {
                    zygote = MakeZygoteRuntime();
                });


            await AnsiConsole.Progress().StartAsync(ctx => RunRender(ctx,zygote,levelpath));
            
        }

        static async Task RunRender(ProgressContext ctx, LingoRuntime zygote, string levelPath)
        {

            var renderRuntime = zygote.Clone();
            var levelName = Path.GetFileNameWithoutExtension(levelPath);

                EditorRuntimeHelpers.RunLoadLevel(renderRuntime, levelPath);

            
            
            var _renderer = new LevelRenderer(renderRuntime, null, 0);
            _renderer.EnabledStages = Enum.GetValues<RenderStage>().Except([ RenderStage.RenderLight, RenderStage.RenderColors, RenderStage.Finalize,RenderStage.Finished]).ToArray();
            
            // Define tasks
            Dictionary<RenderStage, ProgressTask> stageTasks = new Dictionary<RenderStage, ProgressTask>();
            foreach (var stage in _renderer.EnabledStages.Except([RenderStage.CameraSetup,  RenderStage.Start]))
            {
                stageTasks.Add(stage, ctx.AddTask(Enum.GetName(stage), false));
            }
            
            TaskCompletionSource tcs = new TaskCompletionSource();
            _renderer.StageCompleted += s =>
            {
                
                if (stageTasks.TryGetValue(s.Stage, out var task))
                {
                    if (!task.IsStarted)
                    {
                        task.StartTask();
                    }
                    task.Value(task.MaxValue);
                    task.StopTask();
                }
            };
            _renderer.StatusChanged += x =>
            {

                Log.Verbose("Render status next: {Status}", x);

                var CameraIndex = x.CurrentIndex;
                var StageEnum = x.Stage.Stage;


                if (stageTasks.TryGetValue(x.Stage.Stage, out var task)){

                    if (x.Stage is RenderStageStatusLayers layers)
                    {
                        task.MaxValue(3);
                        task.Increment(1);
                    }
                    else if (x.Stage is RenderStageStatusEffects effects)
                    {

                        task.MaxValue(effects.TotalEffectsCount * 60);
                        task.Value((effects.CurrentEffect - 1) * 60 + effects.VertRepeater);
                    }
                    else if (x.Stage is RenderStageStatusLight light)
                    {
                        task.MaxValue(30);
                        task.Value(light.CurrentLayer);
                    } else
                    {
                        if (task.MaxValue == 100 && x.TotalCount>x.CurrentIndex)
                        {
                            // still at default
                            task.MaxValue(x.TotalCount);
                            task.IsIndeterminate(false);
                        } else
                        {
                            task.Increment(1);
                        }

                    }
                    if (!task.IsStarted)
                    {
                        task.StartTask();
                    }
                }


                if (stageTasks.Values.All(s => s.IsFinished))
                {
                    tcs.TrySetResult();
                }
            };
            _renderer.DoRender();
            await tcs.Task.ConfigureAwait(false);
        }
        static LingoRuntime MakeZygoteRuntime()
        {
            var runtime = new LingoRuntime(typeof(MovieScript).Assembly);
            runtime.Init();

            EditorRuntimeHelpers.RunStartup(runtime);

            return runtime;
        }
    }
}

