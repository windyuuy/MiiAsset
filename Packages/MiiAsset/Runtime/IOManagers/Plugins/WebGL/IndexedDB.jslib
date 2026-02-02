mergeInto(LibraryManager.library, {
    $INDEXEDDB: {
        db: null,
        cache: {
            keys: [],
            values: []
        },
        initDB: function() {
            if (this.db) return;
            var request = indexedDB.open('UnityIndexedDB', 1);
            request.onerror = function(event) {
                console.error('IndexedDB error:', event.target.error);
            };
            request.onsuccess = function(event) {
                INDEXEDDB.db = event.target.result;
            };
            request.onupgradeneeded = function(event) {
                var db = event.target.result;
                if (!db.objectStoreNames.contains('keyvaluepairs')) {
                    db.createObjectStore('keyvaluepairs');
                }
            };
        }
    },

    // 写入key值(回调形式)
    IndexedDB_WriteKey: function(keyPtr, valuePtr, callbackPtr) {
        INDEXEDDB.initDB();
        var key = Pointer_stringify(keyPtr);
        var value = Pointer_stringify(valuePtr);
        var callback = Module.makeDynCall('vii', callbackPtr);
        
        var transaction = INDEXEDDB.db.transaction(['keyvaluepairs'], 'readwrite');
        var store = transaction.objectStore('keyvaluepairs');
        var request = store.put(value, key);
        
        request.onsuccess = function() {
            var errPtr = 0;
            callback(1, errPtr);
        };
        
        request.onerror = function(event) {
            var error = event.target.error ? event.target.error.message : 'Unknown error';
            var errPtr = allocate(intArrayFromString(error), 'i8', ALLOC_NORMAL);
            callback(0, errPtr);
        };
    },

    // 读取key值(回调形式)
    IndexedDB_ReadKey: function(keyPtr, callbackPtr) {
        INDEXEDDB.initDB();
        var key = Pointer_stringify(keyPtr);
        var callback = Module.makeDynCall('viii', callbackPtr);
        
        var transaction = INDEXEDDB.db.transaction(['keyvaluepairs'], 'readonly');
        var store = transaction.objectStore('keyvaluepairs');
        var request = store.get(key);
        
        request.onsuccess = function(event) {
            var value = event.target.result;
            if (value !== undefined) {
                var valuePtr = allocate(intArrayFromString(value), 'i8', ALLOC_NORMAL);
                var errPtr = 0;
                callback(1, valuePtr, errPtr);
            } else {
                var errPtr = 0;
                callback(0, 0, errPtr);
            }
        };
        
        request.onerror = function(event) {
            var error = event.target.error ? event.target.error.message : 'Unknown error';
            var errPtr = allocate(intArrayFromString(error), 'i8', ALLOC_NORMAL);
            callback(0, 0, errPtr);
        };
    },

    // 写入key值(回调形式)(二进制数据)
    IndexedDB_WriteKeyBinary: function(keyPtr, dataPtr, length, callbackPtr) {
        INDEXEDDB.initDB();
        var key = Pointer_stringify(keyPtr);
        var callback = Module.makeDynCall('vii', callbackPtr);
        
        var data = new Uint8Array(length);
        for (var i = 0; i < length; i++) {
            data[i] = HEAP8[dataPtr + i];
        }
        
        var transaction = INDEXEDDB.db.transaction(['keyvaluepairs'], 'readwrite');
        var store = transaction.objectStore('keyvaluepairs');
        var request = store.put(data.buffer, key);
        
        request.onsuccess = function() {
            var errPtr = 0;
            callback(1, errPtr);
        };
        
        request.onerror = function(event) {
            var error = event.target.error ? event.target.error.message : 'Unknown error';
            var errPtr = allocate(intArrayFromString(error), 'i8', ALLOC_NORMAL);
            callback(0, errPtr);
        };
    },

    // 读取key值(回调形式)(二进制数据)
    IndexedDB_ReadKeyBinary: function(keyPtr, callbackPtr) {
        INDEXEDDB.initDB();
        var key = Pointer_stringify(keyPtr);
        var callback = Module.makeDynCall('viiii', callbackPtr);
        
        var transaction = INDEXEDDB.db.transaction(['keyvaluepairs'], 'readonly');
        var store = transaction.objectStore('keyvaluepairs');
        var request = store.get(key);
        
        request.onsuccess = function(event) {
            var value = event.target.result;
            if (value !== undefined) {
                var buffer = value;
                var length = buffer.byteLength;
                var dataPtr = allocate(length, 'i8', ALLOC_NORMAL);
                var dataView = new Uint8Array(buffer);
                for (var i = 0; i < length; i++) {
                    HEAP8[dataPtr + i] = dataView[i];
                }
                var errPtr = 0;
                callback(1, dataPtr, length, errPtr);
            } else {
                var errPtr = 0;
                callback(0, 0, 0, errPtr);
            }
        };
        
        request.onerror = function(event) {
            var error = event.target.error ? event.target.error.message : 'Unknown error';
            var errPtr = allocate(intArrayFromString(error), 'i8', ALLOC_NORMAL);
            callback(0, 0, 0, errPtr);
        };
    },

    // 是否存在key(回调形式)
    IndexedDB_ExistsKey: function(keyPtr, callbackPtr) {
        INDEXEDDB.initDB();
        var key = Pointer_stringify(keyPtr);
        var callback = Module.makeDynCall('vii', callbackPtr);
        
        var transaction = INDEXEDDB.db.transaction(['keyvaluepairs'], 'readonly');
        var store = transaction.objectStore('keyvaluepairs');
        var request = store.get(key);
        
        request.onsuccess = function(event) {
            var value = event.target.result;
            if (value !== undefined) {
                var errPtr = 0;
                callback(1, errPtr);
            } else {
                var errPtr = 0;
                callback(0, errPtr);
            }
        };
        
        request.onerror = function(event) {
            var error = event.target.error ? event.target.error.message : 'Unknown error';
            var errPtr = allocate(intArrayFromString(error), 'i8', ALLOC_NORMAL);
            callback(0, errPtr);
        };
    },

    // 删除key(回调形式)
    IndexedDB_DeleteKey: function(keyPtr, callbackPtr) {
        INDEXEDDB.initDB();
        var key = Pointer_stringify(keyPtr);
        var callback = Module.makeDynCall('vii', callbackPtr);
        
        var transaction = INDEXEDDB.db.transaction(['keyvaluepairs'], 'readwrite');
        var store = transaction.objectStore('keyvaluepairs');
        var request = store.delete(key);
        
        request.onsuccess = function() {
            var errPtr = 0;
            callback(1, errPtr);
        };
        
        request.onerror = function(event) {
            var error = event.target.error ? event.target.error.message : 'Unknown error';
            var errPtr = allocate(intArrayFromString(error), 'i8', ALLOC_NORMAL);
            callback(0, errPtr);
        };
    },

    // 重命名key(回调形式)
    IndexedDB_RenameKey: function(oldKeyPtr, newKeyPtr, callbackPtr) {
        INDEXEDDB.initDB();
        var oldKey = Pointer_stringify(oldKeyPtr);
        var newKey = Pointer_stringify(newKeyPtr);
        var callback = Module.makeDynCall('vii', callbackPtr);
        
        var transaction = INDEXEDDB.db.transaction(['keyvaluepairs'], 'readwrite');
        var store = transaction.objectStore('keyvaluepairs');
        var getRequest = store.get(oldKey);
        
        getRequest.onsuccess = function(event) {
            var value = event.target.result;
            if (value !== undefined) {
                var deleteRequest = store.delete(oldKey);
                deleteRequest.onsuccess = function() {
                    var putRequest = store.put(value, newKey);
                    putRequest.onsuccess = function() {
                        var errPtr = 0;
                        callback(1, errPtr);
                    };
                    putRequest.onerror = function(event) {
                        var error = event.target.error ? event.target.error.message : 'Unknown error';
                        var errPtr = allocate(intArrayFromString(error), 'i8', ALLOC_NORMAL);
                        callback(0, errPtr);
                    };
                };
                deleteRequest.onerror = function(event) {
                    var error = event.target.error ? event.target.error.message : 'Unknown error';
                    var errPtr = allocate(intArrayFromString(error), 'i8', ALLOC_NORMAL);
                    callback(0, errPtr);
                };
            } else {
                var error = 'Key not found';
                var errPtr = allocate(intArrayFromString(error), 'i8', ALLOC_NORMAL);
                callback(0, errPtr);
            }
        };
        
        getRequest.onerror = function(event) {
            var error = event.target.error ? event.target.error.message : 'Unknown error';
            var errPtr = allocate(intArrayFromString(error), 'i8', ALLOC_NORMAL);
            callback(0, errPtr);
        };
    },

    // 按key前缀重新生成key列表缓存(回调形式)
    IndexedDB_RegenerateKeyListCache: function(prefixPtr, callbackPtr) {
        INDEXEDDB.initDB();
        var prefix = Pointer_stringify(prefixPtr);
        var callback = Module.makeDynCall('vii', callbackPtr);
        
        INDEXEDDB.cache.keys = [];
        INDEXEDDB.cache.values = [];
        
        var transaction = INDEXEDDB.db.transaction(['keyvaluepairs'], 'readonly');
        var store = transaction.objectStore('keyvaluepairs');
        var request = store.openCursor();
        
        request.onsuccess = function(event) {
            var cursor = event.target.result;
            if (cursor) {
                var key = cursor.key;
                if (key.startsWith(prefix)) {
                    INDEXEDDB.cache.keys.push(key);
                    INDEXEDDB.cache.values.push(cursor.value);
                }
                cursor.continue();
            } else {
                var errPtr = 0;
                callback(1, errPtr);
            }
        };
        
        request.onerror = function(event) {
            var error = event.target.error ? event.target.error.message : 'Unknown error';
            var errPtr = allocate(intArrayFromString(error), 'i8', ALLOC_NORMAL);
            callback(0, errPtr);
        };
    },

    // 获取key列表缓存长度(同步形式)
    IndexedDB_GetKeyListCacheLength: function() {
        return INDEXEDDB.cache.keys.length;
    },

    // 按索引从缓存获取列表项key(同步形式)
    IndexedDB_GetKeyFromCacheByIndex: function(index) {
        if (index >= 0 && index < INDEXEDDB.cache.keys.length) {
            var key = INDEXEDDB.cache.keys[index];
            var keyPtr = allocate(intArrayFromString(key), 'i8', ALLOC_NORMAL);
            return keyPtr;
        }
        return 0;
    },

    // 按索引从缓存获取列表项值(同步形式)
    IndexedDB_GetValueFromCacheByIndex: function(index) {
        if (index >= 0 && index < INDEXEDDB.cache.values.length) {
            var value = INDEXEDDB.cache.values[index];
            if (typeof value === 'string') {
                var valuePtr = allocate(intArrayFromString(value), 'i8', ALLOC_NORMAL);
                return valuePtr;
            }
        }
        return 0;
    }
});