using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

[System.Serializable]
public class MessageStep
{
    [TextArea(3, 5)] public string message;
    public string nextPromptText = "왼쪽으로 스와이프하여 다음으로...";
    public AudioClip narrationClip;
}

[RequireComponent(typeof(AudioSource))]
public class UIManager : MonoBehaviour
{
    [Header("UI 연결")]
    public TextMeshProUGUI mainText;
    public TextMeshProUGUI nextPromptTextUI;
    [Header("체크리스트 UI")]
    public GameObject checklistPanel;
    public TextMeshProUGUI checklistText;

    [Header("안내 문구 UI")]
    public GameObject instructionPanel;
    public TextMeshProUGUI instructionText;
    [Tooltip("오디오 클립이 없을 때, 안내 문구를 최소 이 시간(초)만큼 표시합니다.")]
    public float minInstructionTime = 2.0f; // [추가]

    [Header("초기 안내 메시지")]
    public MessageStep[] initialMessages;
    [Header("이벤트")]
    public UnityEvent OnInstructionFinished;
    public UnityEvent OnNarrationFinished;
    public UnityEvent OnSingleInstructionFinished;

    private AudioSource audioSource;
    private Coroutine typingCoroutine;
    private Dictionary<string, bool> checklistItems = new Dictionary<string, bool>();
    private int currentIndex = -1;
    private bool isInitialSequenceFinished = false;
    private int initialFireCount;
    private int extinguishedCount;
    private float currentPercentage;

    void Awake() => audioSource = GetComponent<AudioSource>();

    void Start()
    {
        if (checklistPanel != null) checklistPanel.SetActive(false);
        if (instructionPanel != null) instructionPanel.SetActive(false);

        if (isInitialSequenceFinished) return;
        if (initialMessages == null || initialMessages.Length == 0)
        {
            CompleteInitialSequence();
            return;
        }
        ShowNextInitialMessage();
    }

    public void ShowInstruction(string text, AudioClip clip)
    {
        if (instructionPanel == null || instructionText == null)
        {
            Debug.LogError("[UIManager] Instruction Panel 또는 Text가 연결되지 않았습니다! Inspector 창을 확인해주세요.", this.gameObject);
            return;
        }
        Debug.Log($"<color=cyan>[UIManager] '{text}' 안내 문구 표시 요청 받음.</color>");
        instructionText.text = text;
        instructionPanel.SetActive(true);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(InstructionSequence(clip));
    }

    // [수정] 오디오 클립(clip)이 null일 경우, minInstructionTime 만큼 강제로 대기
    private IEnumerator InstructionSequence(AudioClip clip)
    {
        audioSource.Stop();

        bool hasAudio = (clip != null);

        if (hasAudio)
        {
            audioSource.PlayOneShot(clip);
            while (audioSource.isPlaying) yield return null;
        }
        else
        {
            Debug.LogWarning($"[UIManager] Instruction 오디오 클립이 없습니다. {minInstructionTime}초 대기합니다.");
            yield return new WaitForSeconds(minInstructionTime);
        }

        instructionPanel.SetActive(false);
        typingCoroutine = null;
        Debug.Log("<color=cyan>[UIManager] 단일 안내(TTS) 종료. 이벤트 호출.</color>");
        OnSingleInstructionFinished.Invoke();
    }

    public void ShowChecklist(Dictionary<string, bool> tasks, int totalFires)
    {
        if (checklistPanel == null || checklistText == null)
        {
            Debug.LogError("[UIManager] Checklist Panel 또는 Text가 연결되지 않았습니다! Inspector 창을 확인해주세요.", this.gameObject);
            return;
        }
        Debug.Log("<color=cyan>[UIManager] 체크리스트 표시 요청 받음.</color>");
        initialFireCount = totalFires;
        checklistItems = new Dictionary<string, bool>(tasks);

        checklistPanel.SetActive(true);
        UpdateChecklistDisplay();
    }

    // [수정] 인트로 메시지(ShowMessage)도 오디오 클립이 null일 때 대기
    IEnumerator TypeText(MessageStep step)
    {
        if (nextPromptTextUI != null) nextPromptTextUI.gameObject.SetActive(false);
        audioSource.Stop();

        mainText.text = step.message;

        bool hasAudio = (step.narrationClip != null);

        if (hasAudio)
        {
            audioSource.PlayOneShot(step.narrationClip);
            while (audioSource.isPlaying) yield return null;
        }
        else
        {
            Debug.LogWarning($"[UIManager] Message 오디오 클립이 없습니다. 2초 대기합니다.");
            yield return new WaitForSeconds(2.0f);
        }

        if (nextPromptTextUI != null && !string.IsNullOrEmpty(step.nextPromptText))
        {
            nextPromptTextUI.text = step.nextPromptText;
            nextPromptTextUI.gameObject.SetActive(true);
        }
        typingCoroutine = null;
        OnNarrationFinished.Invoke();
    }

    // --- 이하 변경 없음 ---
    public void HandleSwipeInput()
    {
        if (!isInitialSequenceFinished && typingCoroutine == null) ShowNextInitialMessage();
    }
    private void ShowNextInitialMessage()
    {
        if (isInitialSequenceFinished) return;
        currentIndex++;
        if (currentIndex < initialMessages.Length)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(initialMessages[currentIndex]));
        }
        else
        {
            CompleteInitialSequence();
        }
    }
    public void CompleteInitialSequence()
    {
        if (isInitialSequenceFinished) return;
        isInitialSequenceFinished = true;
        if (mainText != null) mainText.gameObject.SetActive(false);
        if (nextPromptTextUI != null) nextPromptTextUI.gameObject.SetActive(false);
        OnInstructionFinished.Invoke();
    }
    public void ShowMessage(string message, AudioClip clip, string prompt)
    {
        if (mainText == null) return;
        mainText.gameObject.SetActive(true);
        MessageStep step = new MessageStep { message = message, narrationClip = clip, nextPromptText = prompt };
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(step));
    }
    public void HideMessage()
    {
        if (mainText != null) mainText.gameObject.SetActive(false);
        if (nextPromptTextUI != null) nextPromptTextUI.gameObject.SetActive(false);
    }
    public void HideChecklist()
    {
        if (checklistPanel != null) checklistPanel.SetActive(false);
    }
    public void UpdateChecklistItem(string task, bool isCompleted)
    {
        if (checklistItems.ContainsKey(task))
        {
            checklistItems[task] = isCompleted;
            UpdateChecklistDisplay();
        }
    }
    public void UpdateFireCount(int remaining, int initial)
    {
        initialFireCount = initial;
        extinguishedCount = initial - remaining;
        UpdateChecklistDisplay();
    }
    public void UpdateFirePercentage(float percentage)
    {
        currentPercentage = percentage;
        UpdateChecklistDisplay();
    }
    private void UpdateChecklistDisplay()
    {
        string newText = "";
        foreach (var item in checklistItems)
        {
            string statusIcon = item.Value ? "<color=green>V</color>" : "O";
            newText += $"{statusIcon} {item.Key}\n";
        }
        newText += "\n──────────\n";
        newText += $"화재 진압 ({extinguishedCount}/{initialFireCount})\n";
        newText += $"화재 진압률 : {currentPercentage:F0}%";
        checklistText.text = newText;
    }
}