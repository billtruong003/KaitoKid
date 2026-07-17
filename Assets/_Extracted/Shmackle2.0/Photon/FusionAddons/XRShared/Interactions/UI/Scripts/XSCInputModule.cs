using Fusion.XR.Shared.Locomotion;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static Fusion.NetworkRunner;

namespace Fusion.XR.Shared.Core.Interaction.UI
{
    /// <summary>
    /// Requires an EvenSystem and a TrackedDeviceRaycaster on all target canvas
    ///
    ///  Note: a XSCInputModule inheriting from PointerInputModule would be a more elegant approach. Need to check how to properly make several input module cohabit (or accept to have only this one, or use UpdateModule instead of process)
    ///  This one is a simplification for basic needs
    /// </summary>
    [DefaultExecutionOrder(EXECUTION_ORDER)]
    public class XSCInputModule : BaseInputModule
    {
        const int EXECUTION_ORDER = 500;
        IHardwareRig rig;
        protected List<IInteractionTip> interactionTips = null;
        protected Camera headsetCamera;

        [Tooltip("TrackedDeviceGraphicRaycaster do not return sortingModule, we can fix this to improve UI sorting")]
        public bool fixSortingOrder = true;
        [Header("Debug")]
        public bool createPrimitiveOnInteractionPoints = false;

        public static XSCInputModule Instance { get; set; }

        List<RaycastResult> sortedHits = new List<RaycastResult>();
        List<RaycastResult> rawHits = new List<RaycastResult>();
        List<RaycastResult> reverseHits = new List<RaycastResult>();
        List<GameObject> interactionTipTargets = new List<GameObject>();

        [System.Serializable]
        public class InteractionTipTargetState : IInteractionDetailsProvider
        {
            public GameObject target;
            public IInteractionTip tip;
            public ExtendedPointerEventData lastInterationPointer;
            public InteractionTipTargetStateStatus status = InteractionTipTargetStateStatus.NotInteracting;
            public bool interactionThisFrame = false;
            public int framesWithoutInteraction = 0;
            public RaycastResult lastRaycastResult;
            public List<IInteractionTipListener> listeners;
            public GameObject Target => target;

            public bool IsMaintainedInteraction { get; set; } = false;
            public float MaintainDepth { get; set; } = 0;

            public InteractionTipTargetState(GameObject target, IInteractionTip tip)
            {
                this.target = target;
                this.tip = tip;
                listeners = new List<IInteractionTipListener>(tip.gameObject.GetComponentsInChildren<IInteractionTipListener>());
            }

            public void StartInteractionPhase()
            {
                if (interactionThisFrame == false)
                    framesWithoutInteraction++;
                else
                    framesWithoutInteraction = 0;

                interactionThisFrame = false;
            }

            public void DidInteractThisFrame(ExtendedPointerEventData pointer, RaycastResult raycastResult, bool isMaintained = false, float maintainDepth = 0)
            {
                lastInterationPointer = pointer;
                lastRaycastResult = raycastResult;
                interactionThisFrame = true;
                IsMaintainedInteraction = isMaintained;
                MaintainDepth = maintainDepth;
                foreach (var listener in listeners)
                {
                    listener.OnDidInteract(this);
                }
                tip.LastInteractionDetailProvider = this;
            }

            public void DidNotInteractThisFrame()
            {
                if (tip.LastInteractionDetailProvider == this)
                {
                    tip.LastInteractionDetailProvider = null;
                }
            }

            public Vector3 LastInteractionWorldPosition => lastRaycastResult.worldPosition;

            public void RemoveStatus(InteractionTipTargetStateStatus statusToRemove)
            {
                status = status & ~statusToRemove;
            }

            public void AddStatus(InteractionTipTargetStateStatus statusToAdd)
            {
                status = status | statusToAdd;
            }

            public bool HasStatus(InteractionTipTargetStateStatus statusToCheck)
            {
                return (status & statusToCheck) != 0;
            }
        }

        [System.Serializable]
        public class InteractionTipState
        {
            public IInteractionTip tip;
            public bool processedThisFrame = false;
            public Dictionary<GameObject, InteractionTipTargetState> interactionTipTargetStates;

            public InteractionTipState(IInteractionTip tip)
            {
                this.tip = tip;
                interactionTipTargetStates = new Dictionary<GameObject, InteractionTipTargetState>();
            }

