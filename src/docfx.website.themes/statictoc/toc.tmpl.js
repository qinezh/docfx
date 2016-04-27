// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
exports.global = function (global, model) {
  global.toc = global.toc || {};
  transformItem(model, 1);
  if (model.items && model.items.length > 0) model.leaf = false;
  global.toc[model._path] = model;
  return global;
  function transformItem(item, level) {
    item.level = level;
    if (item.items && item.items.length > 0) {
      var length = item.items.length;
      for (var i = 0; i < length; i++) {
        transformItem(item.items[i], level + 1);
      };
    } else {
      item.items = [];
      item.leaf = true;
    }
  }
}
