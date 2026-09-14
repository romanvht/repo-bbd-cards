using System;
using System.Collections.Generic;
using BbdCards.Helpers;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using REPOLib.Modules;
using UnityEngine;

namespace BbdCards;

[BepInPlugin(Id, "BBD Cards", "0.1.3")]
[BepInDependency("REPOLib", "4.2.0")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Id = "romanvht.BbdCards";

    internal static Plugin Instance = null!;

    private readonly List<PrefabRef> registered = new();

    private ConfigEntry<bool> debugSpawn = null!;

    private ConfigEntry<float> min = null!;

    private ConfigEntry<float> max = null!;

    private ConfigEntry<float> mass = null!;

    private ConfigEntry<float> fragility = null!;

    private bool initialized;

    private int nextDebugCard;

    /// <summary>Loads settings and installs the registration hook.</summary>
    private void Awake()
    {
        Instance = this;
        debugSpawn = Config.Bind("Debug", "EnableSpawnKey", false, "Host: F9 spawns the next card during a level.");
        min = Bind("ValueMin", 350f, 1, 100000, "Minimum base value; game multipliers apply.");
        max = Bind("ValueMax", 850f, 1, 100000, "Maximum base value.");
        mass = Bind("Mass", 0.25f, 0.1f, 30, "Mass; use identical settings on all clients.");
        fragility = Bind("Fragility", 20f, 0, 100, "Impact fragility; use identical settings on all clients.");
        new Harmony(Id).PatchAll(typeof(Plugin).Assembly);
        Logger.LogInfo($"BBD Cards loaded: {CardAssets.Ids.Length} embedded images.");
    }

    /// <summary>Binds a card setting that takes effect after restart.</summary>
    private ConfigEntry<float> Bind(string key, float value, float low, float high, string description)
    {
        return ConfigValues.Bind(Config, "Cards", key, value, low, high, description + " Restart after changes.");
    }

    /// <summary>Builds and registers every embedded card once.</summary>
    internal void Initialize()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;

        try
        {
            if (CardAssets.Ids.Length == 0)
            {
                throw new InvalidOperationException("No embedded card images.");
            }

            var donor = ValuablePrefab.LoadDonor();
            var collider = StockCollider.FindTemplate(donor);
            var material = MaterialFactory.FindTemplate(donor);
            var mesh = CardAssets.CreateMesh();
            var storage = ValuablePrefab.CreateStorage("BbdCards Prefabs");

            foreach (string id in CardAssets.Ids)
            {
                try
                {
                    RegisterCard(id, donor, storage.transform, collider, material, mesh);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Cannot register {id}: {ex}");
                }
            }

            Logger.LogInfo($"BBD Cards ready: {registered.Count}/{CardAssets.Ids.Length} valuables registered.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }

    /// <summary>Creates one card with its image, stock collider and value settings.</summary>
    private void RegisterCard(string id, GameObject donor, Transform storage,
        BoxCollider collider, Material template, Mesh mesh)
    {
        using var prefab = new ValuablePrefab(donor, storage, "BbdCards " + id);
        var material = prefab.Own(CardAssets.CreateMaterial(id, template));

        prefab.Own(material.mainTexture);
        prefab.AddVisual("Rounded Card", mesh, material);

        var size = new Vector3(CardGeometry.Side, CardGeometry.Side, CardGeometry.Thickness);
        var center = new Vector3(0, CardGeometry.Side / 2, 0);

        StockCollider.Attach(collider, prefab.Root.transform, "Card Collider", center, size, Quaternion.identity);
        prefab.Configure(min.Value, max.Value, mass.Value, fragility.Value, center);
        prefab.Root.AddComponent<CardMarker>().CardId = id;
        registered.Add(prefab.Register());
    }

    /// <summary>Spawns the next card when the host presses F9.</summary>
    private void Update()
    {
        if (!DebugSpawn.TryGetCamera(debugSpawn.Value, KeyCode.F9, registered.Count, out var camera))
        {
            return;
        }

        var position = camera.transform.position + camera.transform.forward * 1.5f;
        var rotation = Quaternion.Euler(90, camera.transform.eulerAngles.y, 0);

        Valuables.SpawnValuable(registered[nextDebugCard], position, rotation);
        nextDebugCard = (nextDebugCard + 1) % registered.Count;
    }
}

public sealed class CardMarker : MonoBehaviour
{
    public string CardId = "";
}

[HarmonyPatch(typeof(RunManager), "Awake")]
internal static class RunManagerPatch
{
    /// <summary>Registers cards after REPOLib is ready.</summary>
    [HarmonyPostfix, HarmonyAfter("REPOLib")]
    private static void Postfix() => Plugin.Instance.Initialize();
}
