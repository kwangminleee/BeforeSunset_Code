using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Core : MonoBehaviour, IDamageable, ISaveable, IInteractable
{
    [SerializeField] private int _size;
    public int Size => _size;
    [SerializeField] private Image _hpBar;
    public Image HpBar => _hpBar;
    private Animator _animator;
    public bool IsDead { get; private set; }

    private SpriteRenderer _spriter;
    private CoreStatHandler _statHandler;

    private void Awake()
    {
        _spriter = Helper_Component.GetComponentInChildren<SpriteRenderer>(gameObject);
        _statHandler = Helper_Component.GetComponent<CoreStatHandler>(gameObject);
        _animator = Helper_Component.GetComponentInChildren<Animator>(gameObject);

        _statHandler.Init(this);
    }

    public void Interact()
    {
        if (InteractManager.Instance.IsPointerOverRealUI()) return;

        UIManager.Instance.UpgradeUI.Open();
    }

    public bool IsInteractable(Vector3 playerPos, float range, BoxCollider2D playerCollider)
    {
        return true;
    }

    /// <summary>
    /// 실제 hp 변동 메서드
    /// </summary>
    /// <param name="damaged">받은 데미지 정보</param>
    public void OnDamaged(Damaged damaged)
    {
        if (IsDead) return;

        if (damaged.Attacker == null)
        {
            Debug.LogWarning("타격 대상 못찾음!");
            return;
        }

        _statHandler.SetHp(Mathf.Max(_statHandler.CurHp - DamageCalculator.CalcDamage(damaged.Value, 0f, damaged.IgnoreDefense), 0));

        if (_statHandler.CurHp == 0)
        {
            IsDead = true;
            var camController = FindObjectOfType<CameraZoomController>();
            if (camController != null)
            {
                camController.FocusGameOver(transform, () =>
                {
                    _spriter.color = _spriter.color.WithAlpha(0.5f);
                    ShowDeathAnimation();
                });
            }
        }
    }
    private void ShowDeathAnimation()
    {
        _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        // 게임 일시정지
        Time.timeScale = 0f;

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {

        _animator.SetTrigger("IsDead");

        yield return null;

        yield return new WaitUntil(() => _animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f && _animator.GetCurrentAnimatorStateInfo(0).loop == false);

        // 실패 UI 출력
        UIManager.Instance.ResultUI.Open(false);
    }

    /// <summary>
    /// 코어 체력 저장
    /// </summary>
    public void SaveData(GameData data)
    {
        data.coreCurHp = _statHandler.CurHp;
    }

    /// <summary>
    /// 코어 체력 로드
    /// </summary>
    public void LoadData(GameData data)
    {
        _statHandler.SetHp(data.coreCurHp);
    }
    public float GetLightAreaRadius()
    {
        return _statHandler.GetSight() + 1.5f;
    }

    public int GetObejctSize()
    {
        return 3;
    }

    public void OffAttackArea()
    {
        throw new System.NotImplementedException();
    }
}
