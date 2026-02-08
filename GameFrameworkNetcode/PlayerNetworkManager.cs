using Unity.Netcode;

namespace UnityGameFrameworkImplementations.Core.Netcode
{
    public class PlayerNetworkBehavior : NetworkManager
    {
        private void Start()
        {
            StartHost();
        }
    }
}