using System;
using UnityEngine;

public class ObservableFloat
{
    private float _value;

    public event Action<float> OnValueChanged;

    public float Value
    {
        get => _value;
        set
        {
            if (!Mathf.Approximately(_value, value)) {
                _value = value;
                OnValueChanged?.Invoke(value);
            }

        }
    }
}
