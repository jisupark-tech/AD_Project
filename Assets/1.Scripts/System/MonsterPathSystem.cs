using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class MonsterPathSystem : MonoBehaviour
{
    [Header("패스 설정")]
    [SerializeField] private Transform[] pathPoints; // 패스 포인트들
    [SerializeField] private LineRenderer pathRenderer; // 패스를 시각적으로 표시
    [SerializeField] private bool showPath = true;

    [Header("몬스터 설정")]
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private RuntimeAnimatorController monsterAnimatorController; // 몬스터용 애니메이터 컨트롤러
    [SerializeField] private int totalMonsters = 20;
    [SerializeField] private float spawnInterval = 0.5f;
    [SerializeField] private float monsterSpeed = 2f;

    [Header("사망 처리 설정")]
    [SerializeField] private float killInterval = 0.3f;

    private List<Monster> aliveMonsters = new List<Monster>();
    private List<Monster> allMonsters = new List<Monster>();
    private bool isSpawning = false;
    private bool isKillingSequentially = false;

    // 몬스터 클래스
    public class Monster : MonoBehaviour
    {
        public Animator animator;
        public bool isDead = false;
        public float moveSpeed = 2f;
        public Transform[] pathPoints;
        public int currentPathIndex = 0;
        public MonsterPathSystem pathSystem;
        //public TextMeshPro txtDamage;
        public int Damage = 5;
        public SpriteRenderer m_sprite;
        private void Start()
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
                //if(animator==null)
                //{
                //    // Animator가 없으면 생성
                //    animator = gameObject.AddComponent<Animator>();
                //}
            }
            //txtDamage = GetComponent<TextMeshPro>();
            //if(txtDamage==null)
            //{
            //    txtDamage = GetComponentInChildren<TextMeshPro>();
            //    if(txtDamage)
            //    {
            //        txtDamage.gameObject.SetActive(false);
            //    }
            //}

            m_sprite = GetComponent<SpriteRenderer>();
            if (m_sprite == null)
            {
                m_sprite = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void Update()
        {
            if (isDead) return;

            MoveAlongPath();
        }

        private void MoveAlongPath()
        {
            if (pathPoints == null || pathPoints.Length == 0) return;

            // 현재 목표 지점으로 이동
            Transform targetPoint = pathPoints[currentPathIndex];
            Vector3 direction = (targetPoint.position - transform.position).normalized;

            transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, moveSpeed * Time.deltaTime);

            // 목표 지점에 도달했는지 확인
            if (Vector3.Distance(transform.position, targetPoint.position) < 0.1f)
            {
                currentPathIndex++;

                // 마지막 지점에 도달하면 다시 0번 인덱스로 순환
                if (currentPathIndex >= pathPoints.Length)
                {
                    currentPathIndex = 0; // 처음부터 다시 시작
                }
            }

            // 이동 방향에 따라 회전
            //if (direction != Vector3.zero)
            //{
            //    transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
            //}
        }

        // ReachEnd 메서드는 더 이상 사용되지 않음 (무한 순환으로 변경)
        /*
        private void ReachEnd()
        {
            pathSystem.RemoveMonster(this);
            Destroy(gameObject);
        }
        */

        public void Die()
        {
            if (isDead) return;

            isDead = true;

            // 사망 애니메이션 실행 (AnimatorController가 설정되어 있는 경우에만)
            if (animator != null)
            {
                animator.SetTrigger("Death");
            }
            //if (txtDamage)
            //{
            //    txtDamage.gameObject.SetActive(true);
            //    txtDamage.text = Damage.ToString();
            //}
            // 몬스터 리스트에서 제거
            pathSystem.RemoveMonster(this);

            // 일정 시간 후 오브젝트 제거
            StartCoroutine(DestroyAfterDelay(0.9f));
        }

        private IEnumerator DestroyAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            // m_sprite가 이미 선언 및 할당되어 있다고 가정합니다.
            // 예: SpriteRenderer m_sprite = GetComponent<SpriteRenderer>();

            //Color startColor = txtDamage.color; // 텍스트의 시작 색상
            //Color spriteStartColor = m_sprite.color; // 스프라이트의 시작 색상

            // 빨간색 하이라이트 색상 설정 (색상 변화를 더 잘 보이게 하기 위해)
            //Color highlightColor = Color.red;

            //if (txtDamage /*&& m_sprite*/)
            //{
            //    float elapsedTime = 0f;
            //    Vector3 startPosition = txtDamage.transform.position;

            //    float moveDistance = 1.0f;

            //    while (elapsedTime < delay)
            //    {
            //        elapsedTime += Time.deltaTime;
            //        float t = elapsedTime / delay; // 0 (시작) 에서 1 (끝) 으로 진행되는 비율

            //        // --- 1. 위치 이동 (Y축으로 상승) ---
            //        txtDamage.transform.position = Vector3.Lerp(
            //            startPosition,
            //            startPosition + new Vector3(0,0,1) * moveDistance, // Y축(위)으로 이동
            //            t
            //        );


            //        //txtDamage.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);

            //        //float colorT = Mathf.Sin(t * Mathf.PI); // t가 0->1로 갈 때 0->1->0으로 변하는 값
            //        //m_sprite.color = Color.Lerp(spriteStartColor, highlightColor, colorT);

            //        // 다음 프레임까지 대기
            //        yield return null;
            //    }
            //}

            // 애니메이션(delay 시간)이 완료된 후 오브젝트 파괴
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetupPath();
        StartCoroutine(SpawnMonstersSequentially());
    }

    private void Update()
    {
        HandleInput();
        UpdateUI();
    }

    private void HandleInput()
    {
        // Q 키: 랜덤 몬스터 사망
        if (Input.GetKeyDown(KeyCode.Q))
        {
            KillRandomMonster();
        }

        // W 키: 순차적 몬스터 사망
        if (Input.GetKeyDown(KeyCode.W) && !isKillingSequentially)
        {
            StartCoroutine(KillMonstersSequentially());
        }

        // R 키: 몬스터 재소환
        if (Input.GetKeyDown(KeyCode.R) && !isSpawning)
        {
            RestartSpawning();
        }
    }

    private void UpdateUI()
    {
        // 화면에 현재 상태 표시 (선택사항)
        if (Application.isPlaying)
        {
            // Debug.Log로 대신 표시
            if (Time.frameCount % 60 == 0) // 1초마다
            {
                Debug.Log($"살아있는 몬스터: {aliveMonsters.Count} / 총 소환된 몬스터: {allMonsters.Count}");
            }
        }
    }

    private void SetupPath()
    {
        // 패스 포인트가 설정되지 않았으면 원형 패스 생성
        if (pathPoints == null || pathPoints.Length == 0)
        {
            CreateCircularPath();
        }

        //// 패스 라인 렌더러 설정
        //if (pathRenderer == null)
        //{
        //    // LineRenderer가 없으면 생성
        //    pathRenderer = GetComponent<LineRenderer>();
        //    if (pathRenderer == null)
        //    {
        //        pathRenderer = gameObject.AddComponent<LineRenderer>();
        //    }
        //}

        //if (pathRenderer != null && showPath)
        //{
        //    SetupPathRenderer();
        //}
    }

    private void CreateCircularPath()
    {
        int pointCount = 20;
        float radius = 3f;
        Vector3 center = transform.position;

        pathPoints = new Transform[pointCount];

        for (int i = 0; i < pointCount; i++)
        {
            GameObject pathPoint = new GameObject($"PathPoint_{i}");
            pathPoint.transform.parent = transform;

            float angle = (float)i / pointCount * 360f * Mathf.Deg2Rad;
            Vector3 position = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            pathPoint.transform.position = position;

            pathPoints[i] = pathPoint.transform;
        }
    }

    //private void SetupPathRenderer()
    //{
    //    pathRenderer.positionCount = pathPoints.Length + 1; // +1 for closing the loop
    //    pathRenderer.useWorldSpace = true;
    //    pathRenderer.loop = false;

    //    // 기본 머티리얼 설정
    //    if (pathRenderer.material == null)
    //    {
    //        Material lineMaterial = new Material(Shader.Find("Sprites/Default"));
    //        lineMaterial.color = Color.yellow;
    //        pathRenderer.material = lineMaterial;
    //    }

    //    //pathRenderer.width = 0.1f;
    //    //pathRenderer.color = Color.yellow;

    //    // 패스 포인트들의 위치를 라인 렌더러에 설정
    //    for (int i = 0; i < pathPoints.Length; i++)
    //    {
    //        pathRenderer.SetPosition(i, pathPoints[i].position);
    //    }
    //    // 루프를 닫기 위해 첫 번째 포인트를 마지막에 추가
    //    pathRenderer.SetPosition(pathPoints.Length, pathPoints[0].position);
    //}

    private IEnumerator SpawnMonstersSequentially()
    {
        isSpawning = true;

        for (int i = 0; i < totalMonsters; i++)
        {
            SpawnMonster();
            yield return new WaitForSeconds(spawnInterval);
        }

        isSpawning = false;
    }

    private void SpawnMonster()
    {
        if (pathPoints == null || pathPoints.Length == 0) return;

        GameObject monsterObj;

        // 몬스터 프리팹이 있으면 인스턴스화, 없으면 기본 큐브 생성
        if (monsterPrefab != null)
        {
            monsterObj = Instantiate(monsterPrefab, pathPoints[0].position, Quaternion.identity);
        }
        else
        {
            // 기본 몬스터 생성
            monsterObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            monsterObj.transform.position = pathPoints[0].position;
            monsterObj.transform.localScale = Vector3.one * 0.5f;

            // 초록색 머티리얼 적용
            Renderer renderer = monsterObj.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.green;
            renderer.material = mat;
        }

        // Monster 컴포넌트 추가
        Monster monster = monsterObj.AddComponent<Monster>();
        monster.pathPoints = pathPoints;
        monster.moveSpeed = monsterSpeed;
        monster.pathSystem = this;

        // Animator 컴포넌트 추가 및 설정
        //if (monsterObj.GetComponent<Animator>() == null)
        //{
        //    Animator animator = monsterObj.AddComponent<Animator>();

        //    // 미리 설정된 AnimatorController가 있으면 할당
        //    if (monsterAnimatorController != null)
        //    {
        //        animator.runtimeAnimatorController = monsterAnimatorController;
        //    }
        //}

        // 리스트에 추가
        aliveMonsters.Add(monster);
        allMonsters.Add(monster);

        monsterObj.name = $"Monster_{allMonsters.Count}";
    }

    private void KillRandomMonster()
    {
        if (aliveMonsters.Count == 0)
        {
            Debug.Log("사망시킬 몬스터가 없습니다.");
            return;
        }

        int randomIndex = Random.Range(0, aliveMonsters.Count);
        Monster targetMonster = aliveMonsters[randomIndex];

        if (targetMonster != null)
        {
            Debug.Log($"랜덤 몬스터 사망: {targetMonster.name}");
            targetMonster.Die();
        }
    }

    private IEnumerator KillMonstersSequentially()
    {
        isKillingSequentially = true;
        Debug.Log("순차적 몬스터 사망 시작");

        // 현재 살아있는 몬스터들의 복사본을 만듦
        List<Monster> monstersToKill = new List<Monster>(aliveMonsters);

        foreach (Monster monster in monstersToKill)
        {
            if (monster != null && !monster.isDead)
            {
                monster.Die();
                yield return new WaitForSeconds(killInterval);
            }
        }

        Debug.Log("순차적 몬스터 사망 완료");
        isKillingSequentially = false;
    }

    public void RemoveMonster(Monster monster)
    {
        if (aliveMonsters.Contains(monster))
        {
            aliveMonsters.Remove(monster);
        }
    }

    private void RestartSpawning()
    {
        // 기존 몬스터들 정리
        foreach (Monster monster in aliveMonsters.ToArray())
        {
            if (monster != null)
            {
                Destroy(monster.gameObject);
            }
        }
        aliveMonsters.Clear();
        allMonsters.Clear();

        // 새로운 소환 시작
        StartCoroutine(SpawnMonstersSequentially());
    }

    // 패스 포인트를 Scene 뷰에서 시각화
    private void OnDrawGizmos()
    {
        if (pathPoints == null) return;

        Gizmos.color = Color.yellow;

        // 패스 포인트들을 선으로 연결
        for (int i = 0; i < pathPoints.Length; i++)
        {
            if (pathPoints[i] == null) continue;

            // 현재 포인트 그리기
            Gizmos.DrawWireSphere(pathPoints[i].position, 0.2f);

            // 다음 포인트와 연결선 그리기
            int nextIndex = (i + 1) % pathPoints.Length;
            if (pathPoints[nextIndex] != null)
            {
                Gizmos.DrawLine(pathPoints[i].position, pathPoints[nextIndex].position);
            }
        }
    }

    // GUI로 현재 상태 표시
    private void OnGUI()
    {
        if (Application.isPlaying)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.Box("Monster Path System 상태");
            GUILayout.Label($"살아있는 몬스터: {aliveMonsters.Count}");
            GUILayout.Label($"총 소환된 몬스터: {allMonsters.Count}");
            GUILayout.Label($"소환 중: {(isSpawning ? "예" : "아니오")}");
            GUILayout.Label($"순차 사망 중: {(isKillingSequentially ? "예" : "아니오")}");
            GUILayout.Space(10);
            GUILayout.Label("컨트롤:");
            GUILayout.Label("Q - 랜덤 몬스터 사망");
            GUILayout.Label("W - 순차적 몬스터 사망");
            GUILayout.Label("R - 몬스터 재소환");
            GUILayout.EndArea();
        }
    }

    // 에디터에서 패스 재생성 (Inspector에서 호출 가능)
    [ContextMenu("Regenerate Circular Path")]
    public void RegenerateCircularPath()
    {
        // 기존 패스 포인트들 제거
        if (pathPoints != null)
        {
            for (int i = 0; i < pathPoints.Length; i++)
            {
                if (pathPoints[i] != null)
                {
#if UNITY_EDITOR
                    DestroyImmediate(pathPoints[i].gameObject);
#else
                    Destroy(pathPoints[i].gameObject);
#endif
                }
            }
        }

        CreateCircularPath();
        SetupPath();
    }

    // 현재 상태 정보 접근용 메서드들
    public int GetAliveMonsterCount()
    {
        return aliveMonsters.Count;
    }

    public int GetTotalSpawnedCount()
    {
        return allMonsters.Count;
    }

    public bool IsSpawning()
    {
        return isSpawning;
    }

    public bool IsKillingSequentially()
    {
        return isKillingSequentially;
    }

    public Transform[] GetPathPoints()
    {
        return pathPoints;
    }

    public void SetPathPoints(Transform[] newPathPoints)
    {
        pathPoints = newPathPoints;
        SetupPath();
    }
}