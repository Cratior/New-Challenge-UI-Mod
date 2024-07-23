using HarmonyLib;
using ModLoader.Helpers;
using Newtonsoft.Json;
using SFS;
using SFS.Career;
using SFS.IO;
using SFS.UI.ModGUI;
using SFS.World;
using SFS.WorldBase;
using System.Collections.Generic;
using System.Linq;
using UITools;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine;

namespace NewChallengeUImod
{
    public class Mod : ModLoader.Mod
    {
        public List<ChallengeData> Challenges { get; private set; } = new List<ChallengeData>();

        public override string ModNameID => "NewChallengeUImod";
        public override string DisplayName => "New Challenge Ui";
        public override string Author => "Cratior";
        public override string MinimumGameVersionNecessary => "1.5.2";
        public override string ModVersion => "8.5.0";
        public override string Description => "Replaces the normal SFS challenge menu with a custom menu.";
        public static Mod Main { get; private set; }

        private static Mod main;
        public SFS.UI.ModGUI.Button LastClickedChallenge;
        public HubManager HubManagerOb;

        public List<GameObject> createdElements = new List<GameObject>();


        private static SettingsData SettingsData
        {
            get
            {
                return ModSettings<SettingsData>.settings;
            }
        }
        public Dictionary<string, FilePath> UpdatableFiles
        {
            get
            {
                return new Dictionary<string, FilePath>
                {
                    {
                        "https://github.com/Cratior/New-Challenge-UI-Mod/releases/latest/download/NewChallengeUImod.dll",
                        new FolderPath(base.ModFolder).ExtendToFile("NewChallengeUImod.dll")
                    }
                };
            }
        }

        public Mod()
        {
            Main = this;
        }

        public override void Early_Load()
        {
            base.Early_Load();
            Mod.main = this;
            Patches.Patch();
        }

        public override void Load()
        {
            base.Load();
            Settings.Init(new FolderPath(Mod.main.ModFolder).ExtendToFile("settings.txt"));
            SceneHelper.OnHubSceneLoaded += OnSceneLoaded;
            SceneHelper.OnHubSceneUnloaded += OnSceneUnLoad;

        }

        private void OnSceneLoaded()
        {
            DestroyCreatedUI();
            LoadChallenges();
        }
        private void OnSceneUnLoad()
        {
            DestroyCreatedUI();
        }

        private void DestroyCreatedUI()
        {
            Debug.Log("Destroying " + createdElements.Count + " elements");
            foreach (var separator in createdElements)
            {
                UnityEngine.Object.Destroy(separator);
            }
            createdElements.Clear();
            ChallengeUIHelper.lastOpenedButton = null;
            ChallengeUIHelper.FirstGroupWidth = 0;
            ChallengeUIHelper.LastGroupWidth = 0;
        }

