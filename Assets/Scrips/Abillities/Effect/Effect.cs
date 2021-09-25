using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EffectType { Test }
public abstract class Effect
{
#if UNITY_EDITOR
    private string Name => this.ToString();
#endif 
    public abstract void Activate();
    internal static Effect GetEffect(EffectType effectType)
    {
        switch (effectType)
        {

        }
        throw new NotImplementedException();
    }
}
