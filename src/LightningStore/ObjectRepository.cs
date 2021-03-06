namespace LightningStore
{
    using LightningDB;
    using System;
    using System.Collections.Generic;

    public class ObjectRepository<TKey, T> : IDisposable
    {
        private readonly LightningEnvironment _env;
        private readonly LightningDatabase _db;
        private readonly ObjectRepositorySettings<TKey, T> _settings;

        public ObjectRepository(ObjectRepositorySettings<TKey, T> settings)
        {
            _settings = settings;
            _env = new LightningEnvironment(settings.Path);
            _env.Open();
            using (var tx = _env.BeginTransaction())
            {
                _db = tx.OpenDatabase();
            }
        }

        public ObjectRepositoryTransaction<TKey, T> BeginTransaction(bool readOnly = false)
        {
            var tx = _env.BeginTransaction(readOnly ? TransactionBeginFlags.ReadOnly : TransactionBeginFlags.None);
            return new ObjectRepositoryTransaction<TKey, T>(
                _settings,
                tx,
                _db);
        }

        public T Get(TKey key)
        {
            using (var tx = BeginTransaction(true))
                return tx.Get(key);
        }

        public IEnumerable<T> Get(params TKey[] keys)
        {
            using (var tx = BeginTransaction(true))
            {
                foreach (var key in keys)
                {
                    yield return tx.Get(key);
                }
            }
        }

        public long Count
        {
            get
            {
                using (var tx = BeginTransaction(true))
                {
                    return tx.Count;
                }
            }
        }

        public void Put(TKey key, T data)
        {
            _env.WithAutogrowth(() =>
            {
                using (var tx = BeginTransaction())
                {
                    tx.Put(key, data);
                    tx.Commit();
                }
            });
        }

        public void Put(IEnumerable<KeyValuePair<TKey, T>> data)
        {
            _env.WithAutogrowth(() =>
            {
                using (var tx = BeginTransaction())
                {
                    tx.Put(data);
                    tx.Commit();
                }
            });
        }

        public void Delete(params TKey[] keys) => DeleteImpl(keys);
        public void Delete(IEnumerable<TKey> keys) => DeleteImpl(keys);
        private void DeleteImpl(IEnumerable<TKey> keys)
        {
            _env.WithAutogrowth(() =>
            {
                using (var tx = BeginTransaction())
                {
                    tx.Delete(keys);
                    tx.Commit();
                }
            });
        }

        public IEnumerable<KeyValuePair<TKey, T>> List()
        {
            using (var tx = BeginTransaction(true))
                foreach (var p in tx.List())
                    yield return p;
        }

        public void Dispose()
        {
            _db.Dispose();
            _env.Dispose();
        }
    }
}