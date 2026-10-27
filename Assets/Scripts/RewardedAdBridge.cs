using System;
using System.Runtime.InteropServices;
using UnityEngine;

// Google Ad Placement API bridge. The game only shows a continue option when
// beforeReward confirms an ad is available, and only rewards from adViewed.
public class RewardedAdBridge : MonoBehaviour
{
    public static RewardedAdBridge Instance { get; private set; }
    int requestId;
    bool offerAvailable, rewardReceived, adClosed;
    Action onAvailable, onReward, onUnavailable;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern int JAC_RequestRewardedRevive(int id);
    [DllImport("__Internal")] static extern int JAC_ShowRewardedRevive();
#endif

    void Awake() { Instance = this; gameObject.name = "RewardedAdBridge"; }

    public void RequestOffer(Action available, Action rewarded, Action unavailable)
    {
        CancelOffer();
        onAvailable = available; onReward = rewarded; onUnavailable = unavailable;
        int id = requestId;
#if UNITY_WEBGL && !UNITY_EDITOR
        if (JAC_RequestRewardedRevive(id) == 0) OnNoAd(id);
#else
        OnNoAd(id);
#endif
    }

    public bool ShowOffer()
    {
        if (!offerAvailable) return false;
#if UNITY_WEBGL && !UNITY_EDITOR
        if (JAC_ShowRewardedRevive() != 0) return true;
#endif
        offerAvailable = false;
        onUnavailable?.Invoke();
        return false;
    }

    public void CancelOffer()
    {
        requestId++;
        offerAvailable = false; rewardReceived = false; adClosed = false;
        onAvailable = null; onReward = null; onUnavailable = null;
    }

    public void OnRewardAvailable(string id)
    {
        if (!Matches(id)) return;
        offerAvailable = true;
        onAvailable?.Invoke();
    }

    public void OnRewardGranted(string id)
    {
        if (!Matches(id) || rewardReceived) return;
        rewardReceived = true; offerAvailable = false;
        CompleteRewardIfClosed();
    }

    public void OnRewardAdClosed(string id)
    {
        if (!Matches(id)) return;
        adClosed = true;
        CompleteRewardIfClosed();
    }

    void CompleteRewardIfClosed()
    {
        if (!rewardReceived || !adClosed) return;
        var callback = onReward;
        onAvailable = null; onReward = null; onUnavailable = null;
        callback?.Invoke();
    }

    public void OnRewardDismissed(string id)
    {
        if (!Matches(id)) return;
        offerAvailable = false;
        var callback = onUnavailable;
        onAvailable = null; onReward = null; onUnavailable = null;
        callback?.Invoke();
    }

    public void OnRewardBreakDone(string id)
    {
        if (!Matches(id) || rewardReceived) return;
        offerAvailable = false;
        var callback = onUnavailable;
        onAvailable = null; onReward = null; onUnavailable = null;
        callback?.Invoke();
    }

    public void OnNoAd(int id) { if (id == requestId) OnRewardBreakDone(id.ToString()); }
    public void OnRewardUnavailable(string id) { if (Matches(id)) OnRewardBreakDone(id); }
    bool Matches(string id) => int.TryParse(id, out int parsed) && parsed == requestId;

    void OnDestroy() { CancelOffer(); if (Instance == this) Instance = null; }
}
