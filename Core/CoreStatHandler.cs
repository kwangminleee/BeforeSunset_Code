using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoreUpgradeStats
{
    public int Level = 1;
    public int MaxHp = 500;
    public float HpRegen = 1f;
    public float SightRange = 7f;
}

public class CoreStatHandler : MonoBehaviour
{
    [SerializeField] private string coreName;
    public string CoreName => coreName;

    public CoreUpgradeStats Stats { get; private set; } = new CoreUpgradeStats();

    private Core _core;
    [SerializeField] private Transform _lighting;

    [SerializeField] private int _maxHp = 500;
    public int MaxHp => _maxHp;
    public int CurHp { get; private set; }

    // 기본 스탯
    private int _baseMaxHp = 500;
    private float _baseHpRegen = 1f;
    private float _baseSightRange = 7f;

    // 업그레이드
    private float _hpIncrease = 0f;
    private float _hpRegenIncrease = 0f;
    private float _sightIncrease = 0f;

    private float _regenTimer = 2f;

    private void Start()
    {
        StartCoroutine(HpRegenCoroutine());
    }

    private IEnumerator HpRegenCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_regenTimer);

            if (_core != null && !_core.IsDead && CurHp < Stats.MaxHp)
            {
                int regenAmount = Mathf.RoundToInt(Stats.HpRegen);
                SetHp(CurHp + regenAmount);
            }
        }
    }

    public void Init(Core core)
    {
        _core = core;

        Stats = new CoreUpgradeStats();
        if(_lighting == null)
            _lighting = transform.Find("Lighting");

        // 게임 시작시 현재 업그레이드 레벨에 맞춰 스탯 적용
        SetFullHp();
        Invoke(nameof(ApplyAllUpgrades), 0.1f);
    }

    /// <summary>
    /// 모든 업그레이드를 다시 적용 (게임 로드시 사용)
    /// </summary>
    public void ApplyAllUpgrades()
    {
        if (UpgradeManager.Instance == null) return;

        ResetToBaseStats();

        // 현재 레벨에 맞춰 모든 업그레이드 적용
        ApplyHPUpgrade(UpgradeManager.Instance.GetCurrentCoreUpgradeEffect(CORE_STATUS_TYPE.HP));
        ApplyHpRegenUpgrade(UpgradeManager.Instance.GetCurrentCoreUpgradeEffect(CORE_STATUS_TYPE.HpRegen));
        ApplySightRangeUpgrade(UpgradeManager.Instance.GetCurrentCoreUpgradeEffect(CORE_STATUS_TYPE.SightRange));
    }

    private void ResetToBaseStats()
    {
        _hpIncrease = _baseMaxHp;
        _hpRegenIncrease = _baseHpRegen;
        _sightIncrease = _baseSightRange;
    }

    // 개별 업그레이드 적용 메서드들
    public void ApplyHPUpgrade(float increaseAmount)
    {
        _hpIncrease = increaseAmount;
        UpdateStats();
    }

    public void ApplyHpRegenUpgrade(float increaseAmount)
    {
        _hpRegenIncrease = increaseAmount;
        UpdateStats();
    }

    public void ApplySightRangeUpgrade(float increaseAmount)
    {
        Stats.SightRange = increaseAmount;

        if (_lighting != null)
        {
            _lighting.localScale = new Vector3(Stats.SightRange, Stats.SightRange, 1f);
        }
    }

    private void UpdateStats()
    {
        int oldMaxHp = Stats.MaxHp;

        Stats.MaxHp = Mathf.RoundToInt(_hpIncrease);
        Stats.HpRegen = _hpRegenIncrease;

        if (_core != null && oldMaxHp != Stats.MaxHp)
        {
            SetHp(CurHp + (Stats.MaxHp - oldMaxHp));
        }
    }
    public void SetFullHp()
    {
        SetHp(Stats.MaxHp);
    }

    public void SetHp(int hp)
    {
        CurHp = Mathf.Clamp(hp, 0, Stats.MaxHp);

        if (GameManager.Instance.IsTutorial && CurHp <= 10)
        {
            CurHp = 10;
            ToastManager.Instance.ShowToast("코어는 플레이어를 슬프지 하지 않게 하려고 버텼다!");
        }

        if (_core.HpBar != null)
            _core.HpBar.fillAmount = (float)CurHp / Stats.MaxHp;
    }

    public float GetSight()
    {
        return Stats.SightRange;
    }
}
