# 3DRPG_GameProject

마비노기 모바일 모티브 기능 제작 4 - 아이템 드랍 & 획득 시스템 적용 버전

새로 추가된 스크립트 내용 한줄 정리 (3DRPG_Project/Assets/2.Scripts/.)

</br>

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
