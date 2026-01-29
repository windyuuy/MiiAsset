mergeInto(LibraryManager.library, {
    LocalStorage_SetItem: function(keyPtr, valuePtr) {
        var key = UTF8ToString(keyPtr);
        var value = UTF8ToString(valuePtr);
        window.localStorage.setItem(key, value);
    },

    LocalStorage_GetItem: function(keyPtr) {
        var key = UTF8ToString(keyPtr);
        var value = window.localStorage.getItem(key) || "";
        var lengthBytes = lengthBytesUTF8(value) + 1;
        var buffer = _malloc(lengthBytes);
        stringToUTF8(value, buffer, lengthBytes);
        return buffer;
    },

    LocalStorage_RemoveItem: function(keyPtr) {
        var key = UTF8ToString(keyPtr);
        window.localStorage.removeItem(key);
    },

    LocalStorage_Clear: function() {
        window.localStorage.clear();
    },

    LocalStorage_HasKey: function(keyPtr) {
        var key = UTF8ToString(keyPtr);
        return window.localStorage.getItem(key) !== null;
    },

    LocalStorage_RefreshKeysCache: function() {
        // 重新加载所有键到全局缓存数组
        if (!window.__localStorageKeysCache) {
            window.__localStorageKeysCache = [];
        }
        window.__localStorageKeysCache.length = 0; // 清空数组
        for (var i = 0; i < window.localStorage.length; i++) {
            var key = window.localStorage.key(i);
            window.__localStorageKeysCache.push(key);
        }
    },

    LocalStorage_RefreshKeysCacheWithPrefix: function(prefixPtr) {
        var prefix = UTF8ToString(prefixPtr);

        // 重新加载指定前缀的键到全局缓存数组
        if (!window.__localStorageKeysCache) {
            window.__localStorageKeysCache = [];
        }
        window.__localStorageKeysCache.length = 0; // 清空数组

        for (var i = 0; i < window.localStorage.length; i++) {
            var key = window.localStorage.key(i);
            if (key.startsWith(prefix)) {
                window.__localStorageKeysCache.push(key);
            }
        }
    },

    LocalStorage_GetKeysCacheLength: function() {
        if (!window.__localStorageKeysCache) {
            window.__localStorageKeysCache = [];
        }
        return window.__localStorageKeysCache.length;
    },

    LocalStorage_GetKeyByIndex: function(index) {
        if (!window.__localStorageKeysCache) {
            window.__localStorageKeysCache = [];
        }
        var key = window.__localStorageKeysCache[index] || "";
        var lengthBytes = lengthBytesUTF8(key) + 1;
        var buffer = _malloc(lengthBytes);
        stringToUTF8(key, buffer, lengthBytes);
        return buffer;
    },

    LocalStorage_GetValueByIndex: function(index) {
        if (!window.__localStorageKeysCache) {
            window.__localStorageKeysCache = [];
        }
        var key = window.__localStorageKeysCache[index];
        var value = key ? (window.localStorage.getItem(key) || "") : "";
        var lengthBytes = lengthBytesUTF8(value) + 1;
        var buffer = _malloc(lengthBytes);
        stringToUTF8(value, buffer, lengthBytes);
        return buffer;
    },

    LocalStorage_RenameKey: function(oldKeyPtr, newKeyPtr, force) {
        var oldKey = UTF8ToString(oldKeyPtr);
        var newKey = UTF8ToString(newKeyPtr);

        // 检查旧键是否存在
        if (window.localStorage.getItem(oldKey) === null) {
            console.error("Cannot rename key '" + oldKey + "' because it does not exist.");
            return;
        }

        var oldValue = window.localStorage.getItem(oldKey);

        if (window.localStorage.getItem(newKey) !== null) {
            if (force) {
                window.localStorage.setItem(newKey, oldValue);
                window.localStorage.removeItem(oldKey);
            } else {
                console.warn("Key '" + newKey + "' already exists. Use force=true to overwrite.");
            }
        } else {
            window.localStorage.setItem(newKey, oldValue);
            window.localStorage.removeItem(oldKey);
        }
    }
});