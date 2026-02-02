// mergeInto(LibraryManager.library, {
// 	indexeddb_init: function(dbName, storeName) {
// 		var dbNameStr = UTF8ToString(dbName);
// 		var storeNameStr = UTF8ToString(storeName);
//		
// 		if (!window.indexedDB) {
// 			console.error("IndexedDB not supported in this browser");
// 			return;
// 		}
//		
// 		window.idb_dbName = dbNameStr;
// 		window.idb_storeName = storeNameStr;
// 		window.idb_keyCache = [];
// 	},
//	
// 	indexeddb_write: function(key, value, callback) {
// 		var keyStr = UTF8ToString(key);
// 		var valueStr = UTF8ToString(value);
// 		var request = indexedDB.open(window.idb_dbName, 1);
//		
// 		request.onupgradeneeded = function(event) {
// 			var db = event.target.result;
// 			if (!db.objectStoreNames.contains(window.idb_storeName)) {
// 				db.createObjectStore(window.idb_storeName);
// 			}
// 		};
//		
// 		request.onsuccess = function(event) {
// 			var db = event.target.result;
// 			var transaction = db.transaction([window.idb_storeName], "readwrite");
// 			var store = transaction.objectStore(window.idb_storeName);
//			
// 			var putRequest = store.put(valueStr, keyStr);
//			
// 			putRequest.onsuccess = function() {
// 				var success = true;
// 				// 转换字符串为UTF8指针
// 				var keyPtr = stringToNewUTF8(keyStr);
// 				makeDynCall('vii', callback)(keyPtr, success);
// 				// 释放内存
// 				_remove(keyPtr);
// 			};
//			
// 			putRequest.onerror = function() {
// 				var success = false;
// 				var keyPtr = stringToNewUTF8(keyStr);
// 				makeDynCall('vii', callback)(keyPtr, success);
// 				_remove(keyPtr);
// 			};
// 		};
//		
// 		request.onerror = function() {
// 			var success = false;
// 			var keyPtr = stringToNewUTF8(keyStr);
// 			makeDynCall('vii', callback)(keyPtr, success);
// 			_remove(keyPtr);
// 		};
// 	},
//	
// 	indexeddb_read: function(key, callback) {
// 		var keyStr = UTF8ToString(key);
// 		var request = indexedDB.open(window.idb_dbName, 1);
//		
// 		request.onsuccess = function(event) {
// 			var db = event.target.result;
// 			var transaction = db.transaction([window.idb_storeName], "readonly");
// 			var store = transaction.objectStore(window.idb_storeName);
//			
// 			var getRequest = store.get(keyStr);
//			
// 			getRequest.onsuccess = function() {
// 				var result = getRequest.result;
// 				var found = result !== undefined;
// 				var keyPtr = stringToNewUTF8(keyStr);
//				
// 				if (found) {
// 					var valuePtr = stringToNewUTF8(result);
// 					makeDynCall('viii', callback)(keyPtr, valuePtr, 1);
// 					_remove(valuePtr);
// 				} else {
// 					var valuePtr = stringToNewUTF8("");
// 					makeDynCall('viii', callback)(keyPtr, valuePtr, 0);
// 					_remove(valuePtr);
// 				}
//				
// 				_remove(keyPtr);
// 			};
//			
// 			getRequest.onerror = function() {
// 				var keyPtr = stringToNewUTF8(keyStr);
// 				var valuePtr = stringToNewUTF8("");
// 				makeDynCall('viii', callback)(keyPtr, valuePtr, 0);
// 				_remove(keyPtr);
// 				_remove(valuePtr);
// 			};
// 		};
//		
// 		request.onerror = function() {
// 			var keyPtr = stringToNewUTF8(keyStr);
// 			var valuePtr = stringToNewUTF8("");
// 			makeDynCall('viii', callback)(keyPtr, valuePtr, 0);
// 			_remove(keyPtr);
// 			_remove(valuePtr);
// 		};
// 	},
//	
// 	indexeddb_exists: function(key, callback) {
// 		var keyStr = UTF8ToString(key);
// 		var request = indexedDB.open(window.idb_dbName, 1);
//		
// 		request.onsuccess = function(event) {
// 			var db = event.target.result;
// 			var transaction = db.transaction([window.idb_storeName], "readonly");
// 			var store = transaction.objectStore(window.idb_storeName);
//			
// 			var getRequest = store.get(keyStr);
//			
// 			getRequest.onsuccess = function() {
// 				var exists = getRequest.result !== undefined;
// 				var keyPtr = stringToNewUTF8(keyStr);
// 				makeDynCall('vii', callback)(keyPtr, exists ? 1 : 0);
// 				_remove(keyPtr);
// 			};
//			
// 			getRequest.onerror = function() {
// 				var keyPtr = stringToNewUTF8(keyStr);
// 				makeDynCall('vii', callback)(keyPtr, 0);
// 				_remove(keyPtr);
// 			};
// 		};
//		
// 		request.onerror = function() {
// 			var keyPtr = stringToNewUTF8(keyStr);
// 			makeDynCall('vii', callback)(keyPtr, 0);
// 			_remove(keyPtr);
// 		};
// 	},
//	
// 	indexeddb_delete: function(key, callback) {
// 		var keyStr = UTF8ToString(key);
// 		var request = indexedDB.open(window.idb_dbName, 1);
//		
// 		request.onsuccess = function(event) {
// 			var db = event.target.result;
// 			var transaction = db.transaction([window.idb_storeName], "readwrite");
// 			var store = transaction.objectStore(window.idb_storeName);
//			
// 			var deleteRequest = store.delete(keyStr);
//			
// 			deleteRequest.onsuccess = function() {
// 				var success = true;
// 				var keyPtr = stringToNewUTF8(keyStr);
// 				makeDynCall('vii', callback)(keyPtr, success ? 1 : 0);
// 				_remove(keyPtr);
// 			};
//			
// 			deleteRequest.onerror = function() {
// 				var success = false;
// 				var keyPtr = stringToNewUTF8(keyStr);
// 				makeDynCall('vii', callback)(keyPtr, success ? 1 : 0);
// 				_remove(keyPtr);
// 			};
// 		};
//		
// 		request.onerror = function() {
// 			var keyPtr = stringToNewUTF8(keyStr);
// 			makeDynCall('vii', callback)(keyPtr, 0);
// 			_remove(keyPtr);
// 		};
// 	},
//	
// 	indexeddb_rename: function(oldKey, newKey, callback) {
// 		var oldKeyStr = UTF8ToString(oldKey);
// 		var newKeyStr = UTF8ToString(newKey);
// 		var request = indexedDB.open(window.idb_dbName, 1);
//		
// 		request.onupgradeneeded = function(event) {
// 			var db = event.target.result;
// 			if (!db.objectStoreNames.contains(window.idb_storeName)) {
// 				db.createObjectStore(window.idb_storeName);
// 			}
// 		};
//		
// 		request.onsuccess = function(event) {
// 			var db = event.target.result;
// 			var transaction = db.transaction([window.idb_storeName], "readwrite");
// 			var store = transaction.objectStore(window.idb_storeName);
//			
// 			var getRequest = store.get(oldKeyStr);
//			
// 			getRequest.onsuccess = function() {
// 				var value = getRequest.result;
// 				if (value === undefined) {
// 					var oldKeyPtr = stringToNewUTF8(oldKeyStr);
// 					var newKeyPtr = stringToNewUTF8(newKeyStr);
// 					makeDynCall('viii', callback)(oldKeyPtr, newKeyPtr, 0);
// 					_remove(oldKeyPtr);
// 					_remove(newKeyPtr);
// 					return;
// 				}
//				
// 				var putRequest = store.put(value, newKeyStr);
//				
// 				putRequest.onsuccess = function() {
// 					var deleteRequest = store.delete(oldKeyStr);
//					
// 					deleteRequest.onsuccess = function() {
// 						var oldKeyPtr = stringToNewUTF8(oldKeyStr);
// 						var newKeyPtr = stringToNewUTF8(newKeyStr);
// 						makeDynCall('viii', callback)(oldKeyPtr, newKeyPtr, 1);
// 						_remove(oldKeyPtr);
// 						_remove(newKeyPtr);
// 					};
//					
// 					deleteRequest.onerror = function() {
// 						var oldKeyPtr = stringToNewUTF8(oldKeyStr);
// 						var newKeyPtr = stringToNewUTF8(newKeyStr);
// 						makeDynCall('viii', callback)(oldKeyPtr, newKeyPtr, 0);
// 						_remove(oldKeyPtr);
// 						_remove(newKeyPtr);
// 					};
// 				};
//				
// 				putRequest.onerror = function() {
// 					var oldKeyPtr = stringToNewUTF8(oldKeyStr);
// 					var newKeyPtr = stringToNewUTF8(newKeyStr);
// 					makeDynCall('viii', callback)(oldKeyPtr, newKeyPtr, 0);
// 					_remove(oldKeyPtr);
// 					_remove(newKeyPtr);
// 				};
// 			};
//			
// 			getRequest.onerror = function() {
// 				var oldKeyPtr = stringToNewUTF8(oldKeyStr);
// 				var newKeyPtr = stringToNewUTF8(newKeyStr);
// 				makeDynCall('viii', callback)(oldKeyPtr, newKeyPtr, 0);
// 				_remove(oldKeyPtr);
// 				_remove(newKeyPtr);
// 			};
// 		};
//		
// 		request.onerror = function() {
// 			var oldKeyPtr = stringToNewUTF8(oldKeyStr);
// 			var newKeyPtr = stringToNewUTF8(newKeyStr);
// 			makeDynCall('viii', callback)(oldKeyPtr, newKeyPtr, 0);
// 			_remove(oldKeyPtr);
// 			_remove(newKeyPtr);
// 		};
// 	},
//	
// 	indexeddb_prefix_cache_keys: function(prefix, callback) {
// 		var prefixStr = UTF8ToString(prefix);
// 		var request = indexedDB.open(window.idb_dbName, 1);
//		
// 		request.onsuccess = function(event) {
// 			var db = event.target.result;
// 			var transaction = db.transaction([window.idb_storeName], "readonly");
// 			var store = transaction.objectStore(window.idb_storeName);
//			
// 			var getAllKeysRequest = store.getAllKeys();
//			
// 			getAllKeysRequest.onsuccess = function() {
// 				var allKeys = getAllKeysRequest.result;
// 				window.idb_keyCache = allKeys.filter(function(key) {
// 					return key.startsWith(prefixStr);
// 				});
//				
// 				makeDynCall('vi', callback)(1);
// 			};
//			
// 			getAllKeysRequest.onerror = function() {
// 				makeDynCall('vi', callback)(0);
// 			};
// 		};
//		
// 		request.onerror = function() {
// 			makeDynCall('vi', callback)(0);
// 		};
// 	},
//	
// 	indexeddb_get_cache_length: function() {
// 		return window.idb_keyCache.length;
// 	},
//	
// 	indexeddb_get_key_at_index: function(index) {
// 		if (index >= 0 && index < window.idb_keyCache.length) {
// 			return stringToNewUTF8(window.idb_keyCache[index]);
// 		} else {
// 			return stringToNewUTF8("");
// 		}
// 	},
//	
// 	indexeddb_get_value_at_index: function(index) {
// 		if (index >= 0 && index < window.idb_keyCache.length) {
// 			var key = window.idb_keyCache[index];
// 			var request = indexedDB.open(window.idb_dbName, 1);
//			
// 			// 这里需要同步返回，所以我们先返回空字符串
// 			// 实际应用中可能需要异步处理
// 			return stringToNewUTF8("");
// 		} else {
// 			return stringToNewUTF8("");
// 		}
// 	}
// });