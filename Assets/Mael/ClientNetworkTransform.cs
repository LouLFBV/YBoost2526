using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Multiplayer.Samples.Utilities.ClientAuthority
{
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        // Cette ligne magique dit à Unity : 
        // "Laisse le propriétaire (le client) décider de sa position"
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}