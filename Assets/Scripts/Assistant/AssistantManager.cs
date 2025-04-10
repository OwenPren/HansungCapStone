using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Security.Principal;
using System;

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

public class AssistantManager : MonoBehaviour
{
    public GameStartEventSO gameStartEvent;
    public RoundStartEventSO roundStartEvent;
    public GameEndEventSO gameEndEvent;

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
        //라운드 시작시 어시스턴트로 부터 사건 생성 요청
        yield return StartCoroutine(GenerationEvent());
        //생성된 사건으로부터 주가 정보 생성
        yield return StartCoroutine(StockPriceAdjustment());
    }

    private void OnGameStart()
    {
        //게임 시작시 쓰레드 생성
        if (!IsThread)
        {
            StartCoroutine(StartThread());
        }
    }

    private void OnGameEnd()
    {
        //게임 종료시 발생. 쓰레드 제거 및 변수 초기화 진행
        if (IsThread && !string.IsNullOrEmpty(threadID))
        {
            StartCoroutine(DeleteThread());
        }

        IsThread = false;
        threadID = "";
        runID = "";
        messageID = "";
        runStatus = "";
        functionCallID = "";
        functionCallArguments = null;
        runInProgress = false;
    }

    private IEnumerator GenerationEvent()
    {
        // 구성 요소 설명:
        // specialEventInfo: 현재 활성화된 특별 이벤트 정보 (없으면 빈 문자열)
        // generateSpecialEvent: 이번 라운드에 특별 이벤트를 생성할지 여부
        // generateUnexpectedEvent: 이번 라운드에 예기치 않은 이벤트를 포함할지 여부
        // eventSectors: 생성할 일반 이벤트의 분야 (랜덤하게 1~3개 선택됨)
        
        // 랜덤으로 1~3개 분야 선택
        int numberOfSectors = UnityEngine.Random.Range(1, 4); // 1 ~ 3
        SectorType[] allSectors = (SectorType[])Enum.GetValues(typeof(SectorType));
        List<SectorType> sectorsList = new List<SectorType>(allSectors);
        
        // Fisher-Yates 셔플
        for (int i = sectorsList.Count - 1; i > 0; i--) {
            int j = UnityEngine.Random.Range(0, i + 1);
            SectorType temp = sectorsList[i];
            sectorsList[i] = sectorsList[j];
            sectorsList[j] = temp;
        }
        
        List<SectorType> chosenSectors = sectorsList.GetRange(0, numberOfSectors);
        JArray eventSectorsArray = new JArray();
        foreach (var sector in chosenSectors) {
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
        
        // 어시스턴트에게 이벤트 생성 요청 (입력 값은 JSON 문자열로 변환되어 전송됨)
        yield return StartCoroutine(GenarationRoutine("user", inputParameters.ToString(), APIUrls.EventGenerationAssistantID, toolChoiceObject));
    }

    private IEnumerator StockPriceAdjustment()
    {
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
        
        //생성된 메시지의 id 조회 - RetrieveRun 단계에서 메세지 생성 대기 및 function call이 반환되므로 사용 할 필요 없음. 
        //yield return StartCoroutine(ListMessage());
        //yield return StartCoroutine(RetrieveMessage());

    }

    private IEnumerator StartThread()
    {
        bool isDone = false;
        //쓰레드 생성, 쓰레드 ID 저장
        yield return StartCoroutine(apiManager.PostRequest(
            APIUrls.CreateThreadURL,
            "{}",
            onSuccess: (response) => {
                Debug.Log("Create Thread POST 성공: " + response);

                //쓰레드 활성 및 ID 저장
                IsThread = true;

                JObject jobj = JObject.Parse(response);
                threadID = jobj["id"].ToString();
                Debug.Log("threadID: " + threadID);
                isDone = true;
            },
            onError: (error) =>
            {
                Debug.LogError("Create Thread POST 실패: " + error);
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

    private IEnumerator ListMessage()
    {
        if (string.IsNullOrEmpty(threadID))
        {
            Debug.LogError("Thread ID Error");
            yield break;
        }
        
        bool isDone = false;

        yield return StartCoroutine(apiManager.GetRequest(
            APIUrls.ListMessageUrl(threadID),
            onSuccess: (response) =>
            {
                Debug.Log("ListMessage GET 성공: " + response);

                // 응답을 JObject로 파싱
                JObject jObj = JObject.Parse(response);

                // "first_id" 추출하여 messageId에 저장
                messageID = jObj["first_id"]?.ToString();
                Debug.Log("first messageId: " + messageID);

                isDone = true;
            },
            onError: (error) =>
            {
                Debug.LogError("ListMessage GET 실패: " + error);
                isDone = true;
            }
        ));

        // 요청이 끝날 때까지 대기
        yield return new WaitUntil(() => isDone);

    }

    private IEnumerator RetrieveMessage()
    {
        if (string.IsNullOrEmpty(threadID))
        {
            Debug.LogError("Thread ID Error");
            yield break;
        }

        if (string.IsNullOrEmpty(messageID))
        {
            Debug.LogError("Message ID Error");
            yield break;
        }

        bool isDone = false;

        yield return StartCoroutine(apiManager.GetRequest(
            APIUrls.RetrieveMessageUrl(threadID, messageID),
            onSuccess: (response) =>
            {
                Debug.Log("RetrieveMessage GET 성공: " + response);

                // 응답 JSON 파싱
                JObject jObj = JObject.Parse(response);

                // 예: role, content, etc.
                string role = jObj["role"]?.ToString();
                string contentObj = jObj["content"]?.ToString();
                //string contentValue = contentObj?["value"]?.ToString();

                Debug.Log($"Message Role: {role}, Content: {contentObj}");


                isDone = true;
            },
            onError: (error) =>
            {
                Debug.LogError("RetrieveMessage GET 실패: " + error);
                isDone = true;
            }
        ));

        yield return new WaitUntil(() => isDone);
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
