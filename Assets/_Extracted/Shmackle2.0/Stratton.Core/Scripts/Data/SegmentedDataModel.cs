using System;
using Stratton.Core;
using UnityEngine;

namespace Stratton.Data
{
    [Serializable]
    public abstract class SegmentedDataModel : VersionedDataModel
    {
        #region Fields

        [SerializeField] protected string _segment = "default";

        #endregion

        #region Properties

        public string Segment { get => _segment; set => _segment = value; }

        #endregion

        #region Public Methods

        public override int GetHashCode()
        {
            return HashUtils.GetHash(Segment);
        }

        #endregion
    }
}