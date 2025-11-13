# RogueTower – 발췌 소스 코드 모음

> Unity 기반 로그라이크 타워 디펜스 게임 **RogueTower**의 핵심 시스템을 선별한 발췌 코드 저장소입니다.  
> 채용 검토 목적에 맞게 아키텍처, 데이터 지향 설계, 성능 최적화, 플랫폼 연동을 보여주는 코드만 담았습니다.

---

## 읽는 방법

- 서브시스템 단위로 대표 파일을 나누어 정리했습니다.
- 각 파일은 구조와 의도를 빠르게 이해할 수 있도록 핵심만 담았습니다.
- 전체 개발 과정과 데모는 [웹 포트폴리오](https://woogeun-nam.github.io/towerdefense-portfolio/projects/roguetower/)에서 확인할 수 있습니다.

---

## 1) 핵심 아키텍처 & 서비스

- `System/ObjectPool.cs` — 오브젝트 풀을 통한 **성능 최적화** (GC 및 프레임 드랍 완화)
- `System/DifficultyManager.cs` — **난이도 상태** 중앙 관리, 이벤트 브로드캐스트
- `System/EquipmentManager.cs` — 장비 데이터와 런타임 상태 연결
- `System/TooltipManager.cs` — **이벤트 기반** 툴팁 서비스 (UI-로직 분리)
- `System/WeightedRandomReward.cs` — 시드 기반 **가중치 보상 테이블**
- `System/SoundManager.cs` — 사운드 파일 로드 및 재생 관리 ( 오디오 풀링 )
- `System/LoadingSceneController.cs` — 로딩신 제어 스크립트
- `System/GameManager.cs` — 인게임 데이터 및 무한모드 제어
- `System/GameDataManager.cs` — 저장 데이터 세이브/로드 및 암호화

---

## 2) 데이터 지향 설계 (ScriptableObjects)

- `Data/Models/TowerArchetype.cs` — 타워 스펙 정의 (사거리, 연사, 데미지 등)
- `Data/EquipmentData.cs` — 장비/슬롯 데이터, 밸런스 친화적 설계
- `Data/DifficultyData.cs` — 난이도 곡선 파라미터 직렬화
- `Data/SpecialSkillBase.cs` — 장비 특수 스킬 구현 추상 클래스

---

## 3) 전투 & 게임플레이

- `Tower/TowerWeapon.cs` — 무기 공격 파이프라인 (투사체, 즉발, 지속형)
- `Tower/TowerTemplate.cs` — **컴포지션 기반** 확장 구조
- `Tower/TowerSpawner.cs` — 안전한 인스턴스 생성/초기화 팩토리
- `Tower/WeaponStates/IWeaponState.cs` — 타워 무기의 상태를 정의하는 인터페이스 / 각 상태는 개별 스크립트에서 구현
- `Enemy/Enemy.cs` — 적 유닛 라이프사이클, 데미지 처리, 사망 이벤트
- `Enemy/EnemySpawner.cs` — 웨이브 단위 적 소환 및 맵 연동
- `Enemy/WaveSystem.cs` — 웨이브 진행 및 규칙 제어
- `Skill/SkillManager.cs` — 런타임 스킬 등록/적용, 버프·디버프 스택 처리
- `Skill/TowerBuffEffect.cs` — 타워에 적용되는 디커플드 버프 예시

---

## 4) 맵 & 절차적 플레이

- `Map/MapGenerator.cs` — 절차적 타일 레이아웃 생성, 난이도 태그, BGM 연동
- `Map/RandomPathGenerator.cs` — **기존 경로 탐색 알고리즘 대신**, 웨이포인트를 절차적으로 생성하는 방식으로 다양한 플레이 경로 제공

---

## 5) UI & UX (이벤트 기반)

- `UI/TowerDataViewer.cs` — 데이터/이벤트 바인딩, 참조 교체 핫스왑
- `UI/TooltipTrigger.cs` — 뷰에서 **툴팁 의도**만 발행, 소비자는 독립적 처리
- `UI/LeaderboardUI.cs` — 랭킹 UI 동적 리스트 구성 및 포매팅
- `UI/ShopManager.cs` — 상점 UI 구성 및 랜덤 뽑기 기능
- `UI/UIManager.cs` - 게임 내 UI 패널의 표시, 전환, 애니메이션을 관리하는 클래스
- `UI/UIEventManager.cs` - UI 관련 이벤트를 중앙 관리

---

## 6) 온라인 / 플랫폼

- `Network/PlayFabLogin.cs` — PlayFab 로그인
- `Network/GameAnalyticsManager.cs` — 세션 캐싱, 플레이 데이터 수집
- `Network/ExceptionReporter.cs` — 오류 데이터 수집

---

## 7) 유틸리티

- `Utils/StatTypeConverter.cs` — 스탯 변환 유틸리티

---

## 8) 기타

- `Data/SpecialSkills/Skill_StatueOfDestructionGod.cs` — '파괴신의 석상' 장비 기능 구현

---

## 제외된 항목

- **비밀키/계정**: 보안상 제외
- **리소스(아트/오디오)**: 용량 문제로 제외
- **Unity 프로젝트 세팅/패키지**: 리뷰 목적과 무관하여 제외
