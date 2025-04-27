using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "KeyToAnimationMap", menuName = "Debug/KeyToAnimationMap")]
public class KeyToAnimationMap : ScriptableObject
{
    public List<Key> digitKeys = new() {
        Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3,
        Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7,
        Key.Digit8, Key.Digit9
    };
}
