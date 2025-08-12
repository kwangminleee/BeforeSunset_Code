using System.Collections.Generic;
using UnityEngine;

public class PortalManager : MonoSingleton<PortalManager>
{
    public Portal.PortalDirection? LastEnteredPortalDirection { get; set; }

    private List<Portal> _portals = new List<Portal>();
    private BasePlayer _player;

    private void Start()
    {
        _player = FindObjectOfType<BasePlayer>();
        _portals.AddRange(FindObjectsOfType<Portal>());
    }

    public void OnPlayerEnteredPortal(Portal portal)
    {
        if (TimeManager.Instance.IsNight && !PlayerInputHandler._isRecallInProgress) return;

        LastEnteredPortalDirection = portal.CurrentPortalDirection;

        bool isEntering = DetermineIsEntering(portal.CurrentPortalDirection);

        StartCoroutine(ScreenFadeController.Instance.FadeInOut(() =>
        {
            if (isEntering)
            {
                MapManager.Instance.MoveToMapByDirection(portal.CurrentPortalDirection);
            }
            else
            {
                MapManager.Instance.MoveToPreviousMap();
            }

            bool isBaseMapAfterMove = MapManager.Instance.CurrentMapIndex == 0;
            _player?.SetPlayerInBase(isBaseMapAfterMove);
        }));
    }

    private bool DetermineIsEntering(Portal.PortalDirection portalDirection)
    {
        int currentMap = MapManager.Instance.CurrentMapIndex;
        if (currentMap == 0) return true;

        var lastDir = LastEnteredPortalDirection ?? Portal.PortalDirection.North;
        var oppositeDir = GetOppositeDirection(lastDir);

        return portalDirection != oppositeDir;
    }

    // 들어온 방향 받아서 나갈 방향 찾기
    public Portal.PortalDirection GetOppositeDirection(Portal.PortalDirection dir)
    {
        return dir switch
        {
            Portal.PortalDirection.North => Portal.PortalDirection.South,
            Portal.PortalDirection.South => Portal.PortalDirection.North,
            Portal.PortalDirection.East => Portal.PortalDirection.West,
            Portal.PortalDirection.West => Portal.PortalDirection.East,
            _ => dir,
        };
    }
}
