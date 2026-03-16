namespace Drizzle.Logic.Rendering;

public enum RenderStage
{
    Start,
    CameraSetup,
    RenderLayerMaterials,
    RenderLayers,
    RenderPropsPreEffects,
    RenderEffects,
    RenderPropsPostEffects,
    RenderLight,
    Unify,
    Finalize,
    RenderColors,
    Finished,
    SaveFile,
}