            public InteractionTipTargetState GetInteractionTipTargetState(GameObject target)
            {
                if (interactionTipTargetStates.ContainsKey(target) == false)
                {
                    interactionTipTargetStates[target] = new InteractionTipTargetState(target, tip);
                }
                return interactionTipTargetStates[target];
            }

            public Dictionary<GameObject, InteractionTipTargetState> AllTargetStates => interactionTipTargetStates;

            public void StartInteractionPhase()
            {
                ClearInteractionTipTargetStates();
                
                foreach (var interactionStatesInfo in interactionTipTargetStates)
                {
                    var state = interactionStatesInfo.Value;
                    state.StartInteractionPhase();
                }
                processedThisFrame = true;
            }

            public void EndInteractionPhase()
            {
                processedThisFrame = false;
            }

            // Remove one state info if not used for a long time
            protected void ClearInteractionTipTargetStates()
            {
                foreach(var stateInfo in interactionTipTargetStates)
                {
                    if (stateInfo.Key == null)
                    {
                        // Destroyed gameobject
                        interactionTipTargetStates.Remove(stateInfo.Key);
                        break;
                    }
                    if (stateInfo.Value.framesWithoutInteraction > 1_000)
                    {
                        interactionTipTargetStates.Remove(stateInfo.Key);
                        break;
                    }
                }
            }
        }

        protected Dictionary<IInteractionTip, InteractionTipState> interactionTipStates = new Dictionary<IInteractionTip, InteractionTipState>();
        
        protected Dictionary<BaseRaycaster, Canvas> canvasByRaycaster = new Dictionary<BaseRaycaster, Canvas>();

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
        }

        protected virtual void PrepareProcess()
        {
            DetectRig();
            if (rig != null)
            {
                if (interactionTips == null)
                {
                    interactionTips = new List<IInteractionTip>(rig.gameObject.GetComponentsInChildren<IInteractionTip>(true));
                }
            }

            foreach (var raycaster in RaycasterManager.GetRaycasters())
            {
                if (raycaster.eventCamera != headsetCamera && headsetCamera != null && raycaster is TrackedDeviceRaycaster)
                {
                    var initialCamera = raycaster.eventCamera;
                    var canvas = raycaster.GetComponentInChildren<Canvas>();
                    canvas.worldCamera = headsetCamera;
                    Debug.Log($"Add missing camera conf on {raycaster}. {initialCamera} => {raycaster.eventCamera}");
                }
            }
        }

        protected virtual void DetectRig()
        {
            if (rig == null)
            {
                foreach (var r in HardwareRigsRegistry.GetAvailableHardwareRigs())
                {
                    if(r.Headset != null)
                    {
                        rig = r;
                        headsetCamera = rig.Headset.gameObject.GetComponentInChildren<Camera>();
                    }
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Application.onBeforeRender += OnBeforeRender;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Application.onBeforeRender -= OnBeforeRender;
        }

        [BeforeRenderOrder(EXECUTION_ORDER)]
        protected virtual void OnBeforeRender()
        {
            try
            {
                InteractionDeviceProcess();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"XCSInputModule exception: {e.Message}\n{e}");
            }
        }

        public override void Process()
        {
            // We delay the Process until OnBeforeRender, to have the most up to date positions
        }

        protected virtual void InteractionDeviceProcess()
        {
            PrepareProcess();
            ProcessInteractionTip();
            foreach(var interactionState in interactionTipStates.Values)
            {
                interactionState.EndInteractionPhase();
            }
        }

        public virtual void ProcessInteractionTip()
        {
            if (interactionTips == null) return;
            foreach (var interactionTip in interactionTips)
            {
                ProcessInteractionTip(interactionTip);
            }
        }

        [System.Flags]
        public enum InteractionTipTargetStateStatus {
            NotInteracting = 0,
            PointingDown = 1,
            Dragging = 2,
            Hovering = 4,
            Clicking = 8,
            Proximity = 16,
        }

        protected InteractionTipTargetState GetInteractionTipTargetState(IInteractionTip interactionTip, GameObject target)
        {
            var tipState = GetInteractionTipState(interactionTip);
            return tipState.GetInteractionTipTargetState(target);
        }

        protected InteractionTipState GetInteractionTipState(IInteractionTip interactiontip)
        {
            if (interactionTipStates.ContainsKey(interactiontip) == false)
            {
                interactionTipStates[interactiontip] = new InteractionTipState(interactiontip);
            }
            return interactionTipStates[interactiontip];
        }

        protected GameObject DebugPrimitive(Vector3 pos, PrimitiveType type = PrimitiveType.Cube, float scale = 0.01f, float delayBeforeDestroy = 3)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.transform.position = pos;
            primitive.transform.localScale = scale * Vector3.one;
            if(delayBeforeDestroy > 0) Destroy(primitive, delayBeforeDestroy);
            return primitive;
        }

