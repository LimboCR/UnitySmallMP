using UnityEngine;
using TMPro;

namespace SimpleMP.Mechanics
{
    public class PlayerScoreTab : MonoBehaviour
    {
        [SerializeField] private TMP_Text nicknameText;
        [SerializeField] private TMP_Text killsText;
        [SerializeField] private TMP_Text deathsText;

        public void SetNickname(string nickname)
        {
            nicknameText.text = nickname;
        }

        public void UpdateKills(int kills)
        {
            killsText.text = kills.ToString();
        }

        public void UpdateDeaths(int deaths)
        {
            deathsText.text = deaths.ToString();
        }
    }
}

