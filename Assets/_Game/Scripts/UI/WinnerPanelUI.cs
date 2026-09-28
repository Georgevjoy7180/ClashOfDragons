using UnityEngine;
using UnityEngine.UI;
using ClashOfDragons.Core;

namespace ClashOfDragons.UI
{
    /// <summary>Shows the winner text and wires the Restart button once the fight ends. Starts hidden.</summary>
    public class WinnerPanelUI : MonoBehaviour
    {
        [SerializeField] private Text winnerText;
        [SerializeField] private Button restartButton;
        [SerializeField] private GameManager gameManager;

        /// <summary>Wired up by GameManagerBuilder (Editor-time).</summary>
        public void Initialize(Text text, Button button)
        {
            winnerText = text;
            restartButton = button;
        }

        /// <summary>
        /// Set once GameManager itself has been created (it needs this panel to exist first).
        /// Wires the button here rather than relying solely on Awake(), since Awake() already runs
        /// the moment this component is added — before gameManager is known.
        /// </summary>
        public void SetGameManager(GameManager manager)
        {
            gameManager = manager;
            WireRestartButton();
        }

        private void Awake()
        {
            WireRestartButton();
        }

        private void WireRestartButton()
        {
            if (restartButton == null || gameManager == null)
            {
                return;
            }

            restartButton.onClick.RemoveListener(gameManager.RestartBattle);
            restartButton.onClick.AddListener(gameManager.RestartBattle);
        }

        public void Show(string message)
        {
            if (winnerText != null)
            {
                winnerText.text = message;
            }

            gameObject.SetActive(true);
        }
    }
}
