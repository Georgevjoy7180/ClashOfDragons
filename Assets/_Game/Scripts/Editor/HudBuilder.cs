using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ClashOfDragons.Combat;
using ClashOfDragons.UI;

namespace ClashOfDragons.EditorTools
{
    /// <summary>
    /// Builds the screen-space Canvas, health bars, and ability bar using Unity's own APIs. See
    /// ArenaBuilder for why this runs as an Editor tool instead of hand-authored scene YAML.
    /// Safe to re-run.
    /// </summary>
    public static class HudBuilder
    {
        private static readonly Vector2 BarSize = new Vector2(320f, 32f);
        private static readonly Color PlayerFillColor = new Color(0.2f, 0.55f, 0.95f);
        private static readonly Color EnemyFillColor = new Color(0.9f, 0.25f, 0.25f);

        private static readonly Vector2 SlotSize = new Vector2(80f, 80f);
        // Matches each ability's own VFX color, so the icon hints at what the ability does.
        private static readonly Color FireIconColor = new Color(1f, 0.45f, 0.1f);
        private static readonly Color TailIconColor = new Color(1f, 0.9f, 0.2f);
        private static readonly Color FlyIconColor = new Color(0.9f, 0.2f, 0.9f);

        [MenuItem("Clash Of Dragons/Build HUD (Phase 11)")]
        public static void BuildHud()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.name != "BattleScene")
            {
                Debug.LogError("Clash Of Dragons: Open Assets/_Game/Scenes/BattleScene.unity as the active scene before running this command.");
                return;
            }

            EnsureEventSystem();
            Transform canvas = EnsureCanvas();

            // Pinned to opposite top corners so they read as "your health vs their health".
            Image playerFill = BuildHealthBar(canvas, "PlayerHealth", new Vector2(0f, 1f), new Vector2(20f, -20f), PlayerFillColor);
            Image enemyFill = BuildHealthBar(canvas, "EnemyHealth", new Vector2(1f, 1f), new Vector2(-20f, -20f), EnemyFillColor);

