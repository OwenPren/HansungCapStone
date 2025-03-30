public static class APIUrls
{
    //Assistant ID
    public static string StockPriceAdjustmentAssistantID => "asst_VWqe8AY39IQHM6a4CcRItHcL";
    public static string EventGenerationAssistantID => "asst_WwO0WtgWWrkGpvYgv51TBFcr"; 

    //Thread API url
    public static string CreateThreadURL => "https://api.openai.com/v1/assistants";

    //Message API url
    public static string CreateMessageUrl(string thread_id)
    {
        return $"https://api.openai.com/v1/threads/{thread_id}/messages";
    }

    public static string RetrieveMessageUrl(string thread_id, string message_id)
    {
        return $"https://api.openai.com/v1/threads/{thread_id}/messages/{message_id}";
    }

    public static string ListMessageUrl(string thread_id)
    {
        return $"https://api.openai.com/v1/threads/{thread_id}/messages";
    }

    //Run API url
    public static string CreateRunUrl(string thread_id)
    {
        return $"https://api.openai.com/v1/threads/{thread_id}/runs";
    }

    public static string RetrieveRunUrl(string thread_id, string run_id)
    {
        return $"https://api.openai.com/v1/threads/{thread_id}/runs/{run_id}";
    }

    public static string SubmitToolOutputsToRunUrl(string thread_id, string run_id)
    {
        return $"https://api.openai.com/v1/threads/{thread_id}/runs/{run_id}/submit_tool_outputs";
    }

}
