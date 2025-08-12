using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapObject : MonoBehaviour, IPoolable
{
    [SerializeField] private int _id;
    public int GetId() => _id;

    public void OnInstantiate()
    {
        //
    }

    public void OnGetFromPool()
    {
        //
    }

    public void OnReturnToPool()
    {
        //
    }
}
