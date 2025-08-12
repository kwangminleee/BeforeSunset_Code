using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System.Linq;

public class MapManager : MonoSingleton<MapManager>, ISaveable
{
    public int CurrentMapIndex { get; private set; } = 0;

    private GameObject _baseMap;
    private Transform _player;
    public BasePlayer Player => _player?.GetComponent<BasePlayer>();

    [SerializeField] private float _mapSpacing = 100f;

    private int _nextMapIndex = 1;
    private Dictionary<int, GameObject> _activeMapInstances = new Dictionary<int, GameObject>();
    private Stack<int> _mapHistory = new Stack<int>();
    private Dictionary<int, int> _mapPrefabIdMap = new Dictionary<int, int>();

    private Dictionary<(int, Portal.PortalDirection), int> _portalMapLinks = new();
    private Dictionary<int, List<InteractableState>> _interactableStates = new();

    private SpawnManager _spawnManager;

    private string _currentBGM = "";

    protected override void Awake()
    {
        base.Awake();

        if (_baseMap == null)
        {
            var baseMapGO = GameObject.Find("BaseMap");
            if (baseMapGO != null)
                _baseMap = baseMapGO;
            else
                Debug.LogError("BaseMap 오브젝트를 찾을 수 없습니다. 이름이 'BaseMap'인지 확인하세요.");
        }
        if (_player == null)
        {
            var playerGO = GameObject.Find("Player");
            if (playerGO != null)
                _player = playerGO.transform;
            else
                Debug.LogError("Player 오브젝트를 찾을 수 없습니다. 이름이 'Player'인지 확인하세요.");
        }

        _baseMap.SetActive(true);
        _spawnManager = FindObjectOfType<SpawnManager>();
        MoveToMap(0, false);
        AudioManager.Instance.PlayBGM("NormalBase");
        _currentBGM = "NormalBase";
    }

    // 포탈 방향 받아서 맵 이동
    public void MoveToMapByDirection(Portal.PortalDirection dir)
    {
        int current = CurrentMapIndex;

        if (_portalMapLinks.TryGetValue((current, dir), out int linkedMapIndex))
        {
            MoveToMap(linkedMapIndex, true);
        }
        else
        {
            int newMapIndex = _nextMapIndex++;
            MoveToMap(newMapIndex, true);

            _portalMapLinks[(current, dir)] = newMapIndex;

            var oppositeDir = PortalManager.Instance.GetOppositeDirection(dir);
            _portalMapLinks[(newMapIndex, oppositeDir)] = current;
        }
    }

    // 이전맵 이동
    public void MoveToPreviousMap()
    {
        if (_mapHistory.Count == 0) return;

        int prev = _mapHistory.Pop();

        if (PortalManager.Instance.LastEnteredPortalDirection.HasValue)
            PortalManager.Instance.LastEnteredPortalDirection = PortalManager.Instance.GetOppositeDirection(PortalManager.Instance.LastEnteredPortalDirection.Value);

        MoveToMap(prev, false);
    }

    // 귀환
    public void ReturnToHomeMap()
    {
        if (CurrentMapIndex == 0) return;

        // 활성화된 모든 맵을 풀에 반환
        foreach (var kvp in _activeMapInstances)
        {
            if (kvp.Key != 0 && _mapPrefabIdMap.ContainsKey(kvp.Key))
            {
                int prefabId = _mapPrefabIdMap[kvp.Key];
                PoolManager.Instance.ReturnToPool(prefabId, kvp.Value);
            }
        }

        if (TimeManager.Instance.IsNight)
        {
            _activeMapInstances.Clear();
            _mapHistory.Clear();
            _mapPrefabIdMap.Clear();
            _portalMapLinks.Clear();
        }

        MoveToMap(0, false);
        _player.position = _player.position = new Vector3(0f, -0.2f, 0f);
    }

