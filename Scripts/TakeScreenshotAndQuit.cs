using Godot;

public partial class TakeScreenshotAndQuit : Node
{
    private int _frames = 0;

    public override void _Process(double delta)
    {
        _frames++;
        if (_frames == 60)
        {
            var img = GetViewport().GetTexture().GetImage();
            img.SavePng("res://screenshot_game.png");
            GD.Print("Saved screenshot_game.png");
            GetTree().Quit();
        }
    }
}
