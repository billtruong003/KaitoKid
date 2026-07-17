using Teabag.Authentication;
using Oculus.Platform;
using System.Collections;
using System.Collections.Generic;
using Squido.JungleXRKit.Core;
using Teabag.Services;
using TMPro;
using UnityEngine;
using Teabag.UI;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Services;

public class ATMOption : GorillaButton
{
    public string sku;
    public int magnitude;
    public TextMeshProUGUI price;
    float originalScale;
    RectTransform rectTransform;
    ATM atm;
    private IAuthenticationService _authenticationService;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale.x;
        atm = GetComponentInParent<ATM>();
        _authenticationService = ServiceLocator.Get<IAuthenticationService>();
        if (_authenticationService.LoggedIn)
            GetPrice();
        else
        {
            _authenticationService.OnLogin += GetPrice;
        }
    }

    private void OnDestroy()
    {
        if (_authenticationService != null)
            _authenticationService.OnLogin -= GetPrice;
    }

    async void GetPrice()
    {
        if (!Core.IsInitialized())
            return;

        var p = await ServiceLocator.Get<IIAPManager>().GetProductAsync(sku);
        price.text = p.Product.FormattedPrice;
    }

    float t = 0;
    public void Update()
    {
        rectTransform.localScale = Vector3.one * (originalScale + (Mathf.Sin(t) / 100));
        t += Time.deltaTime;
        if (t > Mathf.PI)
            t -= Mathf.PI;
    }

    public override void OnPress()
    {
        atm.Purchase(sku, magnitude);
    }
}
