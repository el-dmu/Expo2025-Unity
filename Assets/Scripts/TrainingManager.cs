using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement; // [★★★ SceneManagement 네임스페이스 추가 ★★★]
using UnityEngine.InputSystem;

[System.Serializable]
public struct TaskInstruction
{
    public string taskName;
    [TextArea(3, 5)] public string instructionText;
    public AudioClip instructionClip;
}

public class TrainingManager : MonoBehaviour
{
    [Header("1. UI 및 제어 스크립트 연결")]
    public UIManager uiManager;
    public VrWaypointWalker waypointWalker;

    [Header("2. 소화기 및 컨트롤러 연결")]
    public Transform fireExtinguisher;
    public Transform extinguisherSpawnPoint;
    public FireExtinguisherController extinguisherController;

    [Header("3. 데이터 기록 연결")]
    public DataManager dataManager;

    [Header("4. 디버그 모드 (Debug Mode)")]
    public bool debugMode = false;
    public Transform debugStartWaypoint;
    public Transform debugExtinguisherSpawnPoint;

    [Header("5. 안내 문구 설정")]
    public List<TaskInstruction> taskInstructions;
    [Tooltip("훈련이 성공적으로 종료되었을 때 재생할 오디오 클립")]
    public AudioClip trainingCompleteClip; // [★★★ 훈련 종료 오디오 변수 추가 ★★★]

    private bool hasPathStarted = false;
    private bool isReadyToStart = false;
    private bool isWaitingForNarration = false;
    private bool isFireMissionActive = false;
    private bool isInstructionPlaying = false;
    private string currentInstructionTaskName = "";
    private bool isWaitingForFireMissionIntro = false;
    private bool isRecordingData = false;

    // [★★★ 상태 변수 2개 추가 ★★★]
    private bool isTrainingFinished = false; // 훈련이 종료되었는지 (나레이션 재생 중)
    private bool isReadyToReturnToTitle = false; // 씬 복귀를 위해 키 입력을 대기 중인지

    private List<string> fireMissionTasks = new List<string> { "소화기 잡기", "안전핀 뽑기", "호스 잡기", "레버 눌러 분사" };
    private Dictionary<string, bool> taskCompletionStatus = new Dictionary<string, bool>();
    private Dictionary<string, TaskInstruction> instructionMap;

    void Awake()
    {
        instructionMap = taskInstructions.ToDictionary(item => item.taskName);
        if (extinguisherController == null)
        {
            Debug.LogError("[TrainingManager] Extinguisher Controller가 연결되지 않았습니다! 인스펙터 창을 확인해주세요.", this.gameObject);
        }
        else
        {
            // [정상] 디버그 모드가 아닐 때의 초기 잠금 상태
            // (Wp1->Wp2 이동을 위해 '잡기'만 해제)
            extinguisherController.LockGrabbing(false);
            extinguisherController.LockPinPull(true);
            extinguisherController.LockHoseGrab(true);
            extinguisherController.LockLeverSqueeze(true);
        }
    }

    void OnEnable()
    {
        FireExtinguisherController.OnGrabbed += HandleExtinguisherGrabbed;
        FireExtinguisherController.OnPinPulled += HandlePinPulled;
        FireExtinguisherController.OnHoseGrabbed += HandleHoseGrabbed;
        FireExtinguisherController.OnLeverSqueezed += HandleLeverSqueezed;
        FireManager.OnAllFiresExtinguished += HandleAllFiresExtinguished;
        FireManager.OnFireCountUpdate += HandleFireCountUpdate;
        if (uiManager != null)
        {
            uiManager.OnSingleInstructionFinished.AddListener(HandleSingleInstructionFinished);
            uiManager.OnNarrationFinished.AddListener(HandleNarrationFinished);
        }
    }

