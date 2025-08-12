using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ResourceSpawner<TData> : MonoBehaviour
{
    private Vector3 _spawnAreaCenter3D;
    private Vector2 _spawnAreaSize = new Vector2(90f, 60f);

    [SerializeField] private int _spawnMineralCount = 100;
    [SerializeField] private float _overlapRadius = 1.0f;
    [SerializeField] private LayerMask _obstacleLayerMask;
    [SerializeField] private LayerMask _spawnZoneLayer;

    private List<OreDatabase> _spawnableList = new();
    private List<Vector3> _spawnableTiles = new();
    private List<Vector3> _placedPositions = new();

    private Transform _parentTransform;

    public void SetParentTransform(Transform parent)
    {
        _parentTransform = parent;
    }

    public Transform GetParentTransform()
    {
        return _parentTransform;
    }

    public void SetSpawnCenter(Vector3 center)
    {
        _spawnAreaCenter3D = center;
    }

    /// <summary>
    /// 자원 스폰
    /// </summary>
    public void SpawnResources(int currentStage)
    {
        var oreDatas = DataManager.Instance.OreData;
        _spawnableList.Clear();
        _placedPositions.Clear();

        // 자원 필터링
        foreach (var data in oreDatas.GetAllItems())
        {
            if (currentStage >= data.spawnStage)
                _spawnableList.Add(data);
        }
        if (_spawnableList.Count == 0) return;

        // 스폰 가능 타일 검사
        GetSpawnTiles();

        // 쥬얼 스폰
        foreach (var data in _spawnableList)
        {
            if (data.dropItemType == DROPITEM_TYPE.Jewel)
                SpawnJewels(data);
        }

        // 광물 스폰
        int placed = 0;
        int attempts = 0;
        int maxAttempts = _spawnMineralCount * 10;

        while (placed < _spawnMineralCount && attempts < maxAttempts)
        {
            attempts++;
            Vector3 pos = GetRandomPositionInArea();
            if (pos == Vector3.zero) break;
            if (IsTooClose(pos)) continue;

            OreDatabase selected = GetRandomByProbability();
            if (selected == null || selected.dropItemType == DROPITEM_TYPE.Jewel) continue;

            if (TryPlace(selected, pos))
            {
                placed++;
                _spawnableTiles.Remove(pos);
            }
        }
    }

    /// <summary>
    /// 쥬얼 스폰
    /// </summary>
    private void SpawnJewels(OreDatabase data)
    {
        int attempts = 0;
        int maxAttempts = 10;

        while (attempts < maxAttempts && _spawnableTiles.Count > 0)
        {
            attempts++;

            // 타일 랜덤 선택
            Vector3 pos = GetRandomPositionInArea();
            if (pos == Vector3.zero) break;
            if (IsTooClose(pos)) continue;

            float probability = data.spawnProbability / 100f;

            // 맵 타입 가져오기
            int currentMapId = MapManager.Instance.CurrentMapIndex;
            var mapData = DataManager.Instance.MapData.GetById(currentMapId);

            // 레어 광산
            if (mapData != null && mapData.mapType == MAP_TYPE.MineRare)
            {
                probability += 0.4f;
            }

            // 확률 적용
            if (UnityEngine.Random.value <= probability)
            {
                if (TryPlace(data, pos))
                {
                    _spawnableTiles.Remove(pos);
                }
            }
        }
    }

    /// <summary>
    /// 실제 자원 배치
    /// </summary>
    private bool TryPlace(OreDatabase data, Vector3 pos)
    {
        int id = data.id;
        PoolManager.Instance.GetFromPool(id, pos, _parentTransform);
        _placedPositions.Add(pos);
        return true;
    }

    /// <summary>
    /// 스폰 영역 먼저 세팅
    /// </summary>
    public void GetSpawnTiles()
    {
        _spawnableTiles.Clear();

        float minX = _spawnAreaCenter3D.x - _spawnAreaSize.x / 2f;
        float maxX = _spawnAreaCenter3D.x + _spawnAreaSize.x / 2f;
        float minY = _spawnAreaCenter3D.y - _spawnAreaSize.y / 2f;
        float maxY = _spawnAreaCenter3D.y + _spawnAreaSize.y / 2f;

        // 타일 위치 찾기
        for (float x = minX; x < maxX; x++)
        {
            for (float y = minY; y < maxY; y++)
            {
                Vector3 pos = new Vector3(Mathf.Floor(x) + 0.5f, Mathf.Floor(y) + 0.5f, _spawnAreaCenter3D.z);
                if (IsValidSpawnPosition(pos))
                {
                    _spawnableTiles.Add(pos);
                }
            }
        }
    }

    /// <summary>
    /// 스폰 영역 내 랜덤 위치
    /// </summary>
    /// <returns></returns>
    private Vector3 GetRandomPositionInArea()
    {
        if (_spawnableTiles.Count == 0)
            return Vector3.zero;

        int randomIndex = UnityEngine.Random.Range(0, _spawnableTiles.Count);
        return _spawnableTiles[randomIndex];
    }

    /// <summary>
    /// 스폰존 레이어 찾기
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    private bool IsValidSpawnPosition(Vector3 position)
    {
        // 스폰 가능 영역 체크
        Collider2D spawnZoneCollider = Physics2D.OverlapPoint(position, _spawnZoneLayer);
        if (spawnZoneCollider == null) return false;

        // 장애물 체크
        Collider2D obstacleCollider = Physics2D.OverlapPoint(position, _obstacleLayerMask);
        if (obstacleCollider != null) return false;

        return true;
    }

    // 다른 광석들이랑 가까운지
    private bool IsTooClose(Vector3 pos)
    {
        foreach (var otherPos in _placedPositions)
        {
            if (Vector3.Distance(pos, otherPos) < _overlapRadius)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 광석 가중치 기반 랜덤 뽑기
    /// </summary>
    /// <returns></returns>
    private OreDatabase GetRandomByProbability()
    {
        int totalWeight = _spawnableList.Sum(d => d.spawnProbability);
        if (totalWeight <= 0) return default;

        int rand = UnityEngine.Random.Range(0, totalWeight);
        int sum = 0;

        foreach (var data in _spawnableList)
        {
            sum += data.spawnProbability;
            if (rand < sum)
                return data;
        }

        return default;
    }

    /// <summary>
    /// 배치된 자원들 저장 (맵 그대로 꺼낼 때 사용)
    /// </summary>
    /// <param name="inactive"></param>
    /// <returns></returns>
    public List<ResourceState> SaveCurrentStates(bool inactive = true)
    {
        var savedStates = new List<ResourceState>();
        var parent = GetParentTransform();
        if (parent == null) return savedStates;

        List<Transform> toReturn = new();

        for (int i = 0; i < parent.childCount; i++)
        {
            var obj = parent.GetChild(i).gameObject;
            var stateComp = obj.GetComponent<IResourceStateSavable>();

            if (stateComp != null)
            {
                // 위치 변환 없이 저장
                savedStates.Add(stateComp.SaveState());
            }

            toReturn.Add(parent.GetChild(i));
        }

        if (inactive)
            InactiveResources(toReturn);

        return savedStates;
    }

    /// <summary>
    /// 저장이 완료된 자원 풀로 반환
    /// </summary>
    private void InactiveResources(List<Transform> savedResources)
    {
        foreach (var tr in savedResources)
        {
            var poolable = tr.GetComponent<IPoolable>();
            if (poolable != null)
            {
                PoolManager.Instance.ReturnToPool(poolable.GetId(), tr.gameObject);
            }
            else
            {
                Debug.LogWarning($"[ReturnToPool] IPoolable 컴포넌트가 없습니다: {tr.name}");
            }
        }
    }

    /// <summary>
    /// 저장된 상태 불러오기
    /// </summary>
    /// <param name="savedStates"></param>
    /// <returns></returns>
    public List<GameObject> SpawnFromSavedStates(List<ResourceState> savedStates)
    {
        List<GameObject> spawnedObjects = new List<GameObject>();

        foreach (var state in savedStates)
        {
            // 저장된 위치 그대로 사용
            GameObject obj = PoolManager.Instance.GetFromPool(state.Id, state.Position);
            if (obj == null)
            {
                Debug.LogWarning($"풀에서 꺼내기 실패 ID: {state.Id}");
                continue;
            }

            if (obj.TryGetComponent<IResourceStateSavable>(out var resource))
            {
                resource.LoadState(state);
            }

            if (_parentTransform != null)
                obj.transform.SetParent(_parentTransform, false);

            obj.SetActive(true);
            spawnedObjects.Add(obj);
        }

        return spawnedObjects;
    }
}