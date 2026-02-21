using System.ComponentModel;
using System.Runtime.CompilerServices;
using ToolSnap.Mobile.Dtos;

namespace ToolSnap.Mobile.Models;

public class ConfirmDetectedToolItem : INotifyPropertyChanged
{
    public Guid PhotoSessionId { get; }

    public float Confidence { get; }
    public bool RedFlagged { get; }

    // Довідники (для Picker-ів)
    public List<ToolTypeDto> ToolTypes { get; }
    public List<BrandDto> Brands { get; }
    public List<ModelDto> Models { get; private set; } = new();
    public List<ToolDto> Tools { get; private set; } = new();

    private ToolTypeDto? _selectedToolType;
    public ToolTypeDto? SelectedToolType
    {
        get => _selectedToolType;
        set
        {
            if (SetField(ref _selectedToolType, value))
            {
                OnPropertyChanged(nameof(SelectedToolType));
                // коли змінюється тип — треба буде перевантажити моделі/толзи з сервісу
            }
        }
    }

    private BrandDto? _selectedBrand;
    public BrandDto? SelectedBrand
    {
        get => _selectedBrand;
        set
        {
            if (SetField(ref _selectedBrand, value))
            {
                OnPropertyChanged(nameof(SelectedBrand));
            }
        }
    }

    private ModelDto? _selectedModel;
    public ModelDto? SelectedModel
    {
        get => _selectedModel;
        set
        {
            if (SetField(ref _selectedModel, value))
            {
                OnPropertyChanged(nameof(SelectedModel));
            }
        }
    }

    private ToolDto? _selectedTool;
    public ToolDto? SelectedTool
    {
        get => _selectedTool;
        set
        {
            if (SetField(ref _selectedTool, value))
            {
                OnPropertyChanged(nameof(SelectedTool));
                SerialNumber = value?.SerialNumber;
            }
        }
    }

    private string? _serialNumber;
    public string? SerialNumber
    {
        get => _serialNumber;
        set => SetField(ref _serialNumber, value);
    }

    public ConfirmDetectedToolItem(
        Guid photoSessionId,
        float confidence,
        bool redFlagged,
        List<ToolTypeDto> toolTypes,
        List<BrandDto> brands)
    {
        PhotoSessionId = photoSessionId;
        Confidence = confidence;
        RedFlagged = redFlagged;
        ToolTypes = toolTypes;
        Brands = brands;
    }

    public void SetModels(List<ModelDto> models)
    {
        Models = models;
        OnPropertyChanged(nameof(Models));
    }

    public void SetTools(List<ToolDto> tools)
    {
        Tools = tools;
        OnPropertyChanged(nameof(Tools));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
