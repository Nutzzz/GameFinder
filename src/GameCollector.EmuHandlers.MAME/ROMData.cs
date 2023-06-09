using System;

namespace GameCollector.EmuHandlers.MAME;

internal record ROMData
{
    internal ROMData(string name, Machine machine, Category category, string verAdded = "", string path = "", bool isVerified = false)
    {
        Name = name;
        Machine = machine;
        Category = category;
        VerAdded = verAdded;
        Path = path;
        IsVerified = isVerified;
    }

    internal string? Name { get; set; }
    internal Machine? Machine { get; set; }
    internal Category? Category { get; set; }
    internal string VerAdded { get; set; }
    internal string Path { get; set; }
    internal bool IsVerified { get; set; }
}

internal record Category
{
    internal Category(string category1, string category2, string category3, bool mature = false)
    {
        One = category1;
        Two = category2;
        Three = category3;
        Mature = mature;
    }

    internal string? One { get; set; }
    internal string? Two { get; set; }
    internal string? Three { get; set; }
    internal bool Mature { get; set; }
}
