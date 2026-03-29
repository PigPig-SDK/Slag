namespace Core;

public enum HierarchyType
{
    Model = 1 << 0,
    Tool = 1 << 1,
    All = Tool | Model,
}
