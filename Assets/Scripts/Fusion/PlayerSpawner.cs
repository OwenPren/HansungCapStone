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
  [SerializeField] private float initialCash = 5000000;
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

  public void ShowTextBox(string message) // , float duration = 2f
  {
      // 텍스트 박스 오브젝트 생성
      GameObject textBox = new GameObject("TextBox");
      textBox.transform.SetParent(canvas.transform);

      // 배경 이미지 추가
      Image bgImage = textBox.AddComponent<Image>();
      bgImage.color = new Color(0, 0, 0, 0.6f); // 반투명 검정색 배경

      // 텍스트 추가
      GameObject textObj = new GameObject("Text");
      textObj.transform.SetParent(textBox.transform);
      TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
      text.text = message;
      text.fontSize = 36;
      text.alignment = TextAlignmentOptions.Center;
      text.color = Color.white;

      // **RectTransform 설정 (앵커를 이용한 배치)**
      RectTransform boxRect = textBox.GetComponent<RectTransform>();
      boxRect.sizeDelta = new Vector2(500, 100);
      boxRect.anchorMin = new Vector2(0.5f, 1f); // 화면 상단 중앙 기준
      boxRect.anchorMax = new Vector2(0.5f, 1f);
      boxRect.pivot = new Vector2(0.5f, 1f); // 피벗을 상단 중앙에 설정
      boxRect.anchoredPosition = new Vector2(0, -50); // 화면 최상단에서 50px 아래

      RectTransform textRect = textObj.GetComponent<RectTransform>();
      textRect.sizeDelta = new Vector2(480, 80);
      textRect.anchorMin = new Vector2(0.5f, 0.5f);
      textRect.anchorMax = new Vector2(0.5f, 0.5f);
      textRect.pivot = new Vector2(0.5f, 0.5f);
      textRect.anchoredPosition = Vector2.zero;

      //Destroy(textBox, duration);
  }


  [SerializeField] private NetworkPrefabRef _playerPrefab;
  private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

  public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) 
  {
    if (runner.IsServer)
    {
      Debug.Log("On Player Joined");
      //Create a player
      Vector2 spawnPosition = new Vector2((player.RawEncoded % runner.Config.Simulation.PlayerCount) * 3, 0);
      NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab,spawnPosition, Quaternion.identity, player);

      var pm = networkPlayerObject.GetComponent<PlayerManager>();
      pm.SetPlayerRef(player);
      pm.Initialize(initialCash);

      GameManager.Instance.RegisterPlayerManager(player, pm);

      _joinOrder.Add(player);
      int slotIndex = _joinOrder.IndexOf(player);
      Sprite characterSprite = pm.character;
      GameUIManager.Instance.SetPlayerSlots(slotIndex, characterSprite);


      _spawnedCharacters.Add(player, networkPlayerObject);
    }
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
   CreateCanvas();
   ShowTextBox(runner.SessionInfo.Name);
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