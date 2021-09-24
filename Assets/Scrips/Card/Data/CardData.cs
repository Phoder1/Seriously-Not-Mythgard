using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CardDataHelper
{
#if UNITY_EDITOR
    public const string CardDataFolder = "Card Data/";
#endif
}
[CreateAssetMenu(menuName = CardDataHelper.CardDataFolder + nameof(CardData))]
public abstract class CardData : ScriptableObject
{
    [SerializeField]
    private string _name;
    public string Name => _name;

}
