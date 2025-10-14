using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("UI 연결")]
    [Tooltip("안내 문구가 표시될 TextMeshPro UI 오브젝트")]
    public TextMeshProUGUI instructionText;
    [Tooltip("하단에 고정된 '다음' 안내 텍스트 게임 오브젝트")]
    public GameObject nextPromptObject;

    [Header("메시지 내용")]
    [Tooltip("표시할 안내 문장들을 순서대로 입력하세요.")]
    [TextArea(3, 10)]
    public string[] messages;

    [Header("타이핑 효과 설정")]
    [Tooltip("한 글자가 표시되는 시간 (초)")]
    public float typingSpeed = 0.05f;

    public UnityEvent OnInstructionFinished;

    private int _currentMessageIndex = 0;
    private Coroutine _typingCoroutine;
    private bool _isTyping = false;

    // [추가] 이중 입력을 막기 위한 플래그 변수
    private bool _inputCooldown = false;

    void Start()
    {
        if (instructionText == null)
        {
            Debug.LogError("Instruction Text가 할당되지 않았습니다!");
            return;
        }

        if(nextPromptObject != null)
        {
            nextPromptObject.SetActive(true);
        }

        instructionText.gameObject.SetActive(true);
        StartTyping(messages[_currentMessageIndex]);
    }

    public void OnNextPressed()
    {
        // [수정] 쿨다운 중이라면 함수를 즉시 종료시켜 이중 입력을 방지
        if (_inputCooldown)
        {
            return;
        }

        Debug.Log("OnNextPressed 함수가 " + this.gameObject.name + "에 의해 호출됨!");

        if (_isTyping)
        {
            StopCoroutine(_typingCoroutine);
            instructionText.text = messages[_currentMessageIndex];
            _isTyping = false;
        }
        else
        {
            _currentMessageIndex++;

            if (_currentMessageIndex < messages.Length)
            {
                StartTyping(messages[_currentMessageIndex]);
            }
            else
            {
                instructionText.gameObject.SetActive(false);
                if (nextPromptObject != null)
                {
                    nextPromptObject.SetActive(false);
                }
                Debug.Log("모든 안내가 종료되었습니다.");
                OnInstructionFinished.Invoke();
            }
        }
    }

    private void StartTyping(string message)
    {
        _typingCoroutine = StartCoroutine(TypeText(message));
    }

    private IEnumerator TypeText(string message)
    {
        // [수정] 코루틴이 시작될 때 쿨다운을 활성화하고 끝날 때 비활성화
        _inputCooldown = true;
        _isTyping = true;
        instructionText.text = "";
        foreach (char letter in message.ToCharArray())
        {
            instructionText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        _isTyping = false;
        
        // 0.1초 후에 쿨다운을 풀어 다음 입력을 받을 수 있게 함
        yield return new WaitForSeconds(0.1f);
        _inputCooldown = false;
    }
}
