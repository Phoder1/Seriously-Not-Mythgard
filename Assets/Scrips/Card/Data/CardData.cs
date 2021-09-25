using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;

public static class CardDataHelper
{
#if UNITY_EDITOR
    public const string CardDataFolder = "Card Data/";
#endif
}
public abstract class CardData : ScriptableObject
{
    [SerializeField]
    private string _name;
    public string Name => _name;

    [SerializeField]
    List<Ability> abillities;

}