    void OnDisable()
    {
        FireExtinguisherController.OnGrabbed -= HandleExtinguisherGrabbed;
        FireExtinguisherController.OnPinPulled -= HandlePinPulled;
        FireExtinguisherController.OnHoseGrabbed -= HandleHoseGrabbed;
        FireExtinguisherController.OnLeverSqueezed -= HandleLeverSqueezed;
        FireManager.OnAllFiresExtinguished -= HandleAllFiresExtinguished;
        FireManager.OnFireCountUpdate -= HandleFireCountUpdate;
        if (uiManager != null)
        {
            uiManager.OnSingleInstructionFinished.RemoveListener(HandleSingleInstructionFinished);
            uiManager.OnNarrationFinished.RemoveListener(HandleNarrationFinished);
        }
    }

    // (이하 Handle... 함수들은 변경 없음)
    private void HandlePinPulled()
    {
        if (isInstructionPlaying || isWaitingForNarration) return;
        UpdateChecklist("안전핀 뽑기");
    }

    private void HandleHoseGrabbed()
    {
        if (isInstructionPlaying || isWaitingForNarration) return;
        UpdateChecklist("호스 잡기");
    }

    private void HandleLeverSqueezed()
    {
        if (isInstructionPlaying || isWaitingForNarration) return;
        UpdateChecklist("레버 눌러 분사");
    }

    private void HandleExtinguisherGrabbed()
    {
        if (isFireMissionActive && !isRecordingData && dataManager != null)
        {
            isRecordingData = true;
            dataManager.StartRecording();
            Debug.Log("<color=blue>[TrainingManager] 소화기 잡기 감지. 센서 데이터 기록을 시작합니다.</color>");
        }

        if (isInstructionPlaying || isWaitingForNarration) return;

        if (isFireMissionActive)
        {
            bool taskWasAlreadyCompleted = IsTaskCompleted("소화기 잡기");
            UpdateChecklist("소화기 잡기");

            if (!taskWasAlreadyCompleted && waypointWalker != null && !waypointWalker.IsMoving)
            {
                Debug.Log("<color=yellow>[TrainingManager] '소화기 잡기' 완료. 다음 웨이포인트로 이동합니다.</color>");
                ProceedToNextWaypoint();
            }
        }
        else if (waypointWalker != null && !waypointWalker.IsMoving)
        {
            // [정상] 디버그 모드 아닐 때 (Wp1 -> Wp2)
            ProceedToNextWaypoint();
        }
    }

    // (Start 함수는 변경 없음)
    void Start()
    {
        if (debugMode)
        {
            Debug.LogWarning("--- 디버그 모드가 활성화되었습니다 ---");
            if (uiManager != null) uiManager.CompleteInitialSequence();
            isReadyToStart = true;
            hasPathStarted = true;
            Transform spawnPoint = debugExtinguisherSpawnPoint != null ? debugExtinguisherSpawnPoint : extinguisherSpawnPoint;
            if (fireExtinguisher != null && spawnPoint != null)
            {
                fireExtinguisher.position = spawnPoint.position;
                fireExtinguisher.rotation = spawnPoint.rotation;
            }
            if (waypointWalker != null && debugStartWaypoint != null)
            {
                waypointWalker.transform.position = debugStartWaypoint.position;
                if (fireExtinguisher != null)
                {
                    Vector3 directionToLook = fireExtinguisher.position - waypointWalker.transform.position;
                    directionToLook.y = 0;
                    waypointWalker.transform.rotation = Quaternion.LookRotation(directionToLook);
                }
                else waypointWalker.transform.rotation = debugStartWaypoint.rotation;
                int startIndex = waypointWalker.waypoints.IndexOf(debugStartWaypoint);
                if (startIndex != -1) waypointWalker.SetupDebugStart(startIndex);
                else
                {
                    Debug.LogError($"Debug Start Waypoint '{debugStartWaypoint.name}'가 VrWaypointWalker의 waypoints 리스트에 없습니다!", this.gameObject);
                    return;
                }
            }
            else
            {
                Debug.LogError("디버그 모드를 시작하려면 Waypoint Walker와 Debug Start Waypoint가 모두 연결되어야 합니다!", this.gameObject);
                return;
            }
            StartFireMissionForDebug();
        }
        else
        {
            // [정상] 디버그 모드 아닐 때의 시작 흐름
            if (uiManager != null)
            {
                uiManager.OnInstructionFinished.AddListener(HandleInstructionFinished);
            }
            else isReadyToStart = true;

            if (fireExtinguisher != null && extinguisherSpawnPoint != null)
            {
                fireExtinguisher.position = extinguisherSpawnPoint.position;
                fireExtinguisher.rotation = extinguisherSpawnPoint.rotation;
            }
        }
    }