            WireHealthBar("PlayerHealth", "PlayerDragon", playerFill);
            WireHealthBar("EnemyHealth", "EnemyDragon", enemyFill);

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("Clash Of Dragons: HUD built. Press Ctrl+S to save BattleScene.");
        }

        [MenuItem("Clash Of Dragons/Build Ability Bar (Phase 12)")]
        public static void BuildAbilityBar()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.name != "BattleScene")
            {
                Debug.LogError("Clash Of Dragons: Open Assets/_Game/Scenes/BattleScene.unity as the active scene before running this command.");
                return;
            }

            EnsureEventSystem();
            Transform canvas = EnsureCanvas();
            Transform abilityBar = EnsureAbilityBar(canvas);

            GameObject player = GameObject.Find("PlayerDragon");
            DragonCombat playerCombat = player != null ? player.GetComponent<DragonCombat>() : null;

            BuildAbilitySlot(abilityBar, "Fire", "Q", FireIconColor, -110f, playerCombat, AbilitySlot.Fire);
            BuildAbilitySlot(abilityBar, "Tail", "E", TailIconColor, 0f, playerCombat, AbilitySlot.Tail);
            BuildAbilitySlot(abilityBar, "Fly", "R", FlyIconColor, 110f, playerCombat, AbilitySlot.Fly);

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("Clash Of Dragons: Ability bar built. Press Ctrl+S to save BattleScene.");
        }

        private static Transform EnsureAbilityBar(Transform canvas)
        {
            GameObject abilityBarObject = FindOrCreateUiChild(canvas, "AbilityBar", addImage: false);
            RectTransform rect = abilityBarObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 30f);
            rect.sizeDelta = new Vector2(400f, SlotSize.y);
            return abilityBarObject.transform;
        }

        private static void BuildAbilitySlot(Transform abilityBar, string name, string keyLabel, Color iconColor, float xOffset, DragonCombat playerCombat, AbilitySlot slot)
        {
            GameObject root = FindOrCreateUiChild(abilityBar, name, addImage: false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = new Vector2(xOffset, 0f);
            rootRect.sizeDelta = SlotSize;

            GameObject icon = FindOrCreateUiChild(root.transform, "Icon", addImage: true);
            StretchFull(icon.GetComponent<RectTransform>());
            icon.GetComponent<Image>().color = iconColor;

            // Radial cooldown overlay: covers the icon fully on activation, wipes away to 0 as the
            // cooldown finishes, so the icon reads as "available" again exactly when fill hits 0.
            GameObject overlay = FindOrCreateUiChild(root.transform, "CooldownOverlay", addImage: true);
            StretchFull(overlay.GetComponent<RectTransform>());
            Image overlayImage = overlay.GetComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.75f);
            overlayImage.type = Image.Type.Filled;
            overlayImage.fillMethod = Image.FillMethod.Radial360;
            overlayImage.fillOrigin = (int)Image.Origin360.Top;
            overlayImage.fillClockwise = false;
            overlayImage.fillAmount = 0f;

            GameObject keyLabelObject = FindOrCreateUiChild(root.transform, "KeyLabel", addImage: false);
            RectTransform keyRect = keyLabelObject.GetComponent<RectTransform>();
            keyRect.anchorMin = new Vector2(0f, 1f);
            keyRect.anchorMax = new Vector2(0f, 1f);
            keyRect.pivot = new Vector2(0f, 1f);
            keyRect.anchoredPosition = new Vector2(4f, -2f);
            keyRect.sizeDelta = new Vector2(24f, 20f);
            Text keyText = keyLabelObject.GetComponent<Text>();
            if (keyText == null)
            {
                keyText = keyLabelObject.AddComponent<Text>();
            }
            ConfigureText(keyText, keyLabel, 16, TextAnchor.UpperLeft);

            GameObject cooldownTextObject = FindOrCreateUiChild(root.transform, "CooldownText", addImage: false);
            StretchFull(cooldownTextObject.GetComponent<RectTransform>());
            Text cooldownText = cooldownTextObject.GetComponent<Text>();
            if (cooldownText == null)
            {
                cooldownText = cooldownTextObject.AddComponent<Text>();
            }
            ConfigureText(cooldownText, string.Empty, 22, TextAnchor.MiddleCenter);
            cooldownText.fontStyle = FontStyle.Bold;
            cooldownText.enabled = false;

            AbilityBase ability = playerCombat != null ? playerCombat.GetAbility(slot) : null;
            AbilityUI abilityUi = root.GetComponent<AbilityUI>();
            if (abilityUi == null)
            {
                abilityUi = root.AddComponent<AbilityUI>();
            }
            abilityUi.Initialize(ability, overlayImage, cooldownText);
        }

        private static void ConfigureText(Text text, string content, int fontSize, TextAnchor alignment)
        {
            text.font = EditorSceneUtility.GetDefaultFont();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
        }

        private static void EnsureEventSystem()
        {
            if (GameObject.Find("EventSystem") != null)
            {
                return;
            }

            // InputSystemUIInputModule (not the legacy StandaloneInputModule) — this project's
            // Active Input Handling is locked to the new Input System only (see Phase 1).
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private static Transform EnsureCanvas()
        {
            GameObject canvasObject = GameObject.Find("Canvas");
            if (canvasObject == null)
            {
                canvasObject = new GameObject("Canvas");
                Canvas canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);

                canvasObject.AddComponent<GraphicRaycaster>();
            }

            return canvasObject.transform;
        }

        private static Image BuildHealthBar(Transform canvas, string name, Vector2 corner, Vector2 anchoredPosition, Color fillColor)
        {
            GameObject root = FindOrCreateUiChild(canvas, name, addImage: false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = corner;
            rootRect.anchorMax = corner;
            rootRect.pivot = corner;
            rootRect.anchoredPosition = anchoredPosition;
            rootRect.sizeDelta = BarSize;

            GameObject background = FindOrCreateUiChild(root.transform, "Background", addImage: true);
            StretchFull(background.GetComponent<RectTransform>());
            background.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            GameObject fill = FindOrCreateUiChild(root.transform, "Fill", addImage: true);
            StretchFull(fill.GetComponent<RectTransform>());
            Image fillImage = fill.GetComponent<Image>();
            fillImage.color = fillColor;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 1f;

            return fillImage;
        }

        private static void WireHealthBar(string barName, string dragonName, Image fillImage)
        {
            GameObject barRoot = GameObject.Find(barName);
            GameObject dragon = GameObject.Find(dragonName);
            if (barRoot == null || dragon == null)
            {
                return;
            }

            DragonHealth health = dragon.GetComponent<DragonHealth>();
            HealthBarUI healthBarUi = barRoot.GetComponent<HealthBarUI>();
            if (healthBarUi == null)
            {
                healthBarUi = barRoot.AddComponent<HealthBarUI>();
            }

            healthBarUi.Initialize(health, fillImage);
        }

        private static GameObject FindOrCreateUiChild(Transform parent, string name, bool addImage)
        {
            GameObject go = EditorSceneUtility.FindOrCreateUiChild(parent, name);

            if (addImage && go.GetComponent<Image>() == null)
            {
                go.AddComponent<Image>();
            }

            return go;
        }

        private static void StretchFull(RectTransform rect)
        {
            EditorSceneUtility.StretchFull(rect);
        }
    }
}
