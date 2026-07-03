using Godot;

public partial class OneShotEffect : Node3D
{
    [Export] public float Lifetime = 0.6f;

    public override void _Ready()
    {
        foreach (Node node in FindChildren("*", "", true, false))
        {
            if (node is GpuParticles3D particles)
            {
                particles.Restart();
                particles.Emitting = true;
            }
        }

        foreach (Node node in FindChildren("*", "", true, false))
        {
            if (node is AudioStreamPlayer3D audio)
            {
                audio.Play();
            }
        }

        CleanupLater();
    }

    private async void CleanupLater()
    {
        await ToSignal(GetTree().CreateTimer(Lifetime), SceneTreeTimer.SignalName.Timeout);
        QueueFree();
    }
}
