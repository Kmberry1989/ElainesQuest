using Godot;

public partial class OneShotEffect : Node3D
{
    [Export] public float Lifetime = 0.6f;
    [Export] public int SpellIndex = -1;

    public override void _Ready()
    {
        AddSpellCue();

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

    private void AddSpellCue()
    {
        string cuePath = SpellIndex switch
        {
            0 => "res://sounds/break.ogg",
            1 => "res://sounds/coin.ogg",
            2 => "res://sounds/fall.ogg",
            _ => string.Empty,
        };

        if (string.IsNullOrWhiteSpace(cuePath) || !ResourceLoader.Exists(cuePath))
        {
            return;
        }

        AudioStreamPlayer3D audio = new AudioStreamPlayer3D
        {
            Stream = GD.Load<AudioStream>(cuePath),
            VolumeDb = -16.0f,
            MaxDistance = 18.0f,
            Autoplay = false
        };
        AddChild(audio);
    }

    private async void CleanupLater()
    {
        await ToSignal(GetTree().CreateTimer(Lifetime), SceneTreeTimer.SignalName.Timeout);
        QueueFree();
    }
}
