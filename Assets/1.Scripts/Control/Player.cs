using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using Spine.Unity;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float minX = -3f;
    public float maxX = 3f;

    [Header("Combat")]
    public float fireRate = 0.3f;
    public Transform firePoint;
    public int attackPower = 1;

    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;
    public Text healthText;
    public Text attackText;
    public Text characterCountText;
    public Slider healthBar;

    [Header("Character Management")]
    public int maxCharacters = 20;
    public GameObject characterPrefab;
    public float characterSpacing = 1.5f;
    public int charactersPerKill = 1;
    public int charactersPerItem = 2;

    private float fireTimer;
    private List<Transform> characters = new List<Transform>();
    private List<SkeletonAnimation> character_Skins = new List<SkeletonAnimation>();
    private int m_PlayerLevel = 1;
    void Start()
    {
        currentHealth = maxHealth;

        // 첫 번째 캐릭터 생성 (기존 플레이어를 첫 번째 캐릭터로 사용)
        CreateInitialCharacter();
        UpdateUI();
    }

    void Update()
    {
        HandleMovement();
        HandleShooting();
    }

    void HandleMovement()
    {
        float horizontal = 0f;

        // 키보드 입력
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            horizontal = -1f;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            horizontal = 1f;

        // 터치 입력 (모바일)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 touchPos = Camera.main.ScreenToWorldPoint(touch.position);

            if (touchPos.x < transform.position.x - 0.5f)
                horizontal = -1f;
            else if (touchPos.x > transform.position.x + 0.5f)
                horizontal = 1f;
        }

        Vector3 movement = new Vector3(horizontal * moveSpeed * Time.deltaTime, 0, 0);
        Vector3 newPos = transform.position + movement;
        newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
        transform.position = newPos;
    }

    void HandleShooting()
    {
        fireTimer += Time.deltaTime;
        if (fireTimer >= fireRate)
        {
            ShootAll();
            fireTimer = 0f;
        }
    }

    void ShootAll()
    {
        // 모든 캐릭터가 동시에 사격
        foreach (Transform character in characters)
        {
            if (character != null)
            {
                Transform characterFirePoint = null;

                if (character == transform)
                {
                    // 메인 플레이어인 경우 기존 firePoint 사용
                    characterFirePoint = firePoint;
                }
                else
                {
                    // 자식 캐릭터인 경우 FirePoint 찾기
                    characterFirePoint = character.Find("FirePoint");
                }

                if (characterFirePoint != null)
                {
                    GameObject bullet = ObjectPool.Instance.GetPooledObject($"Bullet_{m_PlayerLevel}");
                    if (bullet == null)
                        bullet = ObjectPool.Instance.GetPooledObject($"Bullet_{m_PlayerLevel - 1}");

                    if (bullet != null)
                    {
                        bullet.transform.position = characterFirePoint.position;
                        bullet.transform.rotation = characterFirePoint.rotation;
                        bullet.GetComponent<Bullet>().damage = attackPower;
                        bullet.SetActive(true);
                    }
                  
                }
            }
        }
    }

    void CreateInitialCharacter()
    {
        // 현재 플레이어를 첫 번째 캐릭터로 등록
        characters.Add(transform);
        character_Skins.Add(this.gameObject.GetComponentInChildren<SkeletonAnimation>());
        UpdateCharacterFormation();
    }

    public void AddCharacter(int _cnt)
    {
        if (characters.Count >= maxCharacters) return;

        GameObject newCharacter;

        for(int i= 0; i < _cnt; i++)
        {
            if (characterPrefab != null)
            {
                newCharacter = Instantiate(characterPrefab, transform);
            }
            else
            {
                // 기본 큐브 캐릭터 생성
                newCharacter = CreateBasicCharacter();
            }

            // 새 캐릭터를 리스트에 추가
            characters.Add(newCharacter.transform);
            character_Skins.Add(newCharacter.GetComponentInChildren<SkeletonAnimation>());

            StartCoroutine(CharacterSpawnEffect(newCharacter.transform));
        }
        UpdateCharacterFormation();
        UpdateUI();
    }

    GameObject CreateBasicCharacter()
    {
        GameObject character = GameObject.CreatePrimitive(PrimitiveType.Cube);
        character.name = "PlayerCharacter_" + characters.Count;
        character.tag = "Player";

        // Player 오브젝트의 자식으로 설정
        character.transform.SetParent(transform);
        character.transform.localPosition = Vector3.zero;

        // 색상 설정
        Renderer renderer = character.GetComponent<Renderer>();
        renderer.material.color = Color.blue;

        // Fire Point 생성
        GameObject firePointObj = new GameObject("FirePoint");
        firePointObj.transform.SetParent(character.transform);
        firePointObj.transform.localPosition = new Vector3(0, 0.5f, 0.5f);

        // Collider를 Trigger로 설정
        Collider collider = character.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        // Rigidbody 제거 (메인 플레이어가 움직임 제어)
        if (character.GetComponent<Rigidbody>() != null)
        {
            DestroyImmediate(character.GetComponent<Rigidbody>());
        }

        return character;
    }

    IEnumerator CharacterSpawnEffect(Transform character)
    {
        Vector3 originalScale = Vector3.one;
        character.localScale = Vector3.zero;

        float duration = 0.3f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            character.localScale = Vector3.Lerp(Vector3.zero, originalScale, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        character.localScale = originalScale;
    }

    void UpdateCharacterFormation()
    {
        int characterCount = characters.Count;
        if (characterCount <= 1) return;

        // 둘러싸는 배치 시스템
        List<Vector3> positions = GetFormationPositions(characterCount);

        for (int i = 0; i < characterCount; i++)
        {
            if (characters[i] != null)
            {
                if (characters[i] == transform)
                {
                    // 메인 플레이어는 항상 중앙(0,0,0)에 위치
                    continue;
                }
                else
                {
                    // 자식 캐릭터들을 둘러싸는 위치에 배치
                    int positionIndex = i - 1; // 메인 플레이어 제외
                    if (positionIndex < positions.Count)
                    {
                        characters[i].localPosition = positions[positionIndex];
                    }
                }
            }
        }
    }

    List<Vector3> GetFormationPositions(int totalCount)
    {
        List<Vector3> positions = new List<Vector3>();
        int extraCharacters = totalCount - 1; // 메인 플레이어 제외

        switch (extraCharacters)
        {
            case 0:
                // 메인 플레이어만 있음
                break;

            case 1:
                // 1명 추가: 오른쪽
                positions.Add(new Vector3(characterSpacing, 0, 0));
                break;

            case 2:
                // 2명 추가: 좌우
                positions.Add(new Vector3(-characterSpacing, 0, 0));
                positions.Add(new Vector3(characterSpacing, 0, 0));
                break;

            case 3:
                // 3명 추가: 좌우 + 뒤
                positions.Add(new Vector3(-characterSpacing, 0, 0));
                positions.Add(new Vector3(characterSpacing, 0, 0));
                positions.Add(new Vector3(0, 0, -characterSpacing));
                break;

            case 4:
                // 4명 추가: 사방향
                positions.Add(new Vector3(-characterSpacing, 0, 0));     // 왼쪽
                positions.Add(new Vector3(characterSpacing, 0, 0));      // 오른쪽
                positions.Add(new Vector3(0, 0, characterSpacing));      // 앞
                positions.Add(new Vector3(0, 0, -characterSpacing));     // 뒤
                break;

            case 5:
                // 5명 추가: 사방향 + 대각선 1개
                positions.Add(new Vector3(-characterSpacing, 0, 0));
                positions.Add(new Vector3(characterSpacing, 0, 0));
                positions.Add(new Vector3(0, 0, characterSpacing));
                positions.Add(new Vector3(0, 0, -characterSpacing));
                positions.Add(new Vector3(-characterSpacing * 0.7f, 0, characterSpacing * 0.7f));
                break;

            case 6:
                // 6명 추가: 사방향 + 대각선 2개
                positions.Add(new Vector3(-characterSpacing, 0, 0));
                positions.Add(new Vector3(characterSpacing, 0, 0));
                positions.Add(new Vector3(0, 0, characterSpacing));
                positions.Add(new Vector3(0, 0, -characterSpacing));
                positions.Add(new Vector3(-characterSpacing * 0.7f, 0, characterSpacing * 0.7f));
                positions.Add(new Vector3(characterSpacing * 0.7f, 0, characterSpacing * 0.7f));
                break;

            case 7:
                // 7명 추가: 사방향 + 대각선 3개
                positions.Add(new Vector3(-characterSpacing, 0, 0));
                positions.Add(new Vector3(characterSpacing, 0, 0));
                positions.Add(new Vector3(0, 0, characterSpacing));
                positions.Add(new Vector3(0, 0, -characterSpacing));
                positions.Add(new Vector3(-characterSpacing * 0.7f, 0, characterSpacing * 0.7f));
                positions.Add(new Vector3(characterSpacing * 0.7f, 0, characterSpacing * 0.7f));
                positions.Add(new Vector3(-characterSpacing * 0.7f, 0, -characterSpacing * 0.7f));
                break;

            default:
                // 8명 이상: 원형 배치
                float radius = characterSpacing * 1.2f;
                for (int i = 0; i < extraCharacters; i++)
                {
                    float angle = (360f / extraCharacters) * i * Mathf.Deg2Rad;
                    float x = Mathf.Cos(angle) * radius;
                    float z = Mathf.Sin(angle) * radius;
                    positions.Add(new Vector3(x, 0, z));
                }
                break;
        }

        return positions;
    }

    public void OnItemCollected(int _val)
    {
        AddCharacter(_val);
    }

    public void UPdatePlayerCharacter()
    {
        m_PlayerLevel++;
        foreach(SkeletonAnimation _anim in character_Skins)
        {
            _anim.skeleton.SetSkin($"Veil_{m_PlayerLevel}");
            _anim.Skeleton.SetSlotsToSetupPose();
            _anim.AnimationState.Apply(_anim.Skeleton);
        };
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        UpdateUI();

        if (currentHealth <= 0)
        {
            GameManager.Instance.GameOver();
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        UpdateUI();
    }

    public void IncreaseAttack(int amount)
    {
        attackPower += amount;
        UpdateUI();
    }

    public void IncreaseFireRate(float amount)
    {
        fireRate = Mathf.Max(0.1f, fireRate - amount);
    }

    void UpdateUI()
    {
        if (healthText != null)
            healthText.text = currentHealth + "/" + maxHealth;
        if (attackText != null)
            attackText.text = "ATK: " + attackPower;
        if (characterCountText != null)
            characterCountText.text = "Characters: " + characters.Count;
        if (healthBar != null)
            healthBar.value = (float)currentHealth / maxHealth;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Monster"))
        {
            TakeDamage(10);
            other.gameObject.SetActive(false);
        }
    }

    [ContextMenu("Simulate Bullet Hit")]
    public void SimulateUpgraed()
    {
        UPdatePlayerCharacter();
    }
}