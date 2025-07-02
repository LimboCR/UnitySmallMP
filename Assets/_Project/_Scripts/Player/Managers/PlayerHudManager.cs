using TMPro;
using UnityEngine;

namespace SimpleMP.Characters
{
    public class PlayerHudManager : MonoBehaviour
    {
        [SerializeField] private TMP_Text playerHealth;
        [SerializeField] private TMP_Text playerBullets;
        [SerializeField] private TMP_Text playerKills;
        [SerializeField] private TMP_Text playersNickname;

        public void SetPlayerHealth(float value) => playerHealth.text = $"HP: {value}";
        public void SetPlayerBullets(int value) => playerBullets.text = $"Ammo: {value}";
        public void SetPlayerKills(int value) => playerKills.text = $"Kills: {value}";
        public void SetPlayerNickname(string name) => playersNickname.text = name;

        private void OnEnable()
        {
            Mechanics.ClientEventsManager.OnHealthChanged.AddListener(SetPlayerHealth);
            Mechanics.ClientEventsManager.OnBulletsChanged.AddListener(SetPlayerBullets);
            //Mechanics.ClientEventsManager.OnHudNickSet.AddListener(SetPlayerNickname);
            Mechanics.ClientEventsManager.OnKillScoreUI.AddListener(SetPlayerKills);
        }

        private void OnDisable()
        {
            Mechanics.ClientEventsManager.OnHealthChanged.RemoveListener(SetPlayerHealth);
            Mechanics.ClientEventsManager.OnBulletsChanged.RemoveListener(SetPlayerBullets);
            //Mechanics.ClientEventsManager.OnHudNickSet.RemoveListener(SetPlayerNickname);
            Mechanics.ClientEventsManager.OnKillScoreUI.RemoveListener(SetPlayerKills);
        }
    }
}

