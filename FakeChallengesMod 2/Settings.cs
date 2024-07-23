using SFS.IO;
using System;
using UITools;
using UnityEngine;
using SFS.UI.ModGUI;

namespace NewChallengeUImod
{
    public class Settings : ModSettings<SettingsData>
    {
        protected override FilePath SettingsFile => Settings.settingsPath;

        protected override void RegisterOnVariableChange(Action onChange)
        {
            Application.quitting += onChange;
        }

        public static void Init(FilePath path)
        {
            Settings.main = new Settings();
            Settings.settingsPath = path;
            Settings.main.Initialize();
            Settings.main.AddUI();
        }

        private void AddUI()
        {
            ConfigurationMenu.Add("Challenge UI Settings", new ValueTuple<string, Func<Transform, GameObject>>[]
            {
        new ValueTuple<string, Func<Transform, GameObject>>("Colors", (Transform transform) => CHallengeUI(transform, ConfigurationMenu.ContentSize))
            });
        }


        private GameObject CHallengeUI(Transform parent, Vector2Int size)
        {
            Box box = Builder.CreateBox(parent, size.x, size.y, 0, 0, 0.3f);
            box.CreateLayoutGroup(SFS.UI.ModGUI.Type.Vertical, TextAnchor.UpperLeft, 10f, null, true);

            CreateColorInput(box, size, "Not Achived Start Color", ModSettings<SettingsData>.settings.NAStartColor, (input) => ModSettings<SettingsData>.settings.NAStartColor = input);
            CreateColorInput(box, size, "Not Achived End Color", ModSettings<SettingsData>.settings.NAEndColor, (input) => ModSettings<SettingsData>.settings.NAEndColor = input);

            CreateColorInput(box, size, "Achived Start Color", ModSettings<SettingsData>.settings.AStartColor, (input) => ModSettings<SettingsData>.settings.AStartColor = input);
            CreateColorInput(box, size, "Achived End Color", ModSettings<SettingsData>.settings.AEndColor, (input) => ModSettings<SettingsData>.settings.AEndColor = input);

            CreateColorInput(box, size, "Achived Text Color", ModSettings<SettingsData>.settings.ATextColor, (input) => ModSettings<SettingsData>.settings.ATextColor = input);

            return box.gameObject;
        }

        private void CreateColorInput(Box box, Vector2Int size, string label, string color, Action<string> onInputChange)
        {
            Container container = Builder.CreateContainer(box, 0, 0);
            container.CreateLayoutGroup(SFS.UI.ModGUI.Type.Horizontal, TextAnchor.MiddleLeft, 0f, null, true);
            Builder.CreateInputWithLabel(container.rectTransform, size.x - 20, 40, 0, 0, $"{label}: ", color, (input) =>
            {
                if (IsValidHexColor(input))
                {
                    onInputChange(input);
                }
                else
                {
                    Debug.LogWarning("Invalid hex color input.");
                }
            });
        }

        private bool IsValidHexColor(string input)
        {
            if (string.IsNullOrEmpty(input) || input[0] != '#' || input.Length != 7)
            {
                return false;
            }

            for (int i = 1; i < input.Length; i++)
            {
                if (!Uri.IsHexDigit(input[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static FilePath settingsPath;
        public static Settings main;
    }
}
