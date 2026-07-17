using PlayFab.ClientModels;
using Cysharp.Threading.Tasks;

namespace Teabag.Economy
{
    public interface IConsumableContainer
    {
        void Render();
        UniTask BuyConsumable(string name);
        CatalogItem GetCatalogItem(string name);
    }
}
