namespace Kosync.Database;

public class KosyncDb
{
    public LiteDatabase Context { get; } = default!;

    public KosyncDb()
    {
        Context = new LiteDatabase("data/Kosync.db");
    }
}