    // 맵이동
    public void MoveToMap(int targetIndex, bool addToHistory = true)
    {
        if (TimeManager.Instance.IsNight && !PlayerInputHandler._isRecallInProgress) return;

        if (targetIndex == CurrentMapIndex) return;

        // 현재 맵 비활성화
        if (CurrentMapIndex == 0)
        {
            _baseMap.SetActive(false);
        }
        else if (CurrentMapIndex != 0 && _activeMapInstances.TryGetValue(CurrentMapIndex, out var currentChunk))
        {
            SaveInteractableStates(currentChunk, CurrentMapIndex);

            if (_mapPrefabIdMap.ContainsKey(CurrentMapIndex))
            {
                int prefabId = _mapPrefabIdMap[CurrentMapIndex];
                PoolManager.Instance.ReturnToPool(prefabId, currentChunk);
                _activeMapInstances.Remove(CurrentMapIndex);
            }
            else
            {
                currentChunk.SetActive(false);
            }
        }

        // 타겟 맵 활성화 또는 생성
        if (targetIndex > 0 && !_activeMapInstances.ContainsKey(targetIndex))
        {
            // 새로운 맵 생성 - 풀에서 가져오기
            if (!_mapPrefabIdMap.ContainsKey(targetIndex))
            {
                // 랜덤하게 맵 타입 선택 (기본 광산, 거대 광산, 희귀 광산 중)
                int prefabId = GetRandomMapPrefabId();
                _mapPrefabIdMap[targetIndex] = prefabId;
            }

            int mapPrefabId = _mapPrefabIdMap[targetIndex];
            Vector3 spawnPos = _baseMap.transform.position + new Vector3(_mapSpacing * targetIndex, 0, 0);

            GameObject mapInstance = PoolManager.Instance.GetFromPool(mapPrefabId, spawnPos);

            if (mapInstance != null)
            {
                mapInstance.transform.SetParent(this.transform);
                mapInstance.SetActive(false);

                // Cinemachine 카메라 설정
                var vcam = mapInstance.GetComponentInChildren<CinemachineVirtualCamera>();
                if (vcam != null)
                {
                    vcam.Follow = _player;
                    vcam.LookAt = _player;
                }

                _activeMapInstances[targetIndex] = mapInstance;
            }
            else
            {
                Debug.LogError($"풀에서 맵을 가져올 수 없습니다. ID: {mapPrefabId}");
                return;
            }
        }

        // 맵 활성화 및 플레이어 위치 설정
        if (targetIndex == 0)
        {
            _baseMap.SetActive(true);
            Vector3 spawnPos = GetSpawnPositionByEnteredPortal(_baseMap, PortalManager.Instance.LastEnteredPortalDirection);
            _player.position = spawnPos;

            Vector2[] smallSpawnAreas = new Vector2[] { new Vector2(60f, 32f), new Vector2(60f, 32f) };
            _spawnManager?.OnMapChanged(_baseMap.transform.position, 0);
        }
        else if (_activeMapInstances.TryGetValue(targetIndex, out var nextMap))
        {
            RestoreInteractableStates(nextMap, targetIndex);
            nextMap.SetActive(true);
            Vector3 spawnPos = GetSpawnPositionByEnteredPortal(nextMap, PortalManager.Instance.LastEnteredPortalDirection);
            _player.position = spawnPos;

            Vector2[] spawnAreas = GetSpawnAreasByMapType(_mapPrefabIdMap[targetIndex]);
            _spawnManager?.SetMapPositionAndArea(nextMap.transform.position);
            _spawnManager?.OnMapChanged(nextMap.transform.position, targetIndex);
        }

        if (addToHistory)
            _mapHistory.Push(CurrentMapIndex);

        ChangeMapBGM(targetIndex);
        CurrentMapIndex = targetIndex;
    }