    void Update()
    {
        if (isReadyToReturnToTitle)
        {
            // 키보드 또는 마우스 입력 확인 (null 체크 포함)
            bool keyboardPressed = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
            bool mousePressed = Mouse.current != null &&
                                (Mouse.current.leftButton.wasPressedThisFrame ||
                                 Mouse.current.rightButton.wasPressedThisFrame ||
                                 Mouse.current.middleButton.wasPressedThisFrame);

            if (keyboardPressed || mousePressed)
            {
                isReadyToReturnToTitle = false; // 중복 실행 방지
                LoadTitleScene(); // 씬 로드 함수 호출
            }
        }


        if (isFireMissionActive && !isInstructionPlaying && uiManager != null && FireManager.Instance != null && FireManager.Instance != null)
        {
            float overallProgress = FireManager.Instance.GetOverallProgress();
            uiManager.UpdateFirePercentage(overallProgress * 100f);
        }
    }

    private void StartFireMissionForDebug()
    {
        if (debugStartWaypoint == null || uiManager == null) return;
        WaypointInfo debugWaypointInfo = debugStartWaypoint.GetComponent<WaypointInfo>();
        if (debugWaypointInfo == null || !debugWaypointInfo.isFirefightingZone)
        {
            Debug.LogError("Debug Start Waypoint에 WaypointInfo가 없거나 isFirefightingZone이 체크되지 않았습니다!", this.gameObject);
            return;
        }

        isFireMissionActive = true;
        foreach (var task in fireMissionTasks) taskCompletionStatus[task] = false;

        isWaitingForFireMissionIntro = true;

        LockAllActions(true);

        uiManager.ShowMessage(debugWaypointInfo.message, debugWaypointInfo.narrationClip, "");
    }

    public void HandleInstructionFinished()
    {
        // [정상] UIManager의 초기 안내 종료 시 호출됨 (디버그 모드 아닐 때)
        isReadyToStart = true;
    }

    public void HandleNarrationFinished()
    {
        isWaitingForNarration = false;

        // [★★★ 훈련 종료 확인 로직 수정 ★★★]
        if (isTrainingFinished)
        {
            isTrainingFinished = false; // 중복 실행 방지
            isReadyToReturnToTitle = true; // 씬 로드 대신 키 입력 대기 상태로 변경

            // UIManager에 프롬프트 표시 요청
            if (uiManager != null)
            {
                // UIManager.cs에 추가한 ShowReturnPrompt 함수를 호출
                uiManager.ShowReturnPrompt("아무 키나 눌러 타이틀로 돌아가세요.");
            }
            return; // 다른 로직을 실행하지 않고 종료
        }
        // [★★★ 여기까지 수정 ★★★]


        if (isWaitingForFireMissionIntro)
        {
            isWaitingForFireMissionIntro = false;
            Debug.Log("<color=yellow>[TrainingManager] 화재 미션 소개(Wp4) 나레이션 종료. 첫 번째 안내('소화기 잡기')를 시작합니다.</color>");

            uiManager?.HideMessage();
            ShowNextInstruction();
        }
        // [정상] 디버그 모드 아닐 때 (Wp1, Wp2...) 나레이션 종료 시엔 아무것도 안 함
    }

