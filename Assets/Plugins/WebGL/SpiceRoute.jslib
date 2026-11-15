mergeInto(LibraryManager.library, {
  SR_HideLoader: function () {
    if (typeof window !== 'undefined' && window.spiceRouteHideLoader) window.spiceRouteHideLoader();
  }
});
