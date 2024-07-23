using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace NewChallengeUImod
{
    public static class Tools
    {
        public static void ApplyGradientToTexture(Texture2D texture, Color startColor, Color endColor)
        {
            for (int y = 0; y < texture.height; y++)
            {
                float t = (float)y / (texture.height - 1);
                Color gradientColor = Color.Lerp(startColor, endColor, t);

                for (int x = 0; x < texture.width; x++)
                {
                    Color originalColor = texture.GetPixel(x, y);
                    // Skip fully transparent pixels
                    if (originalColor.a == 0)
                        continue;

                    Color finalColor = Color.Lerp(originalColor, gradientColor, originalColor.a); // Apply the gradient preserving original alpha
                    texture.SetPixel(x, y, finalColor);
                }
            }
            texture.Apply();
        }

        public static Color HexToRGB(string hex, float opacity)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            color.a = opacity;
            return color;
        }

        public static void UpdateLayout(Transform parent)
        {
            // Force a layout update
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent as RectTransform);
        }


        public static Texture2D LoadEmbeddedTexture(string resourceName)
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var resourceNames = assembly.GetManifestResourceNames();

                // remove " (UnityEngine.Sprite)" if it exists
                var sanitizedResourceName = resourceName.Replace(" (UnityEngine.Sprite)", "").Trim();

                // Append .png if it's not included in the sanitized resource name
                if (!sanitizedResourceName.EndsWith(".png"))
                {
                    sanitizedResourceName += ".png";
                }

                // Find the resource with the full path
                var resourcePath = resourceNames.FirstOrDefault(str => str.EndsWith(sanitizedResourceName));

                if (resourcePath != null)
                {
                    using (var stream = assembly.GetManifestResourceStream(resourcePath))
                    {
                        if (stream != null)
                        {
                            byte[] buffer = new byte[stream.Length];
                            stream.Read(buffer, 0, buffer.Length);
                            Texture2D texture = new Texture2D(2, 2);
                            texture.LoadImage(buffer);
                            return texture;
                        }
                        else
                        {
                            Debug.LogError("Failed to load embedded resource stream: " + sanitizedResourceName);
                        }
                    }
                }
                else
                {
                    Debug.LogError("Embedded resource not found: '" + sanitizedResourceName + "'");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Exception loading embedded resource: " + ex.Message);
            }

            return null;
        }
    }
}
