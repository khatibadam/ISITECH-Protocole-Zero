using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace ProtocoleZero
{
    /// <summary>
    /// Lives on the XR Interaction Simulator. The simulator is a development aid for
    /// playing in the editor without a headset; on real hardware its simulated devices
    /// coexist with the physical controllers and can hijack input bindings or the
    /// input modality. As soon as a real XR display is running, the simulator is removed.
    /// </summary>
    public sealed class SimulatorKillSwitch : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // Give the XR loader a few seconds to bring the headset up.
            var displays = new List<XRDisplaySubsystem>();
            float deadline = Time.unscaledTime + 5f;
            while (Time.unscaledTime < deadline)
            {
                SubsystemManager.GetSubsystems(displays);
                for (int i = 0; i < displays.Count; i++)
                {
                    if (displays[i].running)
                    {
                        Debug.Log("[SimulatorKillSwitch] Real XR display detected: removing XR Interaction Simulator.");
                        Destroy(gameObject);
                        yield break;
                    }
                }

                yield return new WaitForSecondsRealtime(0.5f);
            }
        }
    }
}
