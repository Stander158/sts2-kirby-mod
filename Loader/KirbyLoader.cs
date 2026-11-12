using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace KirbyMod.Loader;

/// <summary>
/// The KirbyMod.dll the game loads. The real mod is built once per game API version into compat/&lt;version&gt;/;
/// this picks the newest build that is not newer than the running game, loads it, makes its types visible to the
/// game's model discovery, and calls its initializer. Uses only APIs that exist in every supported game version.
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class KirbyLoader
{
    private const string CoreFile = "KirbyMod.dll";

    private static Assembly? _core;
    private static Type[]? _coreTypes;
    private static Type[]? _source;
    private static Type[]? _merged;

    public static void Initialize()
    {
        var root = Path.GetDirectoryName(typeof(KirbyLoader).Assembly.Location)
                   ?? throw new InvalidOperationException("KirbyMod: cannot find the mod folder.");
        var host = ReadGameVersion();
        var builds = Directory.GetDirectories(Path.Combine(root, "compat"))
            .Select(dir => (Dir: dir, Version: Version.TryParse(Path.GetFileName(dir), out var v) ? v : null))
            .Where(build => build.Version != null && File.Exists(Path.Combine(build.Dir, CoreFile)))
            .OrderByDescending(build => build.Version)
            .ToList();
        if (builds.Count == 0)
            throw new InvalidOperationException("KirbyMod: no build found under compat/.");

        // Newest build not newer than the game; a game older than every build gets the oldest one.
        var pick = builds.FirstOrDefault(build => host == null || build.Version <= host);
        if (pick.Dir == null)
            pick = builds[^1];
        Log.Info($"[KirbyMod.Loader] Game {host?.ToString() ?? "unknown"}; loading build {pick.Version}.");

        var context = AssemblyLoadContext.GetLoadContext(typeof(KirbyLoader).Assembly) ?? AssemblyLoadContext.Default;
        _core = context.LoadFromAssemblyPath(Path.Combine(pick.Dir, CoreFile));

        // The game discovers models (cards, powers, ...) only in the assembly it loaded itself.
        new Harmony("com.kirbymod.loader").Patch(
            AccessTools.PropertyGetter(typeof(ReflectionHelper), nameof(ReflectionHelper.ModTypes)),
            postfix: new HarmonyMethod(typeof(KirbyLoader), nameof(AddCoreTypes)));

        foreach (var type in _core.GetTypes())
        {
            var attribute = type.GetCustomAttribute<ModInitializerAttribute>();
            if (attribute == null)
                continue;
            var method = type.GetMethod(attribute.initializerMethod, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                         ?? throw new MissingMethodException(type.FullName, attribute.initializerMethod);
            method.Invoke(null, null);
        }
    }

    private static void AddCoreTypes(ref Type[] __result)
    {
        if (_core == null)
            return;
        if (ReferenceEquals(__result, _source) && _merged != null)
        {
            __result = _merged;
            return;
        }

        _coreTypes ??= _core.GetTypes();
        var seen = new HashSet<Type>(__result);
        var added = _coreTypes.Where(seen.Add).ToArray();
        _source = __result;
        _merged = added.Length == 0 ? __result : [.. __result, .. added];
        __result = _merged;
    }

    /// <summary>The game's version from release_info.json next to the executable ("v0.113.0" -> 0.113.0).</summary>
    private static Version? ReadGameVersion()
    {
        try
        {
            var exeDir = Path.GetDirectoryName(Environment.ProcessPath);
            if (exeDir == null)
                return null;
            var path = Path.Combine(exeDir, "release_info.json");
            if (!File.Exists(path))
                return null;
            using var json = JsonDocument.Parse(File.ReadAllText(path));
            var text = json.RootElement.GetProperty("version").GetString()?.TrimStart('v', 'V');
            var core = text?.Split('-', '+')[0];
            return Version.TryParse(core, out var version) ? version : null;
        }
        catch (Exception ex)
        {
            Log.Warn($"[KirbyMod.Loader] Could not read the game version: {ex.Message}");
            return null;
        }
    }
}
