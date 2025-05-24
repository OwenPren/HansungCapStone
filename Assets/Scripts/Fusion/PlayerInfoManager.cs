using UnityEngine;
using Fusion;
using System.Collections.Generic;

[System.Serializable]
public struct NetworkPlayerInfo : INetworkStruct
{
    public NetworkString<_32> userID;
    public NetworkString<_32> nickname;
    public int selectedCharacterIndex;
    
    public NetworkPlayerInfo(string uid, string nick, int charIndex)
    {
        userID = uid;
        nickname = nick;
        selectedCharacterIndex = charIndex;
    }
}

public class PlayerInfoManager : NetworkBehaviour
{
    [Networked, Capacity(8)] public NetworkDictionary<PlayerRef, NetworkPlayerInfo> PlayerInfos { get; }
    
    public static PlayerInfoManager Instance { get; private set; }
    
    public override void Spawned()
    {
        Instance = this;
        Debug.Log("[PlayerInfoManager] Spawned!");
        Debug.Log($"[PlayerInfoManager] HasInputAuthority: {Object.HasInputAuthority}");
        Debug.Log($"[PlayerInfoManager] HasStateAuthority: {Object.HasStateAuthority}");
        Debug.Log($"[PlayerInfoManager] InputAuthority: {Object.InputAuthority}");
        
        // PlayerData 상태 확인
        if (PlayerData.instance == null)
        {
            Debug.LogError("[PlayerInfoManager] PlayerData.instance is NULL!");
        }
        else
        {
            Debug.Log($"[PlayerInfoManager] PlayerData found - UserID: '{PlayerData.instance.userID}', Nickname: '{PlayerData.instance.nickname}', CharIndex: {PlayerData.instance.selectedCharacterIndex}");
        }
        
        // 모든 클라이언트에서 자신의 정보를 서버로 전송 시도
        if (PlayerData.instance != null)
        {
            Debug.Log("[PlayerInfoManager] Will attempt to send player info in 1 second");
            Invoke(nameof(TrySendPlayerInfo), 1.0f);
        }
    }
    
    private void TrySendPlayerInfo()
    {
        Debug.Log("[PlayerInfoManager] TrySendPlayerInfo called");
        
        if (PlayerData.instance == null)
        {
            Debug.LogError("[PlayerInfoManager] PlayerData.instance is null in TrySendPlayerInfo!");
            return;
        }
        
        if (Runner == null)
        {
            Debug.LogError("[PlayerInfoManager] Runner is null in TrySendPlayerInfo!");
            return;
        }
        
        string userID = PlayerData.instance.userID;
        string nickname = PlayerData.instance.nickname;
        int characterIndex = PlayerData.instance.selectedCharacterIndex;
        
        Debug.Log($"[PlayerInfoManager] Attempting to send player info - UserID: '{userID}', Nickname: '{nickname}', CharIndex: {characterIndex}");
        
        // 서버인 경우 직접 처리
        if (Object.HasStateAuthority)
        {
            Debug.Log("[PlayerInfoManager] This is server, processing locally");
            ProcessLocalPlayerInfo(userID, nickname, characterIndex);
        }
        // 클라이언트인 경우 RPC 전송
        else
        {
            Debug.Log("[PlayerInfoManager] This is client, sending RPC to server");
            try
            {
                RpcSendPlayerInfoToServer(userID, nickname, characterIndex);
                Debug.Log("[PlayerInfoManager] RPC call completed successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlayerInfoManager] RPC call failed: {e.Message}");
            }
        }
    }
    
