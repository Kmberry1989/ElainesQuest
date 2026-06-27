using Godot;

public partial class DialogueUI : CanvasLayer
{
    public static DialogueUI Instance { get; private set; }

    [Export] public Control Panel;
    [Export] public Label NameLabel;
    [Export] public Label DialogueLabel;

    public override void _Ready()
    {
        Instance = this;
        Panel ??= GetNodeOrNull<Control>("Control/Panel") ?? FindChild("Panel", true, false) as Control;
        NameLabel ??= GetNodeOrNull<Label>("Control/Panel/NameLabel") ?? FindChild("NameLabel", true, false) as Label;
        DialogueLabel ??= GetNodeOrNull<Label>("Control/Panel/DialogueLabel") ?? FindChild("DialogueLabel", true, false) as Label;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.DialogueRequested += OnDialogueRequested;
        }

        HideDialogue();
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.DialogueRequested -= OnDialogueRequested;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Panel == null || !Panel.Visible)
        {
            return;
        }

        if (@event.IsActionPressed("interact") || @event.IsActionPressed("ui_cancel"))
        {
            HideDialogue();
            GetViewport().SetInputAsHandled();
        }
    }

    public void ShowDialogue(string speaker, string text)
    {
        if (NameLabel != null)
        {
            NameLabel.Text = speaker;
        }

        if (DialogueLabel != null)
        {
            DialogueLabel.Text = text;
        }

        Panel?.Show();
    }

    public void HideDialogue()
    {
        Panel?.Hide();
    }

    private void OnDialogueRequested(string speaker, string text)
    {
        ShowDialogue(speaker, text);
    }
}
