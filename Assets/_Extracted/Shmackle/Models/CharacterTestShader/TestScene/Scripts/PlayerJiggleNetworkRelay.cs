using Fusion;
using UnityEngine;

[RequireComponent(typeof(PlayerJiggleController))]
public class PlayerJiggleNetworkRelay : NetworkBehaviour
{
    private PlayerJiggleController _jiggleController;

    private void Awake()
    {
        _jiggleController = GetComponent<PlayerJiggleController>();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void Rpc_PlayNodEffect(byte soundVariantIndex)
    {
        _jiggleController?.ExecuteNodEffect(soundVariantIndex);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void Rpc_StopAllSounds()
    {
        _jiggleController?.StopAllSounds();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_ApplyExternalImpact(Vector3 worldSpaceDirection)
    {
        _jiggleController?.ApplyExternalImpact(worldSpaceDirection);
    }
}