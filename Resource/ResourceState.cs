using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ResourceState
{
    public int Id;
    public Vector3 Position;
    public int HP;
    public bool IsMined;
}

public interface IResourceStateSavable
{
    ResourceState SaveState();
    void LoadState(ResourceState state);
    //void OnGetFromPool();
    //void OnReturnToPool();
}
