using UnityEngine;
using Spine.Unity;
using TMPro;
public enum MonsterType
{
    Normal = 0,
    Boss =1,
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
        if(_anim!=null)
        {
            _anim.AnimationName = _animName;
            _anim.loop = true;
        }
    }
    public void SetMonsterType(MonsterType type)
    {
        monsterType = type;
        SetupMonster();
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

        if(TxtHP!=null)
        {
            TxtHP.text = currentHealth.ToString();
        }
        // 데미지 효과 (간단한 색상 변경)
        StartCoroutine(FlashRed());

        GameObject _effect = ObjectPool.Instance.GetPooledObject("Effect_2");
        if(_effect!=null)
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