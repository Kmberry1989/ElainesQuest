using Godot;

public partial class SpaceAudioCue : Node
{
    [Export] public string CuePath = "res://sounds/coin.ogg";
    [Export] public float VolumeDb = -18.0f;

    public override void _Ready()
    {
        if (string.IsNullOrWhiteSpace(CuePath) || !ResourceLoader.Exists(CuePath))
        {
            return;
        }

        AudioStream stream = GD.Load<AudioStream>(CuePath);
        if (stream == null)
        {
            return;
        }

        AudioStreamPlayer player = new AudioStreamPlayer
        {
            Stream = stream,
            VolumeDb = VolumeDb,
            Autoplay = true
        };
        AddChild(player);
    }
}
