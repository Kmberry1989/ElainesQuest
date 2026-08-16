using Godot;

public partial class HUD : CanvasLayer
{
    [Export] public Label GlimmersLabel;
    [Export] public Label FamilyLabel;
    [Export] public Label HealthLabel;
    [Export] public Label SpellLabel;
    [Export] public Label ObjectiveLabel;
    [Export] public Label CombatFeedLabel;
    [Export] public Label InteractionPromptLabel;
    [Export] public Label TownClueProgressLabel;
    [Export] public Control SpellWheelContainer;
    [Export] public Control SpellWheelMenu;
    [Export] public Button SpellSlot0;
    [Export] public Button SpellSlot1;
    [Export] public Button SpellSlot2;

    private ElaineController _player;
    private bool _spellWheelVisible;
    private float _combatFeedTimer;
    private float _damageFlashTimer;

    public override void _Ready()
    {
        GlimmersLabel ??= GetNodeOrNull<Label>("Control/GlimmersLabel") ?? FindChild("GlimmersLabel", true, false) as Label;
        FamilyLabel ??= GetNodeOrNull<Label>("Control/FamilyLabel") ?? FindChild("FamilyLabel", true, false) as Label;
        HealthLabel ??= GetNodeOrNull<Label>("Control/HealthLabel") ?? FindChild("HealthLabel", true, false) as Label;
        SpellLabel ??= GetNodeOrNull<Label>("Control/SpellLabel") ?? FindChild("SpellLabel", true, false) as Label;
        ObjectiveLabel ??= GetNodeOrNull<Label>("Control/ObjectiveLabel") ?? FindChild("ObjectiveLabel", true, false) as Label;
        CombatFeedLabel ??= GetNodeOrNull<Label>("Control/CombatFeedLabel") ?? FindChild("CombatFeedLabel", true, false) as Label;
        InteractionPromptLabel ??= GetNodeOrNull<Label>("Control/InteractionPromptLabel") ?? FindChild("InteractionPromptLabel", true, false) as Label;
        TownClueProgressLabel ??= GetNodeOrNull<Label>("Control/TownClueProgressLabel") ?? FindChild("TownClueProgressLabel", true, false) as Label;
        SpellWheelContainer ??= GetNodeOrNull<Control>("Control/SpellWheelContainer") ?? FindChild("SpellWheelContainer", true, false) as Control;
        SpellWheelMenu ??= GetNodeOrNull<Control>("Control/SpellWheelContainer/SpellWheelMenu") ?? FindChild("SpellWheelMenu", true, false) as Control;
        SpellSlot0 ??= GetNodeOrNull<Button>("Control/SpellWheelContainer/SpellWheelMenu/SpellSlot0") ?? FindChild("SpellSlot0", true, false) as Button;
        SpellSlot1 ??= GetNodeOrNull<Button>("Control/SpellWheelContainer/SpellWheelMenu/SpellSlot1") ?? FindChild("SpellSlot1", true, false) as Button;
        SpellSlot2 ??= GetNodeOrNull<Button>("Control/SpellWheelContainer/SpellWheelMenu/SpellSlot2") ?? FindChild("SpellSlot2", true, false) as Button;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GlimmersChanged += OnGlimmersChanged;
            GameManager.Instance.FamilyRescuedChanged += OnFamilyRescuedChanged;
            GameManager.Instance.ObjectiveChanged += OnObjectiveChanged;
            GameManager.Instance.TownClueFound += OnTownClueFound;
        }

        if (SpellWheelMenu != null)
        {
            SpellWheelMenu.Connect("selection_changed", Callable.From<long>(OnSpellWheelSelectionChanged));
            SpellWheelMenu.Connect("slot_selected", Callable.From<Control, long>(OnSpellWheelSlotSelected));
            SpellWheelMenu.Set("enabled", false);
        }

