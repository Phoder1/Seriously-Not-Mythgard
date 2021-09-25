using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TriggerType { Test }
public abstract class Trigger
{
#if UNITY_EDITOR
    private string Name => this.ToString();
#endif 
    public abstract TriggerEvent GetEvent();
    internal static Trigger GetTrigger(TriggerType triggerType)
    {
        switch (triggerType)
        {
            
        }
        throw new NotImplementedException();
    }
}
public abstract class TriggerEvent
{

}
