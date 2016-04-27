// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
exports.model = function (model) {
    var _fileNameWithoutExt = getFileNameWithoutExtension(model._path);
    model._jsonPath = _fileNameWithoutExt + ".swagger.json";
    model._disableToc = model._disableToc || !model._tocPath || (model._navPath === model._tocPath);
    model.title = model.title || model.name;

    model.docurl = model.docurl || getImproveTheDocHref(model, model.newFileRepository);
    model.sourceurl = model.sourceurl || getViewSourceHref(model);
    if (model.children) {
        var ordered = [];
        for (var i = 0; i < model.children.length; i++) {
            var child = model.children[i];
            child.docurl = child.docurl || getImproveTheDocHref(child, model.newFileRepository);
            if (child.operation) {
                child.operation = child.operation.toUpperCase();
            }
            child.sourceurl = child.sourceurl || getViewSourceHref(child);
            child.conceptual = child.conceptual || ''; // set to empty incase mustache looks up
            child.footer = child.footer || ''; // set to empty incase mustache looks up
            if (model.sections && child.uid) {
                var index = model.sections.indexOf(child.uid);
                if (index > -1) {
                    ordered[index] = child;
                }
            }
            model.children[i] = transformReference(model.children[i]);
        };
        if (model.sections) {
            // Remove empty values from ordered, in case item in sections is not in swagger json
            model.children = ordered.filter(function(o){ return o; });
        }
    }

    return model;

    function transformReference(obj) {
        if (Array.isArray(obj)) {
            for (var i = 0; i < obj.length; i++) {
                obj[i] = transformReference(obj[i]);
            }
        }
        else if (typeof obj === "object") {
            for (var key in obj) {
                if (obj.hasOwnProperty(key)) {
                    if (key === "schema") {
                        // transform schema.properties from obj to key value pair
                        obj[key] = transformProperties(obj[key]);
                    }
                    else {
                        obj[key] = transformReference(obj[key]);
                    }
                }
            }
        }
        return obj;
    }

    function transformProperties(obj) {
        if (obj.properties) {
            if (obj.required && Array.isArray(obj.required)) {
                for (var i = 0; i < obj.required.length; i++) {
                    var field = obj.required[i];
                    if (obj.properties[field]) {
                        // add required field as property
                        obj.properties[field].required = true;
                    }
                }
                delete obj.required;
            }
            var array = [];
            for (var key in obj.properties) {
                if (obj.properties.hasOwnProperty(key)) {
                    var value = obj.properties[key];
                    // set description to null incase mustache looks up
                    value.description = value.description || null;

                    value = transformPropertiesValue(value);
                    array.push({key:key, value:value});
                }
            }
            obj.properties = array;
        }
        return obj;
    }

    function transformPropertiesValue(obj) {
        if (obj.type === "array" && obj.items) {
            obj.items.properties = obj.items.properties || null;
            obj.items = transformProperties(obj.items);
        }
        return obj;
    }

    function getImproveTheDocHref(item, newFileRepository) {
        if (!item) return '';
        if (!item.documentation || !item.documentation.remote) {
            return getNewFileUrl(item.uid, newFileRepository);
        } else {
            return getRemoteUrl(item.documentation.remote, item.documentation.startLine + 1);
        }
    }

    function getViewSourceHref(item) {
        /* jshint validthis: true */
        if (!item || !item.source || !item.source.remote) return '';
        return getRemoteUrl(item.source.remote, item.source.startLine - '0' + 1);
    }

    function getNewFileUrl(uid, newFileRepository) {
        // do not support VSO for now
        if (newFileRepository && newFileRepository.repo) {
            var repo = newFileRepository.repo;
            if (repo.substr(-4) === '.git') {
                repo = repo.substr(0, repo.length - 4);
            }
            var path = getGithubUrlPrefix(repo);
            if (path != '') {
                path += '/new';
                path += '/' + newFileRepository.branch;
                path += '/' + getOverrideFolder(newFileRepository.path);
                path += '/new?filename=' + getHtmlId(uid) + '.md';
                path += '&value=' + encodeURIComponent(getOverrideTemplate(uid));
            }
            return path;
        } else {
            return '';
        }
    }

    function getOverrideFolder(path) {
        if (!path) return "";
        path = path.replace('\\', '/');
        if (path.charAt(path.length - 1) == '/') path = path.substring(0, path.length - 1);
        return path;
    }

    function getHtmlId(input) {
        return input.replace(/\W/g, '_');
    }

    function getOverrideTemplate(uid) {
        if (!uid) return "";
        var content = "";
        content += "---\n";
        content += "uid: " + uid + "\n";
        content += "description: You can override description for the API here using *MARKDOWN* syntax\n";
        content += "---\n";
        content += "\n";
        content += "*Please type below more information about this API:*\n";
        content += "\n";
        return content;
    }

    function getRemoteUrl(remote, startLine) {
        if (remote && remote.repo) {
            var repo = remote.repo;
            if (repo.substr(-4) === '.git') {
                repo = repo.substr(0, repo.length - 4);
            }
            var linenum = startLine ? startLine : 0;
            if (/https:\/\/.*\.visualstudio\.com\/.*/gi.test(repo)) {
                // TODO: line not working for vso
                return repo + '#path=/' + remote.path;
            }
            var path = getGithubUrlPrefix(repo);
            if (path != '') {
                path += '/blob' + '/' + remote.branch + '/' + remote.path;
                if (linenum > 0) path += '/#L' + linenum;
            }
            return path;
        } else {
            return '';
        }
    }

    function getGithubUrlPrefix(repo) {
        var regex = /^(?:https?:\/\/)?(?:\S+\@)?(?:\S+\.)?(github\.com(?:\/|:).*)/gi;
        if (!regex.test(repo)) return '';
        return repo.replace(regex, function(match, p1, offset, string) {
            return 'https://' + p1.replace(':', '/');
        })
    }

    function getFileNameWithoutExtension(path) {
        if (!path || path[path.length - 1] === '/' || path[path.length - 1] === '\\') return '';
        var fileName = path.split('\\').pop().split('/').pop();
        return fileName.slice(0, fileName.lastIndexOf('.'));
    }
}
