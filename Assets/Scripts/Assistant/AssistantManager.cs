using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Security.Principal;

public class AssistantManager : MonoBehaviour
{
    public GameStartEventSO gameStartEvent;
    public APIManager apiManager;

    private bool IsThread = false;
    private string threadID = "";
    private string runID = "";
    private string messageID = "";
    private string runStatus = "";

    private void OnEnable()
    {
        gameStartEvent.OnGameStart += OnGameStart;
    }

    private void OnDisable()
    {
        gameStartEvent.OnGameStart -= OnGameStart;
    }

    private void OnGameStart()
    {
        if (!IsThread)
        {
            StartCoroutine(StartThread());
        }
    }

    private void GenerationEvent()
    {
        JObject toolChoiceObject = new JObject
        {
            ["type"] = "function",
            ["function"] = new JObject
            {
                ["name"] = "my_function"
            }
        };

        StartCoroutine(GenarationRoutine("user","{}",APIUrls.EventGenerationAssistantID,toolChoiceObject));
    }

    private IEnumerator GenarationRoutine(string role, string data, string assistantId, JObject toolChoice = null)
    {
        //메세지 생성 요청
        yield return StartCoroutine(CreateMessage(role,data));
        yield return StartCoroutine(CreateRun(assistantId,toolChoice));

        //메세지 생성 대기
        yield return StartCoroutine(RetrieveRun());
        
        //생성된 메시지의 id 조회
        yield return StartCoroutine(ListMessage());
        yield return StartCoroutine(RetrieveMessage());

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
                break;
            }
            else
            {
                Debug.Log("Current Run Status: " + runStatus);
                yield return new WaitForSeconds(2.0f);
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
                Debug.Log("messageId: " + messageID);

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
}



