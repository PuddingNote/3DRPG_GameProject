# 3DRPG_GameProject

마비노기 모바일 모티브 기능 제작 3 - 미니맵 & 풀맵 시스템 적용 버전

새로 추가된 스크립트 내용 한줄 정리 (3DRPG_Project/Assets/2.Scripts/.)

</br>

### Minimap
- MapCanvasUI : 미니맵 클릭으로 풀맵 토글, 풀맵 드래그 패닝/닫기 버튼 등 Map Canvas UI 이벤트를 MinimapSystem에 연결하는 UI 컨트롤러
- MinimapIconWorld : 월드 오브젝트에 미니맵 전용 SpriteRenderer 아이콘을 생성하고 Minimap 레이어로만 렌더링되게 처리 (NPC는 퀘스트 가능 시 색상 변경)
- MinimapSystem : RenderTexture 기반 미니맵/풀맵 렌더링, 지연 초기화, 풀맵 패닝/휠 줌/경계 클램프 및 메인 카메라에서 Minimap 레이어 제외 처리
- MinimapVisibilityBlocker : 활성화된 동안 GameManager에 미니맵 숨김 요청을 등록/해제하는 컴포넌트 (중복 UI 상황에서도 안전)
- PlayerMinimapMarkerWorld : 플레이어 위치/방향/카메라 시야 콘을 월드 스프라이트 마커로 생성해 RenderTexture 미니맵/풀맵에서 보이게 하는 컴포넌트
