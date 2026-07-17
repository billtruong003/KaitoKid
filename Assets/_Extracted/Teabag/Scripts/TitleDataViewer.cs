using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Teabag.Authentication;
using Teabag.Core;
using System.Linq;
using GorillaRoyale.Services;
using Squido.JungleXRKit.Core;
using Teabag.Services;

[RequireComponent(typeof(DataViewer))]
public class TitleDataViewer : MonoBehaviour
{
    DataViewer viewer;
    private IAuthenticationService _authManager;

    private void OnEnable()
    {
        viewer = GetComponent<DataViewer>();
        viewer.Show(null);

        _authManager = ServiceLocator.Get<IAuthenticationService>();
        _authManager.OnTitleData += viewer.Show;

        if (AuthenticationUtils.titleData != new Dictionary<string, string>())
            viewer.Show(AuthenticationUtils.titleData);
    }

    private void OnDisable()
    {
        if (_authManager != null)
            _authManager.OnTitleData -= viewer.Show;
    }
}
