

class IndexedDbStream {
    constructor(name, options) {
        this.name = name;
        this.options = options;
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
                const objectStore = db.createObjectStore(storeName, { keyPath: this.options.indexKey });
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
        if (!pages || !Array.isArray(pages)) {
            throw "Invalid buffer";
        }
        const transaction = db.transaction([this.name], "readwrite");
        const objectStore = transaction.objectStore(this.name);
        const promises = [];

        for (var i = 0; i < pages.length; i++) {
            var parsed = this.options.parsePage(pages[i]);
            var record = {};
            record[this.options.indexKey] = parsed.pageIndex;
            record[this.options.contentKey] = parsed.content;

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
    async delete(pages, ll) {
        var db = await this.getDb();
        if (!pages || !Array.isArray(pages))
            throw `Invalid Argument ${pages}`
        for (var i = 0; i < pages.length; i++) {
            const transaction = db.transaction([this.name], "readwrite");
            const objectStore = transaction.objectStore(this.name);
            var idx = this.options.parsePage(pages[i]).pageIndex;
            await this._delete(objectStore, idx);
            //await this._delete(objectStore, pages[i].pageIndex);
        }

    }



    async initialize() {
        await this.getDb();
        const len = await this.getCount();
        return {
            name: this.name,
            length: len
        }


    }


}

class LocalStorageStream {
    constructor(name, options) {
        this.name = name;
        this.options = options;
        this.db = null;
        this.prefix = "$" + this.name;

    }
   
    isValidKey(k) {
        return k && k != null && k.startsWith(this.prefix);
    }
    async getCount() {
        var count = 0;
        localStorage.k
        for (var i = 0; i < localStorage.length; i++) {
            var k = localStorage.key(i);
            if (this.isValidKey(k)) {
                count++;
            }
        }
        return count;
    }
    async initialize() {
        const len = await this.getCount();
        return {
            name: this.name,
            length: len
        }
    }
    getKey(index) {
        return this.prefix + "_" + index.toString();
    }
    async read(index) {
        const res = localStorage.getItem(this.getKey(index));
        if (res) {
            return this.options.encodePage(index, res);
            //return {
            //    pageIndex: index,
            //    content: res || ''
            //};
        }
        else {
            return null;
        }
    }
    async write(pages, n) {
        for (var i = 0; i < pages.length; i++) {
            var parse = this.options.parsePage(pages[i]);
            if (typeof parse.pageIndex === 'undefined' || typeof parse.pageIndex !== 'number') {
                console.log('errr');
            }
            localStorage.setItem(this.getKey(parse.pageIndex), parse.content);
            //localStorage.setItem(this.getKey(pages[i].pageIndex), pages[i].content);
        }
        return 0;
    }
    async delete(pages, ll) {
        if (!pages || !Array.isArray(pages))
            throw `Invalid Argument ${pages}`
        for (var i = 0; i < pages.length; i++) {
            
            //localStorage.removeItem(this.getKey(pages[i].pageIndex))
            localStorage.removeItem(this.getKey(this.options.parsePage(pages[i]).pageIndex));
        }
    }
}

export function createInstance(name, options) {

    options = options || {
        backend: "indexeddb",
        indexKey: "index",
        contentKey: "content",
        callBack: null
    }
    options.parsePage = function (page) {
        return {
            pageIndex: page[this.indexKey],
            content: page[this.contentKey]
        }
    }
    options.encodePage = function (index,content) {
        var result = {};
        result[this.indexKey] = index;
        result[this.contentKey] = content;
        return result;
    }



    switch (options.backend) {
        case 'localstorage':
            return new LocalStorageStream(name, options);
        default:
            return new IndexedDbStream(name, options);
    }

}

