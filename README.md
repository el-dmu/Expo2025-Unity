# GLIFE — 스마트 글러브와 AI를 활용한 산업안전사고 교육 플랫폼

> **2025 동양미래EXPO 출품작** · EL · 동양미래대학교 컴퓨터소프트웨어공학과

---

## 팀 정보

| 이름 | 깃허브 | 담당 역할 |
|------|--------|-----------|
| 김준혁 | [@ddo0122](https://github.com/ddo0122) | Unity Leap Motion 연동 및 소화기 분사 VFX 구현 |
| 김민정 | [@miiniminimo](https://github.com/miiniminimo) | 프로젝트 기획 및 백엔드 개발 |
| 신동수 | [@Dungsu](https://github.com/Dungsu) | Unity 웨이포인트 이동·스와이프 UI 구현, 씬 에셋 배치 |
| 심민식 | [@minsik1014](https://github.com/minsik1014) | 프론트엔드 리드 및 React 웹 대시보드 구현 |
| 정용환 | [@wjddydghks](https://github.com/wjddydghks) | 스마트 글러브 하드웨어 제작(납땜) 및 아두이노 펌웨어 개발 |
| 이연호 | [@lyh030526](https://github.com/lyh030526) | 스마트 글러브 하드웨어 제작(납땜) 및 아두이노 펌웨어 개발 |
| 최유성 | [@yousung1020](https://github.com/yousung1020) | 백엔드 리드 및 DTW 기반 AI 동작 평가 로직 구현 |
| 최정규 | [@JeongGyul](https://github.com/JeongGyul) | Unity 프로젝트 총괄 및 상호작용 스크립트 구현 |
| 하은현 | [@gkdmsgus](https://github.com/gkdmsgus) | 하드웨어 총괄 |

## 출품 행사

| 구분 | 내용 |
|------|------|
| 행사명 | 2025 동양미래EXPO (제43회 졸업작품전시회) |
| 일시 | 2025년 10월 21일(화) ~ 10월 24일(금) |
| 장소 | 코엑스(COEX) Hall A |
| 비고 | KES 2025 (한국전자전)과 공동 개최 |

---

## 목차
1. [프로젝트 개요](#프로젝트-개요)
2. [시연 영상](#시연-영상)
3. [전체 시스템 아키텍처](#전체-시스템-아키텍처)
4. [주요 기능](#주요-기능)
5. [하드웨어 구성](#하드웨어-구성)
6. [소프트웨어 스택](#소프트웨어-스택)
7. [씬 구성](#씬-구성)
8. [훈련 플로우](#훈련-플로우)
9. [센서 데이터 파이프라인](#센서-데이터-파이프라인)
10. [프로젝트 구조](#프로젝트-구조)
11. [시작하기](#시작하기)
12. [관련 레포지토리](#관련-레포지토리)

---

## 프로젝트 개요

**GLIFE**는 산업현장의 중대재해를 예방하기 위한 **AI 기반 스마트 글러브 연동 VR 안전교육 시스템**입니다.

Ultraleap(Leap Motion) 핸드 트래킹과 HTC VIVE VR 헤드셋으로 VR 내 모든 손 인터랙션을 구현하고, Flex 센서와 MPU9250(9축) IMU가 탑재된 스마트 글러브로 훈련 중 손 동작 데이터를 수집하여 AI 평가에 활용합니다.

- VR 내 소화기 조작(잡기·안전핀·호스·레버)은 모두 Leap Motion으로 처리합니다.
- 훈련 중 스마트 글러브의 Flex 센서(손가락 굽힘)와 IMU(손목 자세) 값이 별도로 수집됩니다.
- 수집된 센서 데이터는 Django 백엔드로 전송, **DTW(Dynamic Time Warping) 알고리즘**으로 정답 동작 대비 수행 정확도를 평가합니다.
- 평가 결과는 React 웹 대시보드에서 사원·관리자 모두 확인할 수 있으며, 사번(직원 ID) 기반으로 교육 이력이 누적 관리됩니다.

---

## 시연 영상

<!-- 시연 영상을 여기에 드래그하여 업로드하세요 -->

---

## 전체 시스템 아키텍처

```
┌──────────────── VR 하드웨어 ──────────────────────┐
│                                                   │
│   HTC VIVE Headset  ◄──── Base Station(IR)        │
│        │                                          │
│   Leap Motion Controller 2 (손 위치 트래킹)       │
│                                                   │
│   스마트 글러브 (양손 / LOLIN D32 + ESP32)        │
│     - Flex Sensor × 5  (손가락 굽힘)              │
│     - MPU9250 9축      (가속도+자이로+지자기)     │
│     └── Bluetooth Serial ──► PC                   │
└───────────────────────────────────────────────────┘
                    │
          ┌─────────▼──────────┐
          │   Unity (GLIFE)     │
          │   C# / URP          │
          │   XR Interaction    │
          │   Ultraleap SDK     │
          └─────────┬──────────┘
                    │ HTTP POST (JSON)
          ┌─────────▼────────────────────────────────┐
          │     Django REST API 백엔드               │
          │                                          │
          │  /api/ai/evaluate/   ← 사용자 동작 평가  │
          │  /api/ai/recordings/ ← 정답 데이터 등록  │
          │            │                             │
          │     DTW 알고리즘 (동작 일치율 분석)      │
          │            │                             │
          │         MySQL DB                         │
          └─────────┬────────────────────────────────┘
                    │
          ┌─────────▼───────────┐
          │  React 웹 대시보드  │
          │  (훈련 결과 시각화) │
          └─────────────────────┘
```

---

## 주요 기능

### 1. VR 소화기 훈련
순차적으로 잠금/해제되는 단계별 훈련으로 올바른 소화기 사용 절차를 체득합니다.

| 단계 | 동작 | 설명 |
|------|------|------|
| 1 | 소화기 잡기 | 오른손을 소화기 핸들 위치에 가져가 쥐기 |
| 2 | 안전핀 뽑기 | 왼손으로 안전핀 제거 |
| 3 | 호스 잡기 | 왼손으로 호스 그립 |
| 4 | 레버 눌러 분사 | 오른손 레버 가압 → 파티클 분사 |

각 단계는 나레이션 종료 후에만 잠금이 해제되어, 절차 역순 조작을 방지합니다.

### 2. 손 트래킹 (Leap Motion) + 센서 데이터 수집 (스마트 글러브)
- VR 내 모든 손 인터랙션은 **Leap Motion**이 담당합니다. 손 위치·3D 모델 구동·쥐기(`GrabStrength`) 감지 모두 Ultraleap SDK 기반입니다.
- **스마트 글러브**는 인터랙션과 무관하며, 오직 AI 평가용 센서 데이터 수집에만 사용됩니다.
- `ReceiveHandDataL/R`(`LeftHandReceiver` / `RightHandReceiver` 오브젝트)이 글러브의 Flex·IMU 시리얼 데이터를 수신하여 `DataManager`에 전달합니다.

### 3. 웨이포인트 이동 시스템
- VR 공간에서 사용자를 지정된 경로를 따라 이동시킵니다.
- **Leap Motion 손 스와이프 제스처**로 다음 지점으로 진행합니다.
- 화재 구역(Waypoint 4) 도달 시 소화기 훈련 미션이 자동 시작됩니다.

### 4. AI 기반 동작 평가 (DTW)
- 훈련 중 양손 센서 데이터를 0.1초 간격으로 수집합니다.
- 훈련 완료 후 사번과 함께 Django 서버로 전송합니다.
- 서버에서 **DTW(Dynamic Time Warping)** 알고리즘으로 동작 일치율, 수행 시간, 오류 구간을 분석합니다.
- 정답 데이터(`scoreCategory: reference`)와 사용자 데이터를 분리 관리합니다.

### 5. React 웹 대시보드
- 기업 관리자 계정으로 로그인 (JWT 인증)
- 직원별 훈련 결과(정확도) 및 교육 이수율 확인
- 직원 등록 및 엑셀 대량 등록 지원
- 연도/분기별 교육 과정 생성 및 수강 등록 관리

### 6. 세션 관리
- 타이틀 화면에서 입력한 **사번**이 씬 전환 후에도 `DontDestroyOnLoad`로 유지됩니다.
- 훈련 종료 시 나레이션 재생 → 키 입력 대기 → 타이틀 씬 복귀 흐름으로 마무리됩니다.

---

## 하드웨어 구성

| 장치 | 모델 | 역할 | 연결 방식 |
|------|------|------|-----------|
| VR 헤드셋 | HTC VIVE | 시각 출력 + 위치 트래킹 | Base Station(IR 레이저) |
| 핸드 트래킹 | Ultraleap Leap Motion Controller 2 | VR 공간 내 손 위치 인식 | USB (헤드셋 전면 부착) |
| 마이크로컨트롤러 | LOLIN D32 (ESP32) | 글러브 센서 데이터 처리 및 BT 전송 | Bluetooth Serial → PC |
| 굽힘 센서 | Flex Sensor × 5 (손당) | AI 평가용 손가락 굽힘 값 수집 | Arduino 아날로그 입력 |
| IMU | MPU9250 (9축) | AI 평가용 손목 자세(쿼터니언) 수집 | I2C → LOLIN D32 |

> **MPU9250 9축 구성**: 가속도계 3축 + 자이로센서 3축 + 지자기센서(나침반) 3축

### 글러브 시리얼 데이터 포맷
```
flex1,flex2,flex3,flex4,flex5,accelX,accelY,accelZ,qw,qx,qy,qz
 [0]  [1]  [2]  [3]  [4]   [5]    [6]    [7]  [8] [9][10][11]
```
- `flex1~5` (인덱스 0~4): 각 손가락 ADC 원시 값 (엄지 → 새끼) — **수집 사용**
- `accelX, accelY, accelZ` (인덱스 5~7): MPU9250 가속도계 값 — 전송되나 Unity에서 미사용
- `qw, qx, qy, qz` (인덱스 8~11): Madgwick 필터 쿼터니언 값 — **수집 사용**

통신 속도: 115200 baud / 전송 주기: 약 33Hz (30ms)

---

## 소프트웨어 스택

### Unity 클라이언트 (이 레포지토리)

| 항목 | 버전 |
|------|------|
| Unity Editor | 2022.3.61f1 LTS |
| Render Pipeline | Universal Render Pipeline (URP) 14.0.12 |
| XR Interaction Toolkit | 2.6.5 |
| OpenXR Plugin | 1.14.3 |
| Ultraleap Tracking SDK | 7.2.0 |
| TextMeshPro | 3.0.7 |
| Input System | 내장 패키지 |

### 백엔드 / 인프라

| 항목 | 기술 |
|------|------|
| 서버 프레임워크 | Django (Python) |
| 데이터베이스 | MySQL |
| 동작 분석 알고리즘 | DTW (Dynamic Time Warping) |
| REST API | Django REST Framework |
| 웹 대시보드 | React + Vite + Tailwind CSS |
| 인증 | Simple JWT |
| 마이크로컨트롤러 펌웨어 | Arduino (C++) |

---

## 씬 구성

| 씬 | 설명 |
|----|------|
| `TitleScene` | 사번 입력 및 훈련 시작 화면 |
| `LeapMotionScene_edit` | **VR 풀 트레이닝 씬** — HTC VIVE 착용 + 웨이포인트 이동 + 소화기 훈련 |
| `LeapMotionScene_desk` | **데스크 시연 씬** — VR 미착용자를 위한 비VR 모드. 지정된 위치에서 Leap Motion만으로 소화기 화재 진압 |

> **전시 운영 방침**: VR 착용에 거부감이 있거나 착용이 어려운 방문객은 `LeapMotionScene_desk`로 체험합니다. 웨이포인트 이동 없이 소화기 조작 핵심 인터랙션만 수행할 수 있습니다.

---

## 훈련 플로우

```
TitleScene
  │ 사번 입력 → StartTraining()
  ▼
LeapMotionScene_edit (훈련 씬 로드)
  │
  ├─ UIManager: 초기 안내 메시지 표시 (Leap Motion 스와이프로 넘기기)
  │
  ├─ 스와이프 감지 → VrWaypointWalker.StartPath()
  │    Wp1 → Wp2 → Wp3 → Wp4(화재구역)
  │    (각 지점 도착 시 나레이션 재생, 소화기 잡아서 이동)
  │
  ├─ Wp4 도착 → 소화기 훈련 미션 시작
  │    DataManager.StartRecording()  ← 소화기 잡는 순간부터 데이터 수집
  │    ┌─ 소화기 잡기       (GrabStrength 임계값 초과)
  │    ├─ 안전핀 뽑기       (InteractionTarget 트리거)
  │    ├─ 호스 잡기         (GrabbableHorn 트리거)
  │    └─ 레버 눌러 분사    (파티클 분사 → FireInteraction 히트)
  │
  ├─ 모든 화재 진압 → FireManager.OnAllFiresExtinguished
  │    DataManager.StopRecordingAndSend(empNo)
  │    → Django 서버 POST /api/ai/evaluate/
  │    → DTW 분석 → MySQL 저장 → React 대시보드 반영
  │
  └─ 나레이션 종료 → 키 입력 대기 → TitleScene 복귀
```

---

## 센서 데이터 파이프라인

### Unity → Django 전송 JSON 구조

**사용자 평가 데이터** (`/api/ai/evaluate/`)
```json
{
  "motionName": "소화기 사용 훈련",
  "empNo": "12345",
  "sensorData": [
    {
      "flex01": 1200, "flex02": 3100, "flex03": 3200, "flex04": 3000, "flex05": 3400,
      "flex06": 1100, "flex07": 2900, "flex08": 3000, "flex09": 2800, "flex10": 3300,
      "qw1": 0.98, "qx1": 0.01, "qy1": 0.12, "qz1": 0.05,
      "qw2": 0.97, "qx2": 0.02, "qy2": 0.10, "qz2": 0.07
    }
  ]
}
```

**정답 데이터 등록** (`/api/ai/recordings/`)
```json
{
  "motionName": "소화기 사용 훈련",
  "scoreCategory": "reference",
  "sensorData": [ ... ]
}
```

### API 엔드포인트

| 엔드포인트 | 메서드 | 용도 |
|-----------|--------|------|
| `/api/ai/evaluate/` | POST | 사용자 동작 평가 (DTW 비교) |
| `/api/ai/recordings/` | POST | 정답(reference) 데이터 등록 |

> **파일 저장 모드**: `DataManager.saveToFileInsteadOfServer = true`로 설정하면 서버 전송 대신 `Assets/SensorDataOutput/` 폴더에 JSON 파일로 로컬 저장합니다.

---

## 프로젝트 구조

```
GLIFE/
├── Assets/
│   ├── Scripts/                            # 커스텀 C# 스크립트
│   │   ├── TrainingManager.cs              # 훈련 전체 흐름 제어
│   │   ├── FireManager.cs                  # 화재 상태 및 진압 관리 (싱글턴)
│   │   ├── FireExtinguisherController.cs   # 소화기 물리 인터랙션 + 단계별 잠금
│   │   ├── FireInteraction.cs              # 개별 화재 오브젝트 진압 처리
│   │   ├── DataManager.cs                  # 센서 데이터 수집 및 서버 전송
│   │   ├── ReceiveHandDataL.cs             # 왼손 글러브 Bluetooth Serial 수신
│   │   ├── ReceiveHandDataR.cs             # 오른손 글러브 Bluetooth Serial 수신
│   │   ├── VrWaypointWalker.cs             # 웨이포인트 이동 시스템
│   │   ├── SwipeDetector.cs                # Leap Motion 스와이프 제스처 감지
│   │   ├── UIManager.cs                    # 나레이션/체크리스트/진행 UI
│   │   ├── SessionManager.cs               # 씬 간 사번 유지 (DontDestroyOnLoad)
│   │   ├── TitleUI.cs                      # 타이틀 화면 사번 입력 처리
│   │   ├── GuideManager.cs                 # 페이드 인/아웃 안내문 시스템
│   │   ├── GrabbableHorn.cs                # 호스 잡기 인터랙션
│   │   ├── InteractionTarget.cs            # 일반 인터랙션 타겟
│   │   ├── InteractionTargetSeal.cs        # 안전핀 인터랙션 타겟
│   │   ├── InteractionTargetLever.cs       # 레버 인터랙션 타겟
│   │   └── WaypointInfo.cs                 # 웨이포인트 메타데이터
│   │
│   ├── Scenes/
│   │   ├── TitleScene.unity                # 사번 입력 화면
│   │   ├── LeapMotionScene_edit.unity      # VR 풀 트레이닝 씬
│   │   ├── LeapMotionScene_desk.unity      # 비VR 데스크 시연 씬
│   │   └── LeapMotionScene_edit/           # 메인 씬 라이트맵 데이터
│   │
│   ├── Prefab/
│   │   ├── fire_extinguisher.prefab        # 소화기 오브젝트
│   │   ├── FireEx_PS.prefab                # 소화기 파티클 시스템
│   │   └── VFX_Fire_01_Big_Smoke.prefab    # 화재 VFX 프리팹
│   │
│   ├── Vefects/
│   │   └── Free Fire VFX URP/              # 화재 VFX 에셋 (파티클, 머티리얼, 오디오)
│   │
│   ├── Stylized_Construction_Zone/         # 공사 현장 환경 에셋
│   │   ├── Art/                            # 머티리얼, 메시, 텍스처, 셰이더
│   │   └── Prefabs/                        # 환경 구성 프리팹
│   │
│   ├── Voices/                             # 훈련 나레이션 오디오
│   │   ├── start_voice.mp3                 # 시작 안내
│   │   ├── voice_1~4.mp3                   # 웨이포인트별 나레이션
│   │   ├── inst_voice_1~4.mp3              # 소화기 단계별 안내
│   │   └── end_voice.mp3                   # 훈련 종료 안내
│   │
│   ├── Images/
│   │   └── title_background.JPG            # 타이틀 배경 이미지
│   │
│   ├── Materials/                          # 공용 머티리얼
│   ├── Lights/                             # 조명 프리셋
│   ├── Render/                             # 렌더 텍스처
│   ├── SensorDataOutput/                   # 로컬 저장 모드 시 JSON 출력 경로
│   ├── Resources/
│   │   └── Ultraleap Settings.asset        # Leap Motion 런타임 설정
│   ├── XR/                                 # OpenXR 빌드 설정
│   ├── XRI/                                # XR Interaction Toolkit 설정
│   ├── Settings/                           # URP 파이프라인 에셋
│   ├── TextMesh Pro/                       # TMP 폰트 및 셰이더
│   ├── PyeojinGothic-Bold.otf              # 커스텀 한글 폰트
│   └── GameControls.inputactions           # Input System 액션 맵
│
├── Packages/
│   └── manifest.json                       # Unity 패키지 의존성
├── ProjectSettings/                        # Unity 프로젝트 설정
└── README.md
```

---

## 시작하기

### 필수 환경
- Unity 2022.3.61f1 LTS
- Ultraleap Hyperion 드라이버 설치 (Leap Motion Controller 2 인식)
- HTC VIVE + SteamVR 또는 OpenXR 호환 VR HMD
- 스마트 글러브 (양손, Bluetooth 페어링 완료 상태)
- Django 백엔드 서버 실행 중 (또는 파일 저장 모드 사용)

### 글러브 연결 설정
1. 글러브 전원을 켜고 Windows 블루투스 설정에서 페어링합니다. (왼손: `Glove_Left` / 오른손: `Glove_Right`)
2. 장치 관리자 → 포트(COM & LPT)에서 블루투스 포트 번호를 확인합니다. 포트가 여러 개 뜨는 경우 아두이노 IDE 시리얼 모니터로 각 포트를 확인하여 숫자 값이 출력되는 포트가 실제 글러브 포트입니다.
3. `LeapMotionScene_edit` 씬의 Hierarchy에서 `Sensor` 오브젝트 하위의 `LeftHandReceiver` → `ReceiveHandDataL`의 `portName`을 왼손 포트로, `RightHandReceiver` → `ReceiveHandDataR`의 `portName`을 오른손 포트로 설정합니다.

> **글러브 미사용 시**: Hierarchy에서 `Sensor` 오브젝트를 비활성화하세요. 비활성화하지 않으면 Unity가 연결 불가능한 글러브에 계속 연결을 시도하여 로딩 시간이 매우 길어집니다.

### AI 서버 연결
`DataManager` 컴포넌트의 `evaluateUrl`과 `recordUrl`을 실제 Django 서버 주소로 변경합니다.  
(기본값: `http://127.0.0.1:8000/`)

서버 없이 테스트할 경우 `DataManager.saveToFileInsteadOfServer = true`로 설정하면 JSON 파일로 로컬 저장됩니다.

### 디버그 모드
웨이포인트 이동을 건너뛰고 특정 지점에서 바로 시작하려면 Hierarchy에서 `GameManagers` 오브젝트의 `TrainingManager` 컴포넌트에서 `Debug Mode`를 활성화하고 `Debug Start Waypoint`를 원하는 웨이포인트로 지정합니다.

---

## 관련 레포지토리

| 레포지토리 | 담당 영역 | 링크 |
|-----------|----------|------|
| **GLIFE (Unity)** | VR 훈련 클라이언트 (현재 레포) | — |
| **flex_glove** | 스마트 글러브 아두이노 펌웨어 (LOLIN D32 / ESP32) | [lyh030526/flex_glove](https://github.com/lyh030526/flex_glove) |
| **Expo2025-Front-Back** | Django 백엔드 + React 웹 대시보드 | [yousung1020/Expo2025-Front-Back](https://github.com/yousung1020/Expo2025-Front-Back) |
