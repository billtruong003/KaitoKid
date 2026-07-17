using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using UnityEngine;

public class ATM : MonoBehaviour
{
    [Header("Visuals")]
    public GameObject loading;
    public GameObject error;
    public CurrencyViewer currency;
    public GameObject purchaseOptions;

    [Header("Effects")]
    public MoneyParticles particles;
    /*
    public AudioSource sceneMusic;
    public AudioSource pay;
    public AudioSource riser;
    public AudioSource confetti;
    public AudioSource money;
    public AudioSource final;
    public ParticleSystem moneyParticles;
    public ParticleSystem confettiParticles;
    */
    public bool isBuying;

    public async UniTaskVoid Purchase(string sku, int magnitude)
    {
        if (isBuying)
            return;

        isBuying = true;
        loading.SetActive(true);
        error.SetActive(false);
        currency.gameObject.SetActive(false);
        purchaseOptions.SetActive(false);
        var result = await ServiceLocator.Get<IIAPManager>().PurchaseAsync(sku);
        loading.SetActive(false);
        if (!result.IsError)
        {
            isBuying = false;
            //StartCoroutine(BuyEffect(magnitude));
            particles.Play(magnitude);
            currency.gameObject.SetActive(true);
        }
        else
        {
            isBuying = false;
            error.SetActive(true);
            await UniTask.Delay(2000);
            error.SetActive(false);
            currency.gameObject.SetActive(true);
            purchaseOptions.SetActive(true);
        }
        isBuying = false;
    }
}
