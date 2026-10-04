using Unity.Netcode;

namespace UnityGameFrameworkImplementations.Core.Netcode
{
    public class PlayerNetworkManager : NetworkManager
    {
        private void Start()
        {
            StartHost();
        }
    }
}