    private void HandleSingleInstructionFinished()
    {
        string finishedTask = currentInstructionTaskName;

        isInstructionPlaying = false;
        currentInstructionTaskName = "";

        if (uiManager != null && isFireMissionActive)
        {
            uiManager.ShowChecklist(taskCompletionStatus, FireManager.Instance.InitialFireCount);
            HandleFireCountUpdate(FireManager.Instance.ActiveFireCount, FireManager.Instance.InitialFireCount);
            Update();
        }

        if (extinguisherController == null)
        {
            Debug.LogError("[TrainingManager] Extinguisher Controller가 null입니다! 물리 잠금을 해제할 수 없습니다.");
            return;
        }

        // 방금 끝난 오디오에 해당하는 행동의 '잠금'을 '해제'
        switch (finishedTask)
        {
            case "소화기 잡기":
                Debug.Log("<color=green>[TrainingManager] '소화기 잡기' 안내 종료. 잡기 잠금을 해제합니다.</color>");
                extinguisherController.LockGrabbing(false);
                break;
            case "안전핀 뽑기":
                Debug.Log("<color=green>[TrainingManager] '안전핀 뽑기' 안내 종료. 핀 잠금을 해제합니다.</color>");
                extinguisherController.LockPinPull(false);
                break;
            case "호스 잡기":
                Debug.Log("<color=green>[TrainingManager] '호스 잡기' 안내 종료. 호스 잠금을 해제합니다.</color>");
                extinguisherController.LockHoseGrab(false);
                break;
            case "레버 눌러 분사":
                Debug.Log("<color=green>[TrainingManager] '레버 분사' 안내 종료. 레버 잠금을 해제합니다.</color>");
                extinguisherController.LockLeverSqueeze(false);
                break;
        }

        // [데드락 방지]
        if (finishedTask == "소화기 잡기" && extinguisherController.IsHeld && !IsTaskCompleted("소화기 잡기"))
        {
            HandleExtinguisherGrabbed();
        }
        else if (finishedTask == "안전핀 뽑기" && extinguisherController.IsPinPulled && !IsTaskCompleted("안전핀 뽑기"))
        {
            HandlePinPulled();
        }
        else if (finishedTask == "호스 잡기" && extinguisherController.IsHoseGrabbed && !IsTaskCompleted("호스 잡기"))
        {
            HandleHoseGrabbed();
        }
    }

    public void HandleSwipeInput()
    {
        // [정상] 디버그 모드 아닐 때, UIManager 초기 안내 종료 후 스와이프
        if (!isReadyToStart || isWaitingForNarration || isInstructionPlaying) return;

        if (!hasPathStarted)
        {
            StartMovementOnly();
        }
    }

    void StartMovementOnly()
    {
        // [정상] 스와이프 시 1번 웨이포인트로 이동 시작
        if (hasPathStarted) return;
        hasPathStarted = true;
        if (waypointWalker != null) waypointWalker.StartPath();
    }

    public void ShowWaypointMessage(string message)
    {
        if (waypointWalker?.CurrentWaypoint == null || uiManager == null) return;
        Debug.Log($"<color=yellow>[TrainingManager] '{waypointWalker.CurrentWaypoint.name}' 도착. ShowWaypointMessage 호출됨.</color>");
        isWaitingForNarration = true;
        WaypointInfo currentWaypoint = waypointWalker.CurrentWaypoint;

        // [정상] Wp4 (화재 구역) 도착 시 분기
        if (currentWaypoint.isFirefightingZone)
        {
            if (!isFireMissionActive)
            {
                Debug.Log($"<color=yellow>[TrainingManager] 화재 진압 구역 '{currentWaypoint.name}' 도착. 미션을 시작합니다.</color>");
                isFireMissionActive = true;
                foreach (var task in fireMissionTasks) taskCompletionStatus[task] = false;

                isWaitingForFireMissionIntro = true;

                LockAllActions(true);

                uiManager.ShowMessage(message, currentWaypoint.narrationClip, "");
            }
            else
            {
                Debug.Log($"<color=yellow>[TrainingManager] 이미 화재 미션 중. '{currentWaypoint.name}'의 메시지를 표시하지 않습니다.</color>");
                isWaitingForNarration = false;
            }

            return;
        }

        // [정상] Wp1, Wp2, Wp3 (일반 구역) 도착 시
        string prompt = "소화기를 잡아서 다음 지점으로 이동하세요.";
        uiManager.ShowMessage(message, currentWaypoint.narrationClip, prompt);
    }

