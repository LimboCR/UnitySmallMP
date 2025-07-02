using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace SimpleMP.Characters
{
    public class Health : NetworkBehaviour, IDamageble
    {
        #region Variables
        #region Settings
        [Header("Health settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private ulong lastAttackerId;
        [SerializeField] private bool isBot;
        #endregion

        #region Network Variables
        private readonly NetworkVariable<float> n_currentHealth = new(writePerm: NetworkVariableWritePermission.Server, readPerm: NetworkVariableReadPermission.Everyone);
        private readonly NetworkVariable<bool> n_alive = new(writePerm: NetworkVariableWritePermission.Server, readPerm: NetworkVariableReadPermission.Everyone);
        #endregion

        public bool Alive => n_alive.Value;
        #endregion

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                n_currentHealth.Value = maxHealth;
                n_alive.Value = true;
                if(!isBot) UpdateHealthClientRpc(n_currentHealth.Value, GetOwnerTarget());
            }
        }

        #region IDamagable
        public void TakeDamage(float amount, ulong sourceClientId)
        {
            if (!IsServer || !n_alive.Value || n_currentHealth.Value <= 0) return;

            lastAttackerId = sourceClientId;
            float newHealth = Mathf.Max(n_currentHealth.Value - amount, 0f);
            n_currentHealth.Value = newHealth;

            if (!isBot) UpdateHealthClientRpc(newHealth, GetOwnerTarget());

            if (n_currentHealth.Value <= 0)
            {
                n_alive.Value = false;
                gameObject.tag = "Dead";
                gameObject.name = $"{gameObject.name} DEAD";
                Die(sourceClientId);
            }
        }

        private void Die(ulong killerClientId)
        {
            if (!isBot) //Checks if it's real player and not bot
                Mechanics.ServerEventsManager.RegisterPlayerDied(OwnerClientId);

            if (killerClientId != lastAttackerId && killerClientId != 999) //checks for any incosistency towards killer and if it's bot
            {
                Debug.LogWarning($"Potentially wrong killer, using passed killerClientId ({killerClientId})");
                Mechanics.ServerEventsManager.RegisterPlayerKilled(killerClientId);
            }
            else if (killerClientId != 999) Mechanics.ServerEventsManager.RegisterPlayerKilled(lastAttackerId);

            if (isBot) Debug.Log($"[{gameObject.name}] Died");

            Destroy(gameObject, 5f);
        }

        #endregion

        #region Gameplay logic
        public void RestoreFullHealth()
        {
            if (IsServer)
                n_currentHealth.Value = maxHealth;
        }

        #endregion
        private ClientRpcParams GetOwnerTarget()
        {
            var target = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new List<ulong> { OwnerClientId }
                }
            };

            return target;
        }
        #region Getters
        public float GetCurrentHealth() => n_currentHealth.Value;
        public NetworkVariable<float> GetHealthVariable() => n_currentHealth;
        #endregion

        #region Helpers

        #endregion

        #region Client RPCs
        [ClientRpc]
        private void UpdateHealthClientRpc(float newHealth, ClientRpcParams rpcParams = default)
        {
            // This will be received only by the target client (owner)
            Mechanics.ClientEventsManager.NotifyHealthChanged(newHealth);
        }
        #endregion
    }

    public interface IDamageble
    {
        void TakeDamage(float amount, ulong damagerId);
    }
}