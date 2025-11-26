using UnityEngine;
using UnityEngine.UI;

public enum SpawnPhase
{
    NormalMonsters,  // 일반 몬스터 5줄
    BossMonster,     // 보스 몬스터 1마리
    Items            // 아이템들
}
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player")]
    public Player m_Player;
    [Header("Game Settings")]
    [Header("몬스터 설정")]
    public float monsterSpawnRate = 2f;
    public int monsterWayCnt = 1;
    public Transform[] monsterSpawnPoint;
    public Transform centerSpawnPoint; // 보스 몬스터용 중앙 스폰 포인트

    [Header("아이템 설정")]
    public float itemSpawnRate = 3f;
    public int itemWayCnt = 1;
    public Transform itemSpawnPoint;

    [Header("스폰 순서 설정")]
    public int normalMonstersPerWave = 5; // 보스 전 일반 몬스터 줄 수
    public int itemsPerWave = 3;          // 한 번에 스폰할 아이템 수

    private float monsterSpawnTimer;
    private float itemSpawnTimer;
    private int score = 0;
    private int wave = 1;
    private bool gameRunning = true;

    // 스폰 순서 관리 변수들
    private SpawnPhase currentPhase = SpawnPhase.NormalMonsters;
    private int currentWaveCount = 0;
    private int normalMonstersKilled = 0;
    private bool bossKilled = false;
    private int itemsSpawned = 0;

    [Header("UpgradeItem")]
    public UpgradeItem[] m_upgradeItems;
    public int[] m_upgradeHP;

    void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if(m_upgradeItems.Length >0)
        {
            for(int i= 0; i < m_upgradeItems.Length; i++ )
            {
                UpgradeItem _item = m_upgradeItems[i];
                if (_item != null)
                {
                    if (m_upgradeHP.Length > 0)
                        _item.SetItemSetting(m_upgradeHP[i]);
                    else
                        _item.SetItemSetting(100);
                }
            }   
        }
    }
    void Update()
    {
        if (!gameRunning) return;

        HandleSpawnSequence();
    }

    void HandleSpawnSequence()
    {
        switch (currentPhase)
        {
            case SpawnPhase.NormalMonsters:
                SpawnNormalMonsters();
                break;

            case SpawnPhase.BossMonster:
                SpawnBossMonster();
                break;

            case SpawnPhase.Items:
                SpawnItems();
                break;
        }
    }

    void SpawnNormalMonsters()
    {
        if (currentWaveCount >= normalMonstersPerWave)
        {
            // 일반 몬스터 5줄 완료, 보스 단계로 이동
            currentPhase = SpawnPhase.BossMonster;
            return;
        }

        monsterSpawnTimer += Time.deltaTime;

        if (monsterSpawnTimer >= monsterSpawnRate)
        {
            for (int j = 0; j < monsterSpawnPoint.Length; j++)
            {
                GameObject monster = ObjectPool.Instance.GetPooledObject("Monster");
                if (monster != null)
                {
                    monster.transform.position = monsterSpawnPoint[j].position;
                    Monster monsterScript = monster.GetComponent<Monster>();
                    monsterScript.SetMonsterType(MonsterType.Normal);
                    monster.SetActive(true);
                }
            }
            monsterSpawnTimer = 0f;
        }
        
        // 한 줄이 완료되면 카운트 증가
        if (monsterSpawnTimer == 0f)
        {
            currentWaveCount++;
        }
    }

    void SpawnBossMonster()
    {
        monsterSpawnTimer += Time.deltaTime;

        if (monsterSpawnTimer >= monsterSpawnRate)
        {
            if (bossKilled)
            {
                // 보스 처치 완료, 아이템 단계로 이동
                currentPhase = SpawnPhase.Items;
                bossKilled = false;
                return;
            }

            // 보스 몬스터 스폰 (한 번만)
            GameObject boss = ObjectPool.Instance.GetPooledObject("Monster");
            if (boss != null)
            {
                Vector3 bossPosition = centerSpawnPoint != null ? centerSpawnPoint.position : Vector3.zero;
                boss.transform.position = bossPosition;
                Monster bossScript = boss.GetComponent<Monster>();
                bossScript.SetMonsterType(MonsterType.Boss);
                boss.SetActive(true);

                // 보스는 한 번만 스폰되도록 설정
                currentPhase = SpawnPhase.Items; // 임시로 아이템 단계로 이동 (보스 처치 대기)
            }

            monsterSpawnTimer = 0f;
        }
        
    }

    void SpawnItems()
    {
        if (itemsSpawned >= itemsPerWave)
        {
            // 아이템 스폰 완료, 다음 웨이브로 이동
            NextWave();
            return;
        }

        itemSpawnTimer += Time.deltaTime;
        if (itemSpawnTimer >= itemSpawnRate)
        {
           
            GameObject item = ObjectPool.Instance.GetPooledObject("Item");
            if (item != null)
            {
                //Debug.LogError("========Spawn Item");
                item.transform.position = centerSpawnPoint.position;
                item.SetActive(true);
                itemsSpawned++;
            }
            itemSpawnTimer = 0f;
        }
    }

    void NextWave()
    {
        // 다음 웨이브 준비
        currentPhase = SpawnPhase.NormalMonsters;
        currentWaveCount = 0;
        normalMonstersKilled = 0;
        itemsSpawned = 0;
        wave++;

        Debug.Log($"Wave {wave} 시작!");
    }

    // Monster.cs에서 호출되는 메소드
    public void OnMonsterKilled(MonsterType monsterType)
    {
        switch (monsterType)
        {
            case MonsterType.Normal:
                normalMonstersKilled++;
                break;

            case MonsterType.Boss:
                bossKilled = true;
                // 보스가 죽으면 즉시 아이템 단계로 이동
                currentPhase = SpawnPhase.Items;
                break;
        }
    }

    public void AddScore(int points)
    {
        score += points;
        Debug.Log($"Score: {score}");
    }

    public void GameOver()
    {
        gameRunning = false;
        Time.timeScale = 0f;
        Debug.Log("Game Over!");
    }

    public Player GetPlayer() => m_Player;
    // 디버그용 정보 출력
    [ContextMenu("Debug Spawn Info")]
    public void DebugSpawnInfo()
    {
        Debug.Log($"현재 단계: {currentPhase}");
        Debug.Log($"웨이브: {wave}");
        Debug.Log($"현재 웨이브 카운트: {currentWaveCount}/{normalMonstersPerWave}");
        Debug.Log($"일반 몬스터 처치: {normalMonstersKilled}");
        Debug.Log($"보스 처치: {bossKilled}");
        Debug.Log($"아이템 스폰: {itemsSpawned}/{itemsPerWave}");
    }
}