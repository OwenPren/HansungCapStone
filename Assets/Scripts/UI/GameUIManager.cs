using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }
    public RoundStartEventSO roundStartEvent;

    private bool isStartGame;

    [Header ("GameUI")]
    [SerializeField] private GameObject gameUI;


    [Header ("WatingRoomUI")]
    [SerializeField] private GameObject watingRoomUI;
    [SerializeField] private List<Image> playerSlots;
    [SerializeField] private Button StartButton;

    private void OnEnable()
    {
        roundStartEvent.AddListener(StartGame);
    }

    private void OnDisable()
    {
        roundStartEvent.RemoveListener(StartGame);
    }


    private void Awake()
    {
        // 이미 인스턴스가 있으면 중복 제거
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    // Start is called before the first frame update
    void Start()
    {
        HideUI(gameUI);
        ShowUI(watingRoomUI);
        isStartGame = false;
        StartButton.interactable = false;
            //FindObjectOfType<NetworkRunner>()?.IsServer == true;

    }

    public void ShowUI(GameObject obj)
    {
        obj.SetActive(true);
    }
    public void HideUI(GameObject obj)
    {
        obj.SetActive(false);
    }
    public void ToggleUI(GameObject obj)
    {
        obj.SetActive(!obj.activeSelf);
    }

    public void ToggleStartButton()
    {
        StartButton.interactable =
            FindObjectOfType<NetworkRunner>()?.IsServer == true;
    }

    public void SetPlayerSlots(int slotIndex, Sprite characterSprite)
    {
        if (slotIndex < 0 || slotIndex >= playerSlots.Count) return;

        var img = playerSlots[slotIndex];
        img.sprite = characterSprite;
        img.enabled = true;

    }

    public void ClearPlayerSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= playerSlots.Count) return;

        var img = playerSlots[slotIndex];
        img.sprite = null;
        img.enabled = false;
    }

    public void StartGame()
    {
        if (isStartGame) return;

        ToggleUI(gameUI);
        ToggleUI(watingRoomUI);

        isStartGame = true;
    }
    

}
