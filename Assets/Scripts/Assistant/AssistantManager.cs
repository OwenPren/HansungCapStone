using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Security.Principal;
using System;
using System.Linq;
using Fusion;

public enum SectorType
{
    Energy,
    Technology,
    Finance,
    Healthcare,
    ConsumerDiscretionary,
    ConsumerStaples,
    Telecom,
    Industrials,
    Materials,
    RealEstate
}

public enum AssistantStatus
{
    NotStarted,          // 시작 안됨
    CreatingThread,      // 스레드 생성 중
    GeneratingEvents,    // 이벤트 생성 중
    ProcessingStocks,    // 주가 정보 처리 중
    Ready,              // 준비 완료
    Error               // 오류 발생
}

[System.Serializable]
public class RoundEventData
{
    public int roundNumber;
    public List<string> eventDescriptions;
    public Dictionary<string, string> sectorImpacts;
    public bool isGenerated;
    public bool isApplied;
    
    public RoundEventData(int round)
    {
        roundNumber = round;
        eventDescriptions = new List<string>();
        sectorImpacts = new Dictionary<string, string>();
        isGenerated = false;
        isApplied = false;
    }
}

public class AssistantManager : NetworkBehaviour
{
    public GameStartEventSO gameStartEvent;
    public RoundStartEventSO roundStartEvent;
    public GameEndEventSO gameEndEvent;
    public FunctionCallArgumentsEvent functionCallArgumentsEvents;

    public APIManager apiManager;

    [SerializeField] private float retrieveWaitTime = 2.0f;

    private bool IsThread = false;
    private string threadID = "";
    private string runID = "";
    private string messageID = "";
    private string runStatus = "";
    private string functionCallID = "";
    private JObject functionCallArguments = null;

    private bool runInProgress = false;
    
    // 라운드별 이벤트 데이터 관리
    [SerializeField] private Dictionary<int, RoundEventData> roundEventsData = new Dictionary<int, RoundEventData>();
    [SerializeField] private int currentProcessingRound = 0;
    [SerializeField] private bool isGeneratingEvents = false;

    // 네트워크 동기화 Assistant 상태 관리
    [Networked] public AssistantStatus NetworkedStatus { get; private set; }
    [Networked] public NetworkString<_64> NetworkedStatusMessage { get; private set; }
    [Networked] public bool NetworkedIsRound1Ready { get; private set; }

    // 로컬 상태 관리 (서버에서만 사용)
    private AssistantStatus localStatus = AssistantStatus.NotStarted;
    private string localStatusMessage = "";

    public static AssistantManager Instance { get; private set; }

    public override void Spawned()
    {
        Instance = this;
        Debug.Log($"[AssistantManager] Spawned on {(Runner.IsServer ? "SERVER" : "CLIENT")}");
    }

    private void OnEnable()
    {
        gameStartEvent.OnGameStart += OnGameStart;
        roundStartEvent.AddListener(OnRoundStart);
        gameEndEvent.OnGameEnd += OnGameEnd;
    }

    private void OnDisable()
    {
        gameStartEvent.OnGameStart -= OnGameStart;
        roundStartEvent.RemoveListener(OnRoundStart);
        gameEndEvent.OnGameEnd -= OnGameEnd;
    }

