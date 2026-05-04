using System;
using UnityEngine;

[Serializable]
public struct AudioEventRef
{
    [SerializeField] private string guid;

    public string Guid => guid;
    public bool IsValid => !string.IsNullOrEmpty(guid);

    public AudioEventRef(string guid)
    {
        this.guid = guid;
    }
}