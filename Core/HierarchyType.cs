namespace Core;

[Flags]
public enum HierarchyType
{
    Model = 1 << 0,
    Tool = 1 << 1,
    EditVisualizer = 1 << 2,
    Selected = 1 << 3,
    All = Tool | Model |  EditVisualizer,
}