        public void DebugHits(List<RaycastResult> hits)
        {
            int i = 0;
            foreach (var h in hits)
            {
                var name = h.gameObject.name;
                var parent = h.gameObject.transform.parent;
                while (parent != null)
                {
                    name += "<" + parent.name;
                    parent = parent.parent;
                }
                Debug.LogError($"Hit #{i} [{h.depth}/{h.sortingOrder}/{h.gameObject.GetComponentInParent<Canvas>().sortingOrder}] {name}  | {h}");
                i++;
            }
        }

        public IInteractionDetailsProvider ProcessInteractionTip(IInteractionTip interactionTip, float maxDistanceOverride = float.MaxValue)
        {
            return ProcessInteractionTip(interactionTip, out _, maxDistanceOverride);
        }

        public IInteractionDetailsProvider ProcessInteractionTip(IInteractionTip interactionTip, out List<RaycastResult> hits, float maxDistanceOverride = float.MaxValue)
        {
            hits = null;
            if (headsetCamera == null) return null;

            InteractionTipTargetState mainInteractionThisFrame = null;
            float maintainDepth = 0;
            GameObject maintainHit = null;
            var interactiontipState = GetInteractionTipState(interactionTip);
            var maxDistance = Mathf.Min(interactionTip.MaxStartInteractionDistance, maxDistanceOverride);
            if (interactiontipState.processedThisFrame)
            {
                // We don't process a same interactor twice
                return null;
            }
            interactiontipState.StartInteractionPhase();

            // Default pointer data (if no one is found)
            var pointerEventData = new ExtendedPointerEventData(EventSystem.current);
            pointerEventData.trackedDevicePosition = interactionTip.Origin;
            pointerEventData.trackedDeviceOrientation = interactionTip.Rotation;
            pointerEventData.pointerType = UIPointerType.Tracked;
            pointerEventData.button = PointerEventData.InputButton.Left;


            // Check if we could/should maintain a previous interaction
            if (interactionTip.CanMaintainInteraction())
            {
                // We do not use foreach, as some new interaction could be added
                interactionTipTargets.AddRange(interactiontipState.AllTargetStates.Keys);
                for (int i = 0; i < interactionTipTargets.Count; i++)
                {
                    var interactionTarget = interactionTipTargets[i];
                    if (interactionTarget == null) continue;
                    var interactionState = interactiontipState.AllTargetStates[interactionTarget];
                    if (interactionState.status != InteractionTipTargetStateStatus.NotInteracting)
                    {
                        bool maintainInteraction = false;
                        RaycastResult mainRaycastResult = default;
                        if (interactionTip.CanInteract && interactionTip.MaxMaintainInteractionDepth > 0)
                        {
                            var offset = interactionTip.Origin - interactionTarget.transform.position;
                            maintainDepth = Vector3.Dot(offset, interactionTarget.transform.forward);
                            //Debug.LogError($"=> depth {depth}");
                            if (maintainDepth > 0 && maintainDepth < interactionTip.MaxMaintainInteractionDepth)
                            {
                                // We maintin the interaction for now
                                //Debug.LogError($"Mainting with depth {depth}");
                                maintainInteraction = true;
                                // Behind the object: we find a proper projection point
                                var reversePointerEventData = new ExtendedPointerEventData(EventSystem.current);
                                reversePointerEventData.trackedDevicePosition = interactionTip.Origin;
                                reversePointerEventData.trackedDeviceOrientation = Quaternion.LookRotation(-interactionTarget.transform.forward);
                                reversePointerEventData.pointerType = UIPointerType.Tracked;
                                reversePointerEventData.button = PointerEventData.InputButton.Left;
                                EventSystem.current.RaycastAll(reversePointerEventData, reverseHits);
                                mainRaycastResult = FindFirstRaycast(reverseHits);
                                maintainHit = mainRaycastResult.gameObject;
                                reverseHits.Clear();

                                if (createPrimitiveOnInteractionPoints)
                                {
                                    DebugPrimitive(pos: mainRaycastResult.worldPosition, type: PrimitiveType.Cube, scale: 0.006f);
                                }

                                pointerEventData.pointerPressRaycast = mainRaycastResult;
                                pointerEventData.position = headsetCamera.WorldToScreenPoint(mainRaycastResult.worldPosition);
                                pointerEventData.trackedDevicePosition = mainRaycastResult.worldPosition - interactionTarget.transform.forward * 0.03f;
                                pointerEventData.trackedDeviceOrientation = interactionTip.Rotation;// Quaternion.LookRotation(interactionTarget.transform.forward);
                            }
                        }

                        if (maintainInteraction)
                        {
                            InteractionTipTargetState mainTargetStateUpdateThisFrame = AnalyseSelectedRaycastTargetForInteractions(interactionTip, pointerEventData, mainRaycastResult, interactionTip.IsSelecting, isMaintained: true, maintainDepth: maintainDepth);
                            mainInteractionThisFrame = mainTargetStateUpdateThisFrame;                            
                        }
                    }
                }
                interactionTipTargets.Clear();
            }

            // If no interaction is maintained, we resume normal interaction logic handling
            if (mainInteractionThisFrame == null && interactionTip.CanInteract)
            {
                pointerEventData.trackedDevicePosition = interactionTip.Origin;
                pointerEventData.trackedDeviceOrientation = interactionTip.Rotation;
                pointerEventData.pointerType = UIPointerType.Tracked;
                pointerEventData.button = PointerEventData.InputButton.Left;

                hits = SortedRaycastAll(pointerEventData);
                RaycastResult mainRaycastResult = FindFirstRaycast(hits);

                pointerEventData.pointerPressRaycast = mainRaycastResult;
                pointerEventData.position = headsetCamera.WorldToScreenPoint(mainRaycastResult.worldPosition);

                bool isSelecting = false;
                bool isInteracting = false;
                if (mainRaycastResult.distance < maxDistance)
                {
                    isInteracting = true;
                    
                    isSelecting = interactionTip.IsSelecting;

                    if (createPrimitiveOnInteractionPoints)
                    {
                        DebugPrimitive(pos: mainRaycastResult.worldPosition, type: PrimitiveType.Sphere, scale: 0.003f);
                    }
                } 
                else if(mainRaycastResult.distance < interactionTip.MaxInteractionScanDistance)
                {
                    // Not in close enough distance to trigger an interaction, but enough to trigger a proximity
                    // We detect these to "remember" a potential interaction, to potentially maintain it (in this case, start it) if the tip goes behind the UI
                    var interactionState = GetInteractionTipTargetState(interactionTip, mainRaycastResult.gameObject);
                    interactionState.AddStatus(InteractionTipTargetStateStatus.Proximity);
                    interactionState.DidInteractThisFrame(pointer: pointerEventData, raycastResult: mainRaycastResult);
                }

                if (isInteracting)
                {
                    InteractionTipTargetState mainTargetStateUpdateThisFrame = AnalyseSelectedRaycastTargetForInteractions(interactionTip, pointerEventData, mainRaycastResult, isSelecting);
                    mainInteractionThisFrame = mainTargetStateUpdateThisFrame;
                }
            }

            // Cancel any non maintained target interaction
            foreach (var interactionStatesInfo in interactiontipState.AllTargetStates)
            {
                pointerEventData.trackedDevicePosition = interactionTip.Origin;
                pointerEventData.trackedDeviceOrientation = interactionTip.Rotation;
                var interactionTarget = interactionStatesInfo.Key;
                var interactionState = interactionStatesInfo.Value;
                if (interactionTarget == null) continue;

                if (interactionState.interactionThisFrame == false)
                {
                    interactionState.DidNotInteractThisFrame();
                    if (interactionState.status != InteractionTipTargetStateStatus.NotInteracting)
                    {
                        if ((interactionState.status & InteractionTipTargetStateStatus.PointingDown) != 0)
                        {
                            // Stop pointing down
                            ExecuteEvents.Execute(interactionTarget, pointerEventData, ExecuteEvents.pointerUpHandler);
                            interactionState.status = interactionState.status & ~InteractionTipTargetStateStatus.PointingDown;
                        }
                        if ((interactionState.status & InteractionTipTargetStateStatus.Dragging) != 0)
                        {
                            // Stop dragging
                            ExecuteEvents.Execute(interactionTarget, pointerEventData, ExecuteEvents.endDragHandler);
                            interactionState.status = interactionState.status & ~InteractionTipTargetStateStatus.Dragging;
                        }
                        if ((interactionState.status & InteractionTipTargetStateStatus.Hovering) != 0)
                        {
                            // Stop hovering
                            ExecuteEvents.Execute(interactionTarget, pointerEventData, ExecuteEvents.pointerExitHandler);
                            interactionState.status = interactionState.status & ~InteractionTipTargetStateStatus.Hovering;
                        }
                        if ((interactionState.status & InteractionTipTargetStateStatus.Clicking) != 0)
                        {
                            // Stop clicking
                            ExecuteEvents.Execute(interactionTarget, pointerEventData, ExecuteEvents.pointerClickHandler);
                            interactionState.status = interactionState.status & ~InteractionTipTargetStateStatus.Clicking;
                        }
                    }
                }
            }

            if (hits != null) hits.Clear();

            return mainInteractionThisFrame;
        }

