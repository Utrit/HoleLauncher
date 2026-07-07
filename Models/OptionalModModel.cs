using HoleLauncher.Core.DTO;
using ReactiveUI;

namespace HoleLauncher.Models;

public class OptionalModModel
{
    private readonly IMessageBus? _messageBus;
    private ModEntry _modEntry;
    private bool _isChecked;
    private bool _canChange = true;
    public string Name => _modEntry.ModSlug;
    public bool IsChecked    {
        get => _isChecked;
        set
        {
            if (_isChecked == value) return;
            _messageBus?.SendMessage(new OptionModSelect(_modEntry, value), "OptionModSelect");
            _isChecked = value;
        }
    }

    public bool CanChange
    {
        get => _canChange;
        set => _canChange = value;
    }

    public void SetLocked(bool status)
    {
        _canChange = !status;
        IsChecked = status;
    }
    
    public OptionalModModel(ModEntry modEntry, bool isChecked, IMessageBus? messageBus)
    {
        _modEntry = modEntry;
        _isChecked = isChecked;
        _messageBus = messageBus;
    }
}