using SFS.UI.ModGUI;
using UITools;
using UnityEngine;

namespace NewChallengeUImod
{
    public class DescriptionBoxUpdater : MonoBehaviour
    {
        public SFS.UI.ModGUI.Button button;
        public Box descriptionBox;

        private void Start()
        {
            InvokeRepeating(nameof(UpdateDescriptionBoxPosition), 0f, 0.05f);
        }

        private void UpdateDescriptionBoxPosition()
        {
            if (descriptionBox != null && button != null)
            {
                Transform parent = UI.Parenti;
                // Convert the button's position to the parent's coordinate system
                Vector3 buttonWorldPosition = button.gameObject.transform.position;
                Vector2 localPoint = parent.parent.parent.parent.InverseTransformPoint(buttonWorldPosition);
                // Adjust for pivot and anchor
                RectTransform parentRect = parent.parent.parent.parent.GetComponent<RectTransform>();
                localPoint.x += parentRect.pivot.x * parentRect.rect.width;
                localPoint.y += parentRect.pivot.y * parentRect.rect.height;

                // Update description box position
                descriptionBox.Position = new Vector2(localPoint.x - (255 + UI.boxWidth), localPoint.y - 342 - UI.boxHeight);
            }
            else
            {
                // Stop updating if either the button or description box is null
                CancelInvoke(nameof(UpdateDescriptionBoxPosition));
            }
        }
    }
    public static class UI
    {
        private static SettingsData SettingsData
        {
            get
            {
                return ModSettings<SettingsData>.settings;
            }
        }


        static SFS.UI.ModGUI.Button lastOpenedButton = NewChallengeUImod.Mod.ChallengeUIHelper.lastOpenedButton;
        public static Box currentDescriptionBox = null;
        public static Transform Parenti;

        public static float boxWidth = 290;
        public static float boxHeight = 170;//140 old value


        public static void ShowChallengeDescription(Transform parent, SFS.UI.ModGUI.Button button, string description, string text, bool isAchived)
        {

            NewChallengeUImod.Mod.Main.LastClickedChallenge = button;
            Parenti = parent;
            Debug.Log(NewChallengeUImod.Mod.Main.LastClickedChallenge);

            // Check if the same button is clicked again
            if (lastOpenedButton == button)
            {
                // Close the current description box if it exists
                if (currentDescriptionBox != null)
                {
                    UnityEngine.Object.Destroy(currentDescriptionBox.gameObject);
                    currentDescriptionBox = null;
                }
                // Reset last opened button
                lastOpenedButton = null;
                return;
            }

            // Close the current description box if it exists and is different from the clicked button
            if (currentDescriptionBox != null)
            {
                UnityEngine.Object.Destroy(currentDescriptionBox.gameObject);
                currentDescriptionBox = null;
            }



            // Create a box for challenge description
            currentDescriptionBox = Builder.CreateBox(
                parent: parent.parent.parent.parent,
                width: (int)boxWidth,
                height: (int)boxHeight,
                posX: 0,
                posY: 0,
                opacity: 1f
            );

            currentDescriptionBox.Color = Tools.HexToRGB("#6d8bb9", 1f);
            SFS.UI.ModGUI.Container descriptionContainer = Builder.CreateContainer(
                parent: currentDescriptionBox,
                posX: 0,
                posY: 0
            );

            // Create box for holding challenge description
            SFS.UI.ModGUI.Box descriptionBox = Builder.CreateBox(
                parent: descriptionContainer,
                width: (int)boxWidth - 20,
                height: (int)boxHeight - 60,
                posX: 0,
                posY: -20,
                opacity: 1f
            );
            descriptionBox.Color = Tools.HexToRGB("#465c7f", 1f);

            // Create label for description text inside the box 
            Label label = Builder.CreateLabel(
                parent: descriptionBox,
                width: (int)(boxWidth - 35),
                height: (int)(boxHeight - 35),
                posY: -26,
                text: description
            );
            // Configure label properties
            label.AutoFontResize = false;
            label.FontSize = 19;
            label.TextAlignment = TMPro.TextAlignmentOptions.TopLeft;

            // Create box for holding challenge name
            SFS.UI.ModGUI.Box NameBox = Builder.CreateBox(
                parent: descriptionContainer,
                width: (int)boxWidth - 20,
                height: (int)40,
                posX: 0,
                posY: (int)(boxHeight - 110),
                opacity: 1f
            );
            NameBox.Color = Tools.HexToRGB("#202e45", 1f);

            // Create label for challenge name inside the box
            Label Name = Builder.CreateLabel(
                parent: NameBox,
                width: (int)NameBox.Size.x - 10,
                height: (int)NameBox.Size.y,
                posY: 0,
                text: text
            );
            if (isAchived)
            {

                Name.Color = Tools.HexToRGB(SettingsData.ATextColor, 1f);
                //Name.FontStyle = TMPro.FontStyles.Bold;
            }
            lastOpenedButton = button;

            // Attach the DescriptionBoxUpdater component to the parent
            DescriptionBoxUpdater updater = parent.gameObject.AddComponent<DescriptionBoxUpdater>();
            updater.button = button;
            updater.descriptionBox = currentDescriptionBox;
        }
    }
}