        ResolvePlayer();
        Refresh();
    }

    public override void _Process(double delta)
    {
        if (_player == null || !IsInstanceValid(_player))
        {
            ResolvePlayer();
        }

        UpdateSpellWheelState();
        RefreshPlayerStatus();

        _damageFlashTimer = Mathf.Max(0.0f, _damageFlashTimer - (float)delta);
        if (HealthLabel != null)
        {
            HealthLabel.Modulate = _damageFlashTimer > 0.0f ? new Color(1.0f, 0.48f, 0.48f) : Colors.White;
        }

        if (_combatFeedTimer > 0.0f)
        {
            _combatFeedTimer = Mathf.Max(0.0f, _combatFeedTimer - (float)delta);
            if (_combatFeedTimer <= 0.0f && CombatFeedLabel != null)
            {
                CombatFeedLabel.Text = string.Empty;
            }
        }
    }

    public override void _ExitTree()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GlimmersChanged -= OnGlimmersChanged;
            GameManager.Instance.FamilyRescuedChanged -= OnFamilyRescuedChanged;
            GameManager.Instance.ObjectiveChanged -= OnObjectiveChanged;
            GameManager.Instance.TownClueFound -= OnTownClueFound;
        }

        DisconnectPlayer();
    }

    private void ResolvePlayer()
    {
        ElaineController nextPlayer = GetTree().GetFirstNodeInGroup("player") as ElaineController;
        if (nextPlayer == _player)
        {
            return;
        }

        DisconnectPlayer();
        _player = nextPlayer;

        if (_player != null)
        {
            _player.HealthChanged += OnPlayerHealthChanged;
            _player.SpellSelected += OnPlayerSpellSelected;
            _player.SpellCastStarted += OnPlayerSpellCastStarted;
            _player.SpellProjectilesReleased += OnPlayerSpellProjectilesReleased;
            _player.SpellHit += OnPlayerSpellHit;
            _player.InteractionPromptChanged += OnInteractionPromptChanged;
            _player.DamageTaken += OnPlayerDamageTaken;
        }

        RefreshPlayerStatus();
    }

    private void DisconnectPlayer()
    {
        if (_player != null && IsInstanceValid(_player))
        {
            _player.HealthChanged -= OnPlayerHealthChanged;
            _player.SpellSelected -= OnPlayerSpellSelected;
            _player.SpellCastStarted -= OnPlayerSpellCastStarted;
            _player.SpellProjectilesReleased -= OnPlayerSpellProjectilesReleased;
            _player.SpellHit -= OnPlayerSpellHit;
            _player.InteractionPromptChanged -= OnInteractionPromptChanged;
            _player.DamageTaken -= OnPlayerDamageTaken;
        }

        _player = null;
    }

    private void OnGlimmersChanged(int _glimmers)
    {
        Refresh();
    }

    private void OnFamilyRescuedChanged(int _familyCount)
    {
        Refresh();
    }

    private void OnTownClueFound(string _clueId, int _foundCount)
    {
        Refresh();
    }

    private void OnPlayerHealthChanged(float _currentHealth, float _maxHealth)
    {
        RefreshPlayerStatus();
    }

    private void OnPlayerDamageTaken(float _amount, float _currentHealth, float _maxHealth)
    {
        _damageFlashTimer = 0.22f;
    }

    private void OnInteractionPromptChanged(string prompt)
    {
        if (InteractionPromptLabel == null) return;
        InteractionPromptLabel.Text = prompt;
        InteractionPromptLabel.Visible = !string.IsNullOrWhiteSpace(prompt);
    }

    private void OnPlayerSpellSelected(int spellIndex, string displayName)
    {
        ShowCombatFeed($"{displayName}: {GetSpellRoleText(spellIndex)}");
        RefreshPlayerStatus();
    }

    private void OnPlayerSpellCastStarted(int _spellIndex, string displayName)
    {
        ShowCombatFeed($"{displayName}: casting");
    }

    private void OnPlayerSpellProjectilesReleased(int spellIndex, int projectileCount, string displayName)
    {
        string detail = spellIndex switch
        {
            1 => $"{projectileCount} bolts released",
            2 => "piercing beam released",
            _ => "bolt released",
        };
        ShowCombatFeed($"{displayName}: {detail}");
    }

    private void OnPlayerSpellHit(int spellIndex, int hitNumber, int maxTargetHits, string displayName)
    {
        string detail = spellIndex == 2
            ? $"pierced {hitNumber}/{maxTargetHits}"
            : "hit";
        ShowCombatFeed($"{displayName}: {detail}");
    }

    private void OnObjectiveChanged(string _objectiveText)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (GameManager.Instance != null)
        {
            if (GlimmersLabel != null)
            {
                GlimmersLabel.Text = $"Glimmers: {GameManager.Instance.Glimmers}";
            }

            if (FamilyLabel != null)
            {
                FamilyLabel.Text = $"Family Rescued: {GameManager.Instance.FamilyRescuedCount} / {GameManager.TotalFamilyMembers}";
            }

            if (TownClueProgressLabel != null)
            {
                TownClueProgressLabel.Text = $"Town Clues: {GameManager.Instance.GetTownClueCount()} / 3";
            }

            if (ObjectiveLabel != null)
            {
                ObjectiveLabel.Text = string.IsNullOrWhiteSpace(GameManager.Instance.CurrentObjective)
                    ? string.Empty
                    : $"Objective: {GameManager.Instance.CurrentObjective}";
            }
        }

        RefreshPlayerStatus();
    }

    private void RefreshPlayerStatus()
    {
        if (_player == null)
        {
            if (HealthLabel != null)
            {
                HealthLabel.Text = "Health: --";
            }

            if (SpellLabel != null)
            {
                SpellLabel.Text = "Spell: --";
            }

            UpdateSpellWheelButtons();
            return;
        }

        if (HealthLabel != null)
        {
            HealthLabel.Text = $"Health: {Mathf.CeilToInt(_player.GetCurrentHealth())} / {Mathf.CeilToInt(_player.GetMaxHealth())}";
        }

        if (SpellLabel != null)
        {
            SpellLabel.Text = _player.GetSpellHudText();
        }

        UpdateSpellWheelButtons();
    }

    private void UpdateSpellWheelState()
    {
        bool shouldShow = _player != null && Input.IsActionPressed("spell_wheel") && !IsDialogueOpen();
        if (_spellWheelVisible == shouldShow)
        {
            return;
        }

        _spellWheelVisible = shouldShow;

        if (SpellWheelContainer != null)
        {
            SpellWheelContainer.Visible = shouldShow;
        }

        if (SpellWheelMenu != null)
        {
            SpellWheelMenu.Set("enabled", shouldShow);
            if (shouldShow)
            {
                SpellWheelMenu.Call("force_update");
            }
        }
    }

    private void UpdateSpellWheelButtons()
    {
        if (_player == null)
        {
            SetSpellButtonText(SpellSlot0, "Spell 1");
            SetSpellButtonText(SpellSlot1, "Spell 2");
            SetSpellButtonText(SpellSlot2, "Spell 3");
            return;
        }

        int selectedIndex = _player.GetSelectedSpellIndex();
        SetSpellButtonText(SpellSlot0, FormatSpellSlot(0, selectedIndex));
        SetSpellButtonText(SpellSlot1, FormatSpellSlot(1, selectedIndex));
        SetSpellButtonText(SpellSlot2, FormatSpellSlot(2, selectedIndex));
    }

    private string FormatSpellSlot(int spellIndex, int selectedIndex)
    {
        string prefix = spellIndex == selectedIndex ? "> " : string.Empty;
        return $"{prefix}{_player.GetSpellDisplayName(spellIndex)}\n{GetSpellRoleText(spellIndex)}";
    }

    private static string GetSpellRoleText(int spellIndex)
    {
        return spellIndex switch
        {
            0 => "Precise single target",
            1 => "Three close-range bolts",
            2 => "Pierces two targets",
            _ => string.Empty,
        };
    }

    private void ShowCombatFeed(string message)
    {
        if (CombatFeedLabel == null)
        {
            return;
        }

        CombatFeedLabel.Text = message;
        _combatFeedTimer = 2.4f;
    }

    private static void SetSpellButtonText(Button button, string text)
    {
        if (button != null)
        {
            button.Text = text;
        }
    }

    private void OnSpellWheelSelectionChanged(long selectionIndex)
    {
        if (_player == null)
        {
            return;
        }

        if (_player.SelectSpell((int)selectionIndex))
        {
            RefreshPlayerStatus();
        }
    }

    private void OnSpellWheelSlotSelected(Control _slot, long selectionIndex)
    {
        OnSpellWheelSelectionChanged(selectionIndex);
    }

    private static bool IsDialogueOpen()
    {
        return DialogueUI.Instance?.Panel?.Visible ?? false;
    }
}
