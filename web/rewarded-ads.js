// Opt-in Google Ad Placement API bridge for a single rewarded revive offer.
(function () {
  let pendingRequest = null;

  function send(method, requestId) {
    if (window.game && typeof window.game.SendMessage === 'function') {
      window.game.SendMessage('RewardedAdBridge', method, String(requestId));
    }
  }

  window.JAC_RequestRewardedRevive = function (requestId) {
    const id = Number(requestId);
    pendingRequest = { id: id, show: null };
    let attempts = 0;

    function startPlacement() {
      if (!pendingRequest || pendingRequest.id !== id) return;
      if (!window.game || !window.__jacAdPlacementReady || typeof window.adBreak !== 'function') {
        if (attempts++ < 20) window.setTimeout(startPlacement, 250);
        else send('OnRewardUnavailable', id);
        return;
      }

      try {
        window.adBreak({
          type: 'reward',
          name: 'continue_after_death',
          beforeReward: function (showAdFn) {
            if (!pendingRequest || pendingRequest.id !== id) return;
            pendingRequest.show = showAdFn;
            send('OnRewardAvailable', id);
          },
          adViewed: function () {
            send('OnRewardGranted', id);
          },
          adDismissed: function () {
            if (pendingRequest && pendingRequest.id === id) pendingRequest.show = null;
            send('OnRewardDismissed', id);
          },
          afterAd: function () {
            send('OnRewardAdClosed', id);
          },
          adBreakDone: function () {
            if (pendingRequest && pendingRequest.id === id) pendingRequest.show = null;
            send('OnRewardBreakDone', id);
          }
        });
      } catch (error) {
        send('OnRewardUnavailable', id);
      }
    }

    startPlacement();
    return true;
  };

  window.JAC_ShowRewardedRevive = function () {
    if (!pendingRequest || typeof pendingRequest.show !== 'function') return false;
    const showAd = pendingRequest.show;
    pendingRequest.show = null;
    try {
      showAd();
      return true;
    } catch (error) {
      return false;
    }
  };
})();
