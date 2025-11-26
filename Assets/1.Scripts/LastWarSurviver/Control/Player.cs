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
                    // 먼저 직접적인 FirePoint 컴포넌트나 자식 오브젝트 찾기
                    characterFirePoint = character.Find("FirePoint");

                    // FirePoint가 없으면 캐릭터 자체의 transform 사용
                    if (characterFirePoint == null)
                    {
                        characterFirePoint = character;
                    }
                }

                if (characterFirePoint != null)
                {
                    GameObject bullet = ObjectPool.Instance.GetPooledObject($"Bullet_{m_PlayerLevel}");
                    if (bullet == null)
                        bullet = ObjectPool.Instance.GetPooledObject($"Bullet_{m_PlayerLevel - 1}");

                    if (bullet != null)
                    {
                        bullet.transform.position = characterFirePoint.position;
                        //bullet.transform.rotation = characterFirePoint.rotation;
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

        for (int i = 0; i < _cnt; i++)
        {
            if (characterPrefab != null)
            {
                newCharacter = Instantiate(characterPrefab, transform);
                // 피라미드 모양을 위한 각도 설정
                newCharacter.transform.localEulerAngles = new Vector3(60, 0, 0);
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
        UpdateIncreasePlayerCharacter();
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
        character.transform.eulerAngles = new Vector3(60, 0, 0);

        // 색상 설정
        Renderer renderer = character.GetComponent<Renderer>();
        renderer.material.color = Color.blue;

        // FirePoint는 characterPrefab에 이미 포함되어 있으므로 별도 생성하지 않음

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

        // 피라미드 배치 시스템
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
                    // 자식 캐릭터들을 피라미드 위치에 배치
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

        if (extraCharacters <= 0)
        {
            return positions;
        }

        // 피라미드 형태로 배치
        int charactersPlaced = 0;
        int currentRow = 1; // 첫 번째 추가 캐릭터부터 시작 (1번째 줄)

        while (charactersPlaced < extraCharacters)
        {
            int charactersInThisRow = currentRow; // 1번째 줄: 1개, 2번째 줄: 2개, 3번째 줄: 3개...
            int charactersToPlace = Mathf.Min(charactersInThisRow, extraCharacters - charactersPlaced);

            // 각 줄의 Z 위치 (앞쪽으로 배치)
            float rowZ = characterSpacing * currentRow;

            // 각 줄에서 캐릭터들의 X 위치 계산
            for (int i = 0; i < charactersToPlace; i++)
            {
                float x;

                if (charactersToPlace == 1)
                {
                    // 한 개일 때는 중앙에
                    x = 0f;
                }
                else
                {
                    // 여러 개일 때는 균등하게 분배
                    float totalWidth = (charactersToPlace - 1) * characterSpacing;
                    x = -totalWidth / 2f + i * characterSpacing;
                }

                positions.Add(new Vector3(x, 0, -rowZ));
                charactersPlaced++;

                if (charactersPlaced >= extraCharacters)
                    break;
            }

            currentRow++;
        }

        return positions;
    }

    public void RemoveCharacter(int _cnt)
    {
        // 메인 플레이어는 제거하지 않으므로 최소 1개는 유지
        int charactersToRemove = Mathf.Min(_cnt, characters.Count - 1);

        if (charactersToRemove <= 0) return;

        Debug.Log($"캐릭터 {charactersToRemove}개 제거 시작. 현재 캐릭터 수: {characters.Count}");

        // 뒤에서부터 제거 (메인 플레이어는 첫 번째이므로 보호됨)
        for (int i = 0; i < charactersToRemove; i++)
        {
            if (characters.Count > 1) // 메인 플레이어만 남을 때까지
            {
                int lastIndex = characters.Count - 1;
                Transform characterToRemove = characters[lastIndex];

                if (characterToRemove != transform) // 메인 플레이어가 아닌지 확인
                {
                    // 캐릭터 제거 효과
                    StartCoroutine(CharacterRemoveEffect(characterToRemove));

                    // 리스트에서 제거
                    characters.RemoveAt(lastIndex);
                    if (character_Skins.Count > lastIndex && character_Skins[lastIndex] != null)
                    {
                        character_Skins.RemoveAt(lastIndex);
                    }

                    Debug.Log($"캐릭터 제거됨. 남은 캐릭터 수: {characters.Count}");
                }
            }
        }

        UpdateCharacterFormation();
        UpdateUI();
    }

    IEnumerator CharacterRemoveEffect(Transform character)
    {
        if (character == null) yield break;

        Vector3 originalScale = character.localScale;
        float duration = 0.5f;
        float elapsedTime = 0f;

        // 크기를 줄여가며 제거 효과
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            character.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 완전히 제거
        if (character != null && character.gameObject != null)
        {
            DestroyImmediate(character.gameObject);
        }
    }

    public void OnItemCollected(int _val)
    {
        if (_val > 0)
        {
            AddCharacter(_val);
        }
        else if (_val < 0)
        {
            RemoveCharacter(Mathf.Abs(_val)); // 절댓값으로 변환하여 제거
        }
        // _val이 0이면 아무것도 하지 않음
    }

    public void UPdatePlayerCharacter()
    {
        m_PlayerLevel++;
        foreach (SkeletonAnimation _anim in character_Skins)
        {
            _anim.skeleton.SetSkin($"Veil_{m_PlayerLevel}");
            _anim.Skeleton.SetSlotsToSetupPose();
            _anim.AnimationState.Apply(_anim.Skeleton);
        };
    }

    public void UpdateIncreasePlayerCharacter()
    {
        foreach (SkeletonAnimation _anim in character_Skins)
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