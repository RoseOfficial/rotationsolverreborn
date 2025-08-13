using Dalamud.Plugin;
using ECommons;
using ECommons.Logging;
using Lumina.Excel;
using System.Runtime.Loader;

namespace RotationSolver.Helpers;

internal class RotationLoadContext(DirectoryInfo? directoryInfo)
    : AssemblyLoadContext(true)
{
    private readonly DirectoryInfo? _directory = directoryInfo;

    private static readonly Dictionary<string, Assembly> _handledAssemblies = [];

    static RotationLoadContext()
    {
        Assembly[] assemblies =
        [
            typeof(RotationSolverPlugin).Assembly,
            typeof(IDalamudPluginInterface).Assembly,
            typeof(DataCenter).Assembly,
            typeof(SheetAttribute).Assembly,
            typeof(ImGui).Assembly,
            typeof(FFXIVClientStructs.ThisAssembly).Assembly,
            typeof(ECommons.DalamudServices.Svc).Assembly,
        ];

        foreach (Assembly assembly in assemblies)
        {
            _handledAssemblies.Add(assembly.GetName().Name ?? string.Empty, assembly);
        }
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name != null && _handledAssemblies.TryGetValue(assemblyName.Name, out Assembly? value))
        {
            return value;
        }

        string file = Path.Join(_directory?.FullName ?? string.Empty, $"{assemblyName.Name}.dll");
        if (File.Exists(file))
        {
            try
            {
                return LoadFromFile(file);
            }
            catch
            {
                //
            }
        }
        return base.Load(assemblyName);
    }

    internal Assembly LoadFromFile(string filePath)
    {
        using FileStream file = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        string pdbPath = Path.ChangeExtension(filePath, ".pdb");
        if (!File.Exists(pdbPath))
        {
#if DEBUG
            PluginLog.Information($"Failed to find {pdbPath}");
#endif
            return LoadFromStream(file);
        }
        using FileStream pdbFile = File.Open(pdbPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        try
        {
            return LoadFromStream(file, pdbFile);
        }
        catch
        {
            return LoadFromStream(file);
        }
    }
}