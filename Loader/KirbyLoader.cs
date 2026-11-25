using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace KirbyMod.Loader;

/// <summary>
/// The KirbyMod.dll the game loads. The real mod is built once per game API version into compat/&lt;version&gt;/;
/// this picks the build that fits the running game, loads it, makes its types visible to the game's model discovery,
/// and calls its initializer. Uses only APIs that exist in every supported game version (newer ones by reflection).
/// Whenever KirbyMod cannot run safely it logs an error and stays out, so the game and the other mods still start.
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class KirbyLoader
{
    private const string Tag = "[KirbyMod.Loader]";
    private const string CoreFile = "KirbyMod.dll";
    private const string ReleaseInfoFile = "release_info.json";

    private static readonly Assembly Game = typeof(ReflectionHelper).Assembly;

    private static Type[]? _coreTypes;
    private static Type[]? _source;
    private static Type[]? _merged;
    private static bool _mergeErrorLogged;

    private sealed record Build(string Dir, Version Version)
    {
        public string Dll => Path.Combine(Dir, CoreFile);
    }

    public static void Initialize()
    {
        var root = Path.GetDirectoryName(typeof(KirbyLoader).Assembly.Location)
                   ?? throw new InvalidOperationException("KirbyMod: cannot find the mod folder.");
        if (!RitsuLibReady())
            return;

        var (host, label) = ReadGameVersion();
        var builds = Directory.GetDirectories(Path.Combine(root, "compat"))
            .Select(dir => (Dir: dir, Version: Version.TryParse(Path.GetFileName(dir), out var v) ? v : null))
            .Where(build => build.Version != null && File.Exists(Path.Combine(build.Dir, CoreFile)))
            .Select(build => new Build(build.Dir, build.Version!))
            .OrderByDescending(build => build.Version)
            .ToList();
        if (builds.Count == 0)
        {
            Log.Error($"{Tag} No build found under {Path.Combine(root, "compat")}; KirbyMod is not loaded.");
            return;
        }

        var pick = PickBuild(builds, host);
        if (pick == null)
            return;
        Log.Info($"{Tag} Game {label}; loading build {pick.Version}.");

        // A build that does not fit the game fails here; the ModTypes patch below would then make every later
        // GetSubtypesInMods call throw and take the game's startup down with it, so it is only installed after this.
        Type[] types;
        try
        {
            var context = AssemblyLoadContext.GetLoadContext(typeof(KirbyLoader).Assembly) ?? AssemblyLoadContext.Default;
            types = context.LoadFromAssemblyPath(pick.Dll).GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            var reasons = ex.LoaderExceptions.Select(e => e?.Message).Where(m => m != null).Distinct().Take(10);
            Log.Error($"{Tag} Build {pick.Version} does not fit game {label}; KirbyMod is not loaded. "
                      + $"Type load errors:\n  {string.Join("\n  ", reasons)}");
            return;
        }
        catch (Exception ex)
        {
            Log.Error($"{Tag} Could not load build {pick.Version}; KirbyMod is not loaded. {ex}");
            return;
        }

        // The game discovers models (cards, powers, ...) only in the assembly it loaded itself.
        try
        {
            new Harmony("com.kirbymod.loader").Patch(
                AccessTools.PropertyGetter(typeof(ReflectionHelper), nameof(ReflectionHelper.ModTypes)),
                postfix: new HarmonyMethod(typeof(KirbyLoader), nameof(AddCoreTypes)));
        }
        catch (Exception ex)
        {
            Log.Error($"{Tag} Could not hook the game's mod type discovery; KirbyMod is not loaded. {ex}");
            return;
        }
        _coreTypes = types;

        foreach (var type in types)
        {
            var attribute = type.GetCustomAttribute<ModInitializerAttribute>();
            if (attribute == null)
                continue;
            var method = type.GetMethod(attribute.initializerMethod, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                         ?? throw new MissingMethodException(type.FullName, attribute.initializerMethod);
            method.Invoke(null, null);
        }
    }

    /// <summary>
    /// KirbyMod registers its content through RitsuLib, which gives every model a KirbyMod-prefixed id. If RitsuLib
    /// failed to start (for example on a game version it does not support yet), the game still loads KirbyMod, and
    /// its models would fall back to plain class-name ids: every localization key misses, and a name the game itself
    /// uses (CARD.FEED since 0.114) crashes the game's startup. So KirbyMod stays out unless RitsuLib is running.
    /// </summary>
    private static bool RitsuLibReady()
    {
        try
        {
            var frameworks = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("STS2RitsuLib.RitsuLibFramework", false))
                .OfType<Type>()
                .ToList();
            if (frameworks.Count == 0)
            {
                Log.Error($"{Tag} RitsuLib is not loaded, so KirbyMod is not loaded either. Install and enable RitsuLib.");
                return false;
            }

            var states = frameworks
                .Select(framework => framework.GetProperty("IsInitialized", BindingFlags.Public | BindingFlags.Static))
                .OfType<PropertyInfo>()
                .ToList();
            if (states.Count == 0)
            {
                Log.Warn($"{Tag} Cannot tell whether RitsuLib started (no RitsuLibFramework.IsInitialized); loading anyway.");
                return true;
            }
            if (states.Any(state => state.GetValue(null) is true))
                return true;

            Log.Error($"{Tag} RitsuLib did not finish initializing (see its errors above; it may not support this game "
                      + "version yet). KirbyMod is not loaded, so its content cannot register under wrong ids.");
            return false;
        }
        catch (Exception ex)
        {
            Log.Warn($"{Tag} Could not check RitsuLib's state; loading anyway. {ex.Message}");
            return true;
        }
    }

    /// <summary>
    /// The newest build not newer than the game (a game older than every build gets the oldest one). When the game's
    /// version is unknown: the newest build whose game types all exist in this game, never simply the newest build.
    /// </summary>
    private static Build? PickBuild(List<Build> builds, Version? host)
    {
        if (host != null)
            return builds.FirstOrDefault(build => build.Version <= host) ?? builds[^1];

        foreach (var build in builds)
        {
            var missing = MissingGameTypes(build);
            if (missing == null)
            {
                Log.Warn($"{Tag} Game version unknown and the builds cannot be checked; using the oldest build {builds[^1].Version}.");
                return builds[^1];
            }
            if (missing.Count == 0)
            {
                Log.Warn($"{Tag} Game version unknown; build {build.Version} is the newest whose game types all exist here.");
                return build;
            }
            Log.Info($"{Tag} Build {build.Version} skipped: this game has no {string.Join(", ", missing.Take(5))}.");
        }

        Log.Error($"{Tag} Game version unknown and no build fits this game; KirbyMod is not loaded.");
        return null;
    }

    /// <summary>The game types the build refers to that this game does not have; null when the check cannot run.</summary>
    private static List<string>? MissingGameTypes(Build build)
    {
        try
        {
            return ScanGameTypeRefs(build.Dll);
        }
        catch (Exception ex)
        {
            Log.Warn($"{Tag} Could not check build {build.Version} against the game: {ex.Message}");
            return null;
        }
    }

    // Kept out of MissingGameTypes so a missing System.Reflection.Metadata surfaces as an exception it can catch.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static List<string> ScanGameTypeRefs(string dll)
    {
        using var pe = new PEReader(File.OpenRead(dll));
        var metadata = pe.GetMetadataReader();
        var gameName = Game.GetName().Name;
        var missing = new List<string>();
        foreach (var handle in metadata.TypeReferences)
        {
            var name = TypeRefName(metadata, handle, out var scope);
            if (scope.Kind != HandleKind.AssemblyReference
                || metadata.GetString(metadata.GetAssemblyReference((AssemblyReferenceHandle)scope).Name) != gameName)
                continue;
            if (Game.GetType(name, false) == null)
                missing.Add(name);
        }
        return missing;
    }

    /// <summary>"Namespace.Outer+Inner", and the scope (assembly) of the outermost type.</summary>
    private static string TypeRefName(MetadataReader metadata, TypeReferenceHandle handle, out EntityHandle scope)
    {
        var type = metadata.GetTypeReference(handle);
        var name = metadata.GetString(type.Name);
        if (type.ResolutionScope.Kind == HandleKind.TypeReference)
            return TypeRefName(metadata, (TypeReferenceHandle)type.ResolutionScope, out scope) + "+" + name;
        scope = type.ResolutionScope;
        var space = metadata.GetString(type.Namespace);
        return space.Length == 0 ? name : space + "." + name;
    }

    /// <summary>Postfix on ReflectionHelper.ModTypes; must never throw, or the game's startup fails with it.</summary>
    private static void AddCoreTypes(ref Type[] __result)
    {
        try
        {
            if (_coreTypes == null)
                return;
            if (ReferenceEquals(__result, _source) && _merged != null)
            {
                __result = _merged;
                return;
            }

            var seen = new HashSet<Type>(__result);
            var added = _coreTypes.Where(seen.Add).ToArray();
            var merged = added.Length == 0 ? __result : [.. __result, .. added];
            _source = __result;
            _merged = merged;
            __result = merged;
        }
        catch (Exception ex)
        {
            if (_mergeErrorLogged)
                return;
            _mergeErrorLogged = true;
            Log.Error($"{Tag} Could not add KirbyMod's types to the game's mod types: {ex}");
        }
    }

    /// <summary>
    /// The game's version ("v0.113.0" -> 0.113.0) and how it was found: the game's own API first (it also knows the
    /// macOS layout), else release_info.json next to the executable or in the macOS app's Contents/Resources.
    /// </summary>
    private static (Version? Version, string Label) ReadGameVersion()
    {
        foreach (var (source, read) in new (string, Func<string?>)[]
                 {
                     ("ReleaseInfoManager", ReleaseInfoManagerVersion),
                     ("NGame.GetGameVersion", NGameVersion),
                 })
        {
            string? text;
            try
            {
                text = read();
            }
            catch (Exception ex)
            {
                Log.Warn($"{Tag} {source} failed: {ex.Message}");
                continue;
            }
            if (ParseVersion(text) is { } version)
                return (version, $"{text} ({source})");
        }

        var exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? "";
        string[] paths =
        [
            Path.Combine(exeDir, ReleaseInfoFile),
            Path.GetFullPath(Path.Combine(exeDir, "..", "Resources", ReleaseInfoFile)),
        ];
        foreach (var path in paths)
        {
            try
            {
                if (!File.Exists(path))
                    continue;
                using var json = JsonDocument.Parse(File.ReadAllText(path));
                var text = json.RootElement.GetProperty("version").GetString();
                if (ParseVersion(text) is { } version)
                    return (version, $"{text} ({path})");
                Log.Warn($"{Tag} {path} has no usable version ({text ?? "null"}).");
            }
            catch (Exception ex)
            {
                Log.Warn($"{Tag} Could not read {path}: {ex.Message}");
            }
        }

        Log.Warn($"{Tag} Could not find the game version: the game API gave none, and no {ReleaseInfoFile} at "
                 + string.Join(" or ", paths) + ".");
        return (null, "unknown");
    }

    private static string? ReleaseInfoManagerVersion()
    {
        var manager = Game.GetType("MegaCrit.Sts2.Core.Debug.ReleaseInfoManager", false);
        var instance = manager?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        var info = instance?.GetType().GetProperty("ReleaseInfo")?.GetValue(instance);
        return info?.GetType().GetProperty("Version")?.GetValue(info) as string;
    }

    private static string? NGameVersion() =>
        Game.GetType("MegaCrit.Sts2.Core.Nodes.NGame", false)
            ?.GetMethod("GetGameVersion", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes)
            ?.Invoke(null, null) as string;

    /// <summary>"v0.113.0", "0.114.0-beta+abc" -> the numeric part; null for anything else (a commit id, "UNKNOWN").</summary>
    private static Version? ParseVersion(string? text)
    {
        var core = text?.Trim().TrimStart('v', 'V').Split('-', '+')[0];
        return Version.TryParse(core, out var version) ? version : null;
    }
}
