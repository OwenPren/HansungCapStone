using UnityEngine;
using TMPro; // TextMeshPro를 사용하므로 이 네임스페이스가 필요합니다.

public class PasswordInputFieldToggle : MonoBehaviour
{
    public TMP_InputField passwordInputField; // TextMeshPro InputField 참조

    private string initialPlaceholderText; // 초기 Placeholder 텍스트 저장

    void Start()
    {
        if (passwordInputField == null)
        {
            Debug.LogError("Password InputField is not assigned in the Inspector!");
            return;
        }

        if (passwordInputField.placeholder is TextMeshProUGUI placeholderTMPText)
        {
            initialPlaceholderText = placeholderTMPText.text;
        }
        else
        {
            Debug.LogWarning("Placeholder TextMeshProUGUI component not found for TMP_InputField.");
        }

        // InputField에 포커스가 맞춰졌을 때 (클릭 시) 호출될 리스너 추가
        passwordInputField.onSelect.AddListener(OnInputFieldSelect);

        // InputField에서 포커스가 벗어났을 때 호출될 리스너 추가
        passwordInputField.onDeselect.AddListener(OnInputFieldDeselect);
    }

    // InputField가 선택되었을 때 호출되는 함수
    private void OnInputFieldSelect(string arg0)
    {
        passwordInputField.contentType = TMP_InputField.ContentType.Password;
        passwordInputField.ForceLabelUpdate();
    }

    // InputField에서 포커스가 벗어났을 때 호출되는 함수
    private void OnInputFieldDeselect(string arg0)
    {
        // 입력된 텍스트가 없으면 ContentType을 다시 Standard로 되돌리고
        // Placeholder 텍스트를 원래대로 복원합니다.
        if (string.IsNullOrEmpty(passwordInputField.text))
        {
            passwordInputField.contentType = TMP_InputField.ContentType.Standard;
            passwordInputField.ForceLabelUpdate(); // 다시 UI 업데이트

            if (passwordInputField.placeholder is TextMeshProUGUI placeholderTMPText)
            {
                placeholderTMPText.text = initialPlaceholderText;
            }
        }
        else
        {
            // 텍스트가 입력되어 있다면, 계속 Password 모드를 유지합니다.
            passwordInputField.contentType = TMP_InputField.ContentType.Password;
            passwordInputField.ForceLabelUpdate();
        }
    }

    void OnDestroy()
    {
        // 리스너를 제거하여 메모리 누수 방지
        if (passwordInputField != null)
        {
            passwordInputField.onSelect.RemoveListener(OnInputFieldSelect);
            passwordInputField.onDeselect.RemoveListener(OnInputFieldDeselect);
        }
    }
}