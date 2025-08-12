using System.Collections.Generic;
using UnityEngine;

public class OreController : MonoBehaviour, IPoolable, IInteractable, IResourceStateSavable
{
    public OreDatabase _data { get; private set; }

    private BasePlayer _player;

    private Collider2D _collider;

    private int _currentHP;

    private SpriteRenderer _spriteRenderer;
    private Animator _animator;

    [SerializeField] private int _id;
    public int GetId() => _id;


    private void Awake()
    {
        _collider = Helper_Component.GetComponent<Collider2D>(gameObject);
        _animator = Helper_Component.GetComponentInChildren<Animator>(gameObject);
        _spriteRenderer = Helper_Component.GetComponentInChildren<SpriteRenderer>(gameObject);
    }

    public void Init(BasePlayer basePlayer)
    {
        _player = basePlayer;
    }

    public void OnInstantiate()
    {
        _data = DataManager.Instance.OreData.GetById(_id);
        FindPlayer();
        Init(_player);
    }

    public void OnGetFromPool()
    {
        _spriteRenderer.size = new Vector3(1, 1, 1);
        _animator.Play("Ore_Idle", 0, 0f);

        _currentHP = _data.hp;
        FindPlayer();
        Init(_player);
    }

    public void OnReturnToPool()
    {
        
    }

    private void FindPlayer()
    {
        if (_player == null)
        {
            // 방법 1: BasePlayer로 찾기
            _player = FindObjectOfType<BasePlayer>();

            // 방법 2: 태그로 찾기 (BasePlayer가 안되면)
            if (_player == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    _player = playerObj.GetComponent<BasePlayer>();
                }
            }
        }
    }

    public ResourceState SaveState()
    {
        return new ResourceState
        {
            Id = _id,
            Position = transform.position,
            HP = _currentHP
        };
    }

    public void LoadState(ResourceState state)
    {
        _currentHP = state.HP;
        transform.position = state.Position;
    }
    /// <summary>
    /// 추후 기획이 방어력 추가로 변경된다면 사용
    /// </summary>
    /// <param name="pickaxePower"></param>
    /// <returns></returns>
    public bool CanBeMined(int pickaxePower)
    {
        if (_data == null) return false;
        return pickaxePower >= _data.def;
    }

    public bool Mine(int damage)
    {
        if (_currentHP <= 0)
            return false;

        _currentHP -= damage;

        if (_currentHP <= 0)
        {
            DropItem();
            PoolManager.Instance.ReturnToPool(_id, gameObject);
            return true;
        }

        return false;
    }

    private void DropItem()
    {
        int dropId = _data.dropItemId;
        int dropAmount = 1;

        if (_player == null)
        {
            Debug.LogError("[OreController] 플레이어를 찾을 수 없습니다.");
        }

        // 확률 계산
        float dropRate = _player.Stat.DropRate;
        float bonusRate = dropRate / 100f - 1f;

        while (bonusRate > 0f)
        {
            float rand = Random.Range(0f, 1f);
            if (rand < bonusRate) dropAmount += 1;
            bonusRate -= 1f;
        }

        ItemDropManager.Instance.DropItem(dropId, dropAmount, transform, false);
    }

    public void Interact()
    {
        int wallLayerMask = LayerMask.GetMask("Wall");
        Vector2 playerPos = _player.transform.position;

        if (Physics2D.Linecast(playerPos, transform.position, wallLayerMask))
        {
            ToastManager.Instance.ShowToast("해당 위치에서 채굴할 수 없습니다.");
            return;
        }

        /*
        if (_player.Stat.Pickaxe.crushingForce < _data.def)
        {
            ToastManager.Instance.ShowToast("곡괭이 힘이 부족합니다.");
            return;
        }*/
        if(gameObject.activeInHierarchy)
            EffectManager.Instance.MiningEffect(transform);
        _animator.ResetTrigger("IsMining");
        _animator.SetTrigger("IsMining");
        Mine(_player.Stat.Pickaxe.damage);
    }

    public bool IsInteractable(Vector3 playerPos, float range, BoxCollider2D playerCollider)
    {
        if (_collider == null) return false;

        Vector2 playerPos2D = new Vector2(playerPos.x, playerPos.y + 0.5f);
        Vector2 closestPoint = _collider.ClosestPoint(playerPos2D);
        float centerToEdge = Vector2.Distance(playerPos2D, closestPoint);

        float playerRadius = playerCollider.size.magnitude * 0.5f * Mathf.Max(playerCollider.transform.lossyScale.x, playerCollider.transform.lossyScale.y);
        float edgeToEdgeDistance = Mathf.Max(0f, centerToEdge - playerRadius);

        return edgeToEdgeDistance <= 1f;
    }

    public int GetObejctSize()
    {
        return 1;

    }

    public void OffAttackArea()
    {
        throw new System.NotImplementedException();
    }
}