        private InteractionTipTargetState AnalyseSelectedRaycastTargetForInteractions(IInteractionTip interactionTip, ExtendedPointerEventData pointerEventData, RaycastResult mainRaycastResult, bool isSelecting, bool isMaintained = false, float maintainDepth = 0)
        {
            InteractionTipTargetState mainTargetStateUpdateThisFrame = null;
            InteractionTipTargetState interactionTargetStateUpdateThisFrame = null;

            AnalyseSelectedRaycastTargetForScrollInteraction(interactionTip, pointerEventData, mainRaycastResult);

            interactionTargetStateUpdateThisFrame = AnalyseSelectedRaycastTargetForEnterInteraction(interactionTip, pointerEventData, mainRaycastResult, isMaintained, maintainDepth: maintainDepth);
            if (interactionTargetStateUpdateThisFrame != null)
            {
                mainTargetStateUpdateThisFrame = interactionTargetStateUpdateThisFrame;
            }

            interactionTargetStateUpdateThisFrame = AnalyseSelectedRaycastTargetForClickInteraction(interactionTip, pointerEventData, mainRaycastResult, isSelecting, isMaintained, maintainDepth: maintainDepth);
            if (interactionTargetStateUpdateThisFrame != null)
            {
                mainTargetStateUpdateThisFrame = interactionTargetStateUpdateThisFrame;
            }

            interactionTargetStateUpdateThisFrame = AnalyseSelectedRaycastTargetForPressInteraction(interactionTip, pointerEventData, mainRaycastResult, isSelecting, isMaintained, maintainDepth: maintainDepth);
            if (interactionTargetStateUpdateThisFrame != null)
            {
                mainTargetStateUpdateThisFrame = interactionTargetStateUpdateThisFrame;
            }

            interactionTargetStateUpdateThisFrame = AnalyseSelectedRaycastTargetForDragInteraction(interactionTip, pointerEventData, mainRaycastResult, isSelecting, isMaintained, maintainDepth: maintainDepth);
            if (interactionTargetStateUpdateThisFrame != null)
            {
                mainTargetStateUpdateThisFrame = interactionTargetStateUpdateThisFrame;
            }

            return mainTargetStateUpdateThisFrame;
        }