    // 랜덤 맵 프리팹 ID 선택 (스몰 60%, 빅 30%, 레어 10%)
    private int GetRandomMapPrefabId()
    {
        var mapDatas = DataManager.Instance.MapData.GetAllItems();
        var basicMaps = new List<MapDatabase>();
        var bigMaps = new List<MapDatabase>();
        var rareMaps = new List<MapDatabase>();

        // 맵 타입별로 분류
        foreach (var map in mapDatas)
        {
            switch (map.mapType)
            {
                case MAP_TYPE.MineSmall:
                    basicMaps.Add(map);
                    break;
                case MAP_TYPE.MineLarge:
                    bigMaps.Add(map);
                    break;
                case MAP_TYPE.MineRare:
                    rareMaps.Add(map);
                    break;
            }
        }

        // 확률 기반 선택
        int randomValue = Random.Range(0, 100);

        if (randomValue < 60)
        {
            if (basicMaps.Count > 0)
            {
                int randomIndex = Random.Range(0, basicMaps.Count);
                return basicMaps[randomIndex].id;
            }
        }
        else if (randomValue < 90)
        {
            if (bigMaps.Count > 0)
            {
                int randomIndex = Random.Range(0, bigMaps.Count);
                return bigMaps[randomIndex].id;
            }
        }
        else
        {
            if (rareMaps.Count > 0)
            {
                int randomIndex = Random.Range(0, rareMaps.Count);
                return rareMaps[randomIndex].id;
            }
        }

        // 폴백: 선택된 타입의 맵이 없을 경우 다른 타입에서 선택
        var allMineMaps = new List<MapDatabase>();
        allMineMaps.AddRange(basicMaps);
        allMineMaps.AddRange(bigMaps);
        allMineMaps.AddRange(rareMaps);

        if (allMineMaps.Count == 0)
        {
            Debug.LogError("사용할 수 있는 광산 맵이 없습니다!");
            return 1110; // 기본값
        }

        int fallbackIndex = Random.Range(0, allMineMaps.Count);
        return allMineMaps[fallbackIndex].id;
    }

    // 맵 타입에 따른 스폰 영역 반환
    private Vector2[] GetSpawnAreasByMapType(int mapId)
    {
        var mapData = DataManager.Instance.MapData.GetById(mapId);
        if (mapData == null)
        {
            return new Vector2[] { new Vector2(60f, 32f), new Vector2(60f, 32f) }; // 기본값
        }

        switch (mapData.mapType)
        {
            case MAP_TYPE.MineSmall:
                return new Vector2[] { new Vector2(60f, 32f), new Vector2(60f, 32f) };
            case MAP_TYPE.MineLarge:
                return new Vector2[] { new Vector2(85f, 50f), new Vector2(85f, 50f) };
            case MAP_TYPE.MineRare:
                return new Vector2[] { new Vector2(60f, 32f), new Vector2(60f, 32f) };
            default:
                return new Vector2[] { new Vector2(60f, 32f), new Vector2(60f, 32f) };
        }
    }

    // 포탈에서 거리두고 플레이어 위치시키기
    private Vector3 GetSpawnPositionByEnteredPortal(GameObject mapInstance, Portal.PortalDirection? enteredDir)
    {
        if (enteredDir == null)
            return mapInstance.transform.position;

        var exitDir = PortalManager.Instance.GetOppositeDirection(enteredDir.Value);

        var offsetByDirection = new Dictionary<Portal.PortalDirection, Vector3>()
        {
            { Portal.PortalDirection.North, new Vector3(0, -2f, 0) },
            { Portal.PortalDirection.South, new Vector3(0, 2f, 0) },
            { Portal.PortalDirection.East,  new Vector3(-2f, 0, 0) },
            { Portal.PortalDirection.West,  new Vector3(2f, 0, 0) }
        };

        var portals = mapInstance.GetComponentsInChildren<Portal>();

        foreach (var portal in portals)
        {
            if (portal.CurrentPortalDirection == exitDir)
            {
                Vector3 offset = offsetByDirection.TryGetValue(exitDir, out var v) ? v : Vector3.zero;
                return portal.transform.position + offset;
            }
        }

        return mapInstance.transform.position;
    }

