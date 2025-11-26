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
    [Header("UI")]
    public TextMeshPro valueText;

    private float destroyY = -6f;
    private bool isMoving = true;

    void OnEnable()
    {
        // 초기 설정
        currentValue = baseValue;
        isMoving = true;

        // 랜덤 아이템 타입과 기본값 설정
        //itemType = (ItemType)Random.Range(0, 4);

        // 초기 색상 및 텍스트 설정
        //SetItemAppearance();
    }

    private void LateUpdate()
    {
        transform.Translate(new Vector3(0, 0, -1) * Time.deltaTime * itemSpeed);

        if (isMoving) // 아직 플레이어에게 닿지 않았으면
        {

        }

        if(transform.position.z < destroyY)
        {
            gameObject.SetActive(false);
        }
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
        // 총알에 맞으면 값 증가
        //currentValue += bulletDamage;

        if (this.gameObject.activeSelf == false)
            return;

        // 외형 업데이트
        SetItemAppearance();

        // 히트 이펙트 (간단한 크기 변화)
        StartCoroutine(HitEffect());

        Debug.Log($"아이템이 총알에 맞음! 현재 값: {currentValue}");
    }

    IEnumerator HitEffect()
    {
        yield return new WaitForSeconds(0.1f);
        //Vector3 originalScale = transform.localScale;
        //transform.localScale = originalScale * 1.2f;
        currentValue++;
        //transform.localScale = originalScale;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet"))
        {
            // 총알과 충돌 시 값 증가
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
                gameObject.SetActive(false);
            }
            else if (currentValue <= 0)
            {
                // 음수나 0이면 효과 없음
                ShowFloatingText("No Effect!", Color.gray);
                isMoving = false;
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