        private InteractionTipTargetState AnalyseSelectedRaycastTargetForDragInteraction(IInteractionTip interactionTip, ExtendedPointerEventData pointerEventData, RaycastResult mainRaycastResult, bool isSelecting, bool isMaintained = false, float maintainDepth = 0)
        {
            InteractionTipTargetState interactionTargetStateUpdateThisFrame = null;
            GameObject dragHandler = null;
            dragHandler = ExecuteEvents.GetEventHandler<IDragHandler>(mainRaycastResult.gameObject);

            if (dragHandler)
            {
                var interactionState = GetInteractionTipTargetState(interactionTip, dragHandler);
                pointerEventData.pointerDrag = dragHandler;
                if (isSelecting == false && interactionState.HasStatus(InteractionTipTargetStateStatus.Dragging))
                {
                    // Stop dragging
                    ExecuteEvents.Execute(dragHandler, pointerEventData, ExecuteEvents.endDragHandler);
                    interactionState.RemoveStatus(InteractionTipTargetStateStatus.Dragging);
                }
                if (isSelecting && interactionState.HasStatus(InteractionTipTargetStateStatus.Dragging) == false)
                {
                    // Start dragging
                    ExecuteEvents.Execute(dragHandler, pointerEventData, ExecuteEvents.initializePotentialDrag);
                    ExecuteEvents.Execute(dragHandler, pointerEventData, ExecuteEvents.beginDragHandler);
                    interactionState.AddStatus(InteractionTipTargetStateStatus.Dragging);
                }
                if (isSelecting && interactionState.HasStatus(InteractionTipTargetStateStatus.Dragging))
                {
                    // Continue dragging
                    ExecuteEvents.Execute(dragHandler, pointerEventData, ExecuteEvents.dragHandler);
                }
                interactionState.DidInteractThisFrame(pointer: pointerEventData, raycastResult: mainRaycastResult, isMaintained, maintainDepth: maintainDepth);
                interactionTargetStateUpdateThisFrame = interactionState;
            }

            return interactionTargetStateUpdateThisFrame;
        }

