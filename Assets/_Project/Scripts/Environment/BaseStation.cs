using UnityEngine;

namespace SolarDrone.Environment
{
    /// <summary>
    /// Designates the home base station, launchpad coordinates, and recharge pad.
    /// </summary>
    public class BaseStation : MonoBehaviour
    {
        [Header("Launch / Land Coordinates")]
        [SerializeField] private Vector3 landingOffset = new Vector3(0f, 0.4f, 0f);
        [SerializeField] private float cruisingAltitude = 5.0f;

        public Vector3 HomePosition => transform.position + landingOffset;
        public float CruisingAltitude => cruisingAltitude;

        public Vector3 GetTakeoffPoint()
        {
            return new Vector3(transform.position.x, cruisingAltitude, transform.position.z);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(HomePosition, new Vector3(2f, 0.2f, 2f));

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(GetTakeoffPoint(), 0.4f);
            Gizmos.DrawLine(HomePosition, GetTakeoffPoint());
        }
    }
}
