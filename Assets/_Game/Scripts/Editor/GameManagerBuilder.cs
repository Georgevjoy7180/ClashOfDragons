using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ClashOfDragons.Combat;
using ClashOfDragons.Core;
using ClashOfDragons.Player;
using ClashOfDragons.AI;
using ClashOfDragons.UI;

namespace ClashOfDragons.EditorTools
{
    /// <summary>
    /// Builds the scene-root GameManager plus the WinnerPanel UI (under the existing Canvas from
    /// Phase 11) and wires them together. Requires Build HUD (Phase 11) and both dragons to already
    /// exist. Safe to re-run.
    /// </summary>
    public static class GameManagerBuilder
    {
        [MenuItem("Clash Of Dragons/Build Game Manager (Phase 13)")]
        public static void BuildGameManager()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.name != "BattleScene")
            {
                Debug.LogError("Clash Of Dragons: Open Assets/_Game/Scenes/BattleScene.unity as the active scene before running this command.");
                return;
            }

            GameObject canvasObject = GameObject.Find("Canvas");
            if (canvasObject == null)
            {
                Debug.LogError("Clash Of Dragons: Run 'Build HUD (Phase 11)' first — no Canvas found.");
                return;
            }

            GameObject player = GameObject.Find("PlayerDragon");
            GameObject enemy = GameObject.Find("EnemyDragon");
            if (player == null || enemy == null)
            {
                Debug.LogError("Clash Of Dragons: Run 'Build Player (Phase 3)' and 'Build Enemy (Phase 4)' first.");
                return;
            }

            WinnerPanelUI winnerPanel = BuildWinnerPanel(canvasObject.transform);

            GameObject gameManagerObject = EditorSceneUtility.GetOrCreateRoot("GameManager").gameObject;
            GameManager gameManager = gameManagerObject.GetComponent<GameManager>();
            if (gameManager == null)
            {
                gameManager = gameManagerObject.AddComponent<GameManager>();
            }

            DragonHealth playerHealth = player.GetComponent<DragonHealth>();
            DragonHealth enemyHealth = enemy.GetComponent<DragonHealth>();
            PlayerController playerController = player.GetComponent<PlayerController>();
            DragonAI enemyAi = enemy.GetComponent<DragonAI>();

            gameManager.Initialize(playerHealth, enemyHealth, playerController, enemyAi, winnerPanel);
            winnerPanel.SetGameManager(gameManager);

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("Clash Of Dragons: Game Manager + Winner Panel built. Press Ctrl+S to save BattleScene.");
        }

        private static WinnerPanelUI BuildWinnerPanel(Transform canvas)
        {
            GameObject panel = EditorSceneUtility.FindOrCreateUiChild(canvas, "WinnerPanel");
            EditorSceneUtility.StretchFull(panel.GetComponent<RectTransform>());

            Image backgroundImage = panel.GetComponent<Image>();
            if (backgroundImage == null)
            {
                backgroundImage = panel.AddComponent<Image>();
            }
            backgroundImage.color = new Color(0f, 0f, 0f, 0.7f);

            Text winnerText = BuildCenteredText(panel.transform, "WinnerText", new Vector2(0f, 60f), new Vector2(800f, 100f), 56, "PLAYER WINS");
            Button restartButton = BuildRestartButton(panel.transform);

            WinnerPanelUI winnerPanelUi = panel.GetComponent<WinnerPanelUI>();
            if (winnerPanelUi == null)
            {
                winnerPanelUi = panel.AddComponent<WinnerPanelUI>();
            }
            winnerPanelUi.Initialize(winnerText, restartButton);

            panel.SetActive(false);

            return winnerPanelUi;
        }

        private static Text BuildCenteredText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize, string content)
        {
            GameObject textObject = EditorSceneUtility.FindOrCreateUiChild(parent, name);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = textObject.GetComponent<Text>();
            if (text == null)
            {
                text = textObject.AddComponent<Text>();
            }
            text.font = EditorSceneUtility.GetDefaultFont();
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = content;

            return text;
        }

        private static Button BuildRestartButton(Transform parent)
        {
            GameObject buttonObject = EditorSceneUtility.FindOrCreateUiChild(parent, "RestartButton");
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -40f);
            rect.sizeDelta = new Vector2(220f, 60f);

            Image buttonImage = buttonObject.GetComponent<Image>();
            if (buttonImage == null)
            {
                buttonImage = buttonObject.AddComponent<Image>();
            }
            buttonImage.color = new Color(0.2f, 0.6f, 0.3f);

            Button button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObject.AddComponent<Button>();
            }

            GameObject textObject = EditorSceneUtility.FindOrCreateUiChild(buttonObject.transform, "Text");
            EditorSceneUtility.StretchFull(textObject.GetComponent<RectTransform>());
            Text buttonText = textObject.GetComponent<Text>();
            if (buttonText == null)
            {
                buttonText = textObject.AddComponent<Text>();
            }
            buttonText.font = EditorSceneUtility.GetDefaultFont();
            buttonText.fontSize = 24;
            buttonText.fontStyle = FontStyle.Bold;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;
            buttonText.text = "RESTART";

            return button;
        }
    }
}