    /// <summary>
    /// 맵 초기화
    /// </summary>
    public void ResetAllMaps()
    {
        // 활성화 맵 풀 반환
        foreach (var kvp in _activeMapInstances)
        {
            if (kvp.Key != 0 && _mapPrefabIdMap.ContainsKey(kvp.Key))
            {
                int prefabId = _mapPrefabIdMap[kvp.Key];
                PoolManager.Instance.ReturnToPool(prefabId, kvp.Value);
            }
            else if (kvp.Key != 0)
            {
                kvp.Value.SetActive(false);
            }
        }

        // 초기화
        _activeMapInstances.Clear();
        _mapHistory.Clear();
        _mapPrefabIdMap.Clear();
        _portalMapLinks.Clear();
        _interactableStates.Clear();

        CurrentMapIndex = 0;
        _nextMapIndex = 1;

        if (PortalManager.Instance != null)
        {
            PortalManager.Instance.LastEnteredPortalDirection = null;
        }

        _currentBGM = "";
    }

    private void ChangeMapBGM(int mapIndex)
    {
        if (TimeManager.Instance.Day == 4 && TimeManager.Instance.IsNight)
        {
            return;
        }

        string targetBGM = "";

        if (mapIndex == 0 && !TimeManager.Instance.IsNight)
        {
            targetBGM = "NormalBase";
        }
        else if (mapIndex > 0)
        {
            int mapId = _mapPrefabIdMap.GetValueOrDefault(mapIndex, -1);
            var mapData = DataManager.Instance.MapData.GetById(mapId);

            if (mapData != null)
            {
                switch (mapData.mapType)
                {
                    case MAP_TYPE.MineSmall:
                    case MAP_TYPE.MineLarge:
                        targetBGM = "BasicMine1";
                        break;
                    case MAP_TYPE.MineRare:
                        targetBGM = "RareMine1";
                        break;
                }
            }
        }

        // 재생 중인 BGM과 다를 때만 새로 재생
        if (!string.IsNullOrEmpty(targetBGM) && _currentBGM != targetBGM)
        {
            AudioManager.Instance.PlayBGM(targetBGM);
            _currentBGM = targetBGM;
        }
    }

    /// <summary>
    /// 맵 링크 데이터 저장
    /// </summary>
    public void SaveData(GameData data)
    {
        data.mapLinks.currentMapIndex = CurrentMapIndex;
        data.mapLinks.nextMapIndex = _nextMapIndex;
        
        data.mapLinks.mapHistory = _mapHistory.ToList();
        
        foreach(var pair in _mapPrefabIdMap)
        {
            data.mapLinks.prefabIdDict.Add(pair);
        }

        foreach(var pair in _portalMapLinks)
        {
            data.mapLinks.portalMapLinks.Add(pair);
        }

        data.playerPosition = _player.transform.position;
    }

    /// <summary>
    /// 맵 링크 데이터 로드
    /// </summary>
    public void LoadData(GameData data)
    {
        _nextMapIndex = data.mapLinks.nextMapIndex;

        foreach(int stack in data.mapLinks.mapHistory)
        {
            _mapHistory.Push(stack);
        }

        foreach(var pair in data.mapLinks.prefabIdDict)
        {
            _mapPrefabIdMap[pair.key] = pair.value;
        }

        foreach(var pair in data.mapLinks.portalMapLinks)
        {
            _portalMapLinks[(pair.key, pair.direction)] = pair.value;
        }
    }

    private void SaveInteractableStates(GameObject map, int mapIndex)
    {
        var objs = map.GetComponentsInChildren<InteractableObject>();
        var states = new List<InteractableState>();

        foreach (var obj in objs)
        {
            states.Add(obj.SaveState());
        }

        _interactableStates[mapIndex] = states;
    }

    private void RestoreInteractableStates(GameObject map, int mapIndex)
    {
        if (!_interactableStates.TryGetValue(mapIndex, out var states)) return;

        var objs = map.GetComponentsInChildren<InteractableObject>();
        for (int i = 0; i < Mathf.Min(objs.Length, states.Count); i++)
        {
            objs[i].LoadState(states[i]);
        }
    }
}