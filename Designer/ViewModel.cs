using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NothingButNeurons.Shared.Messages;

namespace NothingButNeurons.Designer;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public ObservableCollection<string> AccumulationFunctions { get; }
    public ObservableCollection<string> ActivationFunctions { get; }
    public ObservableCollection<string> ResetFunctions { get; }

    public MainWindowViewModel()
    {
        //Accumulation Function
        AccumulationFunctions = new ObservableCollection<string>
        {
            "Random - All",
            "Random - Common"
        };
        foreach (AccumulationFunction function in Enum.GetValues(typeof(AccumulationFunction)))
        {
            AccumulationFunctions.Add(function.ToString());
        }

        //Activation Function
        ActivationFunctions = new ObservableCollection<string>
        {
            "Random - All",
            "Random - Common"
        };
        foreach (ActivationFunction function in Enum.GetValues(typeof(ActivationFunction)))
        {
            ActivationFunctions.Add(function.ToString());
        }

        //Reset Function
        ResetFunctions = new ObservableCollection<string>
        {
            "Random - All",
            "Random - Common"
        };
        foreach (ResetFunction function in Enum.GetValues(typeof(ResetFunction)))
        {
            ResetFunctions.Add(function.ToString());
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
