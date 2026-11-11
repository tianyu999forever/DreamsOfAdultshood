#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 菜单：Mission4 > Build Level (ziyu)
// 一键在 Mission4 场景里生成：起点房间、迷宫、所有站点、玩家、UI、各种脚本引用。
public static class Mission4_Build_Editor_ziyu
{
    const string ScenePath = "Assets/Scenes/Mission4.unity";
    const string AssetDir = "Assets/Mission4_ziyu";
    const string MatDir = AssetDir + "/Materials";
    const string MapPng = AssetDir + "/mission4_map.png";

    const float Cell = 3f;
    const float WallH = 5f;

    // # 墙   . 地面   E 迷宫入口   B 汉堡胚   L 生菜   T 番茄   P 汉堡肉
    // C 案板   S 灶台   X 出口窗口      起点房间 = 左下角 5x5 的空地；迷宫 = 有岔路和死胡同的小迷宫
    static readonly string[] Grid =
    {
        "#########################",
        "#...........#...........#",
        "#.###.#.###.#.#########.#",
        "#...#.#.#.....#.#...#...#",
        "###.#.#.#.#####.#.#.#.#.#",
        "#.#.#.#.#.#.....#.#.....#",
        "#.#.#.#.#.#.#.#.#.#######",
        "#.#.#.#P#.#X#.#.#.......#",
        "#.#.#.###.###.#.#######.#",
        "#...#.........#.#L..#...#",
        "#######.#######.###.#.###",
        "#.....#.#...#C......#...#",
        "#.....###.#.###.###.#.#.#",
        "#.....E...#...#S#...#T#.#",
        "#.....#####.#.###.#.###.#",
        "#.....#B..........#.....#",
        "#########################"
    };

    static int GW { get { return Grid[0].Length; } }
    static int GH { get { return Grid.Length; } }

    static bool IsRoom(int c, int r) { return c >= 1 && c <= 5 && r >= 11 && r <= 15; }

    static Vector3 CellPos(int c, int r, float y = 0f)
    {
        return new Vector3(c * Cell, y, (GH - 1 - r) * Cell);
    }

    static Material whiteMat;
    static Material unlitMat;
    static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();

    // ------------------------------------------------------------------

    [MenuItem("Mission4/Build Level (ziyu)")]
    public static void Build()
    {
        if (!EditorUtility.DisplayDialog(
                "Mission4 Build",
                "This will OPEN scene Mission4 and DELETE everything in it, then rebuild the whole level.\n\nContinue?",
                "Build", "Cancel"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (GameObject go in scene.GetRootGameObjects())
            UnityEngine.Object.DestroyImmediate(go);

        EnsureFolder("Assets", "Mission4_ziyu");
        EnsureFolder(AssetDir, "Materials");
        matCache.Clear();

        whiteMat = GetMat(Color.white);
        unlitMat = null;

        Texture2D mapTex = BuildMapTexture();

        GameObject root = new GameObject("Mission4_Level");

        BuildEnvironment();
        BuildMaze(root.transform);
        Mission4_Game_ziyu game = BuildGameObjects(root.transform, mapTex);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Mission4: level built and saved. Press Play to test.");
        EditorUtility.DisplayDialog("Mission4 Build", "Done! Scene saved. Press Play to test.", "OK");
    }

    // ------------------------------------------------------------------ 资源

    static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            AssetDatabase.CreateFolder(parent, name);
    }

    static Shader LitShader()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        return s;
    }

    static Material GetMat(Color c)
    {
        string key = ColorUtility.ToHtmlStringRGB(c);
        Material m;
        if (matCache.TryGetValue(key, out m) && m != null) return m;

        string path = MatDir + "/m4_" + key + ".mat";
        m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(LitShader());
            m.color = c;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(m, path);
        }
        else
        {
            m.color = c;
        }

