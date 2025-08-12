using UnityEngine;

public class PlayerStatHandler : MonoBehaviour
{
    private BasePlayer _player;
    private Transform _lighting;

    [SerializeField] private int _initialPickaxeId = 700;
    public EquipmentDatabase Pickaxe => (EquipmentDatabase)InventoryManager.Instance.Inventory.Pickaxe.Data;

    // 기본 스텟
    private float _baseMoveSpeed = 3.0f;
    private float _baseMiningSpeed = 100f;
    private float _baseDropRate = 100f;
    private float _baseSightRange = 2.0f;
    private float _baseDashSpeed = 8.0f;
    private float _baseDashDuration = 0.3f;
    private float _baseDashCooldown = 1.5f;

    // 업그레이드 반영
    private float _currentMoveSpeedBonus = 0f;
    private float _currentMiningSpeedBonus = 0f;
    private float _currentDropRateBonus = 0f;
    private float _currentSightRangeBonus = 0f;
    private float _currentDashSpeedBonus = 0f;
    private float _currentDashDurationBonus = 0f;
    private float _currentDashCooldownBonus = 0f;

    // 최종 스탯 프로퍼티들
    public float MoveSpeed => _currentMoveSpeedBonus;
    public float MiningSpeed => _currentMiningSpeedBonus;
    public float DropRate => _currentDropRateBonus;
    public float SightRange => _currentSightRangeBonus;
    public float DashSpeed => _currentDashSpeedBonus;
    public float DashDuration => _currentDashDurationBonus;
    public float DashCooldown => _currentDashCooldownBonus;

    private void Awake()
    {
        _lighting = transform.Find("Lighting");
    }

    public void Init(BasePlayer player)
    {
        _player = player;
        if (InventoryManager.Instance.Inventory.Pickaxe == null)
        {
            InventoryManager.Instance.Inventory.SetPickaxe(DataManager.Instance.EquipmentData.GetById(_initialPickaxeId));
        }

        // 게임 시작시 현재 업그레이드 레벨에 맞춰 스탯 적용
        ApplyAllUpgrades();
    }

    /// <summary>
    /// 모든 업그레이드를 다시 적용 (게임 로드시 사용)
    /// </summary>
    public void ApplyAllUpgrades()
    {
        ResetToBaseStats();

        // 현재 레벨에 맞춰 모든 업그레이드 적용
        ApplyMoveSpeedUpgrade(UpgradeManager.Instance.GetCurrentPlayerUpgradeEffect(PLAYER_STATUS_TYPE.MoveSpeed));
        ApplyMiningSpeedUpgrade(UpgradeManager.Instance.GetCurrentPlayerUpgradeEffect(PLAYER_STATUS_TYPE.MiningSpeed));
        ApplyDropRateUpgrade(UpgradeManager.Instance.GetCurrentPlayerUpgradeEffect(PLAYER_STATUS_TYPE.DropRate));
        ApplySightRangeUpgrade(UpgradeManager.Instance.GetCurrentPlayerUpgradeEffect(PLAYER_STATUS_TYPE.SightRange));
        ApplyDashCooldownUpgrade(UpgradeManager.Instance.GetCurrentPlayerUpgradeEffect(PLAYER_STATUS_TYPE.DashCooldown));
    }

    private void ResetToBaseStats()
    {
        _currentMoveSpeedBonus = _baseMoveSpeed;
        _currentMiningSpeedBonus = _baseMiningSpeed;
        _currentDropRateBonus = _baseDropRate;
        _currentSightRangeBonus = _baseSightRange;
        _currentDashSpeedBonus = _baseDashSpeed;
        _currentDashDurationBonus = _baseDashDuration;
        _currentDashCooldownBonus = _baseDashCooldown;
    }

    // 업그레이드 적용 메서드들
    public void ApplyMoveSpeedUpgrade(float increaseRate)
    {
        _currentMoveSpeedBonus = increaseRate;
    }

    public void ApplyMiningSpeedUpgrade(float increaseRate)
    {
        _currentMiningSpeedBonus = increaseRate;
    }

    public void ApplyDropRateUpgrade(float increaseRate)
    {
        _currentDropRateBonus = increaseRate;
    }

    public void ApplyDashCooldownUpgrade(float increaseRate)
    {
        _currentDashCooldownBonus = increaseRate;
    }

    public void ApplySightRangeUpgrade(float increaseRate)
    {
        _currentSightRangeBonus = increaseRate;

        float finalRange = SightRange;

        if (_lighting != null)
        {
            _lighting.localScale = new Vector3(finalRange, finalRange, 1f);
        }
    }
}