    private void ProcessLocalPlayerInfo(string userID, string nickname, int characterIndex)
    {
        Debug.Log("[PlayerInfoManager] ProcessLocalPlayerInfo called");
        
        if (Runner == null)
        {
            Debug.LogError("[PlayerInfoManager] Runner is null in ProcessLocalPlayerInfo!");
            return;
        }
        
        try
        {
            NetworkPlayerInfo playerInfo = new NetworkPlayerInfo(userID, nickname, characterIndex);
            Debug.Log($"[PlayerInfoManager] Created NetworkPlayerInfo struct for local player");
            
            PlayerRef localPlayer = Runner.LocalPlayer;
            Debug.Log($"[PlayerInfoManager] LocalPlayer: {localPlayer}");
            
            if (localPlayer != default(PlayerRef))
            {
                PlayerInfos.Set(localPlayer, playerInfo);
                Debug.Log($"[PlayerInfoManager] Local player info stored in dictionary. Total players: {PlayerInfos.Count}");
                
                // UI 업데이트 알림
                NotifyPlayerInfoChanged(localPlayer, playerInfo);
            }
            else
            {
                Debug.LogError("[PlayerInfoManager] Local player ref is not valid!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerInfoManager] Error processing local player info: {e.Message}");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcSendPlayerInfoToServer(string userID, string nickname, int characterIndex)
    {
        Debug.Log($"[PlayerInfoManager] *** RPC RECEIVED ON SERVER *** UserID: '{userID}', Nickname: '{nickname}', CharIndex: {characterIndex}");
        
        if (Runner == null)
        {
            Debug.LogError("[PlayerInfoManager] Runner is null in RPC!");
            return;
        }
        
        try
        {
            NetworkPlayerInfo playerInfo = new NetworkPlayerInfo(userID, nickname, characterIndex);
            Debug.Log($"[PlayerInfoManager] Created NetworkPlayerInfo struct in RPC");
            
            // RPC 호출자의 PlayerRef를 찾아야 함
            // Fusion에서는 현재 RPC 호출자를 직접 알기 어려우므로 다른 방법 사용
            PlayerRef rpcCaller = FindRpcCaller(userID, nickname);
            
            if (rpcCaller != default(PlayerRef))
            {
                PlayerInfos.Set(rpcCaller, playerInfo);
                Debug.Log($"[PlayerInfoManager] RPC: Player info stored for {rpcCaller}. Total players: {PlayerInfos.Count}");
                
                // 모든 클라이언트에 UI 업데이트 알림
                RpcNotifyPlayerInfoUpdate(rpcCaller, characterIndex);
                
                // 새로 접속한 클라이언트를 위해 모든 정보 동기화
                Invoke(nameof(DelayedSyncAll), 0.5f);
                
                // 딕셔너리 내용 확인
                foreach(var kvp in PlayerInfos)
                {
                    Debug.Log($"[PlayerInfoManager] Dictionary contains - Player {kvp.Key}: '{kvp.Value.nickname.ToString()}'");
                }
            }
            else
            {
                Debug.LogError("[PlayerInfoManager] Could not find RPC caller PlayerRef!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerInfoManager] Error in RPC: {e.Message}");
        }
    }
    
    // RPC 호출자를 찾는 헬퍼 메서드
    private PlayerRef FindRpcCaller(string userID, string nickname)
    {
        // 현재 접속한 플레이어 중에서 아직 정보가 없는 플레이어 찾기
        if (Runner.ActivePlayers != null)
        {
            foreach (var player in Runner.ActivePlayers)
            {
                if (!PlayerInfos.TryGet(player, out NetworkPlayerInfo existingInfo))
                {
                    Debug.Log($"[PlayerInfoManager] Found RPC caller: {player} for '{nickname}'");
                    return player;
                }
            }
        }
        
        return default(PlayerRef);
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcNotifyPlayerInfoUpdate(PlayerRef playerRef, int characterIndex)
    {
        Debug.Log($"[PlayerInfoManager] RpcNotifyPlayerInfoUpdate - Player: {playerRef}, CharIndex: {characterIndex}");
        
        // 각 클라이언트에서 UI 업데이트
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.OnPlayerInfoUpdated(playerRef, characterIndex);
        }
        else
        {
            Debug.LogWarning("[PlayerInfoManager] GameUIManager.Instance is null in RpcNotifyPlayerInfoUpdate");
        }
    }
    
    // 새로 접속한 클라이언트에게 모든 플레이어 정보 전송
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcSyncAllPlayerInfo()
    {
        Debug.Log($"[PlayerInfoManager] RpcSyncAllPlayerInfo - Total players: {PlayerInfos.Count}");
        
        // 모든 플레이어 정보를 다시 전송
        foreach(var kvp in PlayerInfos)
        {
            PlayerRef player = kvp.Key;
            NetworkPlayerInfo playerInfo = kvp.Value;
            
            Debug.Log($"[PlayerInfoManager] Syncing player {player} with character {playerInfo.selectedCharacterIndex}");
            
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.OnPlayerInfoUpdated(player, playerInfo.selectedCharacterIndex);
            }
        }
    }
    
    // 게임 시작 RPC (서버에서 모든 클라이언트로)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcStartGame()
    {
        Debug.Log("[PlayerInfoManager] RpcStartGame received - switching to game UI");
        
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.StartGame();
        }
        else
        {
            Debug.LogError("[PlayerInfoManager] GameUIManager.Instance is null in RpcStartGame");
        }
    }
    
    // 로컬 플레이어 정보 변경 시 UI 업데이트
    private void NotifyPlayerInfoChanged(PlayerRef playerRef, NetworkPlayerInfo playerInfo)
    {
        Debug.Log($"[PlayerInfoManager] NotifyPlayerInfoChanged - Player: {playerRef}, CharIndex: {playerInfo.selectedCharacterIndex}");
        
        // PlayerSpawner에 알림
        var spawner = FindObjectOfType<PlayerSpawner>();
        if (spawner != null)
        {
            Debug.Log("[PlayerInfoManager] Notifying PlayerSpawner...");
            spawner.OnPlayerInfoReceived(playerRef, playerInfo);
        }
        
        // 서버에서만 모든 클라이언트에 알림
        if (Object.HasStateAuthority)
        {
            RpcNotifyPlayerInfoUpdate(playerRef, playerInfo.selectedCharacterIndex);
            
            // 모든 정보 동기화도 함께 실행
            Invoke(nameof(DelayedSyncAll), 0.2f);
        }
    }
    
    // 지연된 전체 동기화 (새 플레이어 접속 시)
    private void DelayedSyncAll()
    {
        if (Object.HasStateAuthority)
        {
            Debug.Log("[PlayerInfoManager] Executing delayed sync for all players");
            RpcSyncAllPlayerInfo();
        }
    }
    
    public NetworkPlayerInfo? GetPlayerInfo(PlayerRef playerRef)
    {
        if (PlayerInfos.TryGet(playerRef, out NetworkPlayerInfo info))
        {
            return info;
        }
        
        return null;
    }
    
    // 디버그용 메서드
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log($"[PlayerInfoManager] === DEBUG INFO ===");
            
            if (Object != null)
            {
                Debug.Log($"[PlayerInfoManager] HasStateAuthority: {Object.HasStateAuthority}");
            }
            
            if (Runner != null)
            {
                Debug.Log($"[PlayerInfoManager] LocalPlayer: {Runner.LocalPlayer}");
                if (Runner.ActivePlayers != null)
                {
                    Debug.Log($"[PlayerInfoManager] ActivePlayers: {string.Join(", ", Runner.ActivePlayers)}");
                }
            }
            
            Debug.Log($"[PlayerInfoManager] PlayerInfos count: {PlayerInfos.Count}");
            
            foreach(var kvp in PlayerInfos)
            {
                Debug.Log($"[PlayerInfoManager] Player {kvp.Key}: '{kvp.Value.nickname.ToString()}' (ID: '{kvp.Value.userID.ToString()}', Char: {kvp.Value.selectedCharacterIndex})");
            }
            
            if (PlayerData.instance != null)
            {
                Debug.Log($"[PlayerInfoManager] Current PlayerData - UserID: '{PlayerData.instance.userID}', Nickname: '{PlayerData.instance.nickname}', CharIndex: {PlayerData.instance.selectedCharacterIndex}");
            }
        }
    }
}