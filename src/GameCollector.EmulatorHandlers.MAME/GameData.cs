using System;

namespace GameCollector.EmulatorHandlers.MAME;

public class GameData
{
    public GameData(string name, Machine machine, Category category, string verAdded = "", string path = "", bool isVerified = false)
    {
        Name = name;
        Machine = machine;
        Category = category;
        VerAdded = verAdded;
        Path = path;
        IsVerified = isVerified;
    }

    public string? Name { get; set; }
    public Machine? Machine { get; set; }
    public Category? Category { get; set; }
    public string VerAdded { get; set; }
    public string Path { get; set; }
    public bool IsVerified { get; set; }
}

public class Category
{
    public Category(string category1, string category2, string category3, bool mature = false)
    {
        One = category1;
        Two = category2;
        Three = category3;
        Mature = mature;
    }

    public string? One { get; set; }
    public string? Two { get; set; }
    public string? Three { get; set; }
    public bool Mature { get; set; }
}
