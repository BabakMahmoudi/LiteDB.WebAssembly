const keyPath = 'pageIndex';
class IndexedDbStream {
    constructor(name, ref) {
        this.name = name;
        this.ref = ref;
        this.db = null;

    }

    onerror(ev) {
        console.error(ev);
    }
    _getDb(storeName) {
        return new Promise((resolve, reject) => {
            var req = window.indexedDB.open(storeName);
            req.onerror = ev => {
                reject("Error" + ev)
            }
            req.onupgradeneeded = ev => {
                const db = ev.target.result;
                const objectStore = db.createObjectStore(storeName, { keyPath: keyPath });
            }
            req.onsuccess = ev => {
                const db = ev.target.result;
                resolve(db);
            }
        });
    }

    async getDb() {
        if (!this.db) {
            try {
                this.db = await this._getDb(this.name);
                console.info(`Successfully attached to database ${this.name}`);
            }
            catch (err) {
                console.error("An error occured while trying to initialize db. Err:", err);
                throw (err);

            }
        }
        return this.db;

    }
    _getCount(os) {
        return new Promise((resolve, reject) => {
            var req = os.count();
            req.onsuccess = () => resolve(req.result);
            req.onerror = () => reject(false);

        });
    }
    async getCount() {
        const db = await this.getDb();
        var transaction = db.transaction([this.name], 'readonly');
        var os = transaction.objectStore(this.name);
        return await this._getCount(os);


    }
    _write(os, d) {
        return new Promise((resolve, reject) => {
            const req = os.put(d);
            req.onsuccess = (ev) => resolve(ev);
            req.onerror = (ev) => reject(ev);
        })
    }
    async write(pages, n) {
        var db = await this.getDb();
        var fff = pages;
        var hh = n;
        if (!pages || !Array.isArray(pages)) {
            throw "Invalid buffer";
        }
        const transaction = db.transaction([this.name], "readwrite");
        const objectStore = transaction.objectStore(this.name);
        const promises = [];

        for (var i = 0; i < pages.length; i++) {
            var record = {};
            record[keyPath] = pages[i].pageIndex;
            record["content"] = pages[i].content;
            promises.push(this._write(objectStore, record));
        }
        await Promise.all(promises);
        return 0;



    }
    _read(os, offset) {
        return new Promise((resolve, reject) => {
            const req = os.get(offset);
            req.onsuccess = (ev) => {
                return resolve(ev.target.result);
            }
            req.onerror = (ev) => reject(ev);
        })


    }
    async read(index) {
        var db = await this.getDb();
        const transaction = db.transaction([this.name], "readonly");
        const objectStore = transaction.objectStore(this.name);
        const res = await this._read(objectStore, index);
        return res;
    }
    _delete(os, index) {
        return new Promise((resolve, reject) => {
            const req = os.delete(index);
            req.onsuccess = (ev) => {
                var ff = req.result;
                return resolve(ev.target.result);
            }
            req.onerror = (ev) => {
                reject(ev);
            }
        })

    }
    async delete(pages,ll) {
        var db = await this.getDb();
        if (!pages || !Array.isArray(pages))
            throw `Invalid Argument ${pages}`
        var c1 = await this.getCount();
        for (var i = 0; i < pages.length; i++) {
            const transaction = db.transaction([this.name], "readwrite");
            const objectStore = transaction.objectStore(this.name);
            await this._delete(objectStore, pages[i].pageIndex);
        }
        var c2 = await this.getCount();
        var kk = c1 - c2;

    }



    async initialize() {
        await this.getDb();
        const len = await this.getCount();
        return {
            name: this.name,
            length : len
        }

        
    }


}
var instance = new IndexedDbStream();

export function createInstance(name, ref) {

    return new IndexedDbStream(name, ref);
}

