using UnityEngine;
using UnityEditor;
using System.IO;
using PuzzleParty.Board;
using PuzzleParty.Board.Effects;
using UnityEditor.SceneManagement;

public static class GenerateEffectSprites
{
    [MenuItem("Tools/Generate Effect Sprites")]
    public static void Generate()
    {
        string dir = "Assets/Resources/Images/Effects";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        GenerateSparkleGlow(dir);
        GenerateConfettiPiece(dir);
        GenerateWhiteSquare(dir);

        AssetDatabase.Refresh();
        Debug.Log("Effect sprites generated in " + dir);
    }

    [MenuItem("Tools/Setup Effect Prefabs")]
    public static void SetupEffectPrefabs()
    {
        // Step 1: Generate sprites first
        Generate();

        // Step 2: Ensure prefab folders exist
        string prefabDir = "Assets/Resources/Prefabs/Effects";
        if (!Directory.Exists(prefabDir))
        {
            Directory.CreateDirectory(prefabDir);
            AssetDatabase.Refresh();
        }

        // Step 3: Create each effect prefab
        CreateConfettiEffectPrefab(prefabDir);
        CreateSparkleTrailEffectPrefab(prefabDir);
        CreateVictorySparkleEffectPrefab(prefabDir);
        CreateChainBreakEffectPrefab(prefabDir);
        CreateChainOverlayPrefab(prefabDir);

        // Step 4: Wire prefab references to BoardView in the scene
        WirePrefabsToBoardView();

        Debug.Log("All effect prefabs created and wired to BoardView!");
    }

    private static void CreateConfettiEffectPrefab(string dir)
    {
        string path = Path.Combine(dir, "ConfettiEffect.prefab");
        if (File.Exists(path))
        {
            Debug.Log("ConfettiEffect prefab already exists, skipping.");
            return;
        }

        GameObject go = new GameObject("ConfettiEffect");
        go.AddComponent<ConfettiEffect>();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log("Created ConfettiEffect prefab at " + path);
    }

    private static void CreateSparkleTrailEffectPrefab(string dir)
    {
        string path = Path.Combine(dir, "SparkleTrailEffect.prefab");
        if (File.Exists(path))
        {
            Debug.Log("SparkleTrailEffect prefab already exists, skipping.");
            return;
        }

        GameObject go = new GameObject("SparkleTrailEffect");
        go.AddComponent<SparkleTrailEffect>();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log("Created SparkleTrailEffect prefab at " + path);
    }

    private static void CreateVictorySparkleEffectPrefab(string dir)
    {
        string path = Path.Combine(dir, "VictorySparkleEffect.prefab");
        if (File.Exists(path))
        {
            Debug.Log("VictorySparkleEffect prefab already exists, skipping.");
            return;
        }

        GameObject go = new GameObject("VictorySparkleEffect");
        go.AddComponent<VictorySparkleEffect>();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log("Created VictorySparkleEffect prefab at " + path);
    }

    private static void CreateChainBreakEffectPrefab(string dir)
    {
        string path = Path.Combine(dir, "ChainBreakEffect.prefab");
        if (File.Exists(path))
        {
            Debug.Log("ChainBreakEffect prefab already exists, skipping.");
            return;
        }

        GameObject go = new GameObject("ChainBreakEffect");
        go.AddComponent<ChainBreakEffect>();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log("Created ChainBreakEffect prefab at " + path);
    }

    private static void CreateChainOverlayPrefab(string dir)
    {
        string path = Path.Combine(dir, "ChainOverlay.prefab");
        if (File.Exists(path))
        {
            Debug.Log("ChainOverlay prefab already exists, skipping.");
            return;
        }

        // Root with controller
        GameObject root = new GameObject("ChainOverlay");
        ChainOverlayController controller = root.AddComponent<ChainOverlayController>();

        // Child: dark overlay
        GameObject darkOverlay = new GameObject("ChainDarkOverlay");
        darkOverlay.transform.SetParent(root.transform, false);
        SpriteRenderer darkSr = darkOverlay.AddComponent<SpriteRenderer>();
        darkSr.sortingOrder = 19;
        darkSr.color = new Color(0f, 0f, 0f, 0.5f);

        // Child: chain sprite
        GameObject chainSprite = new GameObject("ChainSprite");
        chainSprite.transform.SetParent(root.transform, false);
        SpriteRenderer chainSr = chainSprite.AddComponent<SpriteRenderer>();
        chainSr.sortingOrder = 20;

        // Wire references via serialized fields
        var so = new SerializedObject(controller);
        so.FindProperty("darkOverlayRenderer").objectReferenceValue = darkSr;
        so.FindProperty("chainRenderer").objectReferenceValue = chainSr;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log("Created ChainOverlay prefab at " + path);
    }