    private void ShowNextInstruction()
    {
        string nextTask = fireMissionTasks.FirstOrDefault(task => !IsTaskCompleted(task));

        if (nextTask != null && instructionMap.ContainsKey(nextTask))
        {
            Debug.Log($"<color=yellow>[TrainingManager] 다음 안내 '{nextTask}' 표시를 요청합니다.</color>");
            isInstructionPlaying = true;
            currentInstructionTaskName = nextTask;

            LockAllActions(true); // [★★★ 핵심 수정 ★★★] (이 함수가 수정됨)

            TaskInstruction instruction = instructionMap[nextTask];
            uiManager.HideChecklist();
            uiManager.ShowInstruction(instruction.instructionText, instruction.instructionClip);
        }
        else
        {
            Debug.Log("[TrainingManager] 모든 안내가 완료되었습니다.");
            currentInstructionTaskName = "";
        }
    }

    // [★★★ 핵심 수정 ★★★]
    // 잠금을 실행(true)할 때, '이미 완료된' 태스크는 다시 잠그지 않습니다.
    private void LockAllActions(bool lockState)
    {
        if (extinguisherController == null) return;

        // lockState가 false이면(해제) 무조건 모두 해제 (현재 이 분기는 안 쓰임)
        if (!lockState)
        {
            extinguisherController.LockGrabbing(false);
            extinguisherController.LockPinPull(false);
            extinguisherController.LockHoseGrab(false);
            extinguisherController.LockLeverSqueeze(false);
            return;
        }

        // --- lockState가 true일 때 (잠그기) ---

        // 1. 소화기 잡기: '소화기 잡기'가 아직 완료 안 됐으면 잠금
        if (!IsTaskCompleted("소화기 잡기"))
        {
            extinguisherController.LockGrabbing(true);
        }
        else
        {
            Debug.Log("<color=cyan>[TrainingManager] '소화기 잡기'가 이미 완료되어, 잡기 잠금을 건너뜁니다.</color>");
        }

        // 2. 핀 뽑기: '핀 뽑기'가 아직 완료 안 됐으면 잠금
        if (!IsTaskCompleted("안전핀 뽑기"))
        {
            extinguisherController.LockPinPull(true);
        }

        // 3. 호스 잡기: '호스 잡기'가 아직 완료 안 됐으면 잠금
        if (!IsTaskCompleted("호스 잡기"))
        {
            extinguisherController.LockHoseGrab(true);
        }
        else
        {
            Debug.Log("<color=cyan>[TrainingManager] '호스 잡기'가 이미 완료되어, 호스 잠금을 건너뜁니다.</color>");
        }

        // 4. 레버 누르기: '레버 누르기'가 아직 완료 안 됐으면 잠금
        if (!IsTaskCompleted("레버 눌러 분사"))
        {
            extinguisherController.LockLeverSqueeze(true);
        }
    }


