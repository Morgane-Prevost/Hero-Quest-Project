using System.IO;
using UnityEngine;

namespace HeroQuest.Board
{
    /// <summary>
    /// Génère les textures de sol (1 texture = 1 case, avec lignes de grille intégrées),
    /// ou charge un PNG externe (extensions). Utilitaires de shaders indépendants du pipeline.
    /// </summary>
    public static class FloorStyleFactory
    {
        public static Shader FindLitShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Sprites/Default");
            return s;
        }

        public static Shader FindUnlitShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("Unlit/Color");
            if (s == null) s = Shader.Find("Sprites/Default");
            return s;
        }

        public static Color ParseColor(string hex, Color fallback)
        {
            Color c;
            return (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out c)) ? c : fallback;
        }

        public static int StableHash(string s)
        {
            unchecked
            {
                int h = 17;
                if (s != null) foreach (char ch in s) h = h * 31 + ch;
                return h;
            }
        }

        public static Texture2D BuildTexture(FloorStyleData s, int variant)
        {
            // 1) Texture externe (extension client)
            if (!string.IsNullOrEmpty(s.texturePath))
            {
                string p = Path.Combine(Application.streamingAssetsPath, s.texturePath);
                if (File.Exists(p))
                {
                    var ext = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                    if (ext.LoadImage(File.ReadAllBytes(p))) { ext.wrapMode = TextureWrapMode.Clamp; ext.name = s.id + "_ext"; return ext; }
                    Object.Destroy(ext);
                }
                Debug.LogWarning("[Board] Texture introuvable/illisible : " + s.texturePath + " (génération procédurale)");
            }

            // 2) Génération procédurale
            int n = Mathf.Clamp(s.tileTexturePx, 32, 512);
            int lw = Mathf.Clamp(s.lineWidthPx, 0, n / 8);
            Color a = ParseColor(s.colorA, new Color(0.61f, 0.56f, 0.48f));
            Color b = ParseColor(s.colorB, new Color(0.54f, 0.49f, 0.41f));
            Color line = ParseColor(s.lineColor, new Color(0.23f, 0.2f, 0.17f));
            string pattern = (s.pattern ?? "flagstone").ToLowerInvariant();
            var rng = new System.Random(StableHash(s.id) * 7919 + variant * 104729);

            var tone = new float[16];
            for (int i = 0; i < tone.Length; i++) tone[i] = (float)rng.NextDouble();

            var px = new Color[n * n];
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    Color c;
                    if (pattern == "checker")
                    {
                        int bx = i * 2 / n, by = j * 2 / n;
                        c = (((bx + by + variant) & 1) == 0) ? a : b;
                    }
                    else if (pattern == "planks")
                    {
                        int pl = j * 4 / n;
                        c = Color.Lerp(a, b, tone[pl]);
                        if (j % (n / 4) < 1) c *= 0.65f;                         // joint entre planches
                        c *= 1f + 0.05f * Mathf.Sin(i * 0.12f + pl * 2.3f);      // veinage
                    }
                    else // flagstone / cracked
                    {
                        int bx = i * 2 / n, by = j * 2 / n, bs = n / 2;
                        c = Color.Lerp(a, b, tone[by * 2 + bx]);
                        if (i % bs < 1 || j % bs < 1) c *= 0.7f;                 // joint de mortier
                    }
                    c *= 1f + ((float)rng.NextDouble() - 0.5f) * 0.08f;          // grain
                    c.a = 1f;
                    if (lw > 0 && (i < lw || j < lw || i >= n - lw || j >= n - lw)) c = line; // grille lisible
                    px[j * n + i] = c;
                }

            if (pattern == "cracked")
            {
                for (int k = 0; k < 3; k++)
                {
                    float fx = rng.Next(n / 6, n * 5 / 6), fy = rng.Next(n / 6, n * 5 / 6);
                    float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                    for (int step = 0; step < n / 2; step++)
                    {
                        ang += ((float)rng.NextDouble() - 0.5f) * 0.9f;
                        fx += Mathf.Cos(ang); fy += Mathf.Sin(ang);
                        int ix = (int)fx, iy = (int)fy;
                        if (ix <= lw || iy <= lw || ix >= n - lw - 1 || iy >= n - lw - 1) break;
                        px[iy * n + ix] = Color.Lerp(px[iy * n + ix], Color.black, 0.65f);
                    }
                }
            }

            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true, false);
            tex.name = s.id + "_v" + variant;
            tex.SetPixels(px);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.anisoLevel = 4;
            tex.Apply(true);
            return tex;
        }
    }
}