using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using System;
using System.Text;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;


public class PlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
  private NetworkRunner _runner;
  [SerializeField] private int targetSceneIndex;
  [SerializeField] private TMP_InputField roomCodeField;
  [SerializeField] private float initialCash = 50000000;
  private List<PlayerRef> _joinOrder = new();
  private Canvas canvas;

  async void StartGame(GameMode mode, string roomCode)
  {
    _runner = gameObject.AddComponent<NetworkRunner>();
    _runner.ProvideInput = true;
  
    // Create the NetworkSceneInfo from the current scene
    //var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
    var scene = SceneRef.FromIndex(targetSceneIndex);
    var sceneInfo = new NetworkSceneInfo();
    if (scene.IsValid) {
        sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
    }

    // Start or join (depends on gamemode) a session with a specific name
    await _runner.StartGame(new StartGameArgs()
    {
        GameMode = mode,
        SessionName = roomCode,
        Scene = scene,
        SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
    });
  }

private string GenerateRoomCode(int length)
{
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    StringBuilder result = new StringBuilder(length);
    System.Random random = new System.Random(); // 매번 새로운 시드 기반 생성

    for (int i = 0; i < length; i++)
    {
        result.Append(chars[random.Next(chars.Length)]);
    }

    return result.ToString();
}

  public void StartHostMode()
  {
    if (_runner == null)
    {
      string roomCode = GenerateRoomCode(6);

      Debug.Log("Start Host room. room code: "+roomCode);

      StartGame(GameMode.Host,roomCode);
    }
  }

  
  public void StartClientMode()
  {
    if (_runner == null)
    {
      string roomCode = roomCodeField.text;
      StartGame(GameMode.Client,roomCode);
    }
  }

  void CreateCanvas()
  {
    if (FindObjectOfType<Canvas>() == null)
    {
        GameObject canvasObj = new GameObject("Canvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
    }
    else
    {
        canvas = FindObjectOfType<Canvas>();
    }
  }

  // public void ShowTextBox(string message) // , float duration = 2f
  // {
  //     // 텍스트 박스 오브젝트 생성
  //     GameObject textBox = new GameObject("TextBox");
  //     textBox.transform.SetParent(canvas.transform);

  //     // 배경 이미지 추가
  //     Image bgImage = textBox.AddComponent<Image>();
  //     bgImage.color = new Color(0, 0, 0, 0.6f); // 반투명 검정색 배경

  //     // 텍스트 추가
  //     GameObject textObj = new GameObject("Text");
  //     textObj.transform.SetParent(textBox.transform);
  //     TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
  //     text.text = message;
  //     text.fontSize = 36;
  //     text.alignment = TextAlignmentOptions.Center;
  //     text.color = Color.white;

  //     // **RectTransform 설정 (앵커를 이용한 배치)**
  //     RectTransform boxRect = textBox.GetComponent<RectTransform>();
  //     boxRect.sizeDelta = new Vector2(500, 100);
  //     boxRect.anchorMin = new Vector2(0.5f, 1f); // 화면 상단 중앙 기준
  //     boxRect.anchorMax = new Vector2(0.5f, 1f);
  //     boxRect.pivot = new Vector2(0.5f, 1f); // 피벗을 상단 중앙에 설정
  //     boxRect.anchoredPosition = new Vector2(0, -50); // 화면 최상단에서 50px 아래

  //     RectTransform textRect = textObj.GetComponent<RectTransform>();
  //     textRect.sizeDelta = new Vector2(480, 80);
  //     textRect.anchorMin = new Vector2(0.5f, 0.5f);
  //     textRect.anchorMax = new Vector2(0.5f, 0.5f);
  //     textRect.pivot = new Vector2(0.5f, 0.5f);
  //     textRect.anchoredPosition = Vector2.zero;

  //     //Destroy(textBox, duration);
  // }


  [SerializeField] private NetworkPrefabRef _playerPrefab;
  private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

  public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) 
  {
      if (runner.IsServer)
      {
          Debug.Log("On Player Joined: " + player);
          
          Vector2 spawnPosition = new Vector2((player.RawEncoded % runner.Config.Simulation.PlayerCount) * 3, 0);
          NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);

          var pm = networkPlayerObject.GetComponent<PlayerManager>();
          pm.SetPlayerRef(player);
          
          // 일단 기본값으로 초기화
          pm.Initialize(initialCash);

          GameManager.Instance.RegisterPlayerManager(player, pm);

          _joinOrder.Add(player);
          _spawnedCharacters.Add(player, networkPlayerObject);
          
          // PlayerInfoManager가 준비되면 플레이어 정보를 기다림
          StartCoroutine(WaitForPlayerInfo(player));
          
          // 새 플레이어가 접속했으므로 기존 모든 플레이어 정보를 동기화
          StartCoroutine(SyncAllPlayersForNewJoiner());
      }
  }
  
  private System.Collections.IEnumerator SyncAllPlayersForNewJoiner()
  {
      // PlayerInfoManager가 준비될 때까지 대기
      yield return new WaitUntil(() => PlayerInfoManager.Instance != null);
      yield return new WaitForSeconds(1.0f); // 충분한 지연
      
      if (PlayerInfoManager.Instance != null)
      {
          Debug.Log("[PlayerSpawner] Triggering sync for new joiner");
          
          // 서버에서 모든 클라이언트에 전체 플레이어 정보 동기화
          if (PlayerInfoManager.Instance.Object.HasStateAuthority)
          {
              PlayerInfoManager.Instance.RpcSyncAllPlayerInfo();
          }
      }
  }

  private System.Collections.IEnumerator WaitForPlayerInfo(PlayerRef player)
  {
    float timeout = 5f; // 5초 타임아웃
    float elapsed = 0f;

    while (elapsed < timeout)
    {
      if (PlayerInfoManager.Instance != null)
      {
        var playerInfo = PlayerInfoManager.Instance.GetPlayerInfo(player);
        if (playerInfo.HasValue)
        {
          OnPlayerInfoReceived(player, playerInfo.Value);
          yield break;
        }
      }

      elapsed += Time.deltaTime;
      yield return null;
    }

    Debug.LogWarning($"Player info not received for {player} within timeout");
  }


  public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
  {
    Debug.Log("On Player Left");
    if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
    {
      int slotIndex = _joinOrder.IndexOf(player);
      if (slotIndex >= 0)
      {
        GameUIManager.Instance.ClearPlayerSlot(slotIndex);
        _joinOrder.RemoveAt(slotIndex);
      }


      runner.Despawn(networkObject);
      _spawnedCharacters.Remove(player);
    }

  }

  public void OnSceneLoadDone(NetworkRunner runner)
  {
    Debug.Log("OnSceneLoadDone. room code: " + runner.SessionInfo.Name);
    //CreateCanvas();
    //ShowTextBox(runner.SessionInfo.Name);
    GameUIManager.Instance.SetRoomCode(runner.SessionInfo.Name);
  }

  public void OnPlayerInfoReceived(PlayerRef playerRef, NetworkPlayerInfo playerInfo)
  {
      Debug.Log($"[PlayerSpawner] OnPlayerInfoReceived - Player: {playerRef}, Nickname: {playerInfo.nickname.ToString()}, CharIndex: {playerInfo.selectedCharacterIndex}");
      
      if (_spawnedCharacters.TryGetValue(playerRef, out NetworkObject networkObject))
      {
          var pm = networkObject.GetComponent<PlayerManager>();
          if (pm != null)
          {
              // PlayerManager의 정보 업데이트
              pm.UpdatePlayerInfo(playerInfo.userID.ToString(), playerInfo.nickname.ToString(), playerInfo.selectedCharacterIndex);
              
              // 캐릭터 스프라이트 업데이트 (PlayerManager에 저장)
              string path = "Characters/Character_" + playerInfo.selectedCharacterIndex;
              Sprite characterSprite = Resources.Load<Sprite>(path);
              if (characterSprite != null)
              {
                  pm.character = characterSprite;
                  Debug.Log($"[PlayerSpawner] Character sprite updated for player {playerRef}");
              }
              else
              {
                  Debug.LogError($"[PlayerSpawner] Failed to load character sprite: {path}");
              }
          }
          else
          {
              Debug.LogError($"[PlayerSpawner] PlayerManager component not found on player {playerRef}");
          }
      }
      else
      {
          Debug.LogError($"[PlayerSpawner] No spawned character found for player {playerRef}");
      }
      
      // GameUIManager에 플레이어 정보 업데이트 알림 - 안전하게 접근
      StartCoroutine(SafeUpdateGameUI(playerRef, playerInfo.selectedCharacterIndex));
  }

  private System.Collections.IEnumerator SafeUpdateGameUI(PlayerRef playerRef, int characterIndex)
  {
      // GameUIManager가 준비될 때까지 기다림
      float timeout = 5f;
      float elapsed = 0f;
      
      while (elapsed < timeout)
      {
          if (GameUIManager.Instance != null)
          {
              Debug.Log($"[PlayerSpawner] GameUIManager found, updating UI for player {playerRef}");
              GameUIManager.Instance.OnPlayerInfoUpdated(playerRef, characterIndex);
              yield break;
          }
          
          elapsed += Time.deltaTime;
          yield return null;
      }
      
      Debug.LogError($"[PlayerSpawner] GameUIManager not found within timeout for player {playerRef}");
  }

  private void UpdateCharacterSprite(PlayerRef playerRef, int characterIndex)
  {
    int slotIndex = _joinOrder.IndexOf(playerRef);
    if (slotIndex >= 0 && characterIndex >= 0)
    {
      string path = "Characters/Character_" + characterIndex;
      Sprite characterSprite = Resources.Load<Sprite>(path);
      if (characterSprite != null)
      {
        GameUIManager.Instance.SetPlayerSlots(slotIndex, characterSprite);

        if (_spawnedCharacters.TryGetValue(playerRef, out NetworkObject networkObject))
        {
          var pm = networkObject.GetComponent<PlayerManager>();
          if (pm != null)
          {
            pm.character = characterSprite;
          }
        }
      }
    }
  }

  //interface 

  public void OnInput(NetworkRunner runner, NetworkInput input) { }
  public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
  public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
  public void OnConnectedToServer(NetworkRunner runner) { }
  public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
  public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
  public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
  public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
  public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
  public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
  public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
  
  public void OnSceneLoadStart(NetworkRunner runner) { }
  public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player){ }
  public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player){ }
  public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data){ }
  public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress){ }
}