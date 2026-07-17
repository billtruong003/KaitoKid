using UnityEngine;

namespace Shmackle.Events.UI
{
    public abstract class UIHUDLayerEvent
    {
        public GameObject GameObjectLayer;
    }

    public class UIHUDLayerAddedEvent : UIHUDLayerEvent
    {
        public int LayerIndex;
    }

    public class UIHUDLayerRemovedEvent : UIHUDLayerEvent
    {
    }
}