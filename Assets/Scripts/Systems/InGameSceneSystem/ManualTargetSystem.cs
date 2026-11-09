using UnityEngine;

public class ManualTargetSystem : Singleton<ManualTargetSystem>
{
    [SerializeField] private ArrowView arrowView;
    [SerializeField] private LayerMask targetLayerMask;

    [SerializeField] private SpriteRenderer enemyTargetArrow;
    [SerializeField]
    private Vector3 targetArrowOffset =
        new(0f, 0.65f, -1f);

    private bool isTargeting;

    private void Start()
    {
        CancelTargeting();
    }

    private void LateUpdate()
    {
        if (!isTargeting) return;

        bool battleOver = BattleSystem.Instance != null &&
            BattleSystem.Instance.IsBattleOver;

        bool actionRunning = ActionSystem.Instance != null &&
            ActionSystem.Instance.IsPerforming;

        if (!Input.GetMouseButton(0) || battleOver || actionRunning)
        {
            CancelTargeting();
            return;
        }

        Vector3 mousePosition =
            MouseUtil.GetMousePositionInWorldSpace(-1f);

        EnemyView target = GetTarget(mousePosition);

        if (target == null)
        {
            enemyTargetArrow.gameObject.SetActive(false);
            return;
        }

        enemyTargetArrow.transform.position =
            target.GetTargetArrowPosition() + targetArrowOffset;

        enemyTargetArrow.gameObject.SetActive(true);
    }

    public void StartTargeting(Vector3 startPosition)
    {
        CancelTargeting();

        if (arrowView == null || enemyTargetArrow == null)
        {
            Debug.LogError(
                "Arrow View or Enemy Target Arrow is missing.",
                this
            );

            return;
        }

        isTargeting = true;

        arrowView.gameObject.SetActive(true);
        arrowView.SetupArrow(startPosition);
    }

    public EnemyView EndTargeting(Vector3 endPosition)
    {
        if (!isTargeting) return null;

        EnemyView target = GetTarget(endPosition);

        CancelTargeting();

        return target;
    }

    private EnemyView GetTarget(Vector3 position)
    {
        if (!Physics.Raycast(
            position,
            Vector3.forward,
            out RaycastHit hit,
            10f,
            targetLayerMask
        ))
        {
            return null;
        }

        EnemyView enemy = hit.collider.GetComponentInParent<EnemyView>();

        if (enemy == null ||
            !enemy.gameObject.activeInHierarchy ||
            enemy.CurrentHealth <= 0)
        {
            return null;
        }

        return enemy;
    }

    public void CancelTargeting()
    {
        isTargeting = false;

        if (arrowView != null)
        {
            arrowView.gameObject.SetActive(false);
        }

        if (enemyTargetArrow != null)
        {
            enemyTargetArrow.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        CancelTargeting();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            CancelTargeting();
        }
    }
}