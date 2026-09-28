using UnityEngine;
using UnityEngine.SceneManagement;
using ClashOfDragons.Combat;
using ClashOfDragons.Player;
using ClashOfDragons.AI;
using ClashOfDragons.UI;

namespace ClashOfDragons.Core
{
    /// <summary>
    /// Owns the win condition: listens for either dragon's DragonHealth.OnDeath (from Phase 5),
    /// stops both dragons from acting, and shows the winner panel.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // [SerializeField] is required here, not optional: a plain private field is NOT saved into
        // the scene, so it gets wiped back to null on every script recompile (Unity reloads the
        // C# domain and re-creates these objects from serialized data only). Without this, the
        // win condition silently stops working the moment any script anywhere is edited again.
        [SerializeField] private DragonHealth playerHealth;
        [SerializeField] private DragonHealth enemyHealth;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private DragonAI enemyAi;
        [SerializeField] private WinnerPanelUI winnerPanel;

        private bool gameOver;

        /// <summary>
        /// Wired up by GameManagerBuilder (Editor-time). Re-subscribes rather than relying solely
        /// on OnEnable, since adding this component in the Editor fires OnEnable immediately —
        /// before Initialize() has had a chance to supply the DragonHealth references.
        /// </summary>
        public void Initialize(DragonHealth player, DragonHealth enemy, PlayerController controller, DragonAI ai, WinnerPanelUI panel)
        {
            Unsubscribe();

            playerHealth = player;
            enemyHealth = enemy;
            playerController = controller;
            enemyAi = ai;
            winnerPanel = panel;

            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (playerHealth != null)
            {
                playerHealth.OnDeath += HandlePlayerDeath;
                playerHealth.OnHealthChanged += HandlePlayerHealthChanged;
            }

            if (enemyHealth != null)
            {
                enemyHealth.OnDeath += HandleEnemyDeath;
                enemyHealth.OnHealthChanged += HandleEnemyHealthChanged;
            }
        }

        private void Unsubscribe()
        {
            if (playerHealth != null)
            {
                playerHealth.OnDeath -= HandlePlayerDeath;
                playerHealth.OnHealthChanged -= HandlePlayerHealthChanged;
            }

            if (enemyHealth != null)
            {
                enemyHealth.OnDeath -= HandleEnemyDeath;
                enemyHealth.OnHealthChanged -= HandleEnemyHealthChanged;
            }
        }

        // DragonHealth only ever fires OnHealthChanged from TakeDamage/Heal — never during
        // initialization — so every callback here is already a real event, no "is this the
        // first call" guard needed. Player's health changing means the AI landed an attack.
        private void HandlePlayerHealthChanged(float current, float max)
        {
            LogAttack("AI Dragon", "Player");
        }

        // Enemy's health changing means the player landed an attack.
        private void HandleEnemyHealthChanged(float current, float max)
        {
            LogAttack("Player", "AI Dragon");
        }

        private void LogAttack(string attacker, string defender)
        {
            float playerCurrent = playerHealth != null ? playerHealth.CurrentHealth : 0f;
            float playerMax = playerHealth != null ? playerHealth.MaxHealth : 0f;
            float enemyCurrent = enemyHealth != null ? enemyHealth.CurrentHealth : 0f;
            float enemyMax = enemyHealth != null ? enemyHealth.MaxHealth : 0f;

            Debug.Log($"Clash Of Dragons: {attacker} hit {defender} — Player HP: {playerCurrent}/{playerMax}, AI Dragon HP: {enemyCurrent}/{enemyMax}");
        }

        private void HandlePlayerDeath()
        {
            EndGame("AI DRAGON WINS");
        }

        private void HandleEnemyDeath()
        {
            EndGame("PLAYER WINS");
        }

        private void EndGame(string message)
        {
            if (gameOver)
            {
                return;
            }
            gameOver = true;

            if (playerController != null)
            {
                playerController.enabled = false;
            }

            if (enemyAi != null)
            {
                enemyAi.enabled = false;
            }

            if (winnerPanel != null)
            {
                winnerPanel.Show(message);
            }
        }

        public void RestartBattle()
        {
            // Requires BattleScene to be added to File > Build Settings > Scenes In Build —
            // SceneManager.LoadScene needs that even just to test in the Editor's Play mode.
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
