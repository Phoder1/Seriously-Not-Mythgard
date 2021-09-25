using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Ability
{
    private const int TriggerInspectorOrder = -20;
    private const int EffectsInspectorOrder = -10;
    [PropertyOrder(TriggerInspectorOrder)]
    [ListDrawerSettings(Expanded = true, ListElementLabelName = "Name", HideAddButton = true)]
    [SerializeReference]
    public List<Trigger> triggers = new List<Trigger>();


    [PropertyOrder(EffectsInspectorOrder)]
    [ListDrawerSettings(Expanded = true, ListElementLabelName = "Name", HideAddButton = true)]
    [SerializeReference]
    public List<Effect> effects = new List<Effect>();




    #region Editor
#if UNITY_EDITOR
    //Trigger adding
    [HorizontalGroup("AddTrigger")]
    [SerializeField, PropertyOrder(TriggerInspectorOrder-1)]
    private TriggerType _triggerToAdd;

    [HorizontalGroup("AddTrigger")]
    [PropertyOrder(TriggerInspectorOrder - 1)]
    [Button(Name = "+")]
    private void AddTrigger() => Trigger.GetTrigger(_triggerToAdd);


    //Effect adding
    [HorizontalGroup("AddEffect")]
    [SerializeField,PropertyOrder(EffectsInspectorOrder - 1)]
    private EffectType _effectToAdd;

    [HorizontalGroup("AddEffect")]
    [PropertyOrder(EffectsInspectorOrder - 1)]
    [Button(Name = "+")]
    private void AddEffect() => Effect.GetEffect(_effectToAdd);
#endif
    #endregion
}