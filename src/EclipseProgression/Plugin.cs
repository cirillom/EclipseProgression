using BepInEx;
using RoR2;
using RoR2.UI;
using System.Collections;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace EclipseProgression;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.cirillom.eclipseprogression";
    public const string PluginName = "Eclipse Progression";
    public const string PluginVersion = "1.0.2";

    private void Awake()
    {
        On.RoR2.UI.CharacterSelectController.Awake += CharacterSelectControllerAwake;
        On.RoR2.UI.SurvivorIconController.Rebuild += SurvivorIconControllerRebuild;
        Logger.LogInfo("Eclipse Progression loaded!");
    }

    private void CharacterSelectControllerAwake(
        On.RoR2.UI.CharacterSelectController.orig_Awake orig,
        CharacterSelectController self)
    {
        orig(self);
        StartCoroutine(CreateSummary(self));
    }

    private IEnumerator CreateSummary(CharacterSelectController controller)
    {
        if (!IsEclipseRun())
            yield break;

        RuleCategoryController? difficulty = null;

        for (var frame = 0; frame < 120 && controller && difficulty == null; frame++)
        {
            yield return null;

            var viewer = controller.GetComponentInChildren<RuleBookViewer>(true);

            if (!viewer || !viewer.categoryContainer)
                continue;

            var categories = viewer.categoryContainer.GetComponentsInChildren<RuleCategoryController>(true);
            difficulty = categories.FirstOrDefault(category =>
                category.currentCategory?.displayToken?.IndexOf("DIFFICULTY", System.StringComparison.OrdinalIgnoreCase) >= 0)
                ?? categories.FirstOrDefault();
        }

        if (!controller || difficulty == null)
            yield break;

        var card = new GameObject(
            "EclipseProgressionSummary",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(LayoutElement));
        card.layer = difficulty.gameObject.layer;
        card.transform.SetParent(difficulty.transform.parent, false);
        card.transform.SetSiblingIndex(difficulty.transform.GetSiblingIndex() + 1);

        card.GetComponent<LayoutElement>().preferredHeight = 126f;

        var layout = card.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 2f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var template = difficulty.categoryHeaderLanguageController.GetComponent<TextMeshProUGUI>();
        var headerStyle = difficulty.headerColorImages.FirstOrDefault(image => image);
        var bodyStyle = difficulty.framePanel ? difficulty.framePanel.GetComponent<Image>() : null;

        var header = CreatePanel(card.transform, "Header", 42f, headerStyle, new Color(0.05f, 0.35f, 0.56f, 1f));
        var headerText = CreateText(header.transform, template, TextAlignmentOptions.Center);
        headerText.text = "ECLIPSE PROGRESS";
        headerText.fontStyle = FontStyles.Bold;
        headerText.fontSize = 20f;

        var body = CreatePanel(card.transform, "Body", 82f, bodyStyle, new Color(0.025f, 0.02f, 0.035f, 0.94f));
        var stats = CreateText(body.transform, template, TextAlignmentOptions.Center);
        stats.rectTransform.anchorMin = new Vector2(0.03f, 0.50f);
        stats.rectTransform.anchorMax = new Vector2(0.97f, 1f);
        stats.rectTransform.offsetMin = Vector2.zero;
        stats.rectTransform.offsetMax = Vector2.zero;
        stats.enableAutoSizing = true;
        stats.fontSizeMin = 13f;
        stats.fontSizeMax = 22f;

        var progressBackground = CreateImage(body.transform, "ProgressBackground", new Color(0.22f, 0.24f, 0.27f, 1f));
        progressBackground.rectTransform.anchorMin = new Vector2(0.08f, 0.40f);
        progressBackground.rectTransform.anchorMax = new Vector2(0.92f, 0.50f);

        var progressFill = CreateImage(progressBackground.transform, "ProgressFill", new Color(0.20f, 0.70f, 0.95f, 1f));

        var footer = CreateText(body.transform, template, TextAlignmentOptions.Center);
        footer.rectTransform.anchorMin = new Vector2(0.03f, 0f);
        footer.rectTransform.anchorMax = new Vector2(0.97f, 0.38f);
        footer.rectTransform.offsetMin = Vector2.zero;
        footer.rectTransform.offsetMax = Vector2.zero;
        footer.color = new Color(0.72f, 0.78f, 0.84f, 1f);
        footer.enableAutoSizing = true;
        footer.fontSizeMin = 10f;
        footer.fontSizeMax = 14f;

        RefreshSummary(stats, footer, progressFill);
    }

    private static Image CreatePanel(
        Transform parent,
        string name,
        float height,
        Image? template,
        Color fallbackColor)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        panel.layer = parent.gameObject.layer;
        panel.transform.SetParent(parent, false);
        panel.GetComponent<LayoutElement>().preferredHeight = height;

        var image = panel.GetComponent<Image>();
        image.raycastTarget = false;

        if (template != null)
        {
            image.sprite = template.sprite;
            image.type = template.type;
            image.material = template.material;
            image.color = template.color;
        }
        else
        {
            image.color = fallbackColor;
        }

        return image;
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.layer = parent.gameObject.layer;
        imageObject.transform.SetParent(parent, false);

        var image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.offsetMin = Vector2.zero;
        image.rectTransform.offsetMax = Vector2.zero;
        return image;
    }

    private static TextMeshProUGUI CreateText(
        Transform parent,
        TextMeshProUGUI? template,
        TextAlignmentOptions alignment)
    {
        var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.layer = parent.gameObject.layer;
        textObject.transform.SetParent(parent, false);

        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(8f, 2f);
        text.rectTransform.offsetMax = new Vector2(-8f, -2f);
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;

        if (template != null)
        {
            text.font = template.font;
            text.fontSharedMaterial = template.fontSharedMaterial;
        }

        return text;
    }

    private void SurvivorIconControllerRebuild(
        On.RoR2.UI.SurvivorIconController.orig_Rebuild orig,
        SurvivorIconController self)
    {
        orig(self);

        var bar = self.GetComponentInParent<CharacterSelectBarController>();

        if (!bar || !bar.isEclipseRun || self.survivorDef == null)
            return;

        var badgeTransform = self.transform.Find("EclipseProgressionBadge");
        TextMeshProUGUI badge;

        if (badgeTransform)
        {
            badge = badgeTransform.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            var badgeObject = new GameObject("EclipseProgressionBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
            badgeObject.layer = self.gameObject.layer;
            badgeObject.transform.SetParent(self.transform, false);
            badge = badgeObject.GetComponent<TextMeshProUGUI>();

            var rect = badge.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(4f, 3f);
            rect.offsetMax = new Vector2(-4f, -3f);

            badge.alignment = TextAlignmentOptions.BottomRight;
            badge.color = new Color(1f, 0.78f, 0.2f, 1f);
            badge.fontStyle = FontStyles.Bold;
            badge.fontSize = 18f;
            badge.raycastTarget = false;
        }

        var completed = GetCompletedLevel(self.survivorDef);
        badge.text = completed == 8 ? "E8 ✓" : $"E{completed}";
    }

    private static void RefreshSummary(
        TextMeshProUGUI stats,
        TextMeshProUGUI footer,
        Image progressFill)
    {
        var survivors = GetEligibleSurvivors();
        var completed = survivors.Sum(GetCompletedLevel);
        var maximum = survivors.Length * 8;
        var e8Count = survivors.Count(survivor => GetCompletedLevel(survivor) == 8);
        var progress = maximum == 0 ? 0f : (float)completed / maximum;
        var percent = 100f * progress;

        stats.text =
            $"<size=115%><b>{completed}</b></size> <color=#A5B0BA>/ {maximum} ECLIPSES</color>  " +
            $"<color=#55C7FF><b>{percent:0.0}%</b></color>";
        footer.text = $"{e8Count} SURVIVOR{(e8Count == 1 ? string.Empty : "S")} COMPLETED E8";

        progressFill.rectTransform.anchorMax = new Vector2(progress, 1f);
    }

    private static SurvivorDef[] GetEligibleSurvivors()
    {
        var localUser = LocalUserManager.GetFirstLocalUser();

        if (localUser == null)
            return [];

        return SurvivorCatalog.orderedSurvivorDefs
            .Where(survivor =>
                survivor != null &&
                !survivor.hidden &&
                SurvivorCatalog.SurvivorIsUnlockedOnThisClient(survivor.survivorIndex) &&
                survivor.CheckRequiredExpansionEnabled() &&
                survivor.CheckUserHasRequiredEntitlement(localUser))
            .ToArray();
    }

    private static int GetCompletedLevel(SurvivorDef survivor)
    {
        var localUser = LocalUserManager.GetFirstLocalUser();
        return localUser == null
            ? 0
            : Mathf.Clamp(EclipseRun.GetLocalUserSurvivorCompletedEclipseLevel(localUser, survivor), 0, 8);
    }

    private static bool IsEclipseRun()
    {
        return PreGameController.instance &&
               PreGameController.instance.gameModeIndex == GameModeCatalog.FindGameModeIndex("EclipseRun");
    }
}
