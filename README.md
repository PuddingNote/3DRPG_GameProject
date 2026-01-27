# 3DRPG_GameProject

마비노기 모바일 모티브 기능 제작 2 - NPC & 상호작용 시스템 적용 버전

새로 추가된 스크립트 내용 한줄 정리 (3DRPG_Project/Assets/2.Scripts/.)

</br>

### NPCs
- NPCController : NPC 상호작용(IInteractable) 처리, 대화 UI 호출, 카메라 연출(시네머신 브레인 제어) 및 퀘스트 목록 제공/상태 판별

### Quests
- Quest : 런타임에서 사용하는 퀘스트 인스턴스(QuestData 참조 + 현재 QuestState 보관)
- QuestData : 퀘스트 고유 ID/제목/설명과 시작·진행·완료 대사를 정의하는 데이터 객체(ScriptableObject)
- QuestManager : 전체 퀘스트 데이터로 런타임 퀘스트를 초기화하고, 수락/완료 및 상태 관리를 담당하는 싱글톤 매니저
- QuestState : 퀘스트 진행 상태를 정의하는 enum

### UI
- DialogueUI :  NPC 대화 UI 흐름과 타이핑 효과/버튼 생성 및 콜백 종료 처리를 담당하는 싱글톤 UI
