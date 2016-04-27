// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
exports.model = function (model){
  model._disableToc = model._disableToc || !model._tocPath || (model._navPath === model._tocPath);
  model.docurl = model.docurl || getImproveTheDocHref(model);
  setToc(model, model.__global.toc[model._tocPath]);
  setNav(model, model.__global.toc[model._navPath]);
  return model;

  function setNav(model, nav){
    var tocPath = model._tocPath || '';
    var dir = nav._dir || '';
    var path = model._path;
    normalize(nav, model._navRelDir, function (item) {
      return (item.tocHref && (dir + decodeURIComponent(item.tocHref) === tocPath)) || (item.href && dir + decodeURIComponent(item.href) === path);
    });
    model._nav = nav;
  }

  function setToc(model, toc){
    var tocPath = model._tocPath || '';
    var dir = toc._dir || '';
    var path = model._path;
    normalize(toc, model._tocRelDir, function (item) {
      return (item.tocHref && (dir + decodeURIComponent(item.tocHref) === tocPath)) || (item.href && (dir + decodeURIComponent(item.href) === path));
    });
    model._toc = toc;
  }

  function normalize(toc, rel, comparer){
    if (!toc) return;
    if (toc.items){
      for (var i = toc.items.length - 1; i >= 0; i--) {
        normalizeCore(toc.items[i], rel, comparer);
      };
    }
    return toc;
  }

  function normalizeCore(item, rel, comparer){
    item.active = false;
    if (comparer && comparer(item)){
      item.active = true;
    }

    if (rel && isRelativeUrl(item.href)){
      item.href = rel + item.href;
    }

    if (item.items){
      for (var i = item.items.length - 1; i >= 0; i--) {
        normalizeCore(item.items[i], rel, comparer);
      };
    }
  }

  function isRelativeUrl(href){
    if (!href) return false;
    return !isAbsolutePath(href);
  }

  function isAbsolutePath(href) {
    return (/^(?:[a-z]+:)?\/\//i).test(href);
  }

  function getImproveTheDocHref(item) {
    if (!item || !item.source || !item.source.remote) return '';
    return getRemoteUrl(item.source.remote, item.source.startLine + 1);
  }

  function getRemoteUrl(remote, startLine) {
    if (remote && remote.repo) {
      var repo = remote.repo;
      if (repo.substr(-4) === '.git') {
        repo = repo.substr(0, repo.length - 4);
      }
      var linenum = startLine ? startLine : 0;
      if (repo.match(/https:\/\/.*\.visualstudio\.com\/.*/g)) {
        // TODO: line not working for vso
        return repo + '#path=/' + remote.path;
      }
      if (repo.match(/https:\/\/.*github\.com\/.*/g)) {
        var path = repo + '/blob' + '/' + remote.branch + '/' + remote.path;
        if (linenum > 0) path += '/#L' + linenum;
        return path;
      }
      if (repo.match(/git@.*github\.com:.*/g)) {
        var path = 'https://' + repo.substr(4).replace(':', '/') + '/blob' + '/' + remote.branch + '/' + remote.path;
        if (linenum > 0) path += '/#L' + linenum;
        return path;
      }
    } else {
      return '';
    }
  }
}
