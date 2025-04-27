using Unity.Netcode.Components;
using UnityEngine.InputSystem;
public class OwnerNetworkAnimator : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner) this.enabled = false;
    }
    private void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {

            this.SetTrigger("TrSwing");
        }
    }
}
