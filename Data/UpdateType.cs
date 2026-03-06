namespace Models;
[System.Flags]
public enum UpdateType
{
    None = 0,
    Locational = 1 << 0,
    Membership = 1 << 1,
    Ignore = 1 << 2,
    ComponentAddition = 1 << 3,
    Selection = 1 << 4,
}
