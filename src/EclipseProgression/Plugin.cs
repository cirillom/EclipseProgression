using BepInEx;
using RoR2;
using RoR2.UI;
using System.Collections;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using TMPro;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace EclipseProgression;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.cirillom.eclipseprogression";
    public const string PluginName = "Eclipse Progression";
    public const string PluginVersion = "1.0.0";

    private TextMeshProUGUI? _summary;
    private SurvivorDef? _lastSurvivor;

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
        yield return null;
        yield return null;

        if (!controller || !IsEclipseRun() || controller.readyButton == null)
            yield break;

        var readyPanel = controller.readyButton.transform.parent as RectTransform;

        if (readyPanel == null)
            yield break;

        var summaryObject = new GameObject("EclipseProgressionSummary", typeof(RectTransform), typeof(TextMeshProUGUI));
        summaryObject.layer = readyPanel.gameObject.layer;
        summaryObject.transform.SetParent(readyPanel.parent, false);

        var rect = summaryObject.GetComponent<RectTransform>();
        rect.anchorMin = readyPanel.anchorMin;
        rect.anchorMax = readyPanel.anchorMax;
        rect.pivot = readyPanel.pivot;
        rect.anchoredPosition = readyPanel.anchoredPosition + new Vector2(0f, 160f);
        rect.sizeDelta = new Vector2(620f, 92f);

        _summary = summaryObject.GetComponent<TextMeshProUGUI>();
        _summary.alignment = TextAlignmentOptions.Center;
        _summary.color = Color.white;
        _summary.enableAutoSizing = true;
        _summary.fontSizeMin = 15f;
        _summary.fontSizeMax = 24f;
        _summary.raycastTarget = false;

        _lastSurvivor = controller.currentSurvivorDef;
        RefreshSummary(_lastSurvivor);

        while (controller && _summary)
        {
            if (_lastSurvivor != controller.currentSurvivorDef)
            {
                _lastSurvivor = controller.currentSurvivorDef;
                RefreshSummary(_lastSurvivor);
            }

            yield return null;
        }
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

    private void RefreshSummary(SurvivorDef? selected)
    {
        var summary = _summary;

        if (summary == null)
            return;

        var survivors = GetEligibleSurvivors();
        var completed = survivors.Sum(GetCompletedLevel);
        var maximum = survivors.Length * 8;
        var e8Count = survivors.Count(survivor => GetCompletedLevel(survivor) == 8);
        var percent = maximum == 0 ? 0f : 100f * completed / maximum;
        var selectedText = selected == null
            ? string.Empty
            : $"\n{Language.GetString(selected.displayNameToken)}: E{GetCompletedLevel(selected)} / E8 complete";

        summary.text =
            $"<b>ECLIPSE PROGRESS</b>\n{completed} / {maximum} levels completed — {percent:0.0}% · {e8Count} survivors completed E8{selectedText}";
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
