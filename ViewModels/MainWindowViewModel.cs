using System;
using CommunityToolkit.Mvvm.Input;

namespace MyGuiApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Welcome to Avalonia!";
}
