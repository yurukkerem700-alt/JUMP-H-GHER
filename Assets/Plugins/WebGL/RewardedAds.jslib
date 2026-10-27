mergeInto(LibraryManager.library, {
  JAC_RequestRewardedRevive: function (requestId) {
    if (typeof window.JAC_RequestRewardedRevive !== 'function') return 0;
    return window.JAC_RequestRewardedRevive(requestId) ? 1 : 0;
  },
  JAC_ShowRewardedRevive: function () {
    if (typeof window.JAC_ShowRewardedRevive !== 'function') return 0;
    return window.JAC_ShowRewardedRevive() ? 1 : 0;
  }
});
