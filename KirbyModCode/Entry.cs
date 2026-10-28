using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;

namespace KirbyMod;

[ModInitializer(nameof(Initialize))]
public static class Entry
{
    public const string ModId = "KirbyMod";

    public static Logger Logger { get; private set; } = null!;

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        Logger = RitsuLibFramework.CreateLogger(ModId);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
        new Harmony("com.kirbymod.sts2").PatchAll(assembly);

        Logger.Info("KirbyMod initialized.");
    }
}
