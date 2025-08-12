using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PLAYER_STATUS_TYPE
{
    MoveSpeed,
    MiningSpeed,
    DropRate,
    SightRange,
    DashCooldown,
}

public enum CORE_STATUS_TYPE
{
    HP,
    HpRegen,
    SightRange,
}

public class UpgradeManager : MonoSingleton<UpgradeManager>, ISaveable
{
    public Dictionary<PLAYER_STATUS_TYPE, int> PlayerStatus { get; private set; } = new();
    public Dictionary<CORE_STATUS_TYPE, int> CoreStatus { get; private set; } = new();
    public Dictionary<string, int> BaseUpgrade { get; private set; }
    public Dictionary<string, int> FixedUpgrade { get; private set; }
    public Dictionary<string, int> VirtualUpgrade { get; private set; }

    public int Essence { get; private set; }
    public int EssencePiece { get; private set; }
    public int VirtualEssence { get; private set; }

    public int UsedEssence { get; private set; }
    public int VirtualUsedEssence { get; private set; }
    public int ResetCounter { get; private set; }
    public int ResetCost => 1 + ResetCounter;

    private UpgradeDataHandler _upgradeDataHandler;
    private PlayerStatHandler _playerStatHandler;
    private CoreStatHandler _coreStatHandler;

    protected override void Awake()
    {
        base.Awake();
        InitStatus();

        _upgradeDataHandler = DataManager.Instance.UpgradeData;
        _playerStatHandler = FindObjectOfType<PlayerStatHandler>();
        _coreStatHandler = FindObjectOfType<CoreStatHandler>();

        InitUpgrade();
        BaseUpgrade = new Dictionary<string, int>(FixedUpgrade);
    }

    private void InitStatus()
    {
        PlayerStatus = new()
    {
        { PLAYER_STATUS_TYPE.MoveSpeed, 0 },
        { PLAYER_STATUS_TYPE.MiningSpeed, 0 },
        { PLAYER_STATUS_TYPE.DropRate, 0 },
        { PLAYER_STATUS_TYPE.DashCooldown, 0 },
        { PLAYER_STATUS_TYPE.SightRange, 0 }
    };

        CoreStatus = new()
    {
        { CORE_STATUS_TYPE.HP, 0 },
        { CORE_STATUS_TYPE.HpRegen, 0 },
        { CORE_STATUS_TYPE.SightRange, 0 },
    };
    }

    public void InitUpgrade()
    {
        if (FixedUpgrade != null && FixedUpgrade.Count > 0)
            return;

        List<UpgradeDatabase> upgradeList = DataManager.Instance.UpgradeData.GetAllItems();
        List<UpgradeDatabase> upgrades = upgradeList.Where(u => u.level == 0).ToList();

        FixedUpgrade = new Dictionary<string, int>();
        for (int i = 0; i < upgrades.Count; i++)
        {
            FixedUpgrade.Add(upgrades[i].upgradeName, upgrades[i].id);
        }
    }

    /// <summary>
    /// 플레이어 업그레이드 데이터 가져오기
    /// </summary>
    private UpgradeDatabase GetPlayerUpgradeData(PLAYER_STATUS_TYPE statusType, int level)
    {
        UPGRADE_TYPE upgradeType = ConvertToUpgradeType(statusType);
        return _upgradeDataHandler.GetAllItems()
            .FirstOrDefault(data => data.statType == upgradeType &&
                          data.level == level &&
                          data.category == UPGRADE_CATEGORY.Player);
    }

    /// <summary>
    /// 코어 업그레이드 데이터 가져오기
    /// </summary>
    private UpgradeDatabase GetCoreUpgradeData(CORE_STATUS_TYPE statusType, int level)
    {
        UPGRADE_TYPE upgradeType = ConvertToUpgradeType(statusType);
        return _upgradeDataHandler.GetAllItems()
            .FirstOrDefault(data => data.statType == upgradeType &&
                          data.level == level &&
                          data.category == UPGRADE_CATEGORY.Core);
    }

    /// <summary>
    /// PLAYER_STATUS_TYPE을 UPGRADE_TYPE으로 변환
    /// </summary>
    private UPGRADE_TYPE ConvertToUpgradeType(PLAYER_STATUS_TYPE statusType)
    {
        return statusType switch
        {
            PLAYER_STATUS_TYPE.MoveSpeed => UPGRADE_TYPE.MoveSpeed,
            PLAYER_STATUS_TYPE.MiningSpeed => UPGRADE_TYPE.MiningSpeed,
            PLAYER_STATUS_TYPE.DropRate => UPGRADE_TYPE.DropRate,
            PLAYER_STATUS_TYPE.DashCooldown => UPGRADE_TYPE.DashCooldown,
            PLAYER_STATUS_TYPE.SightRange => UPGRADE_TYPE.SightRange,
            _ => throw new ArgumentException($"Unknown player status type: {statusType}")
        };
    }

    /// <summary>
    /// CORE_STATUS_TYPE을 UPGRADE_TYPE으로 변환
    /// </summary>
    private UPGRADE_TYPE ConvertToUpgradeType(CORE_STATUS_TYPE statusType)
    {
        return statusType switch
        {
            CORE_STATUS_TYPE.HP => UPGRADE_TYPE.HP,
            CORE_STATUS_TYPE.HpRegen => UPGRADE_TYPE.HpRegen,
            CORE_STATUS_TYPE.SightRange => UPGRADE_TYPE.SightRange,
            _ => throw new ArgumentException($"Unknown core status type: {statusType}")
        };
    }

    /// <summary>
    /// 현재 플레이어 업그레이드 효과 가져오기
    /// </summary>
    public float GetCurrentPlayerUpgradeEffect(PLAYER_STATUS_TYPE statusType)
    {
        int currentLevel = PlayerStatus[statusType];
        //if (currentLevel == 0) return 0f;

        UpgradeDatabase currentUpgrade = GetPlayerUpgradeData(statusType, currentLevel);
        return currentUpgrade?.increaseRate ?? 0f;
    }

    /// <summary>
    /// 현재 코어 업그레이드 효과 가져오기
    /// </summary>
    public float GetCurrentCoreUpgradeEffect(CORE_STATUS_TYPE statusType)
    {
        int currentLevel = CoreStatus[statusType];
        //if (currentLevel == 0) return 0f;

        UpgradeDatabase currentUpgrade = GetCoreUpgradeData(statusType, currentLevel);
        return currentUpgrade?.increaseRate ?? 0f;
    }

    public void ApplyAllUpgradesToAll()
    {
        _playerStatHandler?.ApplyAllUpgrades();
        _coreStatHandler?.ApplyAllUpgrades();
        QuestManager.Instance.AddQuestAmount(QUEST_TYPE.UpgradeCore);
    }

    private void UpdateStatusFromFixedUpgrade()
    {
        PlayerStatus.Clear();
        CoreStatus.Clear();

        foreach (var pair in FixedUpgrade)
        {
            var data = DataManager.Instance.UpgradeData.GetById(pair.Value);
            if (data == null) continue;

            if (data.category == UPGRADE_CATEGORY.Player)
            {
                var type = data.statType switch
                {
                    UPGRADE_TYPE.MoveSpeed => PLAYER_STATUS_TYPE.MoveSpeed,
                    UPGRADE_TYPE.MiningSpeed => PLAYER_STATUS_TYPE.MiningSpeed,
                    UPGRADE_TYPE.DropRate => PLAYER_STATUS_TYPE.DropRate,
                    UPGRADE_TYPE.DashCooldown => PLAYER_STATUS_TYPE.DashCooldown,
                    UPGRADE_TYPE.SightRange => PLAYER_STATUS_TYPE.SightRange,
                    _ => throw new ArgumentOutOfRangeException()
                };

                PlayerStatus[type] = data.level;
            }
            else if (data.category == UPGRADE_CATEGORY.Core)
            {
                var type = data.statType switch
                {
                    UPGRADE_TYPE.HP => CORE_STATUS_TYPE.HP,
                    UPGRADE_TYPE.HpRegen => CORE_STATUS_TYPE.HpRegen,
                    UPGRADE_TYPE.SightRange => CORE_STATUS_TYPE.SightRange,
                    _ => throw new ArgumentOutOfRangeException()
                };

                CoreStatus[type] = data.level;
            }
        }
    }

