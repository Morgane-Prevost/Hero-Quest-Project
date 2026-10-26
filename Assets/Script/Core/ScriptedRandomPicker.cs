// Assets/Script/Core/ScriptedRandomPicker.cs
using System;
using System.Collections.Generic;

// Returns predefined values instead of random ones.
// Used in unit tests and to replay recorded games with the same draws.
public class ScriptedRandomPicker : IRandomPicker
{
    private readonly Queue<int> values;

    public ScriptedRandomPicker(params int[] values)
    {
        this.values = new Queue<int>(values);
    }

    public int Pick(int count)
    {
        int value = values.Dequeue();
        if (value >= count) throw new InvalidOperationException($"Valeur {value} >= {count}");
        return value;
    }
}