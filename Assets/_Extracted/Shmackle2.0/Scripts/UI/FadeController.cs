using System;
using Fusion.XR.Shared.Locomotion;
using MessagePipe;
using Stratton.Loading;
using Stratton.Loading.Types;
using UnityEngine;

namespace Shmackle.UI
{
    
    /// <summary>
    /// Controls fading of the screen by listening to loading events.
    /// When loading starts, it fades in to block the player's view, and fades out when loading completes.
    /// </summary>
    public class FadeController : MonoBehaviour
    {
        /// <summary>
        /// Component that shows and hides the black overlay fade
        /// </summary>
        [SerializeField]
        private Fader _fader; 
        
        private ISubscriber<LoadingFlowStartedEvent> _loadingFlowStartedEventSubscriber;
        private ISubscriber<LoadingFlowFinishedEvent> _loadingFlowFinishedEventSubscriber;
        private IDisposable _eventsFullTimeBagDisposable;
        
        private void Awake()
        {
            if(!_fader)
                _fader = GetComponent<Fader>();
            
            _loadingFlowStartedEventSubscriber = GlobalMessagePipe.GetSubscriber<LoadingFlowStartedEvent>();
            _loadingFlowFinishedEventSubscriber = GlobalMessagePipe.GetSubscriber<LoadingFlowFinishedEvent>();
            
            var bag = DisposableBag.CreateBuilder();

            _loadingFlowStartedEventSubscriber.Subscribe(e => OnLoadingFlowStartedEvent(e.FlowType)).AddTo(bag);
            _loadingFlowFinishedEventSubscriber.Subscribe(e => OnLoadingFlowFinishedEvent(e.FlowType)).AddTo(bag);

            _eventsFullTimeBagDisposable = bag.Build();
        }

        
        /// <summary>
        /// Called when loading starts to fade in the screen
        /// </summary>
        /// <param name="objFlowType">Flow type parameter (KV: not used for now as we will not show the current loading progress. This is a temporary workaround until we have scene-based loading)</param>
        private void OnLoadingFlowStartedEvent(LoadingFlowType objFlowType)
        {
            if (_fader) 
                _fader.SetFade(1);
        }

        private void OnLoadingFlowFinishedEvent(LoadingFlowType objFlowType)
        {
            if (_fader) 
                _fader.SetFade(0);
        }

        private void OnDestroy()
        {
            _eventsFullTimeBagDisposable.Dispose();
        }
    }
}