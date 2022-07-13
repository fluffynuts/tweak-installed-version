public class AppInfo
{
    public int Index { get; set; }
    public string Container { get; }
    public string Name { get; }
    public string Version { get; }

    public AppInfo(
        string container,
        string name,
        string version
    )
    {
        Container = container;
        Name = name;
        Version = version;
    }

    public AppInfo SetIndex(int i)
    {
        Index = i;
        return this;
    }
}