    private IEnumerator OnRoundStart()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[AssistantManager] GameManager.Instance is null!");
            yield break;
        }

        int currentRound = GameManager.Instance.CurrentRound;
        Debug.Log($"[AssistantManager] OnRoundStart called for Round {currentRound}");

        // 서버에서만 실행
        if (!Runner.IsServer) yield break;

        // 1. 현재 라운드 이벤트 적용 (이미 생성되어 있어야 함)
        if (roundEventsData.ContainsKey(currentRound) && roundEventsData[currentRound].isGenerated)
        {
            ApplyRoundEvents(currentRound);
        }
        else
        {
            Debug.LogWarning($"[AssistantManager] Round {currentRound} events not ready! Generating now...");
            // 긴급히 현재 라운드 이벤트 생성
            yield return StartCoroutine(GenerateEventsForRound(currentRound));
            ApplyRoundEvents(currentRound);
        }

        // 2. 다음 라운드 이벤트 생성 시작 (백그라운드에서)
        int nextRound = currentRound + 1;
        if (nextRound <= 12) // 최대 라운드 체크
        {
            StartCoroutine(GenerateEventsForRound(nextRound));
        }
    }

    private void OnGameStart()
    {
        // 서버에서만 게임 시작 로직 실행
        if (Runner.IsServer)
        {
            StartCoroutine(GameStartRoutine());
        }
    }

    private void OnGameEnd()
    {
        // 서버에서만 정리 작업 실행
        if (Runner.IsServer)
        {
            if (IsThread && !string.IsNullOrEmpty(threadID))
            {
                StartCoroutine(DeleteThread());
            }

            // 모든 데이터 초기화
            IsThread = false;
            threadID = "";
            runID = "";
            messageID = "";
            runStatus = "";
            functionCallID = "";
            functionCallArguments = null;
            runInProgress = false;
            roundEventsData.Clear();
            currentProcessingRound = 0;
            isGeneratingEvents = false;
            
            // 상태 초기화
            localStatus = AssistantStatus.NotStarted;
            localStatusMessage = "";
            
            // 네트워크 상태 초기화
            UpdateNetworkStatus(AssistantStatus.NotStarted, "", false);
        }
    }

    private IEnumerator GameStartRoutine()
    {
        UpdateStatus(AssistantStatus.NotStarted, "Assistant 초기화 중...");
        
        if (!IsThread)
        {
            yield return StartCoroutine(StartThread());   
        }
        
        // 게임 시작 시 첫 번째 라운드 이벤트 미리 생성
        yield return StartCoroutine(GenerateEventsForRound(1));
    }

    // 특정 라운드의 이벤트 생성 (서버에서만)
    private IEnumerator GenerateEventsForRound(int roundNumber)
    {
        if (!Runner.IsServer) yield break;

        if (isGeneratingEvents)
        {
            Debug.LogWarning($"[AssistantManager] Event generation already in progress. Skipping round {roundNumber}");
            yield break;
        }

        if (roundEventsData.ContainsKey(roundNumber) && roundEventsData[roundNumber].isGenerated)
        {
            Debug.Log($"[AssistantManager] Events for round {roundNumber} already generated");
            yield break;
        }

        Debug.Log($"[AssistantManager] Starting event generation for Round {roundNumber}");
        isGeneratingEvents = true;
        currentProcessingRound = roundNumber;

        // 라운드 데이터 초기화
        if (!roundEventsData.ContainsKey(roundNumber))
        {
            roundEventsData[roundNumber] = new RoundEventData(roundNumber);
        }

        try
        {
            // 1라운드일 때만 UI 상태 업데이트
            if (roundNumber == 1)
            {
                UpdateStatus(AssistantStatus.GeneratingEvents, "1라운드 이벤트 생성 중...");
            }
            
            // 이벤트 생성
            yield return StartCoroutine(GenerationEvent(roundNumber));
            
            if (roundNumber == 1)
            {
                UpdateStatus(AssistantStatus.ProcessingStocks, "주가 정보 처리 중...");
            }
            
            // 주가 정보 생성 (functionCallArguments 사용)
            yield return StartCoroutine(StockPriceAdjustment(roundNumber));

            // 데이터 저장
            if (functionCallArguments != null)
            {
                ParseAndStoreEventData(roundNumber, functionCallArguments);
                roundEventsData[roundNumber].isGenerated = true;
                Debug.Log($"[AssistantManager] Round {roundNumber} events generated and stored successfully");
                
                // 1라운드 완료 시 Ready 상태로 변경
                if (roundNumber == 1)
                {
                    UpdateStatus(AssistantStatus.Ready, "준비 완료!");
                }
            }
            else
            {
                Debug.LogError($"[AssistantManager] Failed to generate events for round {roundNumber}");
                if (roundNumber == 1)
                {
                    UpdateStatus(AssistantStatus.Error, "이벤트 생성 실패");
                }
            }
        }
        finally
        {
            isGeneratingEvents = false;
            currentProcessingRound = 0;
        }
    }

    // 생성된 이벤트를 현재 라운드에 적용
    private void ApplyRoundEvents(int roundNumber)
    {
        if (!Runner.IsServer) return;

        if (!roundEventsData.ContainsKey(roundNumber))
        {
            Debug.LogError($"[AssistantManager] No event data found for round {roundNumber}");
            return;
        }

        var roundData = roundEventsData[roundNumber];
        if (!roundData.isGenerated)
        {
            Debug.LogError($"[AssistantManager] Round {roundNumber} events not yet generated");
            return;
        }

        if (roundData.isApplied)
        {
            Debug.LogWarning($"[AssistantManager] Round {roundNumber} events already applied");
            return;
        }

        Debug.Log($"[AssistantManager] Applying events for Round {roundNumber}");

        // GameManager에 데이터 전달
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ToGmSectorImpacts(roundData.sectorImpacts);
            GameManager.Instance.ToGmHintData(roundData.eventDescriptions);
        }

        roundData.isApplied = true;
        Debug.Log($"[AssistantManager] Round {roundNumber} events applied successfully");
    }

    // functionCallArguments에서 데이터 파싱 및 저장
    private void ParseAndStoreEventData(int roundNumber, JObject arguments)
    {
        if (!roundEventsData.ContainsKey(roundNumber))
        {
            roundEventsData[roundNumber] = new RoundEventData(roundNumber);
        }

        var roundData = roundEventsData[roundNumber];
        
        Dictionary<string, string> sectorImpacts = new Dictionary<string, string>();
        List<string> eventDescriptions = new List<string>();

        if (arguments == null || arguments["eventInfo"] == null)
        {
            Debug.LogError($"[AssistantManager] Invalid arguments for round {roundNumber}");
            return;
        }

        foreach (JToken ev in arguments["eventInfo"]!)
        {
            if (ev["description"] != null)
            {
                eventDescriptions.Add(ev["description"]!.ToString());
            }

            string direction = ev["impactDirection"]!.ToString();
            foreach (JToken sector in ev["affectedSectors"]!)
            {
                sectorImpacts[sector.ToString()] = direction;
            }
        }

        // 데이터 저장
        roundData.eventDescriptions = eventDescriptions;
        roundData.sectorImpacts = sectorImpacts;

        Debug.Log($"[AssistantManager] Stored {eventDescriptions.Count} events and {sectorImpacts.Count} sector impacts for round {roundNumber}");
        
        foreach (var kv in sectorImpacts)
        { 
            Debug.Log($"[Round {roundNumber}] {kv.Key} : {kv.Value}");
        }
    }

    // 네트워크 상태 업데이트 (서버에서만 호출)
    private void UpdateNetworkStatus(AssistantStatus status, string message, bool isRound1Ready)
    {
        if (!Runner.IsServer) return;

        NetworkedStatus = status;
        NetworkedStatusMessage = message;
        NetworkedIsRound1Ready = isRound1Ready;

        Debug.Log($"[AssistantManager] Network status updated - Status: {status}, Message: {message}, Round1Ready: {isRound1Ready}");

        // RPC로 모든 클라이언트에 상태 변경 알림
        RpcNotifyStatusChange(status, message, isRound1Ready);
    }

    // 모든 클라이언트에 상태 변경 알림
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcNotifyStatusChange(AssistantStatus status, string message, bool isRound1Ready)
    {
        Debug.Log($"[AssistantManager] RpcNotifyStatusChange received - Status: {status}, Message: {message}, Round1Ready: {isRound1Ready}");

        // GameUIManager에 상태 변화 알림
        if (GameUIManager.Instance != null)
        {
            bool isReady = (status == AssistantStatus.Ready && isRound1Ready);
            GameUIManager.Instance.UpdateAssistantStatus(message, isReady);
            GameUIManager.Instance.UpdateAssistantProgress(GetProgressFromStatus(status));
            
            // 준비 완료 시 버튼 상태 업데이트
            if (isReady)
            {
                GameUIManager.Instance.OnAssistantReady();
            }
        }
    }

    // 상태 관리 메서드들
    public AssistantStatus GetCurrentStatus()
    {
        // 클라이언트에서는 네트워크 상태 반환, 서버에서는 로컬 상태 반환
        return Runner.IsServer ? localStatus : NetworkedStatus;
    }

    public string GetStatusMessage()
    {
        // 클라이언트에서는 네트워크 메시지 반환, 서버에서는 로컬 메시지 반환
        return Runner.IsServer ? localStatusMessage : NetworkedStatusMessage.ToString();
    }

    public float GetProgress()
    {
        AssistantStatus status = GetCurrentStatus();
        return GetProgressFromStatus(status);
    }

    private float GetProgressFromStatus(AssistantStatus status)
    {
        switch (status)
        {
            case AssistantStatus.NotStarted: return 0.0f;
            case AssistantStatus.CreatingThread: return 0.2f;
            case AssistantStatus.GeneratingEvents: return 0.6f;
            case AssistantStatus.ProcessingStocks: return 0.8f;
            case AssistantStatus.Ready: return 1.0f;
            case AssistantStatus.Error: return 0.0f;
            default: return 0.0f;
        }
    }

    private void UpdateStatus(AssistantStatus status, string message)
    {
        if (!Runner.IsServer) return; // 서버에서만 상태 업데이트

        localStatus = status;
        localStatusMessage = message;
        
        Debug.Log($"[AssistantManager] Local status updated - Status: {status}, Message: {message}");
        
        // 1라운드 준비 상태 확인
        bool isRound1Ready = IsRoundEventsReady(1);
        
        // 네트워크 상태 업데이트
        UpdateNetworkStatus(status, message, isRound1Ready);
    }

    // 디버그용 메서드들
    public void LogRoundEventsStatus()
    {
        Debug.Log($"[AssistantManager] === ROUND EVENTS STATUS ===");
        Debug.Log($"Current Status: {GetCurrentStatus()}");
        Debug.Log($"Current Processing Round: {currentProcessingRound}");
        Debug.Log($"Is Generating Events: {isGeneratingEvents}");
        Debug.Log($"Total Rounds Data: {roundEventsData.Count}");
        Debug.Log($"Is Server: {Runner?.IsServer}");
        
        if (Runner.IsServer)
        {
            foreach (var kvp in roundEventsData)
            {
                var data = kvp.Value;
                Debug.Log($"Round {kvp.Key}: Generated={data.isGenerated}, Applied={data.isApplied}, Events={data.eventDescriptions.Count}, Sectors={data.sectorImpacts.Count}");
            }
        }
        else
        {
            Debug.Log("Client - using networked status only");
        }
    }

    public RoundEventData GetRoundEventData(int roundNumber)
    {
        if (!Runner.IsServer) return null; // 서버에서만 라운드 데이터 접근 가능
        return roundEventsData.ContainsKey(roundNumber) ? roundEventsData[roundNumber] : null;
    }

    public bool IsRoundEventsReady(int roundNumber)
    {
        if (Runner.IsServer)
        {
            return roundEventsData.ContainsKey(roundNumber) && roundEventsData[roundNumber].isGenerated;
        }
        else
        {
            // 클라이언트에서는 네트워크 상태 사용
            return roundNumber == 1 ? NetworkedIsRound1Ready : false;
        }
    }

    // 나머지 기존 메서드들... (GenerationEvent, StockPriceAdjustment, API 관련 메서드들)
    // 이 메서드들은 서버에서만 실행되므로 변경 없음

    private IEnumerator GenerationEvent(int targetRound = 0)
    {
        Debug.Log($"[AssistantManager] GenerationEvent called for round {targetRound}");
        
        // 랜덤으로 1~3개 분야 선택
        SectorType[] allSectors = (SectorType[])Enum.GetValues(typeof(SectorType));
        List<SectorType> sectorsList = new List<SectorType>(allSectors);

        int numberOfSectors = 2; // 숫자 고정

        // Fisher-Yates 셔플
        for (int i = sectorsList.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            SectorType temp = sectorsList[i];
            sectorsList[i] = sectorsList[j];
            sectorsList[j] = temp;
        }

        List<SectorType> chosenSectors = sectorsList.GetRange(0, numberOfSectors);
        JArray eventSectorsArray = new JArray();
        foreach (var sector in chosenSectors)
        {
            eventSectorsArray.Add(sector.ToString());
        }

        JObject inputParameters = new JObject
        {
            ["specialEventInfo"] = "",            // 특별 이벤트 정보 (없을 경우 빈 문자열)
            ["generateSpecialEvent"] = false,       // 특별 이벤트 생성 여부
            ["generateUnexpectedEvent"] = false,    // 예기치 않은 이벤트 포함 여부
            ["eventSectors"] = eventSectorsArray      // 생성할 사건 분야 리스트
        };

        JObject toolChoiceObject = new JObject
        {
            ["type"] = "function",
            ["function"] = new JObject
            {
                ["name"] = "generate_event_titles_and_descriptions"
            }
        };

        // 어시스턴트에게 이벤트 생성 요청
        yield return StartCoroutine(GenarationRoutine("user", inputParameters.ToString(), APIUrls.EventGenerationAssistantID, toolChoiceObject));
    }

    private IEnumerator StockPriceAdjustment(int targetRound = 0)
    {
        Debug.Log($"[AssistantManager] StockPriceAdjustment called for round {targetRound}");
        
        if (functionCallArguments == null)
        {
            Debug.Log("function Argument is not exist");
            yield break; 
        }

        JObject toolChoiceObject = new JObject
        {
            ["type"] = "function",
            ["function"] = new JObject
            {
                ["name"] = "calculate_sector_price_changes"
            }
        };

        yield return StartCoroutine(GenarationRoutine("user", functionCallArguments.ToString(), APIUrls.StockPriceAdjustmentAssistantID, toolChoiceObject));
    }

    private IEnumerator GenarationRoutine(string role, string data, string assistantId, JObject toolChoice = null)
    {
        //메세지 생성 요청
        yield return StartCoroutine(CreateMessage(role,data));
        yield return StartCoroutine(CreateRun(assistantId,toolChoice));

        //메세지 생성 대기
        yield return StartCoroutine(RetrieveRun());

        //function call 수행 완료 요청
        if (runStatus == "requires_action")
        {
            // tool outputs 제출
            yield return StartCoroutine(SubmitToolOutputsToRun());

            // 2차 대기 : completed 될 때까지
            yield return StartCoroutine(RetrieveRun());
        }

        runInProgress = false;
    }

    private IEnumerator StartThread()
    {
        UpdateStatus(AssistantStatus.CreatingThread, "스레드 생성 중...");
        
        bool isDone = false;
        //스레드 생성, 스레드 ID 저장
        yield return StartCoroutine(apiManager.PostRequest(
            APIUrls.CreateThreadURL,
            "{}",
            onSuccess: (response) => {
                Debug.Log("Create Thread POST 성공: " + response);

                //스레드 활성 및 ID 저장
                IsThread = true;

                JObject jobj = JObject.Parse(response);
                threadID = jobj["id"].ToString();
                Debug.Log("threadID: " + threadID);
                isDone = true;
            },
            onError: (error) =>
            {
                Debug.LogError("Create Thread POST 실패: " + error);
                UpdateStatus(AssistantStatus.Error, "스레드 생성 실패");
                isDone = true;
            }
        ));

        yield return new WaitUntil(() => isDone);
    }

    private IEnumerator CreateMessage(string role, string content, string assignment = null, JArray attachments = null, JObject metadata = null)
    {
        if (string.IsNullOrEmpty(threadID))
        {
            Debug.Log("Thread ID Error"); 
            yield break;
        }

        //body data 작성
        JObject requestBody = new JObject
        {
            ["role"] = role,
            ["content"] = content
        };

        if(!string.IsNullOrEmpty(assignment))
        {
            requestBody["assginments"] = assignment;
        }

        if (attachments != null)
        {
            requestBody["attachment"] = attachments;
        }

        if (metadata != null)
        {
            requestBody["metadata"] = metadata;
        }

        bool isDone = false;

        yield return StartCoroutine(apiManager.PostRequest(
            APIUrls.CreateMessageUrl(threadID),
            requestBody.ToString(),
            onSuccess: (response) => {
                Debug.Log("Create Message POST 성공: " + response);
                
                JObject jobj = JObject.Parse(response);
                messageID = jobj["id"]?.ToString();
                Debug.Log("messageId: " + messageID);
                isDone = true;
            },
            onError: (error) =>
            {
                Debug.LogError("Create Message POST 실패: " + error);
                isDone = true;
            }
        ));

        yield return new WaitUntil(()=> isDone);
    }

    private IEnumerator CreateRun(string assistantId, object toolChoice = null, string additionalInstructions = null, string instructions = null, JArray additionalMessages = null, int? maxCompletionTokens = null)
    {
        if (runInProgress) yield break;
        runInProgress = true;

        if (string.IsNullOrEmpty(threadID))
        {
            Debug.Log("Thread ID Error"); 
            yield break;
        }

        JObject requestBody = new JObject
        {
            ["assistant_id"] = assistantId
        };

        if (toolChoice != null)
        {
            if (toolChoice is string)
            {
                requestBody["tool_choice"] = (string)toolChoice;
            }
            else if (toolChoice is JObject)
            {
                requestBody["tool_choice"] = (JObject)toolChoice;
            }
        }

        
        if (!string.IsNullOrEmpty(additionalInstructions))
        {
            requestBody["additional_instructions"] = additionalInstructions;
        }

        if (!string.IsNullOrEmpty(instructions))
        {
            requestBody["instructions"] = instructions;
        }

        if (additionalMessages != null)
        {
            requestBody["additional_messages"] = additionalMessages;
        }

        if (maxCompletionTokens.HasValue && maxCompletionTokens.Value > 0)
        {
            requestBody["max_completion_tokens"] = maxCompletionTokens.Value;
        }

        bool isDone = false;

        yield return StartCoroutine(apiManager.PostRequest(
            APIUrls.CreateRunUrl(threadID),
            requestBody.ToString(),
            onSuccess: (response) =>
            {
                Debug.Log("Create Run POST 성공: " + response);

                JObject jobj = JObject.Parse(response);
                runID = jobj["id"]?.ToString();
                Debug.Log("runID: " + runID);
                isDone = true;
            },
            onError: (error) => {
                Debug.LogError("Create Run POST 실패: " + error);
                isDone = true;
            }
        ));

        yield return new WaitUntil(()=> isDone);
    }

    private IEnumerator RetrieveRun()
    {
        if (string.IsNullOrEmpty(threadID))
        {
            Debug.Log("Thread ID Error");
            yield break;
        }

        if (string.IsNullOrEmpty(runID))
        {
            Debug.Log("Run ID Error");
            yield break;
        }

        while (true)
        {
            bool isDone = false;

            yield return StartCoroutine(apiManager.GetRequest(
                APIUrls.RetrieveRunUrl(threadID,runID),
                onSuccess: (response) =>
                {
                    Debug.Log("Retreive Run 성공: " + response);

                    JObject jobj = JObject.Parse(response);
                    runStatus = jobj["status"]?.ToString();
                    Debug.Log("Current Run Status: " + runStatus);

                    if (runStatus == "requires_action" && jobj["required_action"] != null)
                    {
                        JObject requiredAction = (JObject)jobj["required_action"];
                        JObject submitToolOutputs = requiredAction["submit_tool_outputs"] as JObject;
                        if (submitToolOutputs != null)
                        {
                            JArray toolCalls = submitToolOutputs["tool_calls"] as JArray;
                            if (toolCalls != null && toolCalls.Count > 0)
                            {
                                JObject firstToolCall = toolCalls[0] as JObject;
                                if (firstToolCall != null)
                                {
                                    functionCallID = firstToolCall["id"]?.ToString();
                                    Debug.Log("Function Call ID: " + functionCallID);
 
                                    JObject functionObj = firstToolCall["function"] as JObject;
                                    if (functionObj != null)
                                    {
                                        string argumentsStr = functionObj["arguments"]?.ToString();
                                        try
                                        {
                                            JObject parsedArgs = JObject.Parse(argumentsStr);
                                            functionCallArguments = parsedArgs; // Store as JObject
                                        }
                                        catch (Exception e)
                                        {
                                            Debug.LogError("Failed to parse function arguments: " + e.Message);
                                            functionCallArguments = new JObject();
                                        }
                                        Debug.Log("Function Call Arguments: " + functionCallArguments.ToString());

                                        if (functionCallArgumentsEvents != null)
                                        {
                                            functionCallArgumentsEvents.RaiseEvent(functionCallArguments);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    isDone = true;
                },
                onError: (error) =>
                {
                    Debug.LogError("RetrieveRun GET 실패: " + error);
                    isDone = true;
                }
            ));

            yield return new WaitUntil(()=> isDone);

            if (runStatus == "completed" || runStatus == "requires_action")
            {
                Debug.Log("[RunStatus] "+runStatus);
                break;
            }
            else
            {
                Debug.Log("Current Run Status: " + runStatus);
                yield return new WaitForSeconds(retrieveWaitTime);
            }
        }
    }

    private IEnumerator SubmitToolOutputsToRun()
    {
        if (string.IsNullOrEmpty(threadID))
        {
            Debug.LogError("Thread ID Error");
            yield break;
        }

        if (string.IsNullOrEmpty(runID))
        {
            Debug.LogError("Run ID Error");
            yield break;
        }

        bool isDone = false;

        // Construct the JSON payload with the tool outputs
        // Assumes that functionCallID has been stored from RetrieveRun() and functionCallArguments is a JObject
        JObject requestBody = new JObject
        {
            ["tool_outputs"] = new JArray(
                new JObject
                {
                    ["tool_call_id"] = functionCallID,
                    ["output"] = "Success"//functionCallArguments
                }
            )
        };

        yield return StartCoroutine(apiManager.PostRequest(
            APIUrls.SubmitToolOutputsToRunUrl(threadID, runID),
            requestBody.ToString(),
            onSuccess: (response) =>
            {
                Debug.Log("Submit Tool Outputs 성공: " + response);
                isDone = true;
            },
            onError: (error) =>
            {
                Debug.LogError("Submit Tool Outputs 실패: " + error);
                isDone = true;
            }
        ));

        yield return new WaitUntil(() => isDone);
    }

    private IEnumerator DeleteThread()
    {
        if (string.IsNullOrEmpty(threadID))
        {
            Debug.LogError("Thread ID Error");
            yield break;
        }

        bool isDone = false;

        yield return StartCoroutine(apiManager.DeleteRequest(
            APIUrls.DeleteThreadUrl(threadID),
            onSuccess: (response) =>
            {
                Debug.Log("Delete Thread 성공: " + response);
                isDone = true;
            },
            onError: (error) =>
            {
                Debug.LogError("Delete Thread 실패: " + error);
                isDone = true;
            }
        ));

        yield return new WaitUntil(() => isDone);
    }
}