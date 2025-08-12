using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerItemCollect : MonoBehaviour
{
    private float pickupRadius = 3f;
    private float moveSpeed = 5f;
    private float delayBeforeAttract = 0.5f;
    private Dictionary<Transform, float> attractionSpeedMap = new Dictionary<Transform, float>();

    [Tooltip("감지할 드롭 아이템 레이어 이름들")]
    [SerializeField] private List<string> dropItemLayerNames;

    private LayerMask dropItemLayerMask;
    private float attractTimer = 0f;
    private bool canAttract = false;

    private void Awake()
    {
        dropItemLayerMask = 0;
        foreach (var layerName in dropItemLayerNames)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer == -1)
            {
                Debug.LogWarning($"존재하지 않는 레이어 이름: {layerName}");
                continue;
            }
            dropItemLayerMask |= (1 << layer);
        }
    }

    private void Update()
    {
        if (!canAttract)
        {
            attractTimer += Time.deltaTime;
            if (attractTimer >= delayBeforeAttract)
            {
                canAttract = true;
            }
            else
            {
                return;
            }
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius, dropItemLayerMask);

        foreach (var hit in hits)
        {
            DropItemController dropItem = hit.GetComponent<DropItemController>();
            if (dropItem != null)
            {
                MoveAndCollect(dropItem.transform, dropItem.GetId(), dropItem.gameObject);
            }
        }
    }

    private void MoveAndCollect(Transform itemTransform, int itemId, GameObject itemObject)
    {
        Vector3 targetPosition = transform.position + new Vector3(0, 0.6f, 0);

        // 속도 초기화
        if (!attractionSpeedMap.ContainsKey(itemTransform))
        {
            attractionSpeedMap[itemTransform] = 0f;
        }

        // 점점 빨라지기
        attractionSpeedMap[itemTransform] += moveSpeed * 3f * Time.deltaTime;

        float currentSpeed = attractionSpeedMap[itemTransform];
        itemTransform.position = Vector3.MoveTowards(itemTransform.position, targetPosition, currentSpeed * Time.deltaTime);

        if (Vector3.Distance(itemTransform.position, targetPosition) <= 0.1f)
        {
            InventoryManager.Instance.Inventory.AddItem(itemId, 1);
            PoolManager.Instance.ReturnToPool(itemId, itemObject);

            attractionSpeedMap.Remove(itemTransform);
        }
    }
}