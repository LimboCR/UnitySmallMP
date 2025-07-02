using UnityEngine;
using UnityEngine.Events;

namespace SimpleMP.Mechanics
{
    public class ClientEventsManager : MonoBehaviour
    {
        #region Variables
        #region Instance
        public static ClientEventsManager Instance { get; private set; }
        #endregion

        #region Events Declaration
        public static UnityEvent<float> OnHealthChanged = new();
        public static UnityEvent<int> OnBulletsChanged = new();
        public static UnityEvent OnPlayerRespawned = new();

        public static UnityEvent<ulong> OnPlayerKilledUI = new();
        public static UnityEvent<int> OnKillScoreUI = new();
        #endregion

        #region Debug
        private static bool DebugClientEvents = false;
        #endregion

        #endregion

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static void NotifyHealthChanged(float health)
        {
            if (DebugClientEvents) Debug.Log($"[ClientEventsManager] [NotifyHealthChanged] was called with val {health}");
            OnHealthChanged.Invoke(health);
        }

        public static void NotifyBulletsChanged(int bullets)
        {
            if (DebugClientEvents) Debug.Log($"[ClientEventsManager] [NotifyBulletsChanged] was called with val {bullets}");
            OnBulletsChanged.Invoke(bullets);
        }

        public static void NotifyPlayerRespawned() //Unused for now
        {
            if (DebugClientEvents) Debug.Log($"[ClientEventsManager] [NotifyPlayerRespawned] was called");
            OnPlayerRespawned.Invoke();
        }

        public static void NotifyPlayerKilledUI(ulong killerId) // To give visual response for killing
        {
            if (DebugClientEvents) Debug.Log($"[ClientEventsManager] [NotifyPlayerKilledUI] was called for {killerId}");
            OnPlayerKilledUI.Invoke(killerId);
        }

        public static void NotifyKillScoreUI(int kills)
        {
            if (DebugClientEvents) Debug.Log($"[ClientEventsManager] [NotifyKillScoreUI] was called with val {kills}");
            OnKillScoreUI.Invoke(kills);
        }
    }
}