        private InteractionTipTargetState AnalyseSelectedRaycastTargetForPressInteraction(IInteractionTip interactionTip, ExtendedPointerEventData pointerEventData, RaycastResult mainRaycastResult, bool isSelecting, bool isMaintained = false, float maintainDepth = 0)
        {
            InteractionTipTargetState interactionTargetStateUpdateThisFrame = null;
            GameObject pressHandler = null;
            pressHandler = ExecuteEvents.GetEventHandler<IPointerDownHandler>(mainRaycastResult.gameObject);
            if (pressHandler)
            {
                var pressInteractionTipTargetState = GetInteractionTipTargetState(interactionTip, pressHandler);
                if (isSelecting == false && pressInteractionTipTargetState.HasStatus(InteractionTipTargetStateStatus.PointingDown))
                {
                    // Stop pointing down
                    ExecuteEvents.Execute(pressHandler, pointerEventData, ExecuteEvents.pointerUpHandler);
                    pressInteractionTipTargetState.RemoveStatus(InteractionTipTargetStateStatus.PointingDown);
                }
                if (isSelecting && pressInteractionTipTargetState.HasStatus(InteractionTipTargetStateStatus.PointingDown) == false)
                {
                    // Start pointing down
                    ExecuteEvents.Execute(pressHandler, pointerEventData, ExecuteEvents.pointerDownHandler);
                    pressInteractionTipTargetState.AddStatus(InteractionTipTargetStateStatus.PointingDown);
                }
                if (isSelecting && pressInteractionTipTargetState.HasStatus(InteractionTipTargetStateStatus.PointingDown))
                {
                    // Continue pointing down
                    ExecuteEvents.Execute(pressHandler, pointerEventData, ExecuteEvents.pointerDownHandler);
                }
                pressInteractionTipTargetState.DidInteractThisFrame(pointer: pointerEventData, raycastResult: mainRaycastResult, isMaintained, maintainDepth: maintainDepth);
                interactionTargetStateUpdateThisFrame = pressInteractionTipTargetState;
            }
            return interactionTargetStateUpdateThisFrame;
        }

        private static void AnalyseSelectedRaycastTargetForScrollInteraction(IInteractionTip interactionTip, ExtendedPointerEventData pointerEventData, RaycastResult mainRaycastResult)
        {
            GameObject scrollHandler = null;
            scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(mainRaycastResult.gameObject);

            if (scrollHandler)
            {
                var scrollDelta = interactionTip.ScrollDelta;
                if (scrollDelta.magnitude > 0.01f)
                {
                    pointerEventData.scrollDelta = scrollDelta;
                    ExecuteEvents.Execute(scrollHandler, pointerEventData, ExecuteEvents.scrollHandler);
                }
            }
        }

