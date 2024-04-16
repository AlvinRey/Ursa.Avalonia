using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using Irihi.Avalonia.Shared.Common;
using Size = Avalonia.Size;


namespace Ursa.Controls;

[TemplatePart(PartNames.PART_Popup, typeof(Popup))]
public class TreeComboBox: ItemsControl
{
    private Popup? _popup;
    
    private static readonly FuncTemplate<Panel?> DefaultPanel =
        new FuncTemplate<Panel?>(() => new VirtualizingStackPanel());

    public static readonly StyledProperty<double> MaxDropDownHeightProperty =
        ComboBox.MaxDropDownHeightProperty.AddOwner<TreeComboBox>();

    public double MaxDropDownHeight
    {
        get => GetValue(MaxDropDownHeightProperty);
        set => SetValue(MaxDropDownHeightProperty, value);
    }

    public static readonly StyledProperty<string?> WatermarkProperty =
        TextBox.WatermarkProperty.AddOwner<TreeComboBox>();

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }
    
    public static readonly StyledProperty<bool> IsDropDownOpenProperty =
        ComboBox.IsDropDownOpenProperty.AddOwner<TreeComboBox>();
    
    public bool IsDropDownOpen
    {
        get => GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
        ContentControl.HorizontalContentAlignmentProperty.AddOwner<TreeComboBox>();

    public HorizontalAlignment HorizontalContentAlignment
    {
        get => GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }
    
    public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
        ContentControl.VerticalContentAlignmentProperty.AddOwner<TreeComboBox>();
    
    public VerticalAlignment VerticalContentAlignment
    {
        get => GetValue(VerticalContentAlignmentProperty);
        set => SetValue(VerticalContentAlignmentProperty, value);
    }

    public static readonly StyledProperty<IDataTemplate?> SelectedItemTemplateProperty =
        AvaloniaProperty.Register<TreeComboBox, IDataTemplate?>(nameof(SelectedItemTemplate));

    public IDataTemplate? SelectedItemTemplate
    {
        get => GetValue(SelectedItemTemplateProperty);
        set => SetValue(SelectedItemTemplateProperty, value);
    }

    public static readonly DirectProperty<TreeComboBox, object?> SelectionBoxItemProperty = AvaloniaProperty.RegisterDirect<TreeComboBox, object?>(
        nameof(SelectionBoxItem), o => o.SelectionBoxItem);
    private object? _selectionBoxItem;
    public object? SelectionBoxItem
    {
        get => _selectionBoxItem;
        protected set => SetAndRaise(SelectionBoxItemProperty, ref _selectionBoxItem, value);
    }

    private object? _selectedItem;

    public static readonly DirectProperty<TreeComboBox, object?> SelectedItemProperty = AvaloniaProperty.RegisterDirect<TreeComboBox, object?>(
        nameof(SelectedItem), o => o.SelectedItem, (o, v) => o.SelectedItem = v);

    public object? SelectedItem
    {
        get
        {
            return _selectedItem;
        }
        set
        {
            SetAndRaise(SelectedItemProperty, ref _selectedItem, value);
            
        }
    }
    
    static TreeComboBox()
    {
        ItemsPanelProperty.OverrideDefaultValue<TreeComboBox>(DefaultPanel);
        FocusableProperty.OverrideDefaultValue<TreeComboBox>(true);
        SelectedItemProperty.Changed.AddClassHandler<TreeComboBox, object?>((box, args) => box.OnSelectedItemChanged(args));
    }

    private void OnSelectedItemChanged(AvaloniaPropertyChangedEventArgs<object?> args)
    {
        if (args.NewValue.Value is not null)
        {
            UpdateSelectionBoxItem(args.NewValue.Value);
        }
    }
    
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _popup = e.NameScope.Find<Popup>(PartNames.PART_Popup);
    }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        return NeedsContainer<TreeComboBoxItem>(item, out recycleKey);
    }

    internal bool NeedsContainerInternal(object? item, int index, out object? recycleKey)
    {
        return NeedsContainerOverride(item, index, out recycleKey);
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return new TreeComboBoxItem();
    }

    internal Control CreateContainerForItemInternal(object? item, int index, object? recycleKey)
    {
        return CreateContainerForItemOverride(item, index, recycleKey);
    }

    internal void ContainerForItemPreparedInternal(Control container, object? item, int index)
    {
        ContainerForItemPreparedOverride(container, item, index);
    }
    
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e is { InitialPressMouseButton: MouseButton.Left, Source: Visual source })
        {
            if (_popup is not null && _popup.IsOpen && _popup.IsInsidePopup(source))
            {
                var container = GetContainerFromEventSource(source);
                if (container is null) return;
                var item = TreeItemFromContainer(container);
                if (item is null) return;
                if (SelectedItem is not null)
                {
                    var selectedContainer = TreeContainerFromItem(SelectedItem);
                    if(selectedContainer is TreeComboBoxItem selectedTreeComboBoxItem)
                    {
                        selectedTreeComboBoxItem.IsSelected = false;
                    }
                }
                this.SelectedItem = item;
                container.IsSelected = true;
                IsDropDownOpen = false;
            }
            else
            {
                IsDropDownOpen = !IsDropDownOpen;
            }
            
        }
    }

    private void UpdateSelectionBoxItem(object? item)
    {
        if (item is ContentControl contentControl)
        {
            item = contentControl.Content;
        }
        else if(item is HeaderedItemsControl headeredItemsControl)
        {
            item = headeredItemsControl.Header;
        }

        if (item is Control control)
        {
            if (VisualRoot == null) return;
            control.Measure(Size.Infinity);
            SelectionBoxItem = new Rectangle
            {
                Width = control.DesiredSize.Width,
                Height = control.DesiredSize.Height,
                Fill = new VisualBrush
                {
                    Visual = control,
                    Stretch = Stretch.None,
                    AlignmentX = AlignmentX.Left,
                }
            };
            // TODO: Implement flow direction udpate
        }
        else
        {
            if (ItemTemplate is null && DisplayMemberBinding is { } binding)
            {
                var template = new FuncDataTemplate<object?>((a,_) => new TextBlock
                {
                    [DataContextProperty] = a,
                    [!TextBlock.TextProperty] = binding,
                });
                var textBlock = template.Build(item);
                SelectionBoxItem = textBlock;
            }
            else
            {
                SelectionBoxItem = item;
            }
        }
    }

    private TreeComboBoxItem? GetContainerFromEventSource(object eventSource)
    {
        if (eventSource is Visual visual)
        {
            var item = visual.GetSelfAndVisualAncestors().OfType<TreeComboBoxItem>().FirstOrDefault();
            return item?.Owner == this ? item : null!;
        }

        return null;
    }

    private object? TreeItemFromContainer(Control container)
    {
        return TreeItemFromContainer(this, container);
    }
    
    private Control? TreeContainerFromItem(object item)
    {
        return TreeContainerFromItem(this, item);
    }
    
    private static Control? TreeContainerFromItem(ItemsControl itemsControl, object item)
    {
        if (itemsControl.ContainerFromItem(item) is { } container)
        {
            return container;
        }
        foreach (var child in itemsControl.GetRealizedContainers())
        {
            if(child is ItemsControl childItemsControl && TreeContainerFromItem(childItemsControl, item) is { } childContainer)
            {
                return childContainer;
            }
        }
        return null;
    }
    
    private static object? TreeItemFromContainer(ItemsControl itemsControl, Control container)
    {
        if (itemsControl.ItemFromContainer(container) is { } item)
        {
            return item;
        }
        foreach (var child in itemsControl.GetRealizedContainers())
        {
            if(child is ItemsControl childItemsControl && TreeItemFromContainer(childItemsControl, container) is { } childItem)
            {
                return childItem;
            }
        }
        return null;
    }
} 