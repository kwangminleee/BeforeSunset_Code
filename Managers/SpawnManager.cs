using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpawnManager : MonoSingleton<SpawnManager>, ISaveable
{
    private OreSpawner oreSpawner;
    private OreDataHandler oreHandler;

    private int currentMapIndex = -1;

    private Dictionary<int, List<GameObject>> _mapResources = new Dictionary<int, List<GameObject>>();
    private Dictionary<int, List<ResourceState>> _mapResourceStates = new();

    protected override void Awake()
    {
        base.Awake();

        oreSpawner = Helper_Component.FindChildComponent<OreSpawner>(this.transform, "OreSpawner");

        StartCoroutine(WaitForDataManagerInit());
    }
    private IEnumerator WaitForDataManagerInit()
    {
        while (!DataManagerReady())
            yield return null;

        oreHandler = DataManager.Instance.OreData;
    }

    private bool DataManagerReady()
    {
        var task = DataManager.Instance.InitCheck();
        return task.IsCompleted && DataManager.Instance.OreData != null && DataManager.Instance.JewelData != null;
    }

    private Transform GetContainer(string containerName)
    {
        var container = GameObject.Find(containerName);
        if (container == null)
            Debug.LogWarning($"컨테이너 {containerName}를 찾을 수 없습니다.");
        return container?.transform;
    }

    public void OnMapChanged(Vector3 mapPosition, int mapIndex)
    {
        // 현재 맵 상태 저장
        if (currentMapIndex != -1)
        {
            // 1) 상태 저장
            List<ResourceState> saved = new();
            saved.AddRange(oreSpawner.SaveCurrentStates());
            _mapResourceStates[currentMapIndex] = saved;

            // 2) 이전 맵 자원들 풀에 반환
            if (_mapResources.TryGetValue(currentMapIndex, out var oldResources))
            {
                foreach (var obj in oldResources)
                {
                    if (obj == null) continue;
                    /*
                    if (obj.TryGetComponent<IResourceStateSavable>(out var resource))
                    {
                        resource.OnReturnToPool();
                    }*/
                    PoolManager.Instance.ReturnToPool(obj.GetComponent<IPoolable>().GetId(), obj);
                }
                oldResources.Clear();
            }
        }

        currentMapIndex = mapIndex;
        SetMapPositionAndArea(mapPosition);

        if (currentMapIndex == 0)
        {
            return;
        }

        var oreContainer = GetContainer("OreContainer");

        oreSpawner.SetParentTransform(oreContainer);

        oreContainer.transform.position = Vector3.zero;

        if (_mapResourceStates.TryGetValue(currentMapIndex, out var savedStates))
        {
            List<GameObject> newResources = new List<GameObject>();

            newResources.AddRange(oreSpawner.SpawnFromSavedStates(savedStates.Where(s => s.Id < 1000).ToList()));

            _mapResources[currentMapIndex] = newResources;
        }
        else
        {
            SpawnAllAndStore();
        }
    }

    private void SpawnAllAndStore()
    {
        if (oreHandler == null)
        {
            Debug.LogWarning("SpawnManager: 데이터 핸들러가 세팅되지 않았습니다.");
            return;
        }

        List<GameObject> spawnedObjects = new List<GameObject>();

        if (oreSpawner != null)
        {
            oreSpawner.SpawnResources(TimeManager.Instance.Day);
            spawnedObjects.AddRange(GetChildrenGameObjects(oreSpawner.GetParentTransform()));
        }

        _mapResources[currentMapIndex] = spawnedObjects;
    }

    private List<GameObject> GetChildrenGameObjects(Transform parent)
    {
        List<GameObject> list = new List<GameObject>();
        if (parent == null) return list;

        for (int i = 0; i < parent.childCount; i++)
            list.Add(parent.GetChild(i).gameObject);
        return list;
    }

    public void OnStageChanged()
    {
        ClearAll();
        _mapResources.Clear();
        _mapResourceStates.Clear();
        SetMapPositionAndArea(Vector3.zero);
    }

    private void ClearAll()
    {
        ClearChildren(oreSpawner?.GetParentTransform());
        _mapResources.Clear();
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null) return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    public void SetMapPositionAndArea(Vector3 mapPosition)
    {
        oreSpawner?.SetSpawnCenter(mapPosition);
    }

    /// <summary>
    /// 광산에 스폰된 광석과 쥬얼 데이터 저장
    /// </summary>
    public void SaveData(GameData data)
    {
        MineSaveData mineData;

        // 이전 맵의 리소스 데이터 저장
        foreach (var key in _mapResourceStates.Keys)
        {
            mineData = new MineSaveData();
            mineData.mapId = key;
            foreach(ResourceState resource in _mapResourceStates[key])
            {
                if(!resource.IsMined)
                    mineData.resources.Add(new ResourceSaveData(resource.Id, resource.Position, resource.HP));
            }

            data.spawnedMines.Add(mineData);
        }

        // 현재 맵의 리소스 데이터 저장
        List<ResourceState> saved = new();
        saved.AddRange(oreSpawner.SaveCurrentStates(false));

        mineData = new MineSaveData();
        mineData.mapId = currentMapIndex;
        foreach (ResourceState resource in saved)
        {
            if (!resource.IsMined)
                mineData.resources.Add(new ResourceSaveData(resource.Id, resource.Position, resource.HP));
        }

        data.spawnedMines.Add(mineData);
    }

    /// <summary>
    /// 광산에 스폰된 광석과 쥬얼 데이터 로드
    /// </summary>
    public void LoadData(GameData data)
    {
        foreach(MineSaveData mineData in data.spawnedMines)
        {
            List<ResourceState> resourceStates = new List<ResourceState>();

            foreach(ResourceSaveData resourceData in mineData.resources)
            {
                ResourceState resourceState = new ResourceState()
                {
                    Id = resourceData.resourceId,
                    Position = resourceData.position,
                    HP = resourceData.curHp,
                    IsMined = false
                };

                resourceStates.Add(resourceState);
            }

            _mapResourceStates[mineData.mapId] = resourceStates;
        }
    }
}