        private InteractionTipTargetState AnalyseSelectedRaycastTargetForEnterInteraction(IInteractionTip interactionTip, ExtendedPointerEventData pointerEventData, RaycastResult mainRaycastResult, bool isMaintained = false, float maintainDepth = 0)
        {
            InteractionTipTargetState interactionTargetStateUpdateThisFrame = null;
            GameObject enterHandler = null;
            enterHandler = ExecuteEvents.GetEventHandler<IPointerEnterHandler>(mainRaycastResult.gameObject);
            if (enterHandler)
            {
                var enterInteractionTipTargetState = GetInteractionTipTargetState(interactionTip, enterHandler);
                if (enterInteractionTipTargetState.HasStatus(InteractionTipTargetStateStatus.Hovering) == false)
                {
                    // Start hovering
                    ExecuteEvents.Execute(enterHandler, pointerEventData, ExecuteEvents.pointerEnterHandler);
                    enterInteractionTipTargetState.AddStatus(InteractionTipTargetStateStatus.Hovering);
                }
                enterInteractionTipTargetState.DidInteractThisFrame(pointer: pointerEventData, raycastResult: mainRaycastResult, isMaintained, maintainDepth: maintainDepth);
                interactionTargetStateUpdateThisFrame = enterInteractionTipTargetState;
            }
            return interactionTargetStateUpdateThisFrame;
        }

        private InteractionTipTargetState AnalyseSelectedRaycastTargetForClickInteraction(IInteractionTip interactionTip, ExtendedPointerEventData pointerEventData, RaycastResult mainRaycastResult, bool isSelecting, bool isMaintained = false, float maintainDepth = 0)
        {
            InteractionTipTargetState interactionTargetStateUpdateThisFrame = null;
            GameObject clickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(mainRaycastResult.gameObject);
            if (clickHandler)
            {
                var clickInteractionTipTargetState = GetInteractionTipTargetState(interactionTip, clickHandler);
                if (isSelecting && clickInteractionTipTargetState.HasStatus(InteractionTipTargetStateStatus.Clicking) == false)
                {
                    // Start clicking
                    clickInteractionTipTargetState.AddStatus(InteractionTipTargetStateStatus.Clicking);
                }
                if (isSelecting == false && clickInteractionTipTargetState.HasStatus(InteractionTipTargetStateStatus.Clicking))
                {
                    // Stop clicking
                    ExecuteEvents.Execute(clickHandler, pointerEventData, ExecuteEvents.pointerClickHandler);
                    clickInteractionTipTargetState.RemoveStatus(InteractionTipTargetStateStatus.Clicking);
                }
                clickInteractionTipTargetState.DidInteractThisFrame(pointer: pointerEventData, raycastResult: mainRaycastResult, isMaintained, maintainDepth: maintainDepth);
                interactionTargetStateUpdateThisFrame = clickInteractionTipTargetState;
            }

            return interactionTargetStateUpdateThisFrame;
        }

        protected virtual List<RaycastResult> SortedRaycastAll(ExtendedPointerEventData pointerEventData)
        {
            EventSystem.current.RaycastAll(pointerEventData, rawHits);
            if (fixSortingOrder)
            {
                // TrackedDeviceRaycaster results' sorting order is not set. Fixing it (to handle dropdown properly)
                sortedHits.Clear();
                foreach (var hit in rawHits)
                {
                    var fixedHit = hit;
                    if (hit.gameObject == null) continue;
                    if (hit.module is TrackedDeviceRaycaster raycaster)
                    {
                        if (canvasByRaycaster.ContainsKey(raycaster) == false)
                        {
                            var canvas = raycaster.GetComponent<Canvas>();
                            if (canvas) canvasByRaycaster[raycaster] = raycaster.GetComponent<Canvas>();
                        }
                        if (canvasByRaycaster.ContainsKey(raycaster))
                        {
                            fixedHit.sortingOrder = canvasByRaycaster[raycaster].sortingOrder;
                        }
                    }
                    sortedHits.Add(fixedHit);
                }
                sortedHits.Sort((h1, h2) =>
                {
                    if (h1.sortingOrder > h2.sortingOrder) return -1;
                    if (h1.sortingOrder < h2.sortingOrder) return 1;
                    return 0;// h2.depth.CompareTo(h1.depth); 
                });
                rawHits.Clear();
            }
            else
            {
                sortedHits = rawHits;
            }

            return sortedHits;
        }
    }
}