    private static void WirePrefabsToBoardView()
    {
        // Find BoardView in the current scene
        BoardView boardView = Object.FindFirstObjectByType<BoardView>();
        if (boardView == null)
        {
            Debug.LogWarning("No BoardView found in the current scene. Open the game scene and run Tools > Setup Effect Prefabs again to wire references.");
            return;
        }

        string prefabDir = "Assets/Resources/Prefabs/Effects";

        var so = new SerializedObject(boardView);

        // Wire each prefab reference
        WirePrefabField<ConfettiEffect>(so, "confettiEffectPrefab", Path.Combine(prefabDir, "ConfettiEffect.prefab"));
        WirePrefabField<SparkleTrailEffect>(so, "sparkleTrailEffectPrefab", Path.Combine(prefabDir, "SparkleTrailEffect.prefab"));
        WirePrefabField<VictorySparkleEffect>(so, "victorySparkleEffectPrefab", Path.Combine(prefabDir, "VictorySparkleEffect.prefab"));
        WirePrefabField<ChainBreakEffect>(so, "chainBreakEffectPrefab", Path.Combine(prefabDir, "ChainBreakEffect.prefab"));
        WirePrefabField<ChainOverlayController>(so, "chainOverlayPrefab", Path.Combine(prefabDir, "ChainOverlay.prefab"));

        so.ApplyModifiedProperties();

        // Mark scene as dirty so it saves
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(boardView.gameObject.scene);

        Debug.Log("All effect prefab references wired to BoardView!");
    }

