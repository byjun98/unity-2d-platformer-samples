using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CoyoteJumpDemoSceneBuilder
{
    private const string ScenePath = "Assets/Demos/CoyoteJump/Scenes/CoyoteJumpBufferDemo.unity";
    private const string PixelPath = "Assets/Demos/CoyoteJump/Art/PixelWhite.png";
    private const float StartTopY = -1.85f;
    private const float LandingTopY = -1.85f;
    private const float BufferTopY = -0.85f;

    [MenuItem("Tools/Coyote Jump/Create Demo Scene")]
    public static void CreateDemoScene()
    {
        EnsurePixelSprite();
        NormalizePixelAdventureImporters();

        Sprite pixel = LoadSprite(PixelPath);
        Sprite terrain = LoadSprite("Assets/Pixel Adventure 1/Assets/Terrain/Terrain Sliced (16x16).png");
        Sprite background = LoadSprite("Assets/Pixel Adventure 1/Assets/Background/Blue.png");
        Sprite playerPreview = LoadSprite("Assets/Pixel Adventure 1/Assets/Main Characters/Ninja Frog/Jump (32x32).png");
        Texture2D idleSheet = LoadTexture("Assets/Pixel Adventure 1/Assets/Main Characters/Ninja Frog/Idle (32x32).png");
        Texture2D runSheet = LoadTexture("Assets/Pixel Adventure 1/Assets/Main Characters/Ninja Frog/Run (32x32).png");
        Texture2D jumpSheet = LoadTexture("Assets/Pixel Adventure 1/Assets/Main Characters/Ninja Frog/Jump (32x32).png");
        Texture2D fallSheet = LoadTexture("Assets/Pixel Adventure 1/Assets/Main Characters/Ninja Frog/Fall (32x32).png");
        Sprite startFlag = LoadSprite("Assets/Pixel Adventure 1/Assets/Items/Checkpoints/Start/Start (Idle).png");
        Sprite endFlag = LoadSprite("Assets/Pixel Adventure 1/Assets/Items/Checkpoints/End/End (Idle).png");
        Sprite fruit = LoadSprite("Assets/Pixel Adventure 1/Assets/Items/Fruits/Apple.png");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "CoyoteJumpBufferDemo";

        CreateCamera();
        CreateLights();

        GameObject root = new GameObject("Coyote Jump Buffer Demo");

        CreateBackground("Pixel Sky", background, new Vector3(0f, 0f, 4f), new Vector3(18f, 10f, 1f), root.transform);
        CreatePlatform("Start Ledge", terrain, new Vector2(-4.8f, -2.35f), new Vector2(5.4f, 1f), root.transform);
        CreatePlatform("Landing Ledge", terrain, new Vector2(1.9f, -2.35f), new Vector2(4.6f, 1f), root.transform);
        CreatePlatform("Buffer Practice Ledge", terrain, new Vector2(6.8f, -1.35f), new Vector2(3.6f, 1f), root.transform);
        CreateMarker("Coyote Window Marker", pixel, new Vector2(-2.05f, StartTopY + 0.55f), new Vector2(0.12f, 1.1f), new Color(1f, 0.8f, 0.18f, 0.72f), root.transform);
        CreateMarker("Buffer Landing Marker", pixel, new Vector2(4.95f, LandingTopY + 0.55f), new Vector2(0.12f, 1.1f), new Color(0.27f, 0.9f, 1f, 0.72f), root.transform);

        CreateDecorationOnGround("Start Flag", startFlag, -6.65f, StartTopY, Vector3.one, root.transform, 7);
        CreateDecorationOnGround("End Flag", endFlag, 7.65f, BufferTopY, Vector3.one * 0.68f, root.transform, 7);
        CreateDecorationOnGround("Buffer Apple", fruit, 4.95f, LandingTopY, Vector3.one, root.transform, 7);

        GameObject player = CreatePlayer(playerPreview, idleSheet, runSheet, jumpSheet, fallSheet, root.transform);
        CreateHud(pixel, root.transform);
        CreateSceneLabels(root.transform);

        Selection.activeGameObject = player;
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Created coyote time and jump buffer demo scene at " + ScenePath);
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0.4f, 0.1f, -10f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.6f;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.16f, 1f);
        cameraObject.AddComponent<AudioListener>();
    }

    private static void CreateLights()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.9f;
        lightObject.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

        Type light2DType = Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
        if (light2DType == null)
        {
            return;
        }

        GameObject globalLight = new GameObject("Global Light 2D");
        Component light2D = globalLight.AddComponent(light2DType);
        light2DType.GetProperty("intensity")?.SetValue(light2D, 1f);
    }

    private static GameObject CreatePlayer(Sprite preview, Texture2D idleSheet, Texture2D runSheet, Texture2D jumpSheet, Texture2D fallSheet, Transform parent)
    {
        GameObject player = new GameObject("CoyotePlayer");
        player.transform.SetParent(parent);
        player.transform.position = new Vector3(-6.3f, -1.1f, 0f);

        float playerScale = preview == null || preview.bounds.size.y <= 0f ? 0.75f : 1.5f / preview.bounds.size.y;
        player.transform.localScale = Vector3.one * playerScale;

        SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
        renderer.sprite = preview;
        renderer.sortingOrder = 10;

        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 3.1f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.freezeRotation = true;

        BoxCollider2D collider = player.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.68f / playerScale, 1.12f / playerScale);
        collider.offset = new Vector2(0f, -0.18f / playerScale);

        CoyoteJumpExampleController controller = player.AddComponent<CoyoteJumpExampleController>();
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("idleSprite").objectReferenceValue = preview;
        serialized.FindProperty("runSprite").objectReferenceValue = preview;
        serialized.FindProperty("jumpSprite").objectReferenceValue = preview;
        serialized.FindProperty("fallSprite").objectReferenceValue = preview;
        serialized.FindProperty("idleSheet").objectReferenceValue = idleSheet;
        serialized.FindProperty("runSheet").objectReferenceValue = runSheet;
        serialized.FindProperty("jumpSheet").objectReferenceValue = jumpSheet;
        serialized.FindProperty("fallSheet").objectReferenceValue = fallSheet;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return player;
    }

    private static GameObject CreatePlatform(string name, Sprite sprite, Vector2 position, Vector2 size, Transform parent)
    {
        GameObject platform = new GameObject(name);
        platform.transform.SetParent(parent);
        platform.transform.position = new Vector3(position.x, position.y, 0f);

        int columns = Mathf.CeilToInt(size.x);
        int rows = Mathf.CeilToInt(size.y);
        Vector2 start = new Vector2(-columns * 0.5f + 0.5f, -rows * 0.5f + 0.5f);
        Vector3 tileScale = Vector3.one;

        if (sprite != null && sprite.bounds.size.x > 0f && sprite.bounds.size.y > 0f)
        {
            tileScale = new Vector3(1f / sprite.bounds.size.x, 1f / sprite.bounds.size.y, 1f);
        }

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                GameObject tile = new GameObject("Tile");
                tile.transform.SetParent(platform.transform);
                tile.transform.localPosition = new Vector3(start.x + x, start.y + y, 0f);
                tile.transform.localScale = tileScale;

                SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 1;
            }
        }

        BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
        collider.size = size;

        return platform;
    }

    private static void CreateMarker(string name, Sprite sprite, Vector2 position, Vector2 size, Color color, Transform parent)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent);
        marker.transform.position = new Vector3(position.x, position.y, -0.05f);
        marker.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = 6;
    }

    private static void CreateDecoration(string name, Sprite sprite, Vector3 position, Vector3 scale, Transform parent, int sortingOrder)
    {
        if (sprite == null)
        {
            return;
        }

        GameObject decoration = new GameObject(name);
        decoration.transform.SetParent(parent);
        decoration.transform.position = position;
        decoration.transform.localScale = scale;

        SpriteRenderer renderer = decoration.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
    }

    private static void CreateDecorationOnGround(string name, Sprite sprite, float x, float groundY, Vector3 scale, Transform parent, int sortingOrder)
    {
        if (sprite == null)
        {
            return;
        }

        float y = groundY - sprite.bounds.min.y * scale.y;
        CreateDecoration(name, sprite, new Vector3(x, y, 0f), scale, parent, sortingOrder);
    }

    private static void CreateBackground(string name, Sprite sprite, Vector3 position, Vector3 scale, Transform parent)
    {
        GameObject background = new GameObject(name);
        background.transform.SetParent(parent);
        background.transform.position = position;
        background.transform.localScale = scale;

        SpriteRenderer renderer = background.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = -20;
    }

    private static void CreateHud(Sprite pixel, Transform parent)
    {
        CreateText("DemoTitle", "코요테 타임 + 점프 버퍼", new Vector3(-7.75f, 3.75f, 0f), 0.28f, Color.white, parent);
        CreateText("DemoStateText", "A/D·←/→ 이동  W/↑/스페이스 점프  S/↓ 빠른 낙하", new Vector3(-7.75f, 3.28f, 0f), 0.15f, new Color(0.78f, 0.9f, 1f, 1f), parent);
        CreateText("CoyoteReadout", "코요테  0.00", new Vector3(-7.75f, 2.75f, 0f), 0.17f, new Color(1f, 0.84f, 0.24f, 1f), parent);
        CreateText("BufferReadout", "버퍼  0.00", new Vector3(-7.75f, 2.35f, 0f), 0.17f, new Color(0.34f, 0.95f, 1f, 1f), parent);

        CreateBar("CoyoteBar", "CoyoteBarFill", pixel, new Vector3(-4.65f, 2.83f, 0f), new Color(1f, 0.84f, 0.24f, 1f), parent);
        CreateBar("BufferBar", "BufferBarFill", pixel, new Vector3(-4.65f, 2.43f, 0f), new Color(0.34f, 0.95f, 1f, 1f), parent);
    }

    private static void CreateSceneLabels(Transform parent)
    {
        CreateText("CoyoteLabel", "발판 끝에서 늦게 점프", new Vector3(-3.1f, -0.85f, 0f), 0.16f, new Color(1f, 0.84f, 0.24f, 1f), parent);
        CreateText("BufferLabel", "착지 직전에 점프 입력", new Vector3(3.05f, -0.85f, 0f), 0.16f, new Color(0.34f, 0.95f, 1f, 1f), parent);
    }

    private static void CreateBar(string backgroundName, string fillName, Sprite pixel, Vector3 position, Color fillColor, Transform parent)
    {
        GameObject background = new GameObject(backgroundName);
        background.transform.SetParent(parent);
        background.transform.position = position;
        background.transform.localScale = new Vector3(2.55f, 0.18f, 1f);

        SpriteRenderer backgroundRenderer = background.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = pixel;
        backgroundRenderer.color = new Color(0f, 0f, 0f, 0.72f);
        backgroundRenderer.sortingOrder = 30;

        GameObject fill = new GameObject(fillName);
        fill.transform.SetParent(background.transform);
        fill.transform.localPosition = new Vector3(-1.225f, 0f, -0.05f);
        fill.transform.localScale = new Vector3(0.001f, 0.12f, 1f);

        SpriteRenderer fillRenderer = fill.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = pixel;
        fillRenderer.color = fillColor;
        fillRenderer.sortingOrder = 31;
    }

    private static void CreateText(string name, string text, Vector3 position, float characterSize, Color color, Transform parent)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent);
        textObject.transform.position = position;

        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.characterSize = characterSize;
        textMesh.anchor = TextAnchor.MiddleLeft;
        textMesh.alignment = TextAlignment.Left;
        textMesh.color = color;

        MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 50;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite direct = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (direct != null)
        {
            return direct;
        }

        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }

    private static Texture2D LoadTexture(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void EnsurePixelSprite()
    {
        if (!File.Exists(PixelPath))
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(PixelPath));
            File.WriteAllBytes(PixelPath, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(PixelPath);
        }

        TextureImporter importer = AssetImporter.GetAtPath(PixelPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 1f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    private static void NormalizePixelAdventureImporters()
    {
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Pixel Adventure 1" });

        for (int i = 0; i < textureGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(textureGuids[i]);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            bool changed = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, 16f))
            {
                importer.spritePixelsPerUnit = 16f;
                changed = true;
            }

            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }
    }
}