    public void ResetUpgrades()
    {
        FixResetCounter();

        FixedUpgrade = new Dictionary<string, int>(BaseUpgrade);

        UpdateStatusFromFixedUpgrade();
        ApplyAllUpgradesToAll();

        SetVirtualUpgrade();
        SetVirtualEssence();
    }

    public void SetVirtualUpgrade()
    {
        VirtualUpgrade = new Dictionary<string, int>(FixedUpgrade);
    }

    public void DiscardVirtualUpgrade()
    {
        VirtualUpgrade = null;
    }

    public void ChangeVirtualUpgrade(string upgradeName, int id)
    {
        VirtualUpgrade[upgradeName] = id;
    }

    public void FixUpgrade()
    {
        FixedUpgrade = new Dictionary<string, int>(VirtualUpgrade);

        UpdateStatusFromFixedUpgrade();
    }

    public void AddEssencePiece(int amount)
    {
        EssencePiece += amount;

        if (EssencePiece >= 30)
        {
            Essence += EssencePiece / 30;
            EssencePiece %= 30;
        }

        UIManager.Instance.EssenceUI.Refresh();
    }

    public void SetVirtualEssence()
    {
        VirtualEssence = Essence;
        VirtualUsedEssence = UsedEssence;
    }

    public void UseVirtualEssence(int amount)
    {
        VirtualEssence -= amount;
        VirtualUsedEssence += amount;
    }

    public void FixEssence()
    {
        Essence = VirtualEssence;
        UsedEssence = VirtualUsedEssence;
    }

    public void FixResetCounter()
    {
        Essence = VirtualEssence + UsedEssence - ResetCost;
        ResetCounter++;
        UsedEssence = 0;
    }

    /// <summary>
    /// 플레이어 & 코어 업그레이드 정보 / 정수 관련 정보 저장
    /// </summary>
    public void SaveData(GameData data)
    {
        data.esenceSaveData.essence = Essence;
        data.esenceSaveData.essencePiece = EssencePiece;
        data.esenceSaveData.usedEssence = UsedEssence;
        data.esenceSaveData.resetCounter = ResetCounter;

        foreach (var pair in PlayerStatus)
        {
            data.esenceSaveData.playerUpgradeDict.Add(pair);
        }

        foreach (var pair in CoreStatus)
        {
            data.esenceSaveData.coreUpgradeDict.Add(pair);
        }

        foreach (var pair in FixedUpgrade)
        {
            data.esenceSaveData.doneUpgradeDict.Add(pair);
        }
    }

    /// <summary>
    /// 플레이어 & 코어 업그레이드 로드 / 정수 관련 정보 로드
    /// </summary>
    public void LoadData(GameData data)
    {
        Essence = data.esenceSaveData.essence;
        EssencePiece = data.esenceSaveData.essencePiece;
        UsedEssence = data.esenceSaveData.usedEssence;
        ResetCounter = data.esenceSaveData.resetCounter;

        foreach (var pair in data.esenceSaveData.playerUpgradeDict)
        {
            PlayerStatus[pair.key] = pair.value;
        }

        foreach (var pair in data.esenceSaveData.coreUpgradeDict)
        {
            CoreStatus[pair.key] = pair.value;
        }

        foreach (var pair in data.esenceSaveData.doneUpgradeDict)
        {
            FixedUpgrade[pair.key] = pair.value;
        }

        ApplyAllUpgradesToAll();
        UIManager.Instance.EssenceUI.Refresh();
    }
}