        private void LoadChallenges()
        {

            MsgCollector logger = new MsgCollector();
            WorldSave state = SavingCache.main.LoadWorldPersistent(logger, false, false);

            if (state == null)
            {
                Debug.LogError("Failed to load world save state.");
                return;
            }

            var challengesArray = Base.worldBase?.challengesArray;

            if (challengesArray == null)
            {
                Debug.LogError("No challenges array found in world base.");
                return;
            }

            if (state.completeChallenges == null)
            {
                Debug.LogWarning("CompleteChallenges list is null.");
                state.completeChallenges = new HashSet<string>();
            }

            Challenges.Clear();

            var challengeList = new List<ChallengeData>();
            var achievedStatus = new List<bool>();

            foreach (var challenge in challengesArray)
            {
                if (challenge == null)
                {
                    Debug.LogWarning("Found a null challenge in challenges array.");
                    continue;
                }

                bool isAchieved = state.completeChallenges.Contains(challenge.id.ToString());

                var challengeData = new ChallengeData
                {
                    Name = challenge.title(),
                    Icon = challenge.icon.ToString(),
                    Group = challenge.difficulty.ToString(),
                    Description = challenge.description(),
                    IsAchieved = isAchieved,//(UnityEngine.Random.value > 0.3f), //for testing 😉
                };

                Challenges.Add(challengeData);
                challengeList.Add(challengeData);
                achievedStatus.Add(isAchieved);
            }


            string json = JsonConvert.SerializeObject(challengeList, Formatting.Indented);
            Challenges = JsonConvert.DeserializeObject<List<ChallengeData>>(json);


            // Sort challenges by group order
            var groupedChallenges = Challenges.GroupBy(c => c.Group)
                                              .OrderBy(g => GroupOrder.ContainsKey(g.Key) ? GroupOrder[g.Key] : int.MaxValue);

            // add the challenges to the UI
            if (HubManagerOb != null)
            {
                Transform parent = HubManagerOb.challengesScroller.transform;
                int width = (int)(parent as RectTransform).rect.width;

                Builder.CreateBox(parent, 0, 100, opacity: 0);

                foreach (var group in groupedChallenges.Select((value, index) => new { Index = index, Value = value }))
                {
                    List<string> icons = group.Value.Select(c => c.Icon).ToList();
                    List<string> texts = group.Value.Select(c => c.Name).ToList();
                    List<string> descriptions = group.Value.Select(c => c.Description).ToList();
                    List<bool> isAchieved = group.Value.Select(c => c.IsAchieved).ToList();

                    ChallengeUIHelper.AddSeparator(parent, width, group.Index, groupedChallenges.Count(), group.Value.Count(), group.Value.Key);
                    ChallengeUIHelper.AddElements(parent, width, group.Value.Count(), icons.ToArray(), texts.ToArray(), descriptions.ToArray(), isAchieved.ToArray());

                }
            }
            else
            {
                Debug.LogError("HubManager not found, cannot modify challenges UI.");
            }
        }

        public class ChallengeData
        {
            public string Name { get; set; }
            public string Icon { get; set; }
            public string Group { get; set; }
            public string Description { get; set; }
            public bool IsAchieved { get; set; }
        }

        private static readonly Dictionary<string, int> GroupOrder = new Dictionary<string, int>
        {
            { "Easy", 1 },
            { "Medium", 2 },
            { "Hard", 3 },
            { "Extreme", 4 }
        };

        public static class ChallengeUIHelper
        {
            const int WidthOffset = 470;
            static int firstGroupWidth;
            static int lastGroupWidth;

            public static RectOffset padding = new RectOffset(left: 0, right: 0, top: 3, bottom: 100);
            public static RectOffset padding2 = new RectOffset(left: 0, right: 0, top: -100, bottom: 100);
            public static void AddSeparator(Transform parent, int width, int groupIndex, int totalGroups, int buttonCount, string name)
            {
                int totalButtonWidth = buttonCount * 80;
                int separatorWidth;

                if (groupIndex == 0)
                {
                    separatorWidth = totalButtonWidth + WidthOffset;
                    firstGroupWidth = separatorWidth;
                }
                else if (groupIndex == totalGroups - 1)
                {
                    separatorWidth = totalButtonWidth + WidthOffset;
                    lastGroupWidth = separatorWidth;
                }
                else
                {
                    float t = (float)(groupIndex) / (totalGroups);
                    t = 1 - t;
                    separatorWidth = Mathf.RoundToInt(Mathf.Lerp(firstGroupWidth, lastGroupWidth, t)) + (int)(WidthOffset * 1.13);
                }

                SFS.UI.ModGUI.Container container = Builder.CreateContainer(parent);


                int labelPositionX = -separatorWidth / 2 + width / 2;
                var label = Builder.CreateLabel(container, width, 40, posX: labelPositionX, posY: 20 + padding.bottom, text: name);
                label.TextAlignment = TMPro.TextAlignmentOptions.BaselineLeft;
                NewChallengeUImod.Mod.Main.createdElements.Add(label.gameObject);
                NewChallengeUImod.Mod.Main.createdElements.Add(Builder.CreateSeparator(container, separatorWidth, 0, 0 + padding.bottom).gameObject);
            }

