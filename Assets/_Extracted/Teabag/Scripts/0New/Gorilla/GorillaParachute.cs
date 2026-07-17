using Fusion;
using Squido.JungleXRKit.Core;
using UnityEngine;
using Teabag.Core;
using IAudioService = Teabag.Core.IAudioService;

namespace Teabag.Player
{
    public class GorillaParachute : NetworkBehaviour
    {
        [Networked, OnChangedRender(nameof(OnParachuteChanged))]
        public bool isParachuting { get; set; }

        public AdvancedAudioClip deployClip;
        public GameObject parachute;

        Gorilla gorilla;
        private IGorillaService _gorillaService;
        private IAudioService _audioService;

        bool wasParachuting;

        private void Awake()
        {
            gorilla = GetComponentInParent<Gorilla>();
            _gorillaService = ServiceLocator.Get<IGorillaService>();
            _audioService = ServiceLocator.Get<IAudioService>();
        }

        public override void Spawned()
        {
            base.Spawned();
            OnParachuteChanged();
        }

        void OnParachuteChanged()
        {
            parachute.SetActive(isParachuting);
            if (wasParachuting != isParachuting)
            {
                var localHealth = (_gorillaService?.LocalGorilla as Gorilla)?.health;
                if (gorilla.health != null && localHealth != null)
                {
                    if (gorilla.health.isDead && !localHealth.isDead)
                        return;
                }

                _audioService.Play(deployClip, transform.position);
            }
        }
    }
}
