using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;

    private Dictionary<ulong, FixedString32Bytes> _nicknames = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetPlayerNickname(ulong clientId, string nickname)
    {
        _nicknames[clientId] = new FixedString32Bytes(nickname);
    }

    public string GetNickname(ulong clientId)
    {
        return _nicknames.TryGetValue(clientId, out var name) ? name.ToString() : $"Player {clientId}";
    }
}
