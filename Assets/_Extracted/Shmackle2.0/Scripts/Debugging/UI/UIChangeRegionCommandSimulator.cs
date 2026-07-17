using Fusion.Photon.Realtime;
using Stratton.Debugging;
using Stratton.Debugging.UI;

namespace Shmackle.Debugging.UI
{
    public class UIChangeRegionCommandSimulator : UIDropdownCommandSimulator
    {
        #region ICommandSimulator

        public override void Init(SimulatedCommandInfo simulatedCommandInfo)
        {
            base.Init(simulatedCommandInfo);

            var regionCode = PhotonAppSettings.Global.AppSettings.FixedRegion;

            _dropdown.value = 0;
            for (var i=0; i<_simulatedCommandInfo.Options.Length; i++)
            {
                var regionName = _simulatedCommandInfo.Options[i];
                if (regionName.ToLower() == regionCode)
                {
                    _dropdown.value = i;
                    break;
                }
            }
        }

        #endregion
    }
}