    private static void WirePrefabField<T>(SerializedObject so, string fieldName, string prefabPath) where T : MonoBehaviour
    {
        var prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogWarning($"Field '{fieldName}' not found on BoardView");
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"Prefab not found at '{prefabPath}'");
            return;
        }

        T component = prefab.GetComponent<T>();
        if (component == null)
        {
            Debug.LogWarning($"Component '{typeof(T).Name}' not found on prefab at '{prefabPath}'");
            return;
        }

        prop.objectReferenceValue = component;
        Debug.Log($"Wired {fieldName} -> {prefabPath}");
    }

    [MenuItem("Tools/Setup Ice Prefabs")]
    public static void SetupIcePrefabs()
    {
        string imageDir = "Assets/Resources/Images/Effects";
        if (!Directory.Exists(imageDir)) Directory.CreateDirectory(imageDir);

        string prefabDir = "Assets/Resources/Prefabs/Effects";
        if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);

        GenerateIceOverlaySprite(imageDir);
        AssetDatabase.Refresh();

        // Set ice_overlay.png to Sprite texture type so it can be loaded as Sprite
        string iceImagePath = "Assets/Resources/Images/Effects/ice_overlay.png";
        TextureImporter ti = AssetImporter.GetAtPath(iceImagePath) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            AssetDatabase.ImportAsset(iceImagePath, ImportAssetOptions.ForceUpdate);
        }

        CreateIceOverlayPrefab(prefabDir);
        CreateIceBreakEffectPrefab(prefabDir);
        AssetDatabase.Refresh();

        WireIcePrefabsToScene();

        Debug.Log("Ice prefabs created and wired!");
    }

    private static void GenerateIceOverlaySprite(string dir)
    {
        string path = Path.Combine(dir, "ice_overlay.png");
        if (File.Exists(path)) { Debug.Log("ice_overlay.png already exists, skipping."); return; }

        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        System.Random rng = new System.Random(42);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Base ice blue-white color
                float noise = (float)rng.NextDouble() * 0.15f;
                float edge = 1f - Mathf.Clamp01(
                    Mathf.Min(x, size - 1 - x, y, size - 1 - y) / (size * 0.12f));

                float r = Mathf.Clamp01(0.72f + noise + edge * 0.2f);
                float g = Mathf.Clamp01(0.88f + noise + edge * 0.1f);
                float b = 1f;
                float a = Mathf.Clamp01(0.78f + edge * 0.15f);

                tex.SetPixel(x, y, new Color(r, g, b, a));
            }
        }

        // Draw simple ice crystal lines
        for (int i = 0; i < 6; i++)
        {
            float angle = i * Mathf.PI / 3f;
            for (float t = 0; t <= 1f; t += 0.01f)
            {
                int px = Mathf.RoundToInt(size / 2f + Mathf.Cos(angle) * t * size * 0.4f);
                int py = Mathf.RoundToInt(size / 2f + Mathf.Sin(angle) * t * size * 0.4f);
                if (px >= 0 && px < size && py >= 0 && py < size)
                    tex.SetPixel(px, py, new Color(1f, 1f, 1f, 0.9f));
            }
        }

        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log("Generated ice_overlay.png");
    }

    private static void CreateIceOverlayPrefab(string dir)
    {
        string path = Path.Combine(dir, "IceOverlay.prefab");
        if (File.Exists(path)) { Debug.Log("IceOverlay prefab already exists, skipping."); return; }

        Sprite iceSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Images/Effects/ice_overlay.png");
        if (iceSprite == null)
        {
            Debug.LogWarning("ice_overlay.png sprite not found — run Tools/Generate Effect Sprites first");
            return;
        }

        GameObject root = new GameObject("IceOverlay");
        root.AddComponent<IceOverlayController>();

        GameObject iceChild = new GameObject("IceSprite");
        iceChild.transform.SetParent(root.transform, false);
        SpriteRenderer iceSr = iceChild.AddComponent<SpriteRenderer>();
        iceSr.sprite = iceSprite;
        iceSr.sortingOrder = 21;
        iceSr.color = new Color(0.7f, 0.9f, 1f, 0.85f);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log("Created IceOverlay prefab at " + path);
    }

    private static void CreateIceBreakEffectPrefab(string dir)
    {
        string path = Path.Combine(dir, "IceBreakEffect.prefab");
        if (File.Exists(path)) { Debug.Log("IceBreakEffect prefab already exists, skipping."); return; }

        GameObject go = new GameObject("IceBreakEffect");
        go.AddComponent<IceBreakEffect>();

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = 0.6f;
        main.startSpeed = 2.5f;
        main.startSize = 0.12f;
        main.startColor = new Color(0.7f, 0.93f, 1f, 1f);
        main.maxParticles = 30;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 25) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 25;

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log("Created IceBreakEffect prefab at " + path);
    }

    private static void WireIcePrefabsToScene()
    {
        string prefabDir = "Assets/Resources/Prefabs/Effects";

        // Find or add IceSystem on the Board GameObject
        BoardView boardView = Object.FindFirstObjectByType<BoardView>();
        if (boardView == null)
        {
            Debug.LogWarning("No BoardView found in scene. Open the game scene first.");
            return;
        }

        GameObject boardGO = boardView.gameObject;

        IceSystem iceSystem = boardGO.GetComponent<IceSystem>();
        if (iceSystem == null)
            iceSystem = boardGO.AddComponent<IceSystem>();

        // Wire prefabs on IceSystem
        var iceSo = new SerializedObject(iceSystem);
        WirePrefabField<IceOverlayController>(iceSo, "iceOverlayPrefab", Path.Combine(prefabDir, "IceOverlay.prefab"));
        WirePrefabField<IceBreakEffect>(iceSo, "iceBreakEffectPrefab", Path.Combine(prefabDir, "IceBreakEffect.prefab"));
        iceSo.ApplyModifiedProperties();

        // Wire iceSystem reference on BoardView
        var boardViewSo = new SerializedObject(boardView);
        var iceSystemProp = boardViewSo.FindProperty("iceSystem");
        if (iceSystemProp != null)
        {
            iceSystemProp.objectReferenceValue = iceSystem;
            boardViewSo.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogWarning("'iceSystem' field not found on BoardView");
        }

        EditorSceneManager.MarkSceneDirty(boardGO.scene);
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        Debug.Log("Ice prefabs wired to scene!");
    }

    private static void GenerateSparkleGlow(string dir)
    {
        int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float norm = Mathf.Clamp01(dist / radius);
                float core = Mathf.Clamp01(1f - norm * 3f);
                float glow = (1f - norm) * (1f - norm);
                float alpha = Mathf.Clamp01(core + glow);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        File.WriteAllBytes(Path.Combine(dir, "sparkle_glow.png"), png);
    }

    private static void GenerateConfettiPiece(string dir)
    {
        int width = 8;
        int height = 12;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                tex.SetPixel(x, y, Color.white);
            }
        }
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        File.WriteAllBytes(Path.Combine(dir, "confetti_piece.png"), png);
    }

    private static void GenerateWhiteSquare(string dir)
    {
        int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                tex.SetPixel(x, y, Color.white);
            }
        }
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        File.WriteAllBytes(Path.Combine(dir, "white_square.png"), png);
    }
}
