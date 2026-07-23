using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Replaces the zero-asset circle presentation with layered procedural art.
    /// It intentionally does not change combat data, damage, cooldowns or stage flow.
    /// </summary>
    public sealed class ProceduralBattleArtPrototype : MonoBehaviour
    {
        private enum UnitKind
        {
            Hero,
            CompanionBlade,
            CompanionGuard,
            CompanionMage,
            Mold,
            Weevil,
            Beetle,
            Mycelium,
            Invader,
            BossMold,
            BossBeetle,
            BossPortal
        }

        private sealed class DecoratedUnit
        {
            public Transform Root;
            public Transform VisualRoot;
            public UnitKind Kind;
            public float Seed;
            public Vector3 BaseScale;
        }

        private readonly Dictionary<Transform, DecoratedUnit> decorated = new Dictionary<Transform, DecoratedUnit>();
        private readonly List<Transform> stale = new List<Transform>();
        private readonly List<Transform> companions = new List<Transform>();

        private Sprite circle;
        private Sprite capsule;
        private Sprite diamond;
        private Sprite triangle;
        private Sprite star;
        private Sprite softCircle;
        private Transform worldRoot;
        private Transform heroRoot;
        private Transform environmentRoot;
        private int currentTheme = -1;
        private float scanTimer;
        private float ambientTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<ProceduralBattleArtPrototype>() != null) return;
            GameObject root = new GameObject("Peanut Procedural Art");
            DontDestroyOnLoad(root);
            root.AddComponent<ProceduralBattleArtPrototype>();
        }

        private void Awake()
        {
            circle = CreateShapeSprite("ArtCircle", 64, 64, (x, y) =>
            {
                Vector2 p = new Vector2(x, y);
                float d = p.magnitude;
                return Mathf.Clamp01((1f - d) * 18f);
            });
            softCircle = CreateShapeSprite("ArtSoftCircle", 64, 64, (x, y) =>
            {
                float d = new Vector2(x, y).magnitude;
                return Mathf.Clamp01((1f - d) * 4f) * 0.75f;
            });
            capsule = CreateShapeSprite("ArtCapsule", 64, 64, (x, y) =>
            {
                Vector2 p = new Vector2(x, y);
                p.y *= 0.72f;
                return Mathf.Clamp01((1f - p.magnitude) * 18f);
            });
            diamond = CreateShapeSprite("ArtDiamond", 64, 64, (x, y) =>
                Mathf.Clamp01((1f - (Mathf.Abs(x) + Mathf.Abs(y))) * 18f));
            triangle = CreateShapeSprite("ArtTriangle", 64, 64, (x, y) =>
            {
                float width = Mathf.Lerp(0.05f, 0.95f, (y + 1f) * 0.5f);
                return Mathf.Clamp01((width - Mathf.Abs(x)) * 20f) * Mathf.Clamp01((1f - Mathf.Abs(y)) * 20f);
            });
            star = CreateShapeSprite("ArtStar", 64, 64, StarAlpha);
        }

        private void Update()
        {
            scanTimer -= Time.deltaTime;
            if (scanTimer <= 0f)
            {
                scanTimer = 0.35f;
                ResolveWorld();
                ScanAndDecorate();
                EnsureCompanions();
                RefreshTheme();
            }

            AnimateUnits();
            AnimateCompanions();
            SpawnAmbientParticles();
        }

        private void ResolveWorld()
        {
            if (worldRoot != null) return;
            GameObject found = GameObject.Find("Runtime 2D World");
            if (found == null) return;
            worldRoot = found.transform;
            environmentRoot = new GameObject("Procedural Environment").transform;
            environmentRoot.SetParent(worldRoot, false);
        }

        private void ScanAndDecorate()
        {
            if (worldRoot == null) return;

            stale.Clear();
            foreach (Transform key in decorated.Keys)
                if (key == null) stale.Add(key);
            for (int i = 0; i < stale.Count; i++) decorated.Remove(stale[i]);

            Transform[] all = worldRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform root = all[i];
                if (root == null || decorated.ContainsKey(root)) continue;
                if (root.name == "Peanut Warrior")
                {
                    heroRoot = root;
                    DecorateUnit(root, UnitKind.Hero);
                }
                else if (root.name == "Monster View" || root.name == "Boss View")
                {
                    string label = ReadLabel(root);
                    DecorateUnit(root, ResolveEnemyKind(label, root.name == "Boss View"));
                }
            }
        }

        private void DecorateUnit(Transform root, UnitKind kind)
        {
            Transform oldBody = root.Find("Body");
            if (oldBody != null)
            {
                SpriteRenderer oldRenderer = oldBody.GetComponent<SpriteRenderer>();
                if (oldRenderer != null) oldRenderer.enabled = false;
            }

            Transform visual = new GameObject("Procedural Visual").transform;
            visual.SetParent(root, false);
            BuildUnitVisual(visual, kind);

            decorated[root] = new DecoratedUnit
            {
                Root = root,
                VisualRoot = visual,
                Kind = kind,
                Seed = UnityEngine.Random.Range(0f, 10f),
                BaseScale = visual.localScale
            };
        }

        private void BuildUnitVisual(Transform parent, UnitKind kind)
        {
            switch (kind)
            {
                case UnitKind.Hero:
                    BuildPeanut(parent, new Color(0.86f, 0.53f, 0.16f), new Color(0.42f, 0.17f, 0.06f), true);
                    AddSword(parent, new Vector2(0.47f, 0.02f), 18f, 7);
                    AddCape(parent, new Color(0.73f, 0.12f, 0.08f));
                    AddAura(parent, new Color(1f, 0.74f, 0.18f, 0.24f), 1.4f, -1);
                    break;
                case UnitKind.CompanionBlade:
                    BuildPeanut(parent, new Color(0.91f, 0.65f, 0.24f), new Color(0.47f, 0.22f, 0.08f), false);
                    AddSword(parent, new Vector2(0.38f, 0f), 24f, 7);
                    AddHeadband(parent, new Color(0.78f, 0.08f, 0.06f));
                    break;
                case UnitKind.CompanionGuard:
                    BuildPeanut(parent, new Color(0.77f, 0.56f, 0.24f), new Color(0.37f, 0.18f, 0.08f), false);
                    AddShield(parent, new Vector2(0.4f, -0.02f));
                    AddHelmet(parent, new Color(0.27f, 0.34f, 0.42f));
                    break;
                case UnitKind.CompanionMage:
                    BuildPeanut(parent, new Color(0.82f, 0.59f, 0.26f), new Color(0.38f, 0.18f, 0.08f), false);
                    AddStaff(parent, new Vector2(0.44f, 0f));
                    AddWizardHat(parent, new Color(0.35f, 0.16f, 0.55f));
                    AddAura(parent, new Color(0.48f, 0.23f, 1f, 0.25f), 1.25f, -1);
                    break;
                case UnitKind.Mold:
                    BuildMold(parent, false);
                    break;
                case UnitKind.Weevil:
                    BuildWeevil(parent, false);
                    break;
                case UnitKind.Beetle:
                    BuildBeetle(parent, false);
                    break;
                case UnitKind.Mycelium:
                    BuildMycelium(parent, false);
                    break;
                case UnitKind.Invader:
                    BuildInvader(parent, false);
                    break;
                case UnitKind.BossMold:
                    BuildMold(parent, true);
                    AddAura(parent, new Color(0.44f, 1f, 0.2f, 0.24f), 1.55f, -2);
                    break;
                case UnitKind.BossBeetle:
                    BuildBeetle(parent, true);
                    AddAura(parent, new Color(1f, 0.12f, 0.08f, 0.25f), 1.55f, -2);
                    break;
                default:
                    BuildPortalBoss(parent);
                    break;
            }
        }

        private void BuildPeanut(Transform parent, Color shell, Color outline, bool hero)
        {
            AddPart(parent, "Outline", capsule, outline, new Vector2(0f, 0f), new Vector2(0.82f, 1.15f), 0f, 2);
            AddPart(parent, "Shell", capsule, shell, new Vector2(0f, 0f), new Vector2(0.72f, 1.05f), 0f, 3);
            AddPart(parent, "TopLobe", circle, shell * 1.06f, new Vector2(-0.12f, 0.32f), new Vector2(0.54f, 0.5f), 0f, 4);
            AddPart(parent, "BottomLobe", circle, shell * 0.94f, new Vector2(0.1f, -0.32f), new Vector2(0.58f, 0.52f), 0f, 4);
            AddPart(parent, "Seam", capsule, new Color(0.53f, 0.26f, 0.08f, 0.55f), Vector2.zero, new Vector2(0.055f, 0.78f), -8f, 5);
            AddPart(parent, "EyeL", circle, Color.black, new Vector2(-0.17f, 0.12f), new Vector2(0.08f, 0.11f), 0f, 7);
            AddPart(parent, "EyeR", circle, Color.black, new Vector2(0.17f, 0.12f), new Vector2(0.08f, 0.11f), 0f, 7);
            AddPart(parent, "EyeSparkL", circle, Color.white, new Vector2(-0.19f, 0.15f), new Vector2(0.025f, 0.035f), 0f, 8);
            AddPart(parent, "EyeSparkR", circle, Color.white, new Vector2(0.15f, 0.15f), new Vector2(0.025f, 0.035f), 0f, 8);
            AddPart(parent, "ArmL", capsule, shell * 0.9f, new Vector2(-0.43f, -0.03f), new Vector2(0.13f, 0.42f), -28f, 2);
            AddPart(parent, "ArmR", capsule, shell * 0.9f, new Vector2(0.43f, -0.03f), new Vector2(0.13f, 0.42f), 28f, 2);
            AddPart(parent, "FootL", capsule, outline, new Vector2(-0.2f, -0.58f), new Vector2(0.22f, 0.12f), -8f, 2);
            AddPart(parent, "FootR", capsule, outline, new Vector2(0.2f, -0.58f), new Vector2(0.22f, 0.12f), 8f, 2);
            if (hero)
                AddPart(parent, "CrownNut", star, new Color(1f, 0.76f, 0.16f), new Vector2(0f, 0.68f), new Vector2(0.36f, 0.26f), 0f, 8);
        }

        private void BuildMold(Transform parent, bool boss)
        {
            float s = boss ? 1.25f : 1f;
            AddPart(parent, "MoldBody", softCircle, new Color(0.26f, 0.48f, 0.16f), Vector2.zero, new Vector2(0.95f, 0.84f) * s, 0f, 3);
            for (int i = 0; i < (boss ? 8 : 5); i++)
            {
                float a = i / (float)(boss ? 8 : 5) * Mathf.PI * 2f;
                AddPart(parent, "Spore", circle, new Color(0.55f, 0.82f, 0.18f), new Vector2(Mathf.Cos(a) * 0.42f, Mathf.Sin(a) * 0.34f), Vector2.one * (boss ? 0.18f : 0.13f), 0f, 4);
            }
            AddAngryEyes(parent, boss ? 0.24f : 0.18f, 0.1f);
            if (boss) AddPart(parent, "MoldCrown", triangle, new Color(0.45f, 0.1f, 0.5f), new Vector2(0f, 0.62f), new Vector2(0.7f, 0.42f), 180f, 6);
        }

        private void BuildWeevil(Transform parent, bool boss)
        {
            Color body = boss ? new Color(0.55f, 0.16f, 0.08f) : new Color(0.42f, 0.22f, 0.08f);
            AddPart(parent, "WeevilBody", capsule, body, new Vector2(0f, -0.08f), new Vector2(0.74f, 0.88f), 0f, 3);
            AddPart(parent, "WeevilHead", circle, body * 1.15f, new Vector2(0f, 0.38f), new Vector2(0.55f, 0.5f), 0f, 4);
            AddPart(parent, "Snout", capsule, body * 0.75f, new Vector2(0f, 0.66f), new Vector2(0.15f, 0.5f), 0f, 3);
            AddLegs(parent, body * 0.75f, 3);
            AddAngryEyes(parent, 0.16f, 0.45f);
        }

        private void BuildBeetle(Transform parent, bool boss)
        {
            float s = boss ? 1.2f : 1f;
            Color shell = boss ? new Color(0.35f, 0.05f, 0.06f) : new Color(0.22f, 0.13f, 0.08f);
            AddPart(parent, "BeetleShell", capsule, shell, new Vector2(0f, -0.06f), new Vector2(0.9f, 1.05f) * s, 0f, 3);
            AddPart(parent, "WingL", capsule, new Color(0.65f, 0.1f, 0.08f), new Vector2(-0.2f, -0.02f), new Vector2(0.38f, 0.82f) * s, -8f, 4);
            AddPart(parent, "WingR", capsule, new Color(0.8f, 0.17f, 0.08f), new Vector2(0.2f, -0.02f), new Vector2(0.38f, 0.82f) * s, 8f, 4);
            AddPart(parent, "Horn", triangle, new Color(0.12f, 0.08f, 0.05f), new Vector2(0f, 0.72f), new Vector2(0.28f, 0.56f) * s, 0f, 5);
            AddLegs(parent, shell, 3);
            AddAngryEyes(parent, 0.17f, 0.3f);
        }

        private void BuildMycelium(Transform parent, bool boss)
        {
            AddPart(parent, "Stem", capsule, new Color(0.72f, 0.66f, 0.49f), new Vector2(0f, -0.18f), new Vector2(0.45f, 0.8f), 0f, 3);
            AddPart(parent, "Cap", softCircle, new Color(0.52f, 0.18f, 0.62f), new Vector2(0f, 0.32f), new Vector2(1f, 0.58f), 0f, 4);
            AddPart(parent, "SpotL", circle, new Color(0.92f, 0.74f, 0.94f), new Vector2(-0.25f, 0.38f), Vector2.one * 0.13f, 0f, 5);
            AddPart(parent, "SpotR", circle, new Color(0.92f, 0.74f, 0.94f), new Vector2(0.24f, 0.35f), Vector2.one * 0.1f, 0f, 5);
            AddAngryEyes(parent, 0.14f, -0.1f);
        }

        private void BuildInvader(Transform parent, bool boss)
        {
            AddPart(parent, "InvaderBody", diamond, new Color(0.08f, 0.54f, 0.52f), Vector2.zero, new Vector2(0.82f, 0.92f), 0f, 3);
            AddPart(parent, "Core", circle, new Color(0.75f, 1f, 0.28f), Vector2.zero, Vector2.one * 0.28f, 0f, 5);
            for (int i = 0; i < 4; i++)
                AddPart(parent, "Blade", triangle, new Color(0.05f, 0.22f, 0.25f), Vector2.zero, new Vector2(0.26f, 0.65f), i * 90f, 2);
            AddAngryEyes(parent, 0.18f, 0.26f);
        }

        private void BuildPortalBoss(Transform parent)
        {
            AddPart(parent, "PortalOuter", softCircle, new Color(0.25f, 0.04f, 0.42f), Vector2.zero, Vector2.one * 1.5f, 0f, 2);
            AddPart(parent, "PortalRing", circle, new Color(0.68f, 0.15f, 1f), Vector2.zero, Vector2.one * 1.2f, 0f, 3);
            AddPart(parent, "PortalVoid", circle, new Color(0.03f, 0.01f, 0.07f), Vector2.zero, Vector2.one * 0.82f, 0f, 4);
            AddPart(parent, "PortalCore", star, new Color(0.32f, 0.8f, 1f), Vector2.zero, Vector2.one * 0.42f, 0f, 5);
        }

        private void AddAngryEyes(Transform parent, float x, float y)
        {
            AddPart(parent, "EyeL", capsule, Color.white, new Vector2(-x, y), new Vector2(0.13f, 0.08f), -18f, 7);
            AddPart(parent, "EyeR", capsule, Color.white, new Vector2(x, y), new Vector2(0.13f, 0.08f), 18f, 7);
            AddPart(parent, "PupilL", circle, Color.black, new Vector2(-x, y - 0.01f), Vector2.one * 0.045f, 0f, 8);
            AddPart(parent, "PupilR", circle, Color.black, new Vector2(x, y - 0.01f), Vector2.one * 0.045f, 0f, 8);
        }

        private void AddLegs(Transform parent, Color color, int pairs)
        {
            for (int i = 0; i < pairs; i++)
            {
                float y = -0.35f + i * 0.25f;
                AddPart(parent, "LegL", capsule, color, new Vector2(-0.48f, y), new Vector2(0.12f, 0.45f), -58f, 2);
                AddPart(parent, "LegR", capsule, color, new Vector2(0.48f, y), new Vector2(0.12f, 0.45f), 58f, 2);
            }
        }

        private void AddSword(Transform parent, Vector2 position, float rotation, int order)
        {
            Transform swordRoot = new GameObject("Sword").transform;
            swordRoot.SetParent(parent, false);
            swordRoot.localPosition = position;
            swordRoot.localRotation = Quaternion.Euler(0f, 0f, rotation);
            AddPart(swordRoot, "Blade", diamond, new Color(0.82f, 0.91f, 1f), new Vector2(0f, 0.32f), new Vector2(0.16f, 0.72f), 0f, order);
            AddPart(swordRoot, "Edge", diamond, Color.white, new Vector2(-0.025f, 0.34f), new Vector2(0.045f, 0.58f), 0f, order + 1);
            AddPart(swordRoot, "Guard", capsule, new Color(0.95f, 0.65f, 0.1f), new Vector2(0f, -0.05f), new Vector2(0.38f, 0.1f), 90f, order + 1);
            AddPart(swordRoot, "Grip", capsule, new Color(0.22f, 0.11f, 0.05f), new Vector2(0f, -0.22f), new Vector2(0.1f, 0.32f), 0f, order);
        }

        private void AddShield(Transform parent, Vector2 position)
        {
            AddPart(parent, "Shield", diamond, new Color(0.22f, 0.42f, 0.62f), position, new Vector2(0.55f, 0.7f), 0f, 7);
            AddPart(parent, "ShieldCore", star, new Color(0.83f, 0.9f, 1f), position, Vector2.one * 0.24f, 0f, 8);
        }

        private void AddStaff(Transform parent, Vector2 position)
        {
            AddPart(parent, "Staff", capsule, new Color(0.3f, 0.12f, 0.05f), position, new Vector2(0.09f, 0.95f), -15f, 7);
            AddPart(parent, "Gem", star, new Color(0.55f, 0.22f, 1f), position + new Vector2(-0.12f, 0.48f), Vector2.one * 0.26f, 0f, 8);
        }

        private void AddCape(Transform parent, Color color)
        {
            AddPart(parent, "Cape", triangle, color, new Vector2(-0.22f, -0.12f), new Vector2(0.8f, 0.92f), 18f, 1);
        }

        private void AddHelmet(Transform parent, Color color)
        {
            AddPart(parent, "Helmet", softCircle, color, new Vector2(0f, 0.35f), new Vector2(0.78f, 0.48f), 0f, 7);
            AddPart(parent, "HelmetRidge", triangle, color * 1.2f, new Vector2(0f, 0.72f), new Vector2(0.18f, 0.42f), 0f, 8);
        }

        private void AddHeadband(Transform parent, Color color)
        {
            AddPart(parent, "Headband", capsule, color, new Vector2(0f, 0.33f), new Vector2(0.72f, 0.09f), 90f, 8);
            AddPart(parent, "HeadbandTail", triangle, color, new Vector2(-0.42f, 0.3f), new Vector2(0.25f, 0.34f), 110f, 7);
        }

        private void AddWizardHat(Transform parent, Color color)
        {
            AddPart(parent, "Hat", triangle, color, new Vector2(0f, 0.72f), new Vector2(0.72f, 0.85f), 0f, 8);
            AddPart(parent, "HatBrim", capsule, color * 0.8f, new Vector2(0f, 0.48f), new Vector2(0.82f, 0.12f), 90f, 9);
            AddPart(parent, "HatStar", star, new Color(1f, 0.82f, 0.2f), new Vector2(0.08f, 0.78f), Vector2.one * 0.15f, 0f, 10);
        }

        private void AddAura(Transform parent, Color color, float scale, int order)
        {
            AddPart(parent, "Aura", softCircle, color, Vector2.zero, Vector2.one * scale, 0f, order);
        }

        private SpriteRenderer AddPart(Transform parent, string name, Sprite sprite, Color color, Vector2 position, Vector2 scale, float rotation, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        private void EnsureCompanions()
        {
            if (heroRoot == null || companions.Count > 0) return;
            UnitKind[] kinds = { UnitKind.CompanionBlade, UnitKind.CompanionGuard, UnitKind.CompanionMage };
            for (int i = 0; i < kinds.Length; i++)
            {
                Transform root = new GameObject("Companion Peanut " + (i + 1)).transform;
                root.SetParent(worldRoot, false);
                Transform visual = new GameObject("Procedural Visual").transform;
                visual.SetParent(root, false);
                visual.localScale = Vector3.one * 0.62f;
                BuildUnitVisual(visual, kinds[i]);
                companions.Add(root);
            }
        }

        private void AnimateCompanions()
        {
            if (heroRoot == null || companions.Count == 0) return;
            Vector3 hero = heroRoot.position;
            Vector2[] offsets = { new Vector2(-1.05f, -0.72f), new Vector2(-1.22f, 0.05f), new Vector2(-0.92f, 0.78f) };
            for (int i = 0; i < companions.Count; i++)
            {
                Transform companion = companions[i];
                if (companion == null) continue;
                Vector3 target = hero + (Vector3)offsets[i] + Vector3.up * Mathf.Sin(Time.time * 3.8f + i) * 0.08f;
                companion.position = Vector3.Lerp(companion.position, target, 1f - Mathf.Exp(-8f * Time.deltaTime));
                companion.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 3f + i) * 3f);
            }
        }

        private void AnimateUnits()
        {
            foreach (DecoratedUnit unit in decorated.Values)
            {
                if (unit.Root == null || unit.VisualRoot == null) continue;
                float bob = Mathf.Sin(Time.time * 4.5f + unit.Seed) * 0.035f;
                unit.VisualRoot.localPosition = new Vector3(0f, bob, 0f);
                float tilt = Mathf.Sin(Time.time * 3.2f + unit.Seed) * 2.2f;
                unit.VisualRoot.localRotation = Quaternion.Euler(0f, 0f, tilt);
                Transform aura = unit.VisualRoot.Find("Aura");
                if (aura != null)
                {
                    float pulse = 1f + Mathf.Sin(Time.time * 5f + unit.Seed) * 0.08f;
                    aura.localScale = Vector3.one * pulse * (unit.Kind == UnitKind.Hero ? 1.4f : 1.55f);
                    aura.Rotate(0f, 0f, 18f * Time.deltaTime);
                }
                Transform core = unit.VisualRoot.Find("PortalCore");
                if (core != null) core.Rotate(0f, 0f, 80f * Time.deltaTime);
            }
        }

        private void RefreshTheme()
        {
            if (environmentRoot == null) return;
            int stage = ReadStageNumber();
            int theme = Mathf.Abs(stage / 10) % 4;
            if (theme == currentTheme) return;
            currentTheme = theme;
            for (int i = environmentRoot.childCount - 1; i >= 0; i--)
                Destroy(environmentRoot.GetChild(i).gameObject);

            Camera camera = Camera.main;
            if (theme == 0)
            {
                if (camera != null) camera.backgroundColor = new Color(0.18f, 0.32f, 0.16f);
                BuildFieldTheme();
            }
            else if (theme == 1)
            {
                if (camera != null) camera.backgroundColor = new Color(0.16f, 0.12f, 0.18f);
                BuildMoldWarehouseTheme();
            }
            else if (theme == 2)
            {
                if (camera != null) camera.backgroundColor = new Color(0.2f, 0.12f, 0.06f);
                BuildInsectCaveTheme();
            }
            else
            {
                if (camera != null) camera.backgroundColor = new Color(0.07f, 0.04f, 0.14f);
                BuildPortalTheme();
            }
        }

        private void BuildFieldTheme()
        {
            AddEnvironmentRect("FieldFloor", new Color(0.38f, 0.55f, 0.19f), Vector2.zero, new Vector2(18f, 10f), -40);
            for (int i = 0; i < 28; i++)
            {
                Vector2 p = RandomFieldPosition();
                AddPart(environmentRoot, "Grass", triangle, new Color(0.68f, 0.82f, 0.25f, 0.7f), p, new Vector2(0.15f, UnityEngine.Random.Range(0.22f, 0.45f)), UnityEngine.Random.Range(-18f, 18f), -35);
            }
            for (int i = 0; i < 8; i++)
            {
                Vector2 p = RandomFieldPosition();
                AddPart(environmentRoot, "PeanutPlant", star, new Color(0.82f, 0.67f, 0.18f, 0.75f), p, Vector2.one * UnityEngine.Random.Range(0.18f, 0.32f), 0f, -34);
            }
        }

        private void BuildMoldWarehouseTheme()
        {
            AddEnvironmentRect("WarehouseFloor", new Color(0.24f, 0.2f, 0.22f), Vector2.zero, new Vector2(18f, 10f), -40);
            for (int i = -4; i <= 4; i++)
                AddEnvironmentRect("FloorLine", new Color(0.38f, 0.32f, 0.36f, 0.6f), new Vector2(i * 2f, 0f), new Vector2(0.04f, 10f), -38);
            for (int i = 0; i < 12; i++)
            {
                Vector2 p = RandomFieldPosition();
                AddPart(environmentRoot, "MoldPatch", softCircle, new Color(0.4f, 0.68f, 0.2f, 0.35f), p, new Vector2(UnityEngine.Random.Range(0.45f, 0.9f), UnityEngine.Random.Range(0.2f, 0.45f)), 0f, -35);
            }
        }

        private void BuildInsectCaveTheme()
        {
            AddEnvironmentRect("CaveFloor", new Color(0.29f, 0.17f, 0.08f), Vector2.zero, new Vector2(18f, 10f), -40);
            for (int i = 0; i < 22; i++)
            {
                Vector2 p = RandomFieldPosition();
                AddPart(environmentRoot, "Rock", diamond, new Color(0.46f, 0.29f, 0.13f, 0.8f), p, new Vector2(UnityEngine.Random.Range(0.2f, 0.55f), UnityEngine.Random.Range(0.12f, 0.32f)), UnityEngine.Random.Range(0f, 180f), -35);
            }
            for (int i = 0; i < 7; i++)
            {
                Vector2 p = RandomFieldPosition();
                AddPart(environmentRoot, "Amber", star, new Color(1f, 0.55f, 0.08f, 0.65f), p, Vector2.one * 0.18f, 0f, -34);
            }
        }

        private void BuildPortalTheme()
        {
            AddEnvironmentRect("VoidFloor", new Color(0.1f, 0.04f, 0.18f), Vector2.zero, new Vector2(18f, 10f), -40);
            for (int i = 0; i < 18; i++)
            {
                Vector2 p = RandomFieldPosition();
                AddPart(environmentRoot, "VoidRune", diamond, new Color(0.54f, 0.18f, 1f, 0.38f), p, Vector2.one * UnityEngine.Random.Range(0.18f, 0.4f), 45f, -35);
            }
            for (int i = 0; i < 5; i++)
            {
                float x = Mathf.Lerp(-7f, 7f, i / 4f);
                AddPart(environmentRoot, "PortalPillar", triangle, new Color(0.18f, 0.62f, 0.82f, 0.5f), new Vector2(x, 3.5f), new Vector2(0.5f, 1.5f), 180f, -34);
            }
        }

        private void AddEnvironmentRect(string name, Color color, Vector2 position, Vector2 scale, int order)
        {
            AddPart(environmentRoot, name, circle, color, position, scale, 0f, order);
        }

        private void SpawnAmbientParticles()
        {
            if (environmentRoot == null || currentTheme < 0) return;
            ambientTimer -= Time.deltaTime;
            if (ambientTimer > 0f) return;
            ambientTimer = currentTheme == 3 ? 0.09f : 0.22f;

            GameObject particle = new GameObject("Ambient Particle");
            particle.transform.SetParent(environmentRoot, false);
            particle.transform.localPosition = new Vector3(UnityEngine.Random.Range(-8f, 8f), -4.6f, 0f);
            particle.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.04f, 0.11f);
            SpriteRenderer renderer = particle.AddComponent<SpriteRenderer>();
            renderer.sprite = currentTheme == 3 ? star : circle;
            renderer.color = currentTheme == 0 ? new Color(1f, 0.83f, 0.18f, 0.55f)
                : currentTheme == 1 ? new Color(0.48f, 0.86f, 0.24f, 0.45f)
                : currentTheme == 2 ? new Color(1f, 0.42f, 0.08f, 0.4f)
                : new Color(0.4f, 0.72f, 1f, 0.7f);
            renderer.sortingOrder = -20;
            particle.AddComponent<AmbientParticleMotion>().Configure(UnityEngine.Random.Range(0.35f, 0.85f), UnityEngine.Random.Range(-0.25f, 0.25f));
        }

        private int ReadStageNumber()
        {
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour.GetType().Name != "StageFlowController") continue;
                Type type = behaviour.GetType();
                string[] names = { "CurrentStage", "Stage", "currentStage", "stage" };
                for (int n = 0; n < names.Length; n++)
                {
                    PropertyInfo property = type.GetProperty(names[n], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (property != null && property.PropertyType == typeof(int)) return (int)property.GetValue(behaviour);
                    FieldInfo field = type.GetField(names[n], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field != null && field.FieldType == typeof(int)) return (int)field.GetValue(behaviour);
                }
            }
            return 0;
        }

        private static string ReadLabel(Transform root)
        {
            Transform labelTransform = root.Find("Label");
            if (labelTransform == null) return string.Empty;
            TextMesh text = labelTransform.GetComponent<TextMesh>();
            return text == null ? string.Empty : text.text;
        }

        private static UnitKind ResolveEnemyKind(string label, bool boss)
        {
            if (boss)
            {
                if (label.Contains("곰팡") || label.Contains("균")) return UnitKind.BossMold;
                if (label.Contains("벌레") || label.Contains("바구미") || label.Contains("충")) return UnitKind.BossBeetle;
                return UnitKind.BossPortal;
            }
            if (label.Contains("곰팡")) return UnitKind.Mold;
            if (label.Contains("바구미")) return UnitKind.Weevil;
            if (label.Contains("포식")) return UnitKind.Beetle;
            if (label.Contains("균사")) return UnitKind.Mycelium;
            return UnitKind.Invader;
        }

        private static Vector2 RandomFieldPosition()
        {
            return new Vector2(UnityEngine.Random.Range(-8.2f, 8.2f), UnityEngine.Random.Range(-4.2f, 4.2f));
        }

        private Sprite CreateShapeSprite(string name, int width, int height, Func<float, float, float> alpha)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = name + "Texture";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = x / (float)(width - 1) * 2f - 1f;
                    float ny = y / (float)(height - 1) * 2f - 1f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha(nx, ny))));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 64f);
        }

        private static float StarAlpha(float x, float y)
        {
            float angle = Mathf.Atan2(y, x);
            float radius = Mathf.Sqrt(x * x + y * y);
            float edge = 0.54f + Mathf.Cos(angle * 5f) * 0.34f;
            return Mathf.Clamp01((edge - radius) * 20f);
        }

        private sealed class AmbientParticleMotion : MonoBehaviour
        {
            private float speed;
            private float drift;
            private float life;
            private SpriteRenderer renderer;

            public void Configure(float moveSpeed, float horizontalDrift)
            {
                speed = moveSpeed;
                drift = horizontalDrift;
                life = 0f;
            }

            private void Awake()
            {
                renderer = GetComponent<SpriteRenderer>();
            }

            private void Update()
            {
                life += Time.deltaTime;
                transform.localPosition += new Vector3(drift, speed, 0f) * Time.deltaTime;
                transform.Rotate(0f, 0f, 45f * Time.deltaTime);
                if (renderer != null)
                {
                    Color color = renderer.color;
                    color.a *= 1f - Time.deltaTime * 0.45f;
                    renderer.color = color;
                }
                if (life > 7f || transform.localPosition.y > 5f) Destroy(gameObject);
            }
        }
    }
}
