# 3DRPG_GameProject (3D RPG 게임 (마비노기 모바일) 기능 모작)

## 1. 주요 목표
  - 3D RPG 게임 기능 모작 및 개발 목적
  - 시스템 개발
    - 플레이어 자동이동 & 자동전투
    - 던전 & 상호작용
    - 몬스터 AI
    - NPC 대화 & 퀘스트
    - 미니맵
    - 아이템 드랍 & 획득 연출
## 2. 개발 환경
  - Unity, C#
## 3. 기타 관련
  - [상세 개발 기록 블로그](https://velog.io/@gamedeveloper/series/3D-RPG-%ED%94%84%EB%A1%9C%EC%A0%9D%ED%8A%B8)

</br>
</br>

## [스크립트 한줄 요약] (경로: 3DRPG_Project/Assets/2.Scripts/)

<details>
  <summary>👉 던전 시스템 관련 (2025.12.16)</summary>

### Dungeons
  - DungeonData : 던전 이름과 스테이지 정보를 정의하는 데이터 객체 (ScriptableObject)
  - DungeonDoor : 상호작용 가능한 던전 문의 잠금 해제 및 개폐 애니메이션 처리
  - DungeonEnteranceUI : 던전 입장 버튼 및 스테이지 선택 UI 제어
  - DungeonResultUI : 던전 클리어 시 결과 화면 표시 및 마을 복귀 처리
  - DungeonRoomManager : 던전 방 내부의 몬스터 스폰 및 클리어 조건(문 열림) 관리

### Interfaces
  - IDamageable : 데미지를 입을 수 있는 객체가 구현해야 할 인터페이스
  - IInteractable : 플레이어와 상호작용(대화, 문 열기 등) 가능한 객체가 구현해야 할 인터페이스
  - IState : 상태 패턴(State Pattern) 구현을 위한 인터페이스 (Enter, Execute, Exit)
  - LivingEntity : 체력과 사망 처리를 담당하는 모든 생명체의 기반 클래스

### Monsters
  - MonsterAttackState : 플레이어를 공격하고 쿨타임을 관리하는 상태
  - MonsterChaseState : 발견한 플레이어를 공격 사거리까지 추적하는 상태
  - MonsterController : 몬스터의 능력치, AI 설정 및 FSM(상태 머신) 총괄
  - MonsterIdleState : 몬스터의 대기 상태 및 플레이어 탐색
  - MonsterReturnState : 추적을 포기하거나 거리가 멀어졌을 때 원래 위치로 복귀하는 상태
  - MonsterState : 몬스터 상태들의 부모가 되는 추상 클래스

### Players
  - Fireball : 플레이어가 발사하는 기본 공격(유도 투사체) 로직
  - PlayerAttackState : 플레이어의 공격 애니메이션 재생 및 투사체 발사 처리
  - PlayerAutoState : 자동 사냥 모드에서의 타겟 탐색, 이동, 공격 루틴 처리
  - PlayerChaseState : 타겟(몬스터 또는 상호작용 물체)을 향해 자동으로 이동하는 추적 상태
  - PlayerController : 플레이어의 입력, 이동, 카메라 제어 및 FSM(상태 머신) 총괄
  - PlayerIdleState : 플레이어의 대기 상태 및 입력/자동 모드 감지
  - PlayerMoveState : 플레이어의 수동 이동(WASD) 로직 처리
  - PlayerState : 플레이어 상태들의 부모가 되는 추상 클래스

### Scenes
  - LoadingSceneController : 씬 전환 시 로딩 바 연출 및 비동기 씬 로딩 처리
  - Protal : 던전 입장 UI를 호출하는 포탈 상호작용 객체

### ETC
  - GameManager : 게임 전체 상태, 씬 전환 및 플레이어 스폰을 관리하는 싱글톤 매니저

</details>



<details>
  <summary>👉 NPC & 상호작용 시스템 관련 (2025.12.28)</summary>

### NPCs
  - NPCController : NPC 상호작용(IInteractable) 처리, 대화 UI 호출, 카메라 연출(시네머신 브레인 제어) 및 퀘스트 목록 제공/상태 판별

### Quests
  - Quest : 런타임에서 사용하는 퀘스트 인스턴스(QuestData 참조 + 현재 QuestState 보관)
  - QuestData : 퀘스트 고유 ID/제목/설명과 시작·진행·완료 대사를 정의하는 데이터 객체(ScriptableObject)
  - QuestManager : 전체 퀘스트 데이터로 런타임 퀘스트를 초기화하고, 수락/완료 및 상태 관리를 담당하는 싱글톤 매니저
  - QuestState : 퀘스트 진행 상태를 정의하는 enum

### UI
  - DialogueUI : NPC 대화 UI 흐름과 타이핑 효과/버튼 생성 및 콜백 종료 처리를 담당하는 싱글톤 UI

</details>



<details>
  <summary>👉 미니맵 & 풀맵 시스템 관련 (2026.01.16)</summary>

### Minimap
  - MapCanvasUI : 미니맵 클릭으로 풀맵 토글, 풀맵 드래그 패닝/닫기 버튼 등 Map Canvas UI 이벤트를 MinimapSystem에 연결하는 UI 컨트롤러
  - MinimapIconWorld : 월드 오브젝트에 미니맵 전용 SpriteRenderer 아이콘을 생성하고 Minimap 레이어로만 렌더링되게 처리 (NPC는 퀘스트 가능 시 색상 변경)
  - MinimapSystem : RenderTexture 기반 미니맵/풀맵 렌더링, 지연 초기화, 풀맵 패닝/휠 줌/경계 클램프 및 메인 카메라에서 Minimap 레이어 제외 처리
  - MinimapVisibilityBlocker : 활성화된 동안 GameManager에 미니맵 숨김 요청을 등록/해제하는 컴포넌트 (중복 UI 상황에서도 안전)
  - PlayerMinimapMarkerWorld : 플레이어 위치/방향/카메라 시야 콘을 월드 스프라이트 마커로 생성해 RenderTexture 미니맵/풀맵에서 보이게 하는 컴포넌트

</details>



<details>
  <summary>👉 아이템 드랍 & 획득 시스템 관련 (2026.01.27)</summary>

### Items
- MonsterDropTable : 몬스터 드랍 항목(아이템/확률/수량)을 정의하고 확률 롤링으로 드랍 결과를 생성하는 드랍 테이블(ScriptableObject)
- MonsterLootDropper : 몬스터 사망 시 드랍 확정→인벤 즉시 추가→등급 VFX를 순차 스폰하는 드랍/획득 파이프라인 컴포넌트

</br>

- InventoryService : UI 없이 아이템 획득 데이터를 누적 저장하고 스택/비스택 적재를 처리하는 싱글톤 인벤토리
- ItemStack : 인벤토리 저장 단위(아이템 데이터 + 수량)를 표현하는 직렬화 클래스

</br>

- DroppedItemVFX : 드랍 VFX의 Drop(포물선 낙하)→Idle(대기)→Absorb(가속 흡수) 단계 연출을 수행하는 컴포넌트
- ItemRarityVfxLibrary : 아이템 등급에 맞는 VFX 프리팹을 매핑/조회하는 라이브러리(ScriptableObject)

</br>

- EquipSlot : 아이템 장착 부위(Weapon/Accessory/Armor)를 정의하는 enum
- ItemData : 아이템 정보(ID/이름/아이콘/등급/장착부위/스택)를 정의하는 아이템 데이터(ScriptableObject)
- IteRarity : 아이템 등급(Common/Rare/Epic/Legendary)을 정의하는 enum

</details>
