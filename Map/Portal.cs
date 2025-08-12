using System.Collections;
using UnityEngine;

public class Portal : MonoBehaviour
{
    public enum PortalDirection { North, East, South, West }

    [SerializeField] private PortalDirection _portalDirection;

    public PortalDirection CurrentPortalDirection => _portalDirection;

    private Coroutine _triggerCoroutine;
    private BasePlayer _player;

    private bool _isPlayerInside = false;

    [SerializeField] private float _stayTimeToTrigger = 0.5f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        MapManager.Instance.Player.Controller.SetEnterPortal(true);

        if (_player == null)
            _player = other.GetComponentInChildren<BasePlayer>();

        if (_triggerCoroutine == null)
        {
            _triggerCoroutine = StartCoroutine(WaitAndTrigger());
            _isPlayerInside = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (_triggerCoroutine != null)
        {
            StopCoroutine(_triggerCoroutine);
            _triggerCoroutine = null;
            _isPlayerInside = false;
        }
    }

    private IEnumerator WaitAndTrigger()
    {
        yield return new WaitForSeconds(_stayTimeToTrigger);

        if (!_isPlayerInside || _player == null)
            yield break;

        PortalManager.Instance.OnPlayerEnteredPortal(this);
        _triggerCoroutine = null;
    }
}
