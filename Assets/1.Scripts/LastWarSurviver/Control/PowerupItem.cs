using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public enum ItemType
{
    Attack,
    Health,
    FireRate,
    Shield
}

public class PowerUpItem : MonoBehaviour
{
    [Header("Item Settings")]
    public ItemType itemType;
    public int baseValue = -5;      // 초기 마이너스 값
    public int currentValue;        // 현재 값 (총알에 맞아서 증가)
    public float itemSpeed = 5f;

    [Header("Hit Detection Settings")]
    public float hitTimeout = 0.3f; // 총알이 끊어졌다고 판단하는 시간

    [Header("UI")]
    public TextMeshPro valueText;

    private float destroyY = -6f;
    private bool isMoving = true;

    // 총알 히트 감지 관련 변수들
    private bool isBeingHit = false;           // 현재 총알에 맞고 있는지 여부
    private float lastHitTime = 0f;            // 마지막으로 총알에 맞은 시간
    private Coroutine valueIncreaseCoroutine;  // 값 증가 코루틴 참조

    void OnEnable()
    {
        // 초기 설정
        currentValue = baseValue;
        isMoving = true;

        // 히트 상태 초기화
        ResetHitState();

        // 랜덤 아이템 타입과 기본값 설정
        //itemType = (ItemType)Random.Range(0, 4);

        // 초기 색상 및 텍스트 설정
        SetItemAppearance();
    }

    void Update()
    {
        // 이동 처리
        if (isMoving)
        {
            transform.Translate(new Vector3(0, 0, -1) * Time.deltaTime * itemSpeed);
        }

        // 히트 상태 체크 - 총알이 끊어졌는지 확인
        if (isBeingHit && Time.time - lastHitTime > hitTimeout)
        {
            // 총알이 끊어짐
            StopValueIncrease();
            Debug.Log($"총알 히트 중단됨. 최종 값: {currentValue}");
        }

        // 화면 밖으로 나가면 제거
        if (transform.position.z < destroyY)
        {
            ResetHitState();
            gameObject.SetActive(false);
        }
    }

    void OnDisable()
    {
        // 오브젝트가 비활성화될 때 코루틴 정리
        ResetHitState();
    }

    void SetItemAppearance()
    {
        // 텍스트 업데이트
        if (valueText != null)
        {
            valueText.text = currentValue.ToString();
            valueText.color = currentValue >= 0 ? Color.white : Color.red;
        }
    }

    public void OnBulletHit(int bulletDamage)
    {
        if (this.gameObject.activeSelf == false)
            return;

        // 마지막 히트 시간 업데이트
        lastHitTime = Time.time;

        // 처음 맞는 경우에만 값 증가 시작
        if (!isBeingHit)
        {
            StartValueIncrease();
            Debug.Log($"아이템이 총알에 맞기 시작함! 시작 값: {currentValue}");
        }

        // 히트 이펙트
        StartCoroutine(HitEffect());
    }

    IEnumerator HitEffect()
    {
        // 간단한 시각적 효과 (크기 변화 등)
        Vector3 originalScale = transform.localScale;
        transform.localScale = originalScale * 1.1f;
        yield return new WaitForSeconds(0.1f);
        transform.localScale = originalScale;
    }

    // 연속 값 증가 코루틴
    IEnumerator ContinuousValueIncrease()
    {
        while (isBeingHit)
        {
            // 1초 대기
            yield return new WaitForSeconds(1f);

            // 여전히 맞고 있는지 확인 (Update에서 체크하므로 이중 확인)
            if (isBeingHit)
            {
                currentValue++;
                SetItemAppearance();
                Debug.Log($"값 증가! 현재 값: {currentValue}");
            }
        }
    }

    // 값 증가 시작
    private void StartValueIncrease()
    {
        if (!isBeingHit)
        {
            isBeingHit = true;
            if (valueIncreaseCoroutine != null)
            {
                StopCoroutine(valueIncreaseCoroutine);
            }
            valueIncreaseCoroutine = StartCoroutine(ContinuousValueIncrease());
        }
    }

    // 값 증가 중단
    private void StopValueIncrease()
    {
        isBeingHit = false;
        if (valueIncreaseCoroutine != null)
        {
            StopCoroutine(valueIncreaseCoroutine);
            valueIncreaseCoroutine = null;
        }
    }

    // 히트 상태 초기화
    private void ResetHitState()
    {
        StopValueIncrease();
        lastHitTime = 0f;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet"))
        {
            // 총알과 충돌 시 히트 처리
            Bullet bullet = other.GetComponent<Bullet>();
            OnBulletHit(bullet.damage);

            // 총알은 제거하지 않음 (몬스터도 맞을 수 있도록)
            return;
        }

        if (other.CompareTag("Player"))
        {
            // 플레이어와 충돌 시에만 아이템 효과 적용
            Player player = other.GetComponent<Player>();
            if (player == null)
            {
                player = other.GetComponentInParent<Player>();
            }
            if (player == null)
            {
                player = FindObjectOfType<Player>();
            }

            if (player != null && currentValue > 0) // 양수일 때만 효과 적용
            {
                ApplyEffect(player);
                player.OnItemCollected(currentValue);
                isMoving = false; // 이동 정지
                ResetHitState(); // 히트 상태 정리
                gameObject.SetActive(false);
            }
            else if (currentValue <= 0)
            {
                // 음수나 0이면 효과 없음
                ShowFloatingText("No Effect!", Color.gray);
                isMoving = false;
                ResetHitState(); // 히트 상태 정리
                gameObject.SetActive(false);
            }
        }
    }

    void ApplyEffect(Player player)
    {
        switch (itemType)
        {
            case ItemType.Attack:
                player.IncreaseAttack(currentValue);
                ShowFloatingText("ATK +" + currentValue, Color.red);
                break;
            case ItemType.Health:
                player.Heal(currentValue);
                ShowFloatingText("HP +" + currentValue, Color.green);
                break;
            case ItemType.FireRate:
                player.IncreaseFireRate(0.01f * currentValue); // 더 세밀한 조정
                ShowFloatingText("SPEED UP!", Color.blue);
                break;
            case ItemType.Shield:
                // 임시 무적 효과 등 구현 가능
                ShowFloatingText("SHIELD +" + currentValue, Color.yellow);
                break;
        }
    }

    void ShowFloatingText(string text, Color color)
    {
        // 간단한 플로팅 텍스트 (추후 구현)
        Debug.Log($"[{itemType}] {text}");
    }
}