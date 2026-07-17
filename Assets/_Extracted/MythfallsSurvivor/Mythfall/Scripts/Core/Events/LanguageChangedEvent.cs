using BillGameCore;

namespace Mythfall.Localization
{
    public struct LanguageChangedEvent : IEvent
    {
        public string newLanguage;
    }
}
