    using System;
    using System.IO.Ports;
    using System.Threading;
    using UnityEngine;

    public class HandControllerL : MonoBehaviour
    {
        [Header("연결 설정")]
        [Tooltip("장치 관리자에서 확인한 왼손 COM 포트 번호")]
        public string portName = "COM8";

        [Header("3D 모델 관절 연결")]
        [Tooltip("왼손 손목 또는 손바닥에 해당하는 루트 오브젝트")]
        public Transform handRoot;
        [Tooltip("제어할 5개의 손가락 관절 (엄지부터 새끼 순서로)")]
        public Transform[] fingerJoints = new Transform[5];

        [Header("움직임 캘리브레이션")]
        [Tooltip("각 손가락별로 플렉스 센서가 펴졌을 때의 값 (엄지부터)")]
        public int[] flexMin = new int[5];
        [Tooltip("각 손가락별로 플렉스 센서가 완전히 굽혀졌을 때의 값 (엄지부터)")]
        public int[] flexMax = new int[5];
        [Tooltip("손가락이 최대로 굽혀질 각도")]
        public float maxFingerAngle = 90.0f;


        [Header("데이터 외부 공개")]
        [Tooltip("DataManager가 현재 손의 회전값을 가져가기 위한 변수")]
        public Quaternion currentRotation;

        [Tooltip("DataManager가 현재 손가락 굽힘 값을 가져가기 위한 변수 (0~1)")]
        public float[] fingerValues = new float[5];

        public bool isGripping = false; // 주먹을 쥐었는지 여부

        // --- 내부 변수들 ---
    private SerialPort serialPort;
        private Thread dataReadThread;
        private bool isRunning = false;
        private string receivedString;
        private readonly object lockObject = new object();

        private int[] flexVals = new int[5];
        private float pitch = 0, roll = 0, yaw = 0;

        void Start()
        {
            try
            {
                serialPort = new SerialPort(portName, 115200) { ReadTimeout = 200 };
                serialPort.Open();
                isRunning = true;
                dataReadThread = new Thread(ReadDataThread);
                dataReadThread.IsBackground = true;
                dataReadThread.Start();
                Debug.Log($"✅ [왼손] Bluetooth-Serial 포트 연결 성공: {portName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ [왼손] Bluetooth-Serial 포트 연결 실패: {e.Message}");
            }
        }

        void Update()
        {
            string dataToProcess = null;
            lock (lockObject)
            {
                if (receivedString != null)
                {
                    dataToProcess = receivedString;
                    receivedString = null;
                }
            }

            if (dataToProcess != null)
            {
                ParseData(dataToProcess);
            }

            UpdateHandModel();
        }

        private void ReadDataThread()
        {
            while (isRunning && serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    string line = serialPort.ReadLine();
                    lock (lockObject)
                    {
                        receivedString = line;
                    }
                }
                catch (TimeoutException) { /* 무시 */ }
                catch (Exception e)
                {
                    if (isRunning) Debug.LogWarning($"⚠ [왼손] 데이터 수신 오류: {e.Message}");
                }
            }
        }

        private void ParseData(string data)
        {
            try
            {
                data = data.TrimStart(',');
                
                string[] parts = data.Split(',');
                if (parts.Length >= 8)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        if (i < parts.Length)
                        {
                            flexVals[i] = int.Parse(parts[i]);
                        }
                    }

                    pitch = float.Parse(parts[5]);
                    roll = float.Parse(parts[6]);
                    yaw = float.Parse(parts[7]);
                    
                    if (flexVals[0] > 2900 && flexVals[1] > 3300 &&
                    flexVals[2] > 3000 && flexVals[3] > 3200 &&
                    flexVals[4] > 3500)
                    {
                        isGripping = true;
                    }
                    else
                    {
                        isGripping = false;
                    }
                    // Debug.Log($"주먹을 쥐었나요?: {isGripping}\n flex1: {flexVals[0] > 2980}, flex2: {flexVals[1] > 3870}, flex3: {flexVals[2] > 3930}, flex4: {flexVals[3] > 3770}, flex5: {flexVals[4] > 150}");

                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"⚠ [왼손] 데이터 파싱 실패: {data} | 오류: {e.Message}");
            }
        }

        private void UpdateHandModel()
        {
            // --- 손목 회전 ---
            if (handRoot != null)
            {
                
                float finalWristX = 57.645f - pitch;
                float finalWristY = 135.26f - yaw;
                float finalWristZ = -69.727f + roll; 

                handRoot.localRotation = Quaternion.Euler(finalWristX, finalWristY, finalWristZ);
                currentRotation = handRoot.localRotation;
            }

            // --- 손가락 회전 ---
            for (int i = 0; i < fingerJoints.Length; i++)
            {
                // 안전 장치: fingerJoints 배열의 요소가 제대로 연결되어 있고, flexMin/flexMax 배열 범위 내에 있는지 확인
                if (fingerJoints[i] != null && i < flexMin.Length && i < flexMax.Length)
                {
                    // flexMin과 flexMax는 유니티 인스펙터에서 각 손가락에 맞게 입력해야 합니다.
                    float bendAmount = Mathf.InverseLerp(flexMin[i], flexMax[i], flexVals[i]); // 0 (펴짐) ~ 1 (최대 굽힘)
                    fingerValues[i] = bendAmount;
                    
                    float targetAngle = bendAmount * maxFingerAngle; // 최대 굽힘 각도까지 선형 보간

                    Quaternion initialRotation = Quaternion.identity; // 기본 회전값 초기화

                    switch (i)
                    {
                        case 0: // 엄지 (ThumbB_L)
                            initialRotation = Quaternion.Euler(0f, -0.005f, 17.365f); // Y: -0.005, Z: 17.365
                            fingerJoints[i].localRotation = initialRotation * Quaternion.Euler(0f, 0f, targetAngle);
                            break;

                        case 1: // 검지 (IndexA_L)
                            initialRotation = Quaternion.Euler(14.478f, 85.578f, 11.701f); // X:14.478, Y:85.578, Z:11.701
                            fingerJoints[i].localRotation = initialRotation * Quaternion.Euler(0f, 0f, targetAngle); // Z축으로 굽힘 (부호 조정)
                            break;

                        case 2: // 중지 (MiddleA_L)
                            initialRotation = Quaternion.Euler(3.078f, 99.098f, 9.556f); // X:3.078, Y:99.098, Z:9.556
                            fingerJoints[i].localRotation = initialRotation * Quaternion.Euler(0f, 0f, targetAngle); // Z축으로 굽힘 (부호 조정)
                            break;

                        case 3: // 약지 (RingA_L)
                            initialRotation = Quaternion.Euler(-7.489f, 109.398f, 6.735f); // X:-7.489, Y:109.398, Z:6.735
                            fingerJoints[i].localRotation = initialRotation * Quaternion.Euler(0f, 0f, targetAngle); // Z축으로 굽힘 (부호 조정)
                            break;

                        case 4: // 소지 (PinkyA_L)
                            initialRotation = Quaternion.Euler(-15.24f, 123.18f, 9.379f); // X:-15.24, Y:123.18, Z:9.379
                            fingerJoints[i].localRotation = initialRotation * Quaternion.Euler(0f, 0f, targetAngle); // Z축으로 굽힘 (부호 조정)
                            break;
                    }
                }
            }
        }

        void OnApplicationQuit()
        {
            isRunning = false;
            if (dataReadThread != null && dataReadThread.IsAlive) dataReadThread.Join();
            if (serialPort != null && serialPort.IsOpen) serialPort.Close();
        }
    }