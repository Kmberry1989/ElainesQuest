using Godot;
using System.Threading.Tasks;

public partial class ElainesSceneTransition : CanvasLayer
{
    public static ElainesSceneTransition Instance { get; private set; }

    [Export] public float FadeDuration = 0.18f;

    private ColorRect _overlay;
    private bool _isTransitioning;

    public override void _EnterTree()
    {
        Instance = this;
        Layer = 100;
    }

    public override void _Ready()
    {
        _overlay = new ColorRect
        {
            Color = new Color(0.0f, 0.0f, 0.0f, 0.0f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_overlay);
        CallDeferred(nameof(FadeIn));
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public async void TransitionTo(string targetScene)
    {
        if (_isTransitioning || string.IsNullOrWhiteSpace(targetScene))
        {
            return;
        }

        _isTransitioning = true;
        await FadeTo(1.0f);
        GetTree().ChangeSceneToFile(targetScene);
        await ToSignal(GetTree().CreateTimer(0.05f, true), SceneTreeTimer.SignalName.Timeout);
        await FadeTo(0.0f);
        _isTransitioning = false;
    }

    private async void FadeIn()
    {
        if (_overlay == null)
        {
            return;
        }

        await FadeTo(0.0f);
    }

    private async Task FadeTo(float alpha)
    {
        if (_overlay == null)
        {
            return;
        }

        Tween tween = CreateTween();
        tween.TweenProperty(_overlay, "color:a", alpha, FadeDuration);
        await ToSignal(tween, Tween.SignalName.Finished);
    }
}
