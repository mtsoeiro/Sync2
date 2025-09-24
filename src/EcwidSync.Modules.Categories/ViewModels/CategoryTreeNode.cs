using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using EcwidSync.Domain;

namespace EcwidSync.Modules.Categories.ViewModels;

public class CategoryTreeNode : INotifyPropertyChanged
{
    // dados
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool Enabled { get; set; }
    public DateTime? Updated { get; set; }
    public int ProductCount { get; set; }

    public int? ParentId { get; init; }
    public CategoryTreeNode? Parent { get; internal set; }
    public ObservableCollection<CategoryTreeNode> Children { get; } = new();

    // ---- estado visual (PRECISA de notificação) ----
    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set { if (_isExpanded != value) { _isExpanded = value; OnPropertyChanged(); } }
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
    }

    public CategoryTreeNode() { }
    public CategoryTreeNode(Category c)
    {
        Id = c.Id;
        Name = c.Name ?? "";
        Enabled = c.Enabled;
        Updated = c.Updated;
        ParentId = c.ParentId;
    }

    // INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
