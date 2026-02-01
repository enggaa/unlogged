using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using BrightSouls;
using Patterns.Observer;

public class TestSceneBuilder : MonoBehaviour, IObserver
{
    // ─── Inspector ────────────────────────────────────────────────
    [Header("Player")]
    [SerializeField] private float playerMaxHp       = 100f;
    [SerializeField] private float playerAttackDmg   = 25f;
    [SerializeField] private float playerMoveSpeed   = 5f;
    [SerializeField] private float playerAttackRange = 2.5f;

    [Header("Enemy")]
    [SerializeField] private int   enemyCount          = 3;
    [SerializeField] private float enemyMaxHp          = 50f;
    [SerializeField] private float enemyAttackDmg      = 10f;
    [SerializeField] private float enemyMoveSpeed      = 2.5f;
    [SerializeField] private float enemyAttackRange    = 1.8f;
    [SerializeField] private float enemyAttackCooldown = 2f;
    [SerializeField] private float enemyDetectRange    = 15f;

    // ─── Runtime ──────────────────────────────────────────────────
    private GameObject playerObj;
    private Camera     mainCam;
    private bool       playerDead;

    private HealthAttribute    playerHp;
    private MaxHealthAttribute playerMaxHpAttr;

    private List<EnemyData> enemies     = new List<EnemyData>();
    private Dictionary<GameObject, float> flashTimers = new Dictionary<GameObject, float>();

    private Keyboard _keyboard;
    private Mouse    _mouse;

    // 카메라 궤도
    private float _camYaw         = 0f;
    private float _camPitch       = 30f;
    private float _camDistance     = 12f;
    private float _camTargetDist  = 12f;
    private const float CAM_MIN_DIST   = 4f;
    private const float CAM_MAX_DIST   = 22f;
    private const float CAM_MIN_PITCH  = 10f;
    private const float CAM_MAX_PITCH  = 80f;
    private const float CAM_SENSITIVITY = 3f;
    private const float CAM_ZOOM_SPEED  = 4f;

    // 락온
    private EnemyData _lockOnTarget = null;
    private static readonly Color LOCKON_COLOR = new Color(0f, 1f, 1f);  // cyan

    // UI
    private Text  playerHpText;
    private Text  statusText;
    private Text  lockOnText;
    private Text  damageLogText;
    private float damageLogTimer;

    // ─── Inner Class ──────────────────────────────────────────────
    private class EnemyData
    {
        public GameObject         obj;
        public HealthAttribute    hp;
        public MaxHealthAttribute maxHp;
        public float              lastAttackTime;
        public bool               dead;
    }

    // ─── Unity Events ─────────────────────────────────────────────
    private void Start()
    {
        CreateGround();
        CreateCamera();
        CreatePlayer();
        CreateEnemies();
        CreateUI();

        _keyboard = Keyboard.current;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        this.Observe(Message.Combat_Hit);
        this.Observe(Message.Combat_Death);

        Debug.Log("========================================");
        Debug.Log("🎮 테스트 씬 준비 완료!");
        Debug.Log("WASD: 이동 | 좌클릭: 공격 | RMB드래그: 카메라 | 스크롤: 줌 | R: 재시작");
        Debug.Log("========================================");
    }

    private void Update()
    {
        _keyboard = Keyboard.current;
        _mouse    = Mouse.current;

        if (_keyboard != null && _keyboard.rKey.wasPressedThisFrame)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            return;
        }

        if (playerDead)
        {
            UpdateUI();
            return;
        }