        matCache[key] = m;
        return m;
    }

    static Material GetUnlit(Texture2D tex)
    {
        string path = MatDir + "/m4_map_poster.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) AssetDatabase.DeleteAsset(path);

        Shader s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Unlit/Texture");
        m = new Material(s);
        m.mainTexture = tex;
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static Color StationColor(char ch)
    {
        switch (ch)
        {
            case 'B': return new Color(0.86f, 0.62f, 0.30f);
            case 'L': return new Color(0.30f, 0.75f, 0.25f);
            case 'T': return new Color(0.90f, 0.15f, 0.12f);
            case 'P': return new Color(0.80f, 0.40f, 0.40f);
            case 'C': return new Color(0.35f, 0.60f, 0.95f);
            case 'S': return new Color(1.00f, 0.55f, 0.10f);
            case 'X': return new Color(1.00f, 0.85f, 0.10f);
            case 'E': return new Color(0.40f, 0.95f, 0.50f);
            default: return Color.white;
        }
    }

    static Texture2D BuildMapTexture()
    {
        const int S = 12;
        int w = GW * S, h = GH * S;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

        Color wall = new Color(0.12f, 0.12f, 0.15f);
        Color floor = new Color(0.93f, 0.93f, 0.90f);
        Color room = new Color(0.65f, 0.80f, 0.95f);

        for (int r = 0; r < GH; r++)
        {
            for (int c = 0; c < GW; c++)
            {
                char ch = Grid[r][c];
                Color col;
                if (ch == '#') col = wall;
                else if (ch == '.') col = IsRoom(c, r) ? room : floor;
                else col = StationColor(ch);

                int py = (GH - 1 - r) * S;
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                        tex.SetPixel(c * S + x, py + y, col);
            }
        }
        tex.Apply();

        File.WriteAllBytes(MapPng, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(MapPng);

        TextureImporter imp = AssetImporter.GetAtPath(MapPng) as TextureImporter;
        if (imp != null)
        {
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(MapPng);
    }

    // ------------------------------------------------------------------ 几何体

    static GameObject Cube(Transform parent, string name, Vector3 pos, Vector3 scale, Color color,
        bool collider = true)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.position = pos;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = GetMat(color);
        if (!collider) UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }

    static GameObject Cylinder(Transform parent, string name, Vector3 localPos, Vector3 scale, Color color)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = GetMat(color);
        UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }

    static TextMeshPro Text3D(Transform parent, string name, Vector3 worldPos, Quaternion rot,
        string text, float fontSize, Color color, Vector2 size, TextAlignmentOptions align)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.position = worldPos;
        g.transform.rotation = rot;

        TextMeshPro t = g.AddComponent<TextMeshPro>();
        t.text = text;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = align;
        t.rectTransform.sizeDelta = size;
        return t;
    }

    // ------------------------------------------------------------------ 环境

    static void BuildEnvironment()
    {
        GameObject lightGo = new GameObject("Directional Light");
        Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.color = new Color(1f, 0.97f, 0.9f);
        light.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.57f, 0.62f);
        RenderSettings.fog = false;
    }

    // ------------------------------------------------------------------ 迷宫

    static void BuildMaze(Transform root)
    {
        Transform mazeRoot = new GameObject("Maze").transform;
        mazeRoot.SetParent(root, false);

        // 地面
        Cube(mazeRoot, "Floor",
            new Vector3((GW - 1) * Cell * 0.5f, -0.5f, (GH - 1) * Cell * 0.5f),
            new Vector3(GW * Cell, 1f, GH * Cell),
            new Color(0.42f, 0.42f, 0.46f));

        // 起点房间地毯（整个 5x5）
        Vector3 roomCenter = CellPos(3, 13);
        Cube(mazeRoot, "StartRoomFloor",
            new Vector3(roomCenter.x, 0.02f, roomCenter.z),
            new Vector3(5 * Cell, 0.04f, 5 * Cell),
            new Color(0.62f, 0.46f, 0.30f), false);

        // 墙：同一行连续的 # 合并成一块
        Color wallColor = new Color(0.82f, 0.76f, 0.62f);
        for (int r = 0; r < GH; r++)
        {
            int c = 0;
            while (c < GW)
            {
                if (Grid[r][c] != '#') { c++; continue; }
                int c0 = c;
                while (c < GW && Grid[r][c] == '#') c++;
                int c1 = c - 1;

                float cx = (c0 + c1) * 0.5f * Cell;
                float z = (GH - 1 - r) * Cell;
                Cube(mazeRoot, "Wall_r" + r + "_c" + c0,
                    new Vector3(cx, WallH * 0.5f, z),
                    new Vector3((c1 - c0 + 1) * Cell, WallH, Cell),
                    wallColor);
            }
        }
    }

    // ------------------------------------------------------------------ 站点

    static Mission4_Station_ziyu BuildStation(Transform parent, char ch, int c, int r)
    {
        // 找到唯一开口的方向（死胡同），站点背靠对面的墙
        Vector3 dirW = Vector3.forward;
        int[,] dirs = { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };
        for (int i = 0; i < 4; i++)
        {
            int nc = c + dirs[i, 0], nr = r + dirs[i, 1];
            if (Grid[nr][nc] != '#')
            {
                dirW = new Vector3(dirs[i, 0], 0, -dirs[i, 1]);
                break;
            }
        }

        Vector3 center = CellPos(c, r);
        GameObject go = new GameObject("Station_" + ch);
        go.transform.SetParent(parent, false);
        go.transform.position = center - dirW * 1.0f;
        go.transform.rotation = Quaternion.LookRotation(-dirW); // +Z 朝墙，-Z 朝玩家

        Mission4_Station_ziyu st = go.AddComponent<Mission4_Station_ziyu>();
        st.foodMaterial = whiteMat;
        Transform t = go.transform;

        Color baseColor;
        string label;
        float labelY = 2.0f;
        Color labelColor = Color.white;

        switch (ch)
        {
            case 'B': st.type = M4StationType.Bun; baseColor = new Color(0.55f, 0.40f, 0.25f); label = "BURGER BUN"; labelColor = StationColor('B'); break;
            case 'L': st.type = M4StationType.Lettuce; baseColor = new Color(0.25f, 0.45f, 0.25f); label = "LETTUCE"; labelColor = StationColor('L'); break;
            case 'T': st.type = M4StationType.Tomato; baseColor = new Color(0.50f, 0.20f, 0.18f); label = "TOMATO"; labelColor = StationColor('T'); break;
            case 'P': st.type = M4StationType.Patty; baseColor = new Color(0.45f, 0.28f, 0.28f); label = "PATTY"; labelColor = StationColor('P'); break;
            case 'C': st.type = M4StationType.Board; baseColor = new Color(0.55f, 0.55f, 0.60f); label = "CUTTING BOARD"; labelY = 1.8f; labelColor = StationColor('C'); break;
            case 'S': st.type = M4StationType.Stove; baseColor = new Color(0.22f, 0.22f, 0.25f); label = "STOVE"; labelY = 1.8f; labelColor = StationColor('S'); break;
            default: st.type = M4StationType.Window; baseColor = new Color(0.55f, 0.45f, 0.15f); label = "EXIT WINDOW\n<size=60%>hand in the burger ingredients</size>"; labelY = 4.4f; labelColor = StationColor('X'); break;
        }

        // 柜台（带碰撞体，射线打到它就算看着这个站点）
        Vector3 baseScale = st.type == M4StationType.Window
            ? new Vector3(2.2f, 1.1f, 0.9f)
            : new Vector3(1.8f, 1.0f, 0.9f);
        GameObject counter = Cube(t, "Counter", Vector3.zero, baseScale, baseColor);
        counter.transform.localPosition = new Vector3(0, baseScale.y * 0.5f, 0);
        counter.transform.localRotation = Quaternion.identity;

        float topY = baseScale.y;

        if (st.IsSource)
        {
            M4Food food = st.type == M4StationType.Bun ? M4Food.Bun :
                          st.type == M4StationType.Lettuce ? M4Food.Lettuce :
                          st.type == M4StationType.Tomato ? M4Food.Tomato : M4Food.Patty;

            Vector3[] spots = { new Vector3(-0.45f, 0, -0.05f), new Vector3(0.45f, 0, -0.05f), new Vector3(0, 0.18f, 0.1f) };
            foreach (Vector3 sp in spots)
            {
                GameObject pile = Mission4_Visual_ziyu.Make(new M4Item(food, M4State.Raw), whiteMat, GetMat);
                pile.transform.SetParent(t, false);
                pile.transform.localPosition = new Vector3(sp.x, topY + sp.y, sp.z);
                pile.transform.localScale = Vector3.one * 1.5f;
            }
        }
        else if (st.type == M4StationType.Board)
        {
            Cube(t, "BoardSlab", Vector3.zero, new Vector3(1.3f, 0.06f, 0.75f), new Color(0.80f, 0.62f, 0.38f), false)
                .transform.localPosition = new Vector3(0, topY + 0.03f, 0);
            Cube(t, "Knife", Vector3.zero, new Vector3(0.5f, 0.03f, 0.08f), new Color(0.85f, 0.85f, 0.9f), false)
                .transform.localPosition = new Vector3(0.45f, topY + 0.08f, 0.2f);

            Transform spot = new GameObject("ItemSpot").transform;
            spot.SetParent(t, false);
            spot.localPosition = new Vector3(0, topY + 0.07f, 0);
            st.itemSpot = spot;
        }
        else if (st.type == M4StationType.Stove)
        {
            Cylinder(t, "Burner", new Vector3(0, topY + 0.015f, 0), new Vector3(0.9f, 0.015f, 0.9f), new Color(0.65f, 0.12f, 0.08f));
            Transform spot = new GameObject("ItemSpot").transform;
            spot.SetParent(t, false);
            spot.localPosition = new Vector3(0, topY + 0.05f, 0);
            st.itemSpot = spot;
        }
        else
        {
            // 出口：窗口柜台 + 柜台后面升降的大门
            GameObject door = Cube(t, "ExitDoor", Vector3.zero, new Vector3(2.6f, 3.8f, 0.12f), new Color(0.15f, 0.55f, 0.25f));
            door.transform.localPosition = new Vector3(0, 1.9f, 0.40f);
            st.exitDoor = door.AddComponent<Mission4_Door_ziyu>();
        }

        // 头顶文字
        TextMeshPro lab = Text3D(t, "Label", Vector3.zero, Quaternion.identity, label, 4f, labelColor,
            new Vector2(8f, 2f), TextAlignmentOptions.Center);
        lab.transform.localPosition = new Vector3(0, labelY, -0.1f);
        lab.gameObject.AddComponent<Mission4_Billboard_ziyu>();
        st.statusLabel = (st.type == M4StationType.Board || st.type == M4StationType.Stove) ? lab : null;

        return st;
    }

    // ------------------------------------------------------------------ 玩家、UI、海报

    static Mission4_Game_ziyu BuildGameObjects(Transform root, Texture2D mapTex)
    {
        Transform stationRoot = new GameObject("Stations").transform;
        stationRoot.SetParent(root, false);

        // 站点
        Mission4_Station_ziyu exitStation = null;
        for (int r = 0; r < GH; r++)
            for (int c = 0; c < GW; c++)
            {
                char ch = Grid[r][c];
                if ("BLTPCSX".IndexOf(ch) < 0) continue;
                Mission4_Station_ziyu st = BuildStation(stationRoot, ch, c, r);
                if (ch == 'X') exitStation = st;
            }

        // 玩家
        Vector3 spawn = CellPos(3, 13, 1.05f);
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.SetParent(root, false);
        player.transform.position = spawn;
        player.transform.rotation = Quaternion.Euler(0, 270f, 0);   // 面向墙上的地图

        CapsuleCollider cap = player.AddComponent<CapsuleCollider>();
        cap.height = 2f;
        cap.radius = 0.4f;

        PhysicsMaterial pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(AssetDir + "/M4_NoFriction.asset");
        if (pm == null)
        {
            pm = new PhysicsMaterial("M4_NoFriction");
            pm.dynamicFriction = 0f;
            pm.staticFriction = 0f;
            pm.frictionCombine = PhysicsMaterialCombine.Minimum;
            AssetDatabase.CreateAsset(pm, AssetDir + "/M4_NoFriction.asset");
        }
        cap.material = pm;

        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        Moving moving = player.AddComponent<Moving>();

        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(player.transform, false);
        camGo.transform.localPosition = new Vector3(0, 0.7f, 0);
        Camera cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 200f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.07f, 0.10f);
        camGo.AddComponent<AudioListener>();
        moving.cameraTransform = camGo.transform;

        MouseLook look = camGo.AddComponent<MouseLook>();
        look.player = player.transform;
        HeadBob bob = camGo.AddComponent<HeadBob>();

        // ---- HUD ----
        GameObject canvasGo = new GameObject("HUD");
        canvasGo.transform.SetParent(root, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform cr = canvasGo.GetComponent<RectTransform>();

        TMP_Text timer = UIText(cr, "TimerText", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(30, -20), new Vector2(700, 80), 56, TextAlignmentOptions.TopLeft, Color.white);
        timer.fontStyle = FontStyles.Bold;

        TMP_Text order = UIText(cr, "OrderText", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-30, -20), new Vector2(520, 220), 30, TextAlignmentOptions.TopLeft, Color.white);

        TMP_Text prompt = UIText(cr, "PromptText", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 110), new Vector2(1500, 60), 34, TextAlignmentOptions.Center, Color.white);

        TMP_Text toast = UIText(cr, "ToastText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 170), new Vector2(1500, 80), 38, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.3f));

        // 准星
        GameObject cross = new GameObject("Crosshair");
        cross.transform.SetParent(cr, false);
        Image ci = cross.AddComponent<Image>();
        ci.color = new Color(1, 1, 1, 0.8f);
        RectTransform crr = cross.GetComponent<RectTransform>();
        crr.anchorMin = crr.anchorMax = crr.pivot = new Vector2(0.5f, 0.5f);
        crr.sizeDelta = new Vector2(8, 8);

        // 背包面板
        GameObject bagPanel = Panel(cr, "BagPanel", new Color(0, 0, 0, 0.75f));
        RectTransform bpr = bagPanel.GetComponent<RectTransform>();
        bpr.anchorMin = bpr.anchorMax = new Vector2(0, 0.5f);
        bpr.pivot = new Vector2(0, 0.5f);
        bpr.anchoredPosition = new Vector2(40, 0);
        bpr.sizeDelta = new Vector2(520, 360);
        TMP_Text bagText = UIText(bpr, "BagText", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, 32, TextAlignmentOptions.TopLeft, Color.white);
        bagText.rectTransform.offsetMin = new Vector2(24, 20);
        bagText.rectTransform.offsetMax = new Vector2(-24, -20);

        // 地图面板（R）
        GameObject mapPanel = Panel(cr, "MapPanel", new Color(0, 0, 0, 0.88f));
        RectTransform mpr = mapPanel.GetComponent<RectTransform>();
        mpr.anchorMin = Vector2.zero; mpr.anchorMax = Vector2.one;
        mpr.offsetMin = mpr.offsetMax = Vector2.zero;

        GameObject mapImg = new GameObject("MapImage");
        mapImg.transform.SetParent(mpr, false);
        RawImage raw = mapImg.AddComponent<RawImage>();
        raw.texture = mapTex;
        RectTransform mir = mapImg.GetComponent<RectTransform>();
        mir.anchorMin = mir.anchorMax = mir.pivot = new Vector2(0.5f, 0.5f);
        mir.anchoredPosition = new Vector2(0, -20);
        mir.sizeDelta = new Vector2(GW * 38f, GH * 38f);     

        GameObject dot = new GameObject("PlayerDot");
        dot.transform.SetParent(mir, false);
        Image di = dot.AddComponent<Image>();
        di.color = new Color(1f, 0.1f, 0.1f);
        RectTransform dr = dot.GetComponent<RectTransform>();
        dr.anchorMin = dr.anchorMax = dr.pivot = new Vector2(0.5f, 0.5f);
        dr.sizeDelta = new Vector2(18, 18);

        UIText(mpr, "MapTitle", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -20), new Vector2(1400, 60), 40, TextAlignmentOptions.Center, Color.white).text =
            "MAZE MAP   (press R to close)   -   red dot = you";

        UIText(mpr, "MapLegend", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 15), new Vector2(1800, 50), 30, TextAlignmentOptions.Center, Color.white).text = Legend();

        // 结算面板
        GameObject resultPanel = Panel(cr, "ResultPanel", new Color(0, 0, 0, 0.85f));
        RectTransform rpr = resultPanel.GetComponent<RectTransform>();
        rpr.anchorMin = Vector2.zero; rpr.anchorMax = Vector2.one;
        rpr.offsetMin = rpr.offsetMax = Vector2.zero;
        TMP_Text resultText = UIText(rpr, "ResultText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(1500, 700), 60, TextAlignmentOptions.Center, Color.white);

        // ---- 逻辑对象 ----
        GameObject logic = new GameObject("Mission4_Game");
        logic.transform.SetParent(root, false);
        Mission4_Game_ziyu game = logic.AddComponent<Mission4_Game_ziyu>();
        Mission4_Bag_ziyu bag = logic.AddComponent<Mission4_Bag_ziyu>();

        bag.bagPanel = bagPanel;
        bag.bagText = bagText;
        bag.game = game;

        Mission4_Interact_ziyu interact = camGo.AddComponent<Mission4_Interact_ziyu>();
        interact.game = game;
        interact.bag = bag;
        interact.promptText = prompt;

        game.timerText = timer;
        game.orderText = order;
        game.toastText = toast;
        game.mapPanel = mapPanel;
        game.mapImageRect = mir;
        game.mapPlayerDot = dr;
        game.gridWidth = GW;
        game.gridHeight = GH;
        game.cellSize = Cell;
        game.resultPanel = resultPanel;
        game.resultText = resultText;
        game.bag = bag;
        game.player = player.transform;
        game.moving = moving;
        game.mouseLook = look;
        game.headBob = bob;

        // 迷宫入口触发器（E 格子）
        for (int r = 0; r < GH; r++)
            for (int c = 0; c < GW; c++)
                if (Grid[r][c] == 'E')
                {
                    GameObject trig = new GameObject("MazeEntrance");
                    trig.transform.SetParent(root, false);
                    trig.transform.position = CellPos(c, r, 2f);
                    BoxCollider bc = trig.AddComponent<BoxCollider>();
                    bc.isTrigger = true;
                    bc.size = new Vector3(0.8f, 4f, Cell * 0.9f);
                    Mission4_Entrance_ziyu en = trig.AddComponent<Mission4_Entrance_ziyu>();
                    en.game = game;

                    // 入口牌子
                    TextMeshPro sign = Text3D(root, "EntranceSign", CellPos(c, r, 3.9f),
                        Quaternion.identity, "MAZE ENTRANCE  >>", 3.5f, StationColor('E'),
                        new Vector2(8, 1.5f), TextAlignmentOptions.Center);
                    sign.gameObject.AddComponent<Mission4_Billboard_ziyu>();
                }

        BuildStartRoomPosters(root, mapTex, game);

        bagPanel.SetActive(false);
        mapPanel.SetActive(false);
        resultPanel.SetActive(false);

        return game;
    }

    static string Legend()
    {
        return Mark(StationColor('B')) + " Bun   " +
               Mark(StationColor('L')) + " Lettuce   " +
               Mark(StationColor('T')) + " Tomato   " +
               Mark(StationColor('P')) + " Patty   " +
               Mark(StationColor('C')) + " Cutting board   " +
               Mark(StationColor('S')) + " Stove   " +
               Mark(StationColor('X')) + " Exit window";
    }

    static string Mark(Color c)
    {
        return "<mark=#" + ColorUtility.ToHtmlStringRGB(c) + "FF>    </mark>";
    }

    // 起点房间： x 1.5..16.5, z 1.5..16.5
    static void BuildStartRoomPosters(Transform root, Texture2D mapTex, Mission4_Game_ziyu game)
    {
        Transform posters = new GameObject("StartRoomPosters").transform;
        posters.SetParent(root, false);

        // 西墙：大地图
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "MapPoster";
        quad.transform.SetParent(posters, false);
        quad.transform.position = new Vector3(1.53f, 2.6f, 9f);
        quad.transform.rotation = Quaternion.LookRotation(Vector3.left);
        quad.transform.localScale = new Vector3(4.8f, 4.8f * GH / GW, 1f);
        quad.GetComponent<Renderer>().sharedMaterial = GetUnlit(mapTex);
        UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());

        Quaternion west = Quaternion.LookRotation(Vector3.left);
        Text3D(posters, "MapTitle", new Vector3(1.52f, 4.65f, 9f), west,
            "MAZE MAP  -  memorize it!  (press R any time)", 3f, Color.white,
            new Vector2(8, 1), TextAlignmentOptions.Center);
        Text3D(posters, "MapLegend", new Vector3(1.52f, 0.45f, 9f), west,
            Legend(), 2.0f, Color.white, new Vector2(10, 0.8f), TextAlignmentOptions.Center);

        // 北墙：订单 + 钟
        Quaternion north = Quaternion.LookRotation(Vector3.forward);
        Text3D(posters, "OrderPoster", new Vector3(6.0f, 2.7f, 16.47f), north,
            "<size=130%><b>YOUR ORDER: 1 BURGER</b></size>\n" +
            "Bring ALL of these to the EXIT WINDOW:\n\n" +
            "- Burger Bun  (just take it)\n" +
            "- Lettuce  -> cut  (3 s)\n" +
            "- Tomato  -> cut  (3 s)\n" +
            "- Patty  -> cut (5 s) -> fry on stove (10 s)\n\n" +
            "You get 3:00 in the maze after you leave this room.\n" +
            "Finish faster for a higher score!",
            2.2f, Color.white, new Vector2(8.5f, 5f), TextAlignmentOptions.Center);

        Vector3 clockPos = new Vector3(13.2f, 2.9f, 16.45f);
        GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        face.name = "ClockFace";
        face.transform.SetParent(posters, false);
        face.transform.position = clockPos;
        face.transform.rotation = north * Quaternion.Euler(90f, 0f, 0f);
        face.transform.localScale = new Vector3(2.6f, 0.04f, 2.6f);
        face.GetComponent<Renderer>().sharedMaterial = GetMat(new Color(0.95f, 0.95f, 0.9f));
        UnityEngine.Object.DestroyImmediate(face.GetComponent<Collider>());

        GameObject pivot = new GameObject("ClockHandPivot");
        pivot.transform.SetParent(posters, false);
        pivot.transform.position = clockPos + Vector3.back * 0.06f;
        pivot.transform.rotation = north;

        GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hand.name = "ClockHand";
        hand.transform.SetParent(pivot.transform, false);
        hand.transform.localPosition = new Vector3(0, 0.5f, 0);
        hand.transform.localScale = new Vector3(0.1f, 1.0f, 0.03f);
        hand.GetComponent<Renderer>().sharedMaterial = GetMat(new Color(0.8f, 0.1f, 0.1f));
        UnityEngine.Object.DestroyImmediate(hand.GetComponent<Collider>());

        TextMeshPro clockText = Text3D(posters, "ClockText", clockPos + new Vector3(0, -1.9f, -0.03f), north,
            "OBSERVE: 60", 4f, Color.white, new Vector2(6, 1.2f), TextAlignmentOptions.Center);

        game.clockText = clockText;
        game.clockHandPivot = pivot.transform;

        // 南墙：操作说明
        Text3D(posters, "ControlsPoster", new Vector3(9f, 2.7f, 1.53f), Quaternion.LookRotation(Vector3.back),
            "<size=130%><b>CONTROLS</b></size>\n\n" +
            "WASD  move        Shift  run        Mouse  look\n" +
            "Left click  take food / take from board or stove / hand in\n" +
            "Q  open the bag (holds 4)\n" +
            "With the bag open: 1-4  put that item on the board / stove\n" +
            "Hold C near the board  to cut\n" +
            "R  map",
            2.1f, Color.white, new Vector2(10, 5f), TextAlignmentOptions.Center);
    }

    // ------------------------------------------------------------------ UI 辅助

    static GameObject Panel(RectTransform parent, string name, Color color)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        Image img = g.AddComponent<Image>();
        img.color = color;
        return g;
    }

    static TMP_Text UIText(RectTransform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pivot,
        Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align, Color color)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        TextMeshProUGUI t = g.AddComponent<TextMeshProUGUI>();
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        t.richText = true;

        RectTransform rt = t.rectTransform;
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return t;
    }
}
#endif
