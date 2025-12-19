using UnityEngine;

// 런타임에서 사용될 퀘스트 객체
[System.Serializable]
public class Quest
{
    public QuestData data;      // 정적 데이터 참조 (변하지 않는 정보)
    public QuestState state;    // 현재 상태 (변하는 정보)

    public Quest(QuestData data)
    {
        this.data = data;
        this.state = QuestState.NotStarted;
    }
}
