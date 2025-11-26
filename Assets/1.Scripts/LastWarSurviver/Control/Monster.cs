using UnityEngine;
using Spine.Unity;
using TMPro;
public enum MonsterType
{
    Normal = 0,
    Boss = 1,
}

public class Monster : MonoBehaviour
{
    [Header("Monster Settings")]
    public MonsterType monsterType = MonsterType.Normal;
    public float moveSpeed = 2f;
    public int maxHealth = 3;
    public int scoreValue = 10;

    [Header("Boss Settings")]
    public int bossMaxHealth = 15;
    public int bossScoreValue = 100;
    public GameObject[] monsterBody = new GameObject[2];

    [Header("UI")]
    public TextMeshPro TxtHP;
    private int currentHealth;
    private float destroyY = -6f; // 화면 밖으로 나가면 제거할 Y 좌표

    void OnEnable()
    {
        SetupMonster();
    }

    void SetupMonster()
    {
        switch (monsterType)
        {
            case MonsterType.Normal:
                currentHealth = maxHealth;
                scoreValue = 10;
                monsterBody[0].gameObject.SetActive(true);
                monsterBody[1].gameObject.SetActive(false);

                break;

            case MonsterType.Boss:
                currentHealth = bossMaxHealth;
                scoreValue = bossScoreValue;
                monsterBody[1].gameObject.SetActive(true);
                monsterBody[0].gameObject.SetActive(false);

                break;
        }
        TxtHP.gameObject.SetActive(monsterType == MonsterType.Boss);
        SetAnimation(monsterType, "move");
    }

    public void SetAnimation(MonsterType _type, string _animName)
    {
        int _index = (int)_type;
        SkeletonAnimation _anim = monsterBody[_index].GetComponent<SkeletonAnimation>();
        if (_anim != null)
        {
            _anim.AnimationName = _animName;
            _anim.loop = true;
        }
    }

    public void SetMonsterType(MonsterType type, int _cnt = 1)
    {
        monsterType = type;
        SetupMonster();

        // _cnt가 1보다 크면 추가 몬스터들을 가로로 소환
        if (_cnt > 1)
        {
            SpawnAdditionalMonsters(_cnt - 1);
        }
    }

    void SpawnAdditionalMonsters(int additionalCount)
    {
        float spacing = 2f; // 몬스터 간의 간격

        // 현재 활성화된 몬스터 타입에 따른 원본 몬스터 바디 선택
        int currentTypeIndex = (int)monsterType;
        GameObject originalBody = monsterBody[currentTypeIndex];

        for (int i = 0; i < additionalCount; i++)
        {
            // 원본 몬스터 바디를 복제하여 자식으로 생성
            GameObject additionalMonsterBody = Instantiate(originalBody, transform);
            if (additionalMonsterBody != null)
            {
                // 가로로 배치 (좌우로 번갈아가며)
                float offsetX = (i % 2 == 0) ? spacing * ((i + 2) / 2) : -spacing * ((i + 1) / 2);
                Vector3 localPosition = new Vector3(offsetX, 0, 0);
                additionalMonsterBody.transform.localPosition = localPosition;

                // 복제된 몬스터 바디의 애니메이션 설정
                SkeletonAnimation clonedAnim = additionalMonsterBody.GetComponent<SkeletonAnimation>();
                if (clonedAnim != null)
                {
                    clonedAnim.AnimationName = "move";
                    clonedAnim.loop = true;
                }

                // 복제된 몬스터 바디가 충돌하지 않도록 Collider 제거 또는 비활성화
                Collider clonedCollider = additionalMonsterBody.GetComponent<Collider>();
                if (clonedCollider != null)
                {
                    clonedCollider.enabled = false;
                }

                // 복제된 몬스터 바디에 태그 설정 (시각적 효과만을 위한 것임을 표시)
                additionalMonsterBody.tag = "MonsterVisual";
            }
        }
    }

    void Update()
    {
        // 아래쪽으로 이동
        transform.Translate(Vector3.back * moveSpeed * Time.deltaTime);

        // 화면 밖으로 나가면 비활성화
        if (transform.position.z < destroyY)
        {
            gameObject.SetActive(false);
        }
    }

    public void TakeDamage(int damage)
    {
        if (this.gameObject.activeSelf == false)
            return;

        currentHealth -= damage;

        if (TxtHP != null)
        {
            TxtHP.text = currentHealth.ToString();
        }
        // 데미지 효과 (간단한 색상 변경)
        StartCoroutine(FlashRed());

        GameObject _effect = ObjectPool.Instance.GetPooledObject("Effect_2");
        if (_effect != null)
        {
            _effect.transform.position = this.transform.position;
            _effect.SetActive(true);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // 점수 추가
        GameManager.Instance.AddScore(scoreValue);
        GameManager.Instance.OnMonsterKilled(monsterType);

        gameObject.SetActive(false);
    }

    System.Collections.IEnumerator FlashRed()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.material.color;
            renderer.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            renderer.material.color = originalColor;
        }
        else
        {
            yield return new WaitForSeconds(0.1f);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet"))
        {
            Bullet bullet = other.GetComponent<Bullet>();
            TakeDamage(bullet.damage);
            other.gameObject.SetActive(false);
        }
    }
}