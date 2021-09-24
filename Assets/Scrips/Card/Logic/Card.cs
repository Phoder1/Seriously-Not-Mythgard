using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Card
{
    public CardData data;

    protected Card(CardData data)
    {
        this.data = data ?? throw new ArgumentNullException(nameof(data));
    }
}