    private IEnumerator ShowNextInstructionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowNextInstruction();
    }

    private bool IsTaskCompleted(string task)
    {
        return taskCompletionStatus.ContainsKey(task) && taskCompletionStatus[task];
    }

    private void UpdateChecklist(string task)
    {
        if (IsTaskCompleted(task))
        {
            Debug.LogWarning($"[TrainingManager] 태스크 '{task}'는 이미 완료되었습니다. 중복 호출을 무시합니다.");
            return;
        }

        taskCompletionStatus[task] = true;
        uiManager?.UpdateChecklistItem(task, true);
        StartCoroutine(ShowNextInstructionAfterDelay(0.5f));
    }

    // [★★★ 이 함수 수정됨 ★★★]
    private void HandleAllFiresExtinguished()
    {
        isFireMissionActive = false;
        isTrainingFinished = true; // [★★★ 씬 이동 대신 상태 변수 true로 변경 ★★★]

        if (extinguisherController != null && fireExtinguisher != null && extinguisherSpawnPoint != null)
        {
            // 1. 컨트롤러에게 강제로 손에서 놓으라고 명령
            extinguisherController.ForceRelease();

            // 2. 소화기의 Transform을 스폰 위치로 즉시 이동
            fireExtinguisher.position = extinguisherSpawnPoint.position;
            fireExtinguisher.rotation = extinguisherSpawnPoint.rotation;

            // 3. (중요) 순간이동 후 물리적 힘(속도)을 0으로 초기화
            Rigidbody extinguisherRb = fireExtinguisher.GetComponent<Rigidbody>();
            if (extinguisherRb != null)
            {
                extinguisherRb.velocity = Vector3.zero;
                extinguisherRb.angularVelocity = Vector3.zero;
                extinguisherRb.useGravity = false; // 중력 비활성화
            }
            Debug.Log("<color=magenta>[TrainingManager] 소화기를 스폰 위치로 되돌렸습니다.</color>");
        }
        // [★★★ 여기까지 추가 ★★★]
        if (isRecordingData && dataManager != null)
        {
            isRecordingData = false;
            string motionName = "소화기 사용 훈련"; // 동작 이름 지정

            // SessionManager에서 사번 가져오기
            string empNo = "UNKNOWN";
            if (SessionManager.Instance != null)
            {
                empNo = SessionManager.Instance.employeeID;
            }
            else
            {
                Debug.LogWarning("SessionManager를 찾을 수 없습니다. 기본 사번 'UNKNOWN'을 사용합니다.");
            }

            dataManager.StopRecordingAndSend(motionName, empNo);
            Debug.Log("<color=blue>[TrainingManager] 모든 화재 진압. 센서 데이터 기록을 중지하고 전송합니다.</color>");
        }

        if (uiManager != null)
        {
            uiManager.HideChecklist();

            // [★★★ 수정: 오디오 클립 변수 사용, 프롬프트는 ""(빈칸)으로 변경 ★★★]
            // 나레이션이 끝난 후 HandleNarrationFinished에서 별도 프롬프트를 띄울 것이므로
            uiManager.ShowMessage("모든 화재를 진압했습니다. 훈련을 종료합니다.", trainingCompleteClip, "");
        }
        else
        {
            // [★★★ UIManager가 없을 경우 예외 처리 ★★★]
            Invoke("LoadTitleScene", 0.5f);
        }
    }

    private void HandleFireCountUpdate(int remaining, int initial)
    {
        if (uiManager != null && isFireMissionActive)
        {
            uiManager.UpdateFireCount(remaining, initial);
        }
    }

    private void ProceedToNextWaypoint()
    {
        uiManager?.HideMessage();
        waypointWalker?.ContinueToNextWaypoint();
    }

    // [★★★ 씬 로드 함수 추가 ★★★]
    private void LoadTitleScene()
    {
        if (uiManager != null)
        {
            // UIManager.cs에 추가할 HideReturnPrompt() 또는 기존 HideMessage() 호출
            uiManager.HideReturnPrompt();
        }

        Debug.Log("<color=magenta>[TrainingManager] 훈련 종료. TitleScene으로 이동합니다.</color>");
        SceneManager.LoadScene("TitleScene");
    }
}