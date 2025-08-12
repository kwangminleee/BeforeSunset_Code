using UnityEngine;

public class Lamp : MonoBehaviour, IInteractable
{
    private Collider2D _collider;

    [SerializeField] private GameObject TurnOn;
    [SerializeField] private GameObject TuronOff;
    [SerializeField] private float _interactionRange = 5f;

    private bool _isTurnOn;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
    }

    /// <summary>
    /// Set 하면, 자동으로 불 꺼짐/켜짐 변환
    /// </summary>
    public bool IsTurnOn 
    { 
        get { return _isTurnOn; }
        set
        {
            _isTurnOn = value;
            TurnOn.SetActive(_isTurnOn);
            TuronOff.SetActive(!_isTurnOn);
        }
    }

    /// <summary>
    /// 가로등 불의 사이즈는 1
    /// </summary>
    public int GetObejctSize()
    {
        return 1;
    }

    /// <summary>
    /// 상호작용(클릭) 시에 꺼짐 ↔ 켜짐
    /// </summary>
    public void Interact()
    {
        IsTurnOn = !IsTurnOn;
    }

    /// <summary>
    /// 플레이어와의 거리가 상호작용 거리 안에 있는지 검사
    /// </summary>
    public bool IsInteractable(Vector3 playerPos, float range, BoxCollider2D playerCollider)
    {
        if (_collider == null || playerCollider == null) return false;

        Vector2 playerPos2D = new Vector2(playerPos.x, playerPos.y);
        Vector2 closestPoint = _collider.ClosestPoint(playerPos2D);
        float centerToEdge = Vector2.Distance(playerPos2D, closestPoint);

        float playerRadius = playerCollider.size.magnitude * 0.5f * Mathf.Max(playerCollider.transform.lossyScale.x, playerCollider.transform.lossyScale.y);
        float edgeToEdgeDistance = Mathf.Max(0f, centerToEdge - playerRadius);

        return edgeToEdgeDistance <= _interactionRange;
    }

    public void OffAttackArea()
    {
        throw new System.NotImplementedException();
    }
}
