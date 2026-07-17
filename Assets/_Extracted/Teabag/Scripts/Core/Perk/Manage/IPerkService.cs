using Squido.JungleXRKit.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace Teabag.Core
{
    public interface IPerkService : IService
    {
        BasePerkDataObject GetPerkDataObject(string id);
        public List<BasePerkDataObject> GetAllEquipPerks();

        public void SavePerk();
        public void SavePerkEquipped();
        public void LoadPerk();
        public void LoadPerkEquipped();
    }
}