            public static void AddElements(Transform parent, int width, int count, string[] icons, string[] texts, string[] descriptions, bool[] isAchieved)
            {
                // Create a new container for the group
                SFS.UI.ModGUI.Container groupContainer = Builder.CreateContainer(parent);
                groupContainer.CreateLayoutGroup(SFS.UI.ModGUI.Type.Vertical, spacing: 10, padding: padding);

                const int elementsPerRow = 7;
                int rowCount = Mathf.CeilToInt((float)count / elementsPerRow);

                // Create a horizontal layout group for each row
                for (int row = 0; row < rowCount; row++)
                {
                    SFS.UI.ModGUI.Container rowContainer = Builder.CreateContainer(groupContainer.gameObject.transform);
                    rowContainer.CreateLayoutGroup(SFS.UI.ModGUI.Type.Horizontal, spacing: 10, padding: padding2);

                    // Add elements to the current row
                    for (int i = 0; i < elementsPerRow; i++)
                    {
                        int index = row * elementsPerRow + i;
                        if (index >= count) break;

                        SFS.UI.ModGUI.Button button = Builder.CreateButton(rowContainer.gameObject.transform, 80, 80, 0, 0);
                        button.OnClick = () => UI.ShowChallengeDescription(rowContainer.gameObject.transform, button, descriptions[index], texts[index], isAchieved[index]);
                        NewChallengeUImod.Mod.Main.createdElements.Add(button.gameObject);
                        GameObject imageGO = new GameObject("Image");
                        imageGO.transform.SetParent(button.gameObject.transform);
                        Image imageComponent = imageGO.AddComponent<Image>();

                        Texture2D texture = Tools.LoadEmbeddedTexture(icons[index] + ".png");
                        if (texture != null)
                        {
                            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                            imageComponent.sprite = sprite;
                            Color startColor;
                            Color endColor;

                            // Apply color gradient
                            if (!isAchieved[index])
                            {
                                ColorUtility.TryParseHtmlString(Mod.SettingsData.NAStartColor, out startColor);
                                ColorUtility.TryParseHtmlString(Mod.SettingsData.NAEndColor, out endColor);

                                Tools.ApplyGradientToTexture(texture, startColor, endColor);
                            }
                            else
                            {
                                ColorUtility.TryParseHtmlString(Mod.SettingsData.AStartColor, out startColor);
                                ColorUtility.TryParseHtmlString(Mod.SettingsData.AEndColor, out endColor);

                                Tools.ApplyGradientToTexture(texture, startColor, endColor);
                            }
                        }

                        RectTransform imageRect = imageGO.GetComponent<RectTransform>();
                        imageRect.localScale = Vector3.one;
                        imageRect.anchoredPosition = Vector2.zero;
                        imageRect.sizeDelta = new Vector2(67, 67);
                    }
                }
            }

            public static SFS.UI.ModGUI.Button lastOpenedButton = null;

            public static int FirstGroupWidth { get => firstGroupWidth; set => firstGroupWidth = value; }
            public static int LastGroupWidth { get => lastGroupWidth; set => lastGroupWidth = value; }
            public static SFS.UI.ModGUI.Button LastOpenedButton { get => lastOpenedButton; set => lastOpenedButton = value; }
        }

        static class Patches
        {
            public static void Patch() => new Harmony(Mod.Main.ModNameID).PatchAll();

            [HarmonyPatch(typeof(HubManager), "DrawChallenges")]
            class HubManager_DrawChallenges
            {
                static bool Prefix(HubManager __instance)
                {

                    NewChallengeUImod.Mod.Main.HubManagerOb = __instance;
                    Transform parent = __instance.challengesScroller.transform;
                    int width = (int)(parent as RectTransform).rect.width;
                    __instance.challengesScroller.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
                    __instance.challengesScroller.GetComponent<VerticalLayoutGroup>().childControlHeight = false;

                    var groupedChallenges = Mod.Main.Challenges.GroupBy(c => c.Group);

                    Builder.CreateBox(parent, 0, 100, opacity: 0);

                    foreach (var group in groupedChallenges.Select((value, index) => new { Index = index, Value = value }))
                    {
                        List<string> icons = group.Value.Select(c => c.Icon).ToList();
                        List<string> texts = group.Value.Select(c => c.Name).ToList();
                        List<string> descriptions = group.Value.Select(c => c.Description).ToList();

                        ChallengeUIHelper.AddSeparator(parent, width, group.Index, groupedChallenges.Count(), group.Value.Count(), group.Value.Key);
                    }

                    return false;
                }
            }
        }
    }
}
