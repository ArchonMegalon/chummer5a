window.chummerSessionStorage = (() => {
    const DB_NAME = "chummer-session-web";
    const DB_VERSION = 1;
    const STORE_NAMES = [
        "browse-cache",
        "runtime-bundles",
        "session-ledgers",
        "session-replicas"
    ];

    function ensureIndexedDb() {
        if (!("indexedDB" in window)) {
            throw new Error("IndexedDB is not available in this browser.");
        }
    }

    function openDatabase() {
        ensureIndexedDb();
        return new Promise((resolve, reject) => {
            const request = window.indexedDB.open(DB_NAME, DB_VERSION);

            request.onupgradeneeded = () => {
                const database = request.result;
                for (const storeName of STORE_NAMES) {
                    if (!database.objectStoreNames.contains(storeName)) {
                        database.createObjectStore(storeName, { keyPath: "key" });
                    }
                }
            };

            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error ?? new Error("Failed to open IndexedDB."));
        });
    }

    async function withStore(storeName, mode, work) {
        const database = await openDatabase();
        return await new Promise((resolve, reject) => {
            const transaction = database.transaction(storeName, mode);
            const store = transaction.objectStore(storeName);
            const request = work(store);

            let settled = false;

            request.onsuccess = () => {
                settled = true;
                resolve(request.result ?? null);
            };
            request.onerror = () => reject(request.error ?? new Error(`IndexedDB request failed for ${storeName}.`));
            transaction.onabort = () => reject(transaction.error ?? new Error(`IndexedDB transaction aborted for ${storeName}.`));
            transaction.onerror = () => reject(transaction.error ?? new Error(`IndexedDB transaction failed for ${storeName}.`));
            transaction.oncomplete = () => {
                database.close();
                if (!settled && mode === "readwrite") {
                    resolve(null);
                }
            };
        });
    }

    function buildRecord(key, json) {
        return {
            key,
            json,
            storedAtUtc: new Date().toISOString(),
            storageBackend: "indexeddb"
        };
    }

    async function putJson(storeName, key, json) {
        const record = buildRecord(key, json);
        await withStore(storeName, "readwrite", store => store.put(record));
        return JSON.stringify(record);
    }

    async function getJson(storeName, key) {
        const record = await withStore(storeName, "readonly", store => store.get(key));
        return record ? JSON.stringify(record) : null;
    }

    async function deleteJson(storeName, key) {
        await withStore(storeName, "readwrite", store => store.delete(key));
    }

    async function getQuotaEstimate() {
        const storage = navigator.storage;
        const estimate = storage && typeof storage.estimate === "function"
            ? await storage.estimate()
            : null;
        const isPersistent = storage && typeof storage.persisted === "function"
            ? await storage.persisted()
            : false;

        return JSON.stringify({
            usageBytes: estimate && typeof estimate.usage === "number" ? Math.trunc(estimate.usage) : null,
            quotaBytes: estimate && typeof estimate.quota === "number" ? Math.trunc(estimate.quota) : null,
            indexedDbAvailable: "indexedDB" in window,
            opfsAvailable: !!(navigator.storage && navigator.storage.getDirectory),
            persistenceSupported: !!(storage && typeof storage.persisted === "function"),
            isPersistent,
            capturedAtUtc: new Date().toISOString()
        });
    }

    return Object.freeze({
        putJson,
        getJson,
        deleteJson,
        getQuotaEstimate
    });
})();
