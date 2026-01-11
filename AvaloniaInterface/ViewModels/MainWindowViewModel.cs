using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;

namespace OpenglAvaloniaTest.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public string Greeting { get; } = "Welcome to Avalonia!";

        public MainWindowViewModel()
        {

        }

        [RelayCommand]
        private void OnOpen()
        {
            Console.WriteLine("OnOpen");
        }
    }
}