        HandleLockOn();
        HandlePlayerMovement();
        HandlePlayerAttack();
        HandleCamera();
        UpdateEnemies();
        UpdateFlashes();
        UpdateUI();
    }

    // ─── Scene Setup ──────────────────────────────────────────────
    private void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "TestGround";
        ground.transform.localScale = new Vector3(10, 1, 10);
        SetColor(ground, new Color(0.3f, 0.55f, 0.3f));
    }

    private void CreateCamera()
    {
        GameObject camObj = new GameObject("TestCamera");
        mainCam = camObj.AddComponent<Camera>();
        mainCam.tag = "MainCamera";
        mainCam.fieldOfView = 60f;

        _camYaw       = 0f;
        _camPitch     = 30f;
        _camDistance   = 12f;
        _camTargetDist = 12f;
    }

    private void CreatePlayer()
    {
        playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerObj.name = "TestPlayer";
        playerObj.tag  = "Player";
        playerObj.transform.position = new Vector3(0, 1, 0);
        SetColor(playerObj, new Color(0.2f, 0.4f, 0.9f));

        playerHp        = new HealthAttribute(playerMaxHp);
        playerMaxHpAttr = new MaxHealthAttribute(playerMaxHp);
        playerDead      = false;

        playerHp.onAttributeChanged += (oldVal, newVal) =>
        {
            if (newVal <= 0f && !playerDead)
            {
                playerDead = true;
                SetColor(playerObj, Color.black);
                this.Notify(Message.Combat_Death, "Player");
                ShowDamageLog("💀 Player 사망! R키로 재시작하세요.");
            }
        };
    }

    private void CreateEnemies()
    {
        enemies.Clear();
        for (int i = 0; i < enemyCount; i++)
        {
            float angle = (360f / enemyCount) * i;
            float rad   = angle * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Sin(rad) * 8f, 0.75f, Mathf.Cos(rad) * 8f);

            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = $"Enemy_{i}";
            obj.transform.position   = pos;
            obj.transform.localScale = new Vector3(1, 1.5f, 1);
            SetColor(obj, new Color(0.9f, 0.2f, 0.2f));

            var data = new EnemyData
            {
                obj            = obj,
                hp             = new HealthAttribute(enemyMaxHp),
                maxHp          = new MaxHealthAttribute(enemyMaxHp),
                lastAttackTime = 0f,
                dead           = false
            };

            int idx = i;
            data.hp.onAttributeChanged += (oldVal, newVal) =>
            {
                if (newVal <= 0f && !data.dead)
                {
                    data.dead = true;
                    SetColor(obj, new Color(0.4f, 0.4f, 0.4f));
                    this.Notify(Message.Combat_Death, $"Enemy_{idx}");
                    ShowDamageLog($"💀 Enemy_{idx} 격倒!");
                }
            };

            enemies.Add(data);
        }
    }

    // ─── Player ───────────────────────────────────────────────────
    private void HandlePlayerMovement()
    {
        if (_keyboard == null) return;

        float x = (_keyboard.aKey.isPressed ? -1f : 0f) + (_keyboard.dKey.isPressed ? 1f : 0f);
        float z = (_keyboard.sKey.isPressed ? -1f : 0f) + (_keyboard.wKey.isPressed ? 1f : 0f);

        if (x != 0f || z != 0f)
        {
            Vector3 camRight   = mainCam.transform.right;
            Vector3 camForward = mainCam.transform.forward;
            camRight.y   = 0f; camRight.Normalize();
            camForward.y = 0f; camForward.Normalize();

            Vector3 moveDir = (camRight * x + camForward * z).normalized;
            playerObj.transform.position += moveDir * playerMoveSpeed * Time.deltaTime;

            // 락온 중이면 타겟 응시 (참조: PlayerCombatController.FaceTarget)
            // 락온 아니면 이동 방향으로 회전
            if (_lockOnTarget == null)
                playerObj.transform.rotation = Quaternion.LookRotation(moveDir);
        }

        // 락온 중 → 항상 타겟 응시
        if (_lockOnTarget != null)
        {
            Vector3 toTarget = _lockOnTarget.obj.transform.position - playerObj.transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(toTarget);
                playerObj.transform.rotation = Quaternion.Lerp(
                    playerObj.transform.rotation, lookRot, 7.5f * Time.deltaTime);
            }
        }
    }

    private void HandlePlayerAttack()
    {
        if (_mouse == null || !_mouse.leftButton.wasPressedThisFrame) return;

        EnemyData attackTarget = null;

        // 락온 중 → 락온 타겟 우선
        if (_lockOnTarget != null && !_lockOnTarget.dead)
        {
            attackTarget = _lockOnTarget;
        }
        else
        {
            // 락온 없음 → 가장 가까운 적
            float closestDist = Mathf.Infinity;
            foreach (var e in enemies)
            {
                if (e.dead) continue;
                float dist = Vector3.Distance(playerObj.transform.position, e.obj.transform.position);
                if (dist < closestDist) { closestDist = dist; attackTarget = e; }
            }
        }

        if (attackTarget == null)
        {
            ShowDamageLog("⚔️ 공격할 적이 없습니다.");
            return;
        }

        float distToTarget = Vector3.Distance(playerObj.transform.position, attackTarget.obj.transform.position);
        if (distToTarget <= playerAttackRange)
        {
            attackTarget.hp.Value = Mathf.Max(attackTarget.hp.Value - playerAttackDmg, 0f);

            SetColor(attackTarget.obj, Color.white);
            flashTimers[attackTarget.obj] = 0.15f;

            string log = $"⚔️ Player → {attackTarget.obj.name}: -{playerAttackDmg} (HP: {attackTarget.hp.Value:F0})";
            ShowDamageLog(log);
            this.Notify(Message.Combat_Hit, log);
        }
        else
        {
            ShowDamageLog("⚔️ 공격 범위 밖! 적에게 가까이 가세요.");
        }
    }

    // ─── Lock-On ──────────────────────────────────────────────────
    // 참조: LockOnDetector (가장 가까운 타겟 선택), LockOnCamera (카메라 오토 추적)
    private void HandleLockOn()
    {
        if (_keyboard == null || !_keyboard.tabKey.wasPressedThisFrame) return;

        // 이미 락온 중 → 해제
        if (_lockOnTarget != null)
        {
            UnlockTarget();
            return;
        }

        // 가장 가까운 적 선택 (참조: LockOnDetector → FindLockOnTarget 주석의 가장 가까운 타겟 로직)
        EnemyData closest     = null;
        float     closestDist = Mathf.Infinity;
        foreach (var e in enemies)
        {
            if (e.dead) continue;
            float dist = Vector3.Distance(playerObj.transform.position, e.obj.transform.position);
            if (dist < closestDist) { closestDist = dist; closest = e; }
        }

        if (closest != null)
        {
            _lockOnTarget = closest;
            SetColor(closest.obj, LOCKON_COLOR);
            ShowDamageLog($"🎯 {closest.obj.name}에 락온!");
        }
        else
        {
            ShowDamageLog("🎯 락온할 적이 없습니다.");
        }
    }

    private void UnlockTarget()
    {
        if (_lockOnTarget != null && !_lockOnTarget.dead)
            SetColor(_lockOnTarget.obj, new Color(1f, 0.45f, 0.1f));
        _lockOnTarget = null;
        ShowDamageLog("🎯 락온 해제");
    }

    // ─── Camera ───────────────────────────────────────────────────
    private void HandleCamera()
    {
        if (_mouse == null) return;

        // 락온 타겟이 죽었으면 자동 해제
        if (_lockOnTarget != null && _lockOnTarget.dead)
            UnlockTarget();

        // ESC → 커서 잠금 토글
        if (_keyboard != null && _keyboard.escapeKey.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
            }
        }

        if (_lockOnTarget != null)
        {
            // ── 락온 카메라 (참조: LockOnCamera) ──
            // player와 target의 중간점을 기준으로 궤도, yaw는 오토 추적
            Vector3 playerPos = playerObj.transform.position + Vector3.up * 0.5f;
            Vector3 targetPos = _lockOnTarget.obj.transform.position + Vector3.up * 0.75f;
            Vector3 midPoint  = (playerPos + targetPos) * 0.5f;

            // 카메라는 player 기준 궤도이지만, yaw는 target 방향 반대편으로 오토 lerp
            Vector3 toTarget  = targetPos - playerPos;
            toTarget.y = 0f;
            float desiredYaw  = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg + 180f;

            // 최단 경로 회전 (각도 래핑 처리)
            float diff = Mathf.DeltaAngle(_camYaw, desiredYaw);
            _camYaw += diff * 6f * Time.deltaTime;

            // pitch는 거리에 따라 자동 조정 (가까울수록 높은 각도)
            float dist = Vector3.Distance(playerPos, targetPos);
            float targetPitch = Mathf.Lerp(45f, 25f, Mathf.InverseLerp(1.5f, 10f, dist));
            _camPitch = Mathf.Lerp(_camPitch, targetPitch, 4f * Time.deltaTime);

            // 스크롤 → 줌 (락온 중에도 유지)
            float scroll = _mouse.scroll.ReadValue().y;
            if (scroll != 0f)
                _camTargetDist = Mathf.Clamp(_camTargetDist - scroll * 2f, CAM_MIN_DIST, CAM_MAX_DIST);
            _camDistance = Mathf.Lerp(_camDistance, _camTargetDist, CAM_ZOOM_SPEED * Time.deltaTime);

            // 구면 좌표 → 월드 좌표
            Vector3 offset    = Quaternion.Euler(_camPitch, _camYaw, 0f) * (Vector3.back * _camDistance);
            Vector3 camTarget = playerPos + offset;

            mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, camTarget, 8f * Time.deltaTime);
            // 락온 중 LookAt은 midPoint (참조: LockOnCamera의 Cinemachine LookAt 동일)
            mainCam.transform.LookAt(midPoint);
        }
        else
        {
            // ── 일반 궤도 카메라 ──
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 delta = _mouse.delta.ReadValue();
                _camYaw   += delta.x * CAM_SENSITIVITY;
                _camPitch -= delta.y * CAM_SENSITIVITY;
                _camPitch  = Mathf.Clamp(_camPitch, CAM_MIN_PITCH, CAM_MAX_PITCH);
            }

            float scroll = _mouse.scroll.ReadValue().y;
            if (scroll != 0f)
                _camTargetDist = Mathf.Clamp(_camTargetDist - scroll * 2f, CAM_MIN_DIST, CAM_MAX_DIST);
            _camDistance = Mathf.Lerp(_camDistance, _camTargetDist, CAM_ZOOM_SPEED * Time.deltaTime);

            Vector3 offset    = Quaternion.Euler(_camPitch, _camYaw, 0f) * (Vector3.back * _camDistance);
            Vector3 targetPos = playerObj.transform.position + Vector3.up * 0.5f + offset;

            mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, targetPos, 8f * Time.deltaTime);
            mainCam.transform.LookAt(playerObj.transform.position + Vector3.up * 0.8f);
        }
    }

    // ─── Enemy AI ─────────────────────────────────────────────────
    private void UpdateEnemies()
    {
        foreach (var e in enemies)
        {
            if (e.dead) continue;

            float dist = Vector3.Distance(e.obj.transform.position, playerObj.transform.position);

            if (dist > enemyDetectRange)
            {
                if (!flashTimers.ContainsKey(e.obj) || flashTimers[e.obj] <= 0f)
                    SetColor(e.obj, new Color(0.9f, 0.2f, 0.2f));
                continue;
            }

            // 감지 중 주황색
            if (!flashTimers.ContainsKey(e.obj) || flashTimers[e.obj] <= 0f)
                SetColor(e.obj, new Color(1f, 0.45f, 0.1f));

            if (dist > enemyAttackRange)
            {
                Vector3 dir = (playerObj.transform.position - e.obj.transform.position).normalized;
                e.obj.transform.position += dir * enemyMoveSpeed * Time.deltaTime;
                e.obj.transform.rotation  = Quaternion.LookRotation(dir);
            }
            else if (Time.time - e.lastAttackTime >= enemyAttackCooldown)
            {
                playerHp.Value = Mathf.Max(playerHp.Value - enemyAttackDmg, 0f);
                e.lastAttackTime = Time.time;

                SetColor(e.obj, Color.white);
                flashTimers[e.obj] = 0.1f;

                SetColor(playerObj, new Color(1f, 0.3f, 0.3f));
                flashTimers[playerObj] = 0.2f;

                string log = $"💥 {e.obj.name} → Player: -{enemyAttackDmg} (HP: {playerHp.Value:F0})";
                ShowDamageLog(log);
                this.Notify(Message.Combat_Hit, log);
            }
        }
    }

    // ─── Flash ────────────────────────────────────────────────────
    private void UpdateFlashes()
    {
        var keys = new List<GameObject>(flashTimers.Keys);
        foreach (var key in keys)
        {
            flashTimers[key] -= Time.deltaTime;
            if (flashTimers[key] <= 0f)
            {
                flashTimers.Remove(key);
                if (key == playerObj && !playerDead)
                    SetColor(key, new Color(0.2f, 0.4f, 0.9f));
                else
                {
                    var enemy = enemies.Find(e => e.obj == key);
                    if (enemy != null && !enemy.dead)
                    {
                        // 락온 타겟이면 cyan, 아니면 orange
                        Color restoreColor = (enemy == _lockOnTarget) ? LOCKON_COLOR : new Color(1f, 0.45f, 0.1f);
                        SetColor(key, restoreColor);
                    }
                }
            }
        }
    }

    // ─── UI ───────────────────────────────────────────────────────
    private void CreateUI()
    {
        GameObject canvasObj = new GameObject("TestCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        statusText    = CreateText(canvas, new Vector2(0, 195), "WASD: 이동 | 좌클릭: 공격 | Tab: 락온 | 마우스: 카메라 | 스크롤: 줌 | ESC: 커서 | R: 재시작", new Color(0.85f, 0.85f, 0.85f), 16);
        playerHpText  = CreateText(canvas, new Vector2(-220, 160), $"Player HP: {playerMaxHp:F0} / {playerMaxHp:F0}", Color.green, 22);
        lockOnText    = CreateText(canvas, new Vector2(220, 160), "", new Color(0f, 1f, 1f), 20);
        damageLogText = CreateText(canvas, new Vector2(0, -170), "", Color.yellow, 21);
    }

    private Text CreateText(Canvas canvas, Vector2 pos, string text, Color color, int fontSize)
    {
        GameObject obj = new GameObject("UIText");
        obj.transform.SetParent(canvas.transform);

        Text t = obj.AddComponent<Text>();
        t.text      = text;
        t.color     = color;
        t.fontSize  = fontSize;
        t.alignment = TextAnchor.UpperCenter;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = new Vector2(500, 40);

        return t;
    }

    private void UpdateUI()
    {
        if (lockOnText != null)
        {
            if (_lockOnTarget != null && !_lockOnTarget.dead)
                lockOnText.text = $"🎯 Lock-On: {_lockOnTarget.obj.name}  HP: {_lockOnTarget.hp.Value:F0}/{_lockOnTarget.maxHp.Value:F0}";
            else
                lockOnText.text = "";
        }

        if (playerHpText != null)
        {
            playerHpText.text  = $"Player HP: {playerHp.Value:F0} / {playerMaxHpAttr.Value:F0}";
            float ratio        = playerHp.Value / playerMaxHpAttr.Value;
            playerHpText.color = Color.Lerp(Color.red, Color.green, ratio);
        }

        if (damageLogText != null)
        {
            damageLogTimer -= Time.deltaTime;
            if (damageLogTimer <= 0f)
                damageLogText.text = "";
        }

        if (statusText != null && playerDead)
        {
            statusText.text  = "💀 사망! R키로 재시작하세요.";
            statusText.color = Color.red;
        }
    }

    private void ShowDamageLog(string msg)
    {
        if (damageLogText != null)
        {
            damageLogText.text = msg;
            damageLogTimer    = 2.5f;
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────
    private void SetColor(GameObject obj, Color color)
    {
        var renderer = obj.GetComponent<MeshRenderer>();
        if (renderer == null || renderer.material == null) return;

        if (renderer.material.HasProperty("_BaseColor"))
            renderer.material.SetColor("_BaseColor", color);
        else
            renderer.material.color = color;
    }

    // ─── IObserver ────────────────────────────────────────────────
    public void OnNotification(object sender, Message subject, params object[] args)
    {
        string info = args.Length > 0 ? args[0]?.ToString() ?? "" : "";
        switch (subject)
        {
            case Message.Combat_Hit:
                Debug.Log($"[Observer ✅] Combat_Hit → {info}");
                break;
            case Message.Combat_Death:
                Debug.Log($"[Observer ✅] Combat_Death → {info}");
                break;
        }
    }
}