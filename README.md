# 3DRPG_GameProject (마비노기 모바일 모티브 기능 제작 1)

스크립트 내용 한줄 정리 (3DRPG_Project/Assets/2.Scripts/.)

</br>

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
