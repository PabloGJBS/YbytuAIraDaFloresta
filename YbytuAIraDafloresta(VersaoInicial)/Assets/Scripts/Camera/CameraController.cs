using UnityEngine;

/// <summary>
/// Camera controller para beat 'em up.
/// - Movimentacao livre: acompanha o player no eixo X
/// - Zona de combate: fica fixa no centro da zona com leve elasticidade nas bordas
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow (Movimentacao Livre)")]
    [SerializeField] private float followSmoothSpeed = 5f;
    [SerializeField] private Vector2 followOffset = new Vector2(0f, 0f);

    [Header("Follow Vertical")]
    [Tooltip("Quanto a camera acompanha o eixo Y do player (0=fixo, 1=segue total)")]
    [Range(0f, 1f)]
    [SerializeField] private float verticalFollowStrength = 0.4f;
    [Tooltip("Y considerado 'neutro' do player - quando o player esta aqui, camera fica em Y base")]
    [SerializeField] private float verticalAnchorY = -2.75f;

    [Header("Peek (Olhar Pra Cima)")]
    [Tooltip("Quanto a camera sobe quando o player segura cima na borda superior")]
    [SerializeField] private float peekUpOffset = 3.5f;
    [Tooltip("Velocidade do deslocamento de peek (Lerp)")]
    [SerializeField] private float peekSmoothSpeed = 4f;

    [Header("Zona de Combate")]
    [Tooltip("Quanto a camera acompanha o player nas bordas da zona (0=fixo, 1=segue total)")]
    [SerializeField] private float combatZoneElasticity = 0.15f;
    [Tooltip("Velocidade de retorno ao centro da zona")]
    [SerializeField] private float combatZoneReturnSpeed = 3f;
    [Tooltip("Distancia da borda onde a camera comeca a acompanhar levemente")]
    [SerializeField] private float edgeThreshold = 0.6f;

    [Header("Limites Gerais")]
    [SerializeField] private bool useStageBounds = true;
    [SerializeField] private float stageMinX = -20f;
    [SerializeField] private float stageMaxX = 20f;

    [Header("Intro Cinematografica")]
    [Tooltip("Ao iniciar a fase, faz um pan da extrema direita ate o player antes de liberar o input")]
    [SerializeField] private bool playIntroOnStart = true;
    [SerializeField] private float introPanDuration = 1.2f;
    [SerializeField] private AnimationCurve introEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool inCombatZone;
    private Vector2 combatZoneCenter;
    private float combatZoneMinX;
    private float combatZoneMaxX;
    private float cameraHalfWidth;
    private Camera cam;

    private bool isIntroPanning;
    private float introTimer;
    private Vector3 introStartPos;
    private Vector3 introEndPos;
    private PlayerController frozenPlayer;

    private PlayerController playerRef;
    private float currentPeekY;
    private float baseCameraY;

    public bool InCombatZone => inCombatZone;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        UpdateCameraHalfWidth();
    }

    private void Start()
    {
        var zone = FindAnyObjectByType<StageZone>();
        if (zone != null && zone.HasStageBounds)
        {
            stageMinX = zone.StageMinX;
            stageMaxX = zone.StageMaxX;
            useStageBounds = true;
        }

        if (target == null)
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) target = playerGO.transform;
        }

        if (target != null) playerRef = target.GetComponent<PlayerController>();
        baseCameraY = transform.position.y;

        // Em "Continuar" (retomando de checkpoint) nao toca o pan de intro: a camera e
        // snapada direto no player reposicionado (StageManager.RepositionToCheckpoint).
        bool resuming = GameFlowManager.Instance != null && GameFlowManager.Instance.HasStageCheckpoint;
        if (playIntroOnStart && target != null && !resuming)
            BeginIntroPan();
    }

    /// <summary>
    /// Snapa a camera imediatamente no target (sem pan/lerp). Usado ao retomar de checkpoint.
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        isIntroPanning = false;
        if (frozenPlayer != null) { frozenPlayer.enabled = true; frozenPlayer = null; }

        float x = target.position.x + followOffset.x;
        if (useStageBounds)
            x = Mathf.Clamp(x, stageMinX + cameraHalfWidth, stageMaxX - cameraHalfWidth);
        float verticalFollow = Mathf.Max(0f, (target.position.y - verticalAnchorY) * verticalFollowStrength);
        float y = baseCameraY + verticalFollow;
        transform.position = new Vector3(x, y, transform.position.z);
    }

    private void BeginIntroPan()
    {
        float endX = target.position.x;
        if (useStageBounds)
            endX = Mathf.Clamp(endX, stageMinX + cameraHalfWidth, stageMaxX - cameraHalfWidth);

        // Y final = mesma formula do follow estatico (sem peek). Garante continuidade
        // ao trocar do estado de intro para o follow, evitando o "flick" no fim do pan.
        // Clampado a >= 0 pelo mesmo motivo: nunca enquadrar abaixo do Y base.
        float endVerticalFollow = Mathf.Max(0f, (target.position.y - verticalAnchorY) * verticalFollowStrength);
        float endY = baseCameraY + endVerticalFollow;

        introStartPos = new Vector3(stageMaxX - cameraHalfWidth, transform.position.y, transform.position.z);
        introEndPos = new Vector3(endX, endY, transform.position.z);
        transform.position = introStartPos;

        frozenPlayer = target.GetComponent<PlayerController>();
        if (frozenPlayer != null) frozenPlayer.enabled = false;

        isIntroPanning = true;
        introTimer = 0f;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (isIntroPanning)
        {
            UpdateIntroPan();
            return;
        }

        Vector3 desiredPos;

        if (inCombatZone)
            desiredPos = CalculateCombatZonePosition();
        else
            desiredPos = CalculateFreeFollowPosition();

        // Peek up (quando o player segura cima na borda superior)
        float targetPeek = (playerRef != null && playerRef.IsPushingUpAtBoundary) ? peekUpOffset : 0f;
        currentPeekY = Mathf.Lerp(currentPeekY, targetPeek, peekSmoothSpeed * Time.deltaTime);

        // Follow vertical leve (acompanha Y do player com peso configuravel).
        // So acompanha pra cima, nunca pra baixo: o limite inferior da camera
        // e o Y base, mantendo o rodape do chao no fundo da tela sem revelar
        // o vazio abaixo dele.
        float verticalFollow = (target.position.y - verticalAnchorY) * verticalFollowStrength;
        verticalFollow = Mathf.Max(0f, verticalFollow);
        desiredPos.y = baseCameraY + verticalFollow + currentPeekY;

        // Manter Z da camera
        desiredPos.z = transform.position.z;

        // Aplicar limites do stage
        if (useStageBounds)
            desiredPos.x = Mathf.Clamp(desiredPos.x, stageMinX + cameraHalfWidth, stageMaxX - cameraHalfWidth);

        transform.position = desiredPos;
    }

    private void UpdateIntroPan()
    {
        introTimer += Time.deltaTime;
        float t = Mathf.Clamp01(introTimer / introPanDuration);
        float eased = introEase.Evaluate(t);
        transform.position = Vector3.Lerp(introStartPos, introEndPos, eased);

        if (t >= 1f)
        {
            isIntroPanning = false;
            if (frozenPlayer != null) frozenPlayer.enabled = true;
        }
    }

    /// <summary>
    /// Camera segue o player suavemente no eixo X.
    /// </summary>
    private Vector3 CalculateFreeFollowPosition()
    {
        Vector3 targetPos = new Vector3(
            target.position.x + followOffset.x,
            transform.position.y + followOffset.y, // Y fixo em beat 'em up
            transform.position.z
        );

        return Vector3.Lerp(transform.position, targetPos, followSmoothSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Camera fixa no centro da zona, com leve elasticidade nas bordas.
    /// Quando o player se aproxima da borda, a camera acompanha levemente
    /// e depois retorna ao centro - evita sensacao de tela 100% presa.
    /// </summary>
    private Vector3 CalculateCombatZonePosition()
    {
        float centerX = combatZoneCenter.x;
        float playerX = target.position.x;

        // Calcular o quanto o player esta proximo da borda da zona
        float zoneWidth = combatZoneMaxX - combatZoneMinX;
        float halfWidth = zoneWidth * 0.5f;
        float distFromCenter = playerX - centerX;
        float normalizedDist = Mathf.Abs(distFromCenter) / halfWidth; // 0=centro, 1=borda

        // Elasticidade: so aplica quando o player esta perto da borda
        float elasticOffset = 0f;
        if (normalizedDist > edgeThreshold)
        {
            float overEdge = (normalizedDist - edgeThreshold) / (1f - edgeThreshold);
            elasticOffset = Mathf.Sign(distFromCenter) * overEdge * halfWidth * combatZoneElasticity;
        }

        float desiredX = centerX + elasticOffset;

        // Suavizar o retorno ao centro
        float smoothX = Mathf.Lerp(transform.position.x, desiredX, combatZoneReturnSpeed * Time.deltaTime);

        return new Vector3(smoothX, transform.position.y, transform.position.z);
    }

    /// <summary>
    /// Entrar em modo zona de combate. Chamado pela CombatZone.
    /// </summary>
    public void EnterCombatZone(Vector2 center, float minX, float maxX)
    {
        inCombatZone = true;
        combatZoneCenter = center;
        combatZoneMinX = minX;
        combatZoneMaxX = maxX;
    }

    /// <summary>
    /// Sair do modo zona de combate. Volta a seguir o player.
    /// </summary>
    public void ExitCombatZone()
    {
        inCombatZone = false;
    }

    /// <summary>
    /// Definir limites do stage (chamado no inicio da fase).
    /// </summary>
    public void SetStageBounds(float minX, float maxX)
    {
        stageMinX = minX;
        stageMaxX = maxX;
        useStageBounds = true;
    }

    /// <summary>
    /// Setar target manualmente (se o player spawnar depois).
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void UpdateCameraHalfWidth()
    {
        if (cam != null && cam.orthographic)
            cameraHalfWidth = cam.orthographicSize * cam.aspect;
        else
            cameraHalfWidth = 8f; // fallback
    }
}
