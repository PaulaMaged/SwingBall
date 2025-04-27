using System.Collections.Generic;
using UnityEditor.Animations;  // Required for AnimatorController access at runtime
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
public class AnimationDebugger : MonoBehaviour
{
    public KeyToAnimationMap KeyMap;
    private Dictionary<Key, string> keyToState = new();

    private Animator animator;

    void Start()
    {
        if (!animator) animator = GetComponent<Animator>();
        var controller = animator.runtimeAnimatorController as AnimatorController;
        if (controller == null)
        {
            Debug.LogError("AnimatorController not found!");
            return;
        }

        var allStates = new List<string>();
        foreach (var layer in controller.layers)
        {
            foreach (var state in layer.stateMachine.states)
            {
                allStates.Add(state.state.name);
            }
        }

        for (int i = 0; i < KeyMap.digitKeys.Count && i < allStates.Count; i++)
        {
            keyToState[KeyMap.digitKeys[i]] = allStates[i];
        }

        Debug.Log("Mapped keys to states:");
        foreach (var kvp in keyToState)
            Debug.Log($"{kvp.Key} => {kvp.Value}");
    }

    void Update()
    {
        foreach (var kvp in keyToState)
        {
            if (Keyboard.current[kvp.Key].wasPressedThisFrame)
            {
                animator.Play(kvp.Value);
                Debug.Log($"Playing: {kvp.Value} via {kvp.Key}");
            }
        }
    }
}
