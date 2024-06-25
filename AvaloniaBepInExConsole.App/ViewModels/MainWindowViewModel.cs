using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using DynamicData;
using ReactiveUI;
using Sigurd.AvaloniaBepInExConsole.App.Logs;
using Sigurd.AvaloniaBepInExConsole.Common;

namespace Sigurd.AvaloniaBepInExConsole.App.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ReadOnlyObservableCollection<LogEvent> _logMessages;
    private readonly ILogListener _logListener;
    public ReadOnlyObservableCollection<LogEvent> LogMessages => _logMessages;
    public IReadOnlyList<MenuItemViewModel> LogLevelMenuItems { get; set; }
    public ReactiveCommand<string, Unit> SetLogLevelCommand { get; set; }

    public MainWindowViewModel(ILogListener logListener)
    {
        SetLogLevelCommand = ReactiveCommand.Create<string>(SetLogLevel);

        LogLevelMenuItems = Enum.GetValues<BepInExLogLevel>().Select(x =>
            new MenuItemViewModel
            {
                Header = x.ToString(),
                Command = SetLogLevelCommand,
                CommandParameter = x.ToString()
            }).ToList();

        Console.WriteLine(LogLevelMenuItems.Count);

        _logListener = logListener;
        logListener.LogMessages.Connect()
            .Bind(out _logMessages)
            .Subscribe();
    }

    private void SetLogLevel(string value)
    {
        var logLevel = Enum.Parse<BepInExLogLevel>(value);
        ((BepInExLogListener)_logListener).LogLevel = logLevel;
    }
}
