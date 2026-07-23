using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(25000)]
    public sealed class ProceduralBattleArtPrototype : MonoBehaviour
    {
        const BindingFlags Priv = BindingFlags.Instance | BindingFlags.NonPublic;
        const BindingFlags Pub = BindingFlags.Instance | BindingFlags.Public;
        readonly Sprite[] sprites = new Sprite[24];
        readonly List<Transform> companions = new List<Transform>();
        readonly HashSet<int> cleaned = new HashSet<int>();
        RuntimeWorldViewPrototype view;
        StageFlowController flow;
        FieldInfo playerField, enemiesField, rootField;
        GameObject root;
        Transform player, environment;
        Camera cam;
        int theme = -1;
        bool reflected;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (FindFirstObjectByType<ProceduralBattleArtPrototype>() != null) return;
            var go = new GameObject("Peanut Runtime Illustrated Battle Art");
            DontDestroyOnLoad(go);
            go.AddComponent<ProceduralBattleArtPrototype>();
        }

        void Awake()
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                try { sprites[i] = Art.Make(i); }
                catch (Exception e)
                {
                    Debug.LogWarning($"[PeanutArt] Sprite {i:00} fallback: {e.Message}");
                    sprites[i] = Art.Fallback(i);
                }
            }
            Debug.Log("[PeanutArt] Runtime illustrated sprites generated: 24 ready. External atlas disabled.");
        }

        void LateUpdate()
        {
            if (!Resolve()) return;
            ApplyPlayer(); ApplyEnemies(); EnsureCompanions(); MoveCompanions(); ApplyTheme();
        }

        bool Resolve()
        {
            if (view == null)
            {
                view = FindFirstObjectByType<RuntimeWorldViewPrototype>();
                flow = FindFirstObjectByType<StageFlowController>();
                reflected = false;
            }
            if (view == null) return false;
            if (!reflected)
            {
                var t = typeof(RuntimeWorldViewPrototype);
                playerField = t.GetField("playerView", Priv);
                enemiesField = t.GetField("enemyViews", Priv);
                rootField = t.GetField("worldRoot", Priv);
                if (playerField == null || enemiesField == null || rootField == null)
                {
                    Debug.LogError("[PeanutArt] RuntimeWorldViewPrototype fields were not found.");
                    return false;
                }
                reflected = true;
            }
            root = rootField.GetValue(view) as GameObject;
            if (root == null) return false;
            if (environment == null)
            {
                string[] old = { "Illustrated Stage Environment", "Direct Illustrated Stage Environment", "Generated Illustrated Stage Environment" };
                foreach (string n in old) { var x = root.transform.Find(n); if (x != null) Destroy(x.gameObject); }
                environment = new GameObject("Runtime Illustrated Stage Environment").transform;
                environment.SetParent(root.transform, false);
                cam = Camera.main;
                theme = -1;
            }
            return true;
        }

        void ApplyPlayer()
        {
            var body = Body(playerField.GetValue(view));
            if (body == null) return;
            player = body.transform.parent;
            Set(body, sprites[0], 1.28f, false);
            Hud(player, 1.12f, .83f);
        }

        void ApplyEnemies()
        {
            var list = enemiesField.GetValue(view) as IDictionary;
            if (list == null) return;
            int fallback = 0;
            foreach (DictionaryEntry pair in list)
            {
                var body = Body(pair.Value);
                if (body == null) { fallback++; continue; }
                bool boss = Bool(pair.Value, "IsBoss");
                var label = Label(pair.Value);
                int index = boss ? 9 + Mathf.Abs((flow == null ? 1 : flow.World) - 1) % 3 : Monster(label == null ? "" : label.text, fallback);
                Set(body, sprites[index], boss ? 1.45f : 1.08f, boss);
                Hud(body.transform.parent, boss ? 1.48f : 1f, boss ? 1.16f : .75f);
                fallback++;
            }
        }

        void Set(SpriteRenderer body, Sprite sprite, float scale, bool boss)
        {
            if (body == null || sprite == null) return;
            var r = body.transform.parent;
            if (r != null && cleaned.Add(r.GetInstanceID()))
            {
                string[] names = { "Procedural Visual", "Illustrated Visual", "Illustrated Aura" };
                foreach (string n in names) { var x = r.Find(n); if (x != null) Destroy(x.gameObject); }
            }
            body.gameObject.SetActive(true);
            body.sprite = sprite;
            body.color = Color.white;
            body.sortingOrder = boss ? 7 : 5;
            body.transform.localPosition = new Vector3(0, .11f, 0);
            body.transform.localScale = Vector3.one * scale;
            var h = body.transform.Find("Highlight"); if (h != null) h.gameObject.SetActive(false);
        }

        static void Hud(Transform r, float labelY, float hpY)
        {
            if (r == null) return;
            var l = r.Find("Label"); if (l != null) l.localPosition = new Vector3(0, labelY, 0);
            var h = r.Find("Health Back"); if (h != null) h.localPosition = new Vector3(0, hpY, 0);
            var s = r.Find("Shadow"); if (s != null) s.localPosition = new Vector3(0, -.58f, 0);
        }

        void EnsureCompanions()
        {
            if (root == null || player == null || companions.Count == 3) return;
            for (int i = companions.Count; i < 3; i++)
            {
                var go = new GameObject($"Illustrated Support Peanut {i + 1}");
                go.transform.SetParent(root.transform, false);
                var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprites[i + 1]; sr.sortingOrder = 5;
                go.transform.localScale = Vector3.one * .78f;
                companions.Add(go.transform);
            }
        }

        void MoveCompanions()
        {
            if (player == null || companions.Count != 3) return;
            Vector3[] offset = { new Vector3(-1.28f, -.66f, 0), new Vector3(0, -1.12f, 0), new Vector3(1.28f, -.66f, 0) };
            for (int i = 0; i < 3; i++)
            {
                var t = companions[i]; if (t == null) continue;
                float p = Time.time * .48f + i * 2.094f;
                var target = player.position + offset[i] + new Vector3(Mathf.Cos(p) * .09f, Mathf.Sin(p * 1.6f) * .08f, 0);
                t.position = Vector3.Lerp(t.position, target, 1 - Mathf.Exp(-8 * Time.deltaTime));
                t.localScale = Vector3.one * .78f * (1 + Mathf.Sin(Time.time * 4.2f + i) * .028f);
                t.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(p) * 2.5f);
            }
        }

        void ApplyTheme()
        {
            if (environment == null) return;
            int now = flow == null ? 0 : Mathf.Abs(flow.World - 1) % 4;
            if (now == theme) return;
            theme = now;
            for (int i = environment.childCount - 1; i >= 0; i--) Destroy(environment.GetChild(i).gameObject);
            Color[] colors = { new Color(.10f, .22f, .09f), new Color(.15f, .09f, .12f), new Color(.07f, .12f, .13f), new Color(.06f, .035f, .15f) };
            if (cam == null) cam = Camera.main; if (cam != null) cam.backgroundColor = colors[now];
            Vector3[] pos = { new Vector3(-7.1f, -3.15f, 0), new Vector3(-5.2f, 2.9f, 0), new Vector3(-2.4f, -3.25f, 0), new Vector3(2.2f, 3f, 0), new Vector3(5f, -3.15f, 0), new Vector3(7f, 2.75f, 0) };
            for (int i = 0; i < pos.Length; i++)
            {
                var go = new GameObject($"Theme Decor {i + 1}"); go.transform.SetParent(environment, false); go.transform.localPosition = pos[i]; go.transform.localScale = Vector3.one * (i % 3 == 0 ? 1.22f : .92f);
                var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprites[20 + now]; sr.color = new Color(1, 1, 1, i < 2 ? .7f : .92f); sr.sortingOrder = -16;
            }
        }

        static int Monster(string text, int fallback)
        {
            if (text.Contains("곰팡이")) return 4; if (text.Contains("바구미")) return 5; if (text.Contains("포식")) return 6; if (text.Contains("균사")) return 7; if (text.Contains("침공")) return 8;
            return 4 + Mathf.Abs(fallback) % 5;
        }
        static SpriteRenderer Body(object o) { if (o == null) return null; var f = o.GetType().GetField("Body", Pub); return f == null ? null : f.GetValue(o) as SpriteRenderer; }
        static TextMesh Label(object o) { if (o == null) return null; var f = o.GetType().GetField("Label", Pub); return f == null ? null : f.GetValue(o) as TextMesh; }
        static bool Bool(object o, string n) { if (o == null) return false; var f = o.GetType().GetField(n, Pub); return f != null && Convert.ToBoolean(f.GetValue(o)); }

        static class Art
        {
            const int S = 96;
            static readonly Color32 T = new Color32(0, 0, 0, 0), O = new Color32(35, 23, 23, 255), Shell = new Color32(177, 105, 40, 255), Light = new Color32(235, 164, 66, 255), W = new Color32(250, 247, 229, 255);
            static readonly Color32[] P = { new Color32(42, 112, 218, 255), new Color32(238, 137, 35, 255), new Color32(42, 160, 104, 255), new Color32(126, 62, 194, 255), new Color32(88, 205, 102, 255), new Color32(145, 86, 45, 255), new Color32(222, 66, 70, 255), new Color32(104, 66, 166, 255), new Color32(50, 186, 205, 255), new Color32(225, 67, 165, 255), new Color32(72, 146, 232, 255), new Color32(163, 67, 240, 255) };

            public static Sprite Make(int i)
            {
                var c = new C(S, S);
                if (i < 4) Peanut(c, i); else if (i < 9) Enemy(c, i - 4); else if (i < 12) Boss(c, i - 9); else if (i < 20) Effect(c, i - 12); else Decor(c, i - 20);
                return c.Sprite($"PeanutRuntimeArt_{i:00}");
            }
            public static Sprite Fallback(int i) { var c = new C(S, S); c.E(48, 48, 34, 34, O); c.E(48, 48, 29, 29, P[Mathf.Abs(i) % P.Length]); return c.Sprite($"PeanutFallback_{i:00}"); }

            static void Peanut(C c, int k)
            {
                Color32 a = P[k]; c.E(48, 49, 29, 38, O); c.E(48, 49, 24, 33, Shell); c.L(35, 23, 38, 75, Light, 3); c.L(61, 23, 58, 75, Light, 3); c.R(27, 24, 69, 42, O); c.R(30, 27, 66, 39, a); c.Tri(48, 9, 41, 25, 55, 25, a); Face(c);
                if (k < 2) { c.L(66, 64, 82, 22, W, 5); c.L(66, 64, 82, 22, new Color32(91, 151, 220, 255), 2); c.L(60, 61, 73, 67, O, 5); }
                else if (k == 2) { c.E(27, 58, 16, 20, O); c.E(27, 58, 12, 16, a); c.L(27, 43, 27, 73, W, 2); }
                else { c.L(71, 67, 78, 24, O, 5); c.E(80, 20, 8, 8, new Color32(219, 102, 255, 255)); c.Tri(48, 7, 25, 29, 71, 29, a); }
            }
            static void Face(C c) { c.E(40, 49, 3, 4, W); c.E(56, 49, 3, 4, W); c.L(41, 62, 48, 66, O, 2); c.L(48, 66, 55, 62, O, 2); }
            static void Enemy(C c, int k)
            {
                Color32 a = P[4 + k];
                if (k == 0) { c.E(48, 59, 29, 25, O); c.E(48, 59, 24, 20, a); c.E(34, 30, 13, 13, O); c.E(34, 30, 9, 9, a); c.E(50, 24, 15, 15, O); c.E(50, 24, 11, 11, a); c.E(66, 33, 12, 12, O); c.E(66, 33, 8, 8, a); }
                else if (k == 1) { c.E(48, 50, 27, 35, O); c.E(48, 50, 21, 29, a); c.L(48, 20, 48, 78, Light, 3); c.L(25, 48, 71, 48, O, 3); c.L(33, 22, 17, 7, O, 3); c.L(63, 22, 79, 7, O, 3); }
                else if (k == 2) { c.Tri(48, 12, 15, 74, 81, 74, O); c.Tri(48, 20, 22, 69, 74, 69, a); c.E(38, 48, 4, 5, W); c.E(58, 48, 4, 5, W); c.Tri(48, 58, 38, 72, 58, 72, O); }
                else if (k == 3) { c.E(48, 52, 27, 30, O); c.E(48, 52, 21, 24, a); c.E(48, 25, 31, 14, O); c.E(48, 23, 26, 10, new Color32(167, 101, 225, 255)); c.E(32, 20, 5, 5, W); c.E(48, 14, 5, 5, W); c.E(64, 20, 5, 5, W); }
                else { c.Tri(48, 12, 17, 75, 79, 75, O); c.Tri(48, 21, 24, 68, 72, 68, a); c.E(38, 47, 5, 6, new Color32(255, 242, 99, 255)); c.E(58, 47, 5, 6, new Color32(255, 242, 99, 255)); }
            }
            static void Boss(C c, int k)
            {
                Color32 a = P[9 + k]; c.E(48, 49, 39, 40, a); c.E(48, 49, 27, 34, O); c.E(48, 49, 22, 29, Shell); c.R(29, 24, 67, 41, a); Face(c); c.Tri(48, 7, 37, 27, 43, 25, Light); c.Tri(48, 7, 53, 25, 59, 27, Light);
                if (k == 1) { c.Tri(15, 18, 8, 63, 34, 43, a); c.Tri(81, 18, 62, 43, 88, 63, a); c.L(48, 20, 48, 78, W, 3); }
                if (k == 2) { c.Ring(48, 49, 44, 36, new Color32(91, 223, 255, 255), 5); c.Ring(48, 49, 38, 31, new Color32(212, 91, 255, 255), 4); }
            }
            static void Effect(C c, int k)
            {
                Color32[] a = { new Color32(255,190,35,255), new Color32(255,76,48,255), new Color32(65,207,91,255), new Color32(63,151,240,255), new Color32(190,68,241,255), new Color32(255,235,92,255), new Color32(41,210,218,255), new Color32(255,143,31,255) }; Color32 x = a[k];
                if (k == 0) c.Arc(48,48,36,195,345,x,7); else if (k == 1) { c.E(48,48,33,33,x); c.E(48,48,13,13,W); }
                else if (k == 2) for (int i=0;i<8;i++){float q=i*Mathf.PI/4;c.Tri(48,48,48+Mathf.RoundToInt(Mathf.Cos(q-.18f)*36),48+Mathf.RoundToInt(Mathf.Sin(q-.18f)*36),48+Mathf.RoundToInt(Mathf.Cos(q+.18f)*36),48+Mathf.RoundToInt(Mathf.Sin(q+.18f)*36),x);}
                else if (k == 3) for(int z=23;z<=73;z+=10)c.L(z,18,z-9,78,x,4); else if(k==4){c.Ring(48,48,34,25,x,6);c.L(48,16,48,80,W,3);c.L(16,48,80,48,W,3);} else if(k==5){c.E(48,48,34,34,x);c.E(48,48,18,18,W);c.Ring(48,48,39,35,x,4);} else if(k==6){c.L(35,13,56,42,x,8);c.L(56,42,39,54,x,8);c.L(39,54,61,83,x,8);} else {c.Arc(48,48,36,280,80,x,8);c.Tri(76,27,84,48,63,42,W);}
            }
            static void Decor(C c, int k)
            {
                if(k==0){c.E(48,76,15,8,O);c.E(48,74,11,6,Light);c.L(48,68,48,28,new Color32(75,145,52,255),5);c.E(32,42,15,8,new Color32(80,175,65,255));c.E(64,35,15,8,new Color32(80,175,65,255));c.E(34,57,15,8,new Color32(80,175,65,255));}
                else if(k==1){c.R(19,43,77,83,O);c.R(24,48,72,78,new Color32(101,61,39,255));c.R(31,30,65,51,O);c.E(48,26,23,12,new Color32(127,70,162,255));c.R(31,56,42,77,W);c.R(54,56,65,77,W);}
                else if(k==2){c.Tri(22,81,38,27,52,81,new Color32(45,198,217,255));c.Tri(44,81,61,20,75,81,new Color32(104,76,230,255));}
                else {c.Ring(48,48,40,30,new Color32(118,35,218,255),7);c.Ring(48,48,31,22,new Color32(40,216,237,255),5);c.E(48,48,18,25,new Color32(39,16,68,255));}
            }

            sealed class C
            {
                readonly int w,h; readonly Color32[] px;
                public C(int w,int h){this.w=w;this.h=h;px=new Color32[w*h];for(int i=0;i<px.Length;i++)px[i]=T;}
                public Sprite Sprite(string n){var t=new Texture2D(w,h,TextureFormat.RGBA32,false){name=n+"_Texture",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};t.SetPixels32(px);t.Apply(false,false);var s=UnityEngine.Sprite.Create(t,new Rect(0,0,w,h),new Vector2(.5f,.5f),66,0,SpriteMeshType.FullRect);s.name=n;return s;}
                public void R(int x0,int y0,int x1,int y1,Color32 c){for(int y=Mathf.Max(0,y0);y<=Mathf.Min(h-1,y1);y++)for(int x=Mathf.Max(0,x0);x<=Mathf.Min(w-1,x1);x++)B(x,y,c);}
                public void E(int cx,int cy,int rx,int ry,Color32 c){int a=rx*rx,b=ry*ry,l=a*b;for(int y=-ry;y<=ry;y++)for(int x=-rx;x<=rx;x++)if(x*x*b+y*y*a<=l)B(cx+x,cy+y,c);}
                public void Ring(int cx,int cy,int outer,int inner,Color32 c,int thick){int o=outer*outer,r=Mathf.Max(1,inner-thick),n=r*r;for(int y=-outer;y<=outer;y++)for(int x=-outer;x<=outer;x++){int d=x*x+y*y;if(d<=o&&d>=n)B(cx+x,cy+y,c);}}
                public void L(int x0,int y0,int x1,int y1,Color32 c,int z){int s=Mathf.Max(Mathf.Abs(x1-x0),Mathf.Abs(y1-y0));for(int i=0;i<=s;i++){float q=s==0?0:(float)i/s;E(Mathf.RoundToInt(Mathf.Lerp(x0,x1,q)),Mathf.RoundToInt(Mathf.Lerp(y0,y1,q)),z,z,c);}}
                public void Tri(int ax,int ay,int bx,int by,int cx,int cy,Color32 c){int x0=Mathf.Min(ax,Mathf.Min(bx,cx)),x1=Mathf.Max(ax,Mathf.Max(bx,cx)),y0=Mathf.Min(ay,Mathf.Min(by,cy)),y1=Mathf.Max(ay,Mathf.Max(by,cy));for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++){float a=D(bx,by,cx,cy,x,y),b=D(cx,cy,ax,ay,x,y),d=D(ax,ay,bx,by,x,y);if((a>=0&&b>=0&&d>=0)||(a<=0&&b<=0&&d<=0))B(x,y,c);}}
                public void Arc(int cx,int cy,int r,float start,float end,Color32 c,int z){float span=end-start;if(span<=0)span+=360;int s=Mathf.CeilToInt(span*r/20);for(int i=0;i<=s;i++){float a=(start+span*i/Mathf.Max(1,s))*Mathf.Deg2Rad;E(cx+Mathf.RoundToInt(Mathf.Cos(a)*r),cy+Mathf.RoundToInt(Mathf.Sin(a)*r),z,z,c);}}
                static float D(float ax,float ay,float bx,float by,float px,float py){return(px-ax)*(by-ay)-(py-ay)*(bx-ax);}
                void B(int x,int y,Color32 s){if(x<0||x>=w||y<0||y>=h||s.a==0)return;int i=y*w+x;Color32 d=px[i];float sa=s.a/255f,da=d.a/255f,oa=sa+da*(1-sa);if(oa<=.0001f){px[i]=T;return;}px[i]=new Color32((byte)Mathf.Clamp(Mathf.RoundToInt((s.r*sa+d.r*da*(1-sa))/oa),0,255),(byte)Mathf.Clamp(Mathf.RoundToInt((s.g*sa+d.g*da*(1-sa))/oa),0,255),(byte)Mathf.Clamp(Mathf.RoundToInt((s.b*sa+d.b*da*(1-sa))/oa),0,255),(byte)Mathf.Clamp(Mathf.RoundToInt(oa*255),0,255));}
            }
        }
    }
}
