using System.Windows;
using Hitboxes.Launcher.Models;
using Hitboxes.Launcher.Services;
using Hitboxes.Launcher.Theming;

namespace Hitboxes.Launcher;

/// <summary>Shown once, before MainWindow, when settings.json doesn't exist yet — picks the
/// first account's nickname and the default RAM allocation. MainWindow itself takes care of
/// the "first build" step by auto-opening New Instance when there are zero instances.</summary>
public partial class FirstRunWindow : Window
{
    private readonly SettingsService _settingsService;

    public FirstRunWindow(string rootDir)
    {
        ThemeResources.Register(Resources);
        InitializeComponent();
        _settingsService = new SettingsService(rootDir);

        var memoryOptionsGb = SystemMemory.BuildMemoryOptionsGb();
        MemoryBox.ItemsSource = memoryOptionsGb;
        MemoryBox.SelectedItem = memoryOptionsGb.Contains(4) ? 4 : memoryOptionsGb.FirstOrDefault();

        Loaded += (_, _) => UiAnimations.FadeIn(this);
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        string nickname = NicknameBox.Text.Trim();
        if (!AuthService.IsValidUsername(nickname))
        {
            ErrorText.Text = "Ник: 3–16 символов, латиница/цифры/подчёркивание.";
            return;
        }

        int memoryGb = MemoryBox.SelectedItem is int value ? value : 4;

        var account = new Account { Username = nickname };
        var settings = new LauncherSettings
        {
            Accounts = { account },
            CurrentAccountId = account.Id,
            DefaultMemoryMinMb = 512,
            DefaultMemoryMaxMb = memoryGb * 1024,
        };
        _settingsService.Save(settings);

        DialogResult = true;
    }
}
