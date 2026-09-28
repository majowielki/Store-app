namespace Store.Tests.Unit.TestSupport;

/// <summary>The checked-out repository, for tests that hold data typed by hand against the files it names.</summary>
public static class Repository
{
    /// <summary>The folder holding Store.Microservices.slnx, found above the test binaries.</summary>
    public static string Root { get; } = FindRoot();

    /// <summary>A path inside the repository, from its folder names.</summary>
    public static string PathTo(params string[] parts) => Path.Combine([Root, .. parts]);

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Store.Microservices.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root was not found above " + AppContext.BaseDirectory);
    }
}
