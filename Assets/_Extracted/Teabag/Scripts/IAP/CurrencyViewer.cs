using Teabag.Authentication;
using System.Collections;
using System.Collections.Generic;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Services;
using TMPro;
using UnityEngine;

public class CurrencyViewer : MonoBehaviour
{
    public TextMeshProUGUI currencyText;
    public List<GameObject> others = new List<GameObject>();
    RectTransform rectTransform;
    public RectTransform normalPos;
    public RectTransform focusPos;
    int showingCurrency;
    private IAuthenticationService _authenticationService;

    private void Awake()
    {
        currencyText.text = showingCurrency.ToString();
        rectTransform = GetComponent<RectTransform>();
        _authenticationService = ServiceLocator.Get<IAuthenticationService>();

        if (_authenticationService.LoggedIn)
        {
            showingCurrency = AuthenticationUtils.currency;
            currencyText.text = showingCurrency.ToString();
        }
        else
        {
            _authenticationService.OnLogin += HandleLogin;
        }
    }

    private void OnDestroy()
    {
        if (_authenticationService != null)
            _authenticationService.OnLogin -= HandleLogin;
    }

    private void HandleLogin()
    {
        GameLogger.Debug(this, "Just logged in -- updating currency");
        showingCurrency = AuthenticationUtils.currency;
        currencyText.text = showingCurrency.ToString();
    }

    private void FixedUpdate()
    {
        int currency = AuthenticationUtils.currency;
        if (normalPos)
        {
            rectTransform.localPosition = normalPos.localPosition;
            rectTransform.localScale = normalPos.localScale;
        }

        // turn other objects on
        foreach (GameObject obj in others)
            obj.SetActive(true);

        if (currency > showingCurrency)
        {
            showingCurrency += 3;
            showingCurrency = Mathf.Clamp(showingCurrency, 0, currency);
            currencyText.text = showingCurrency.ToString();

            if (focusPos)
            {
                rectTransform.localPosition = focusPos.localPosition;
                rectTransform.localScale = focusPos.localScale;
            }
            foreach (GameObject obj in others)
                obj.SetActive(false);
        }

        if (currency < showingCurrency)
        {
            showingCurrency -= 3;
            currencyText.text = showingCurrency.ToString();
        }
    }
}
