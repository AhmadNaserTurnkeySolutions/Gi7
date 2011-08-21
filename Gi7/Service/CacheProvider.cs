﻿using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Xml.Serialization;

namespace Gi7.Service
{
    /// <summary>
    /// Provide simple access to store/retrieve typed data from the isolated file storage
    /// These data as considered as cache
    /// </summary>
    public class CacheProvider
    {
        private string _path;

        public CacheProvider(String key)
        {
            _path = String.Format("cache/{0}/", key);

            if (!IsolatedStorageFile.GetUserStoreForApplication().DirectoryExists(_path.TrimEnd('/')))
                IsolatedStorageFile.GetUserStoreForApplication().CreateDirectory(_path.TrimEnd('/'));
        }

        public void Save(String key, object item)
        {
            var file = CleanFilePath(key);
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            using (var stream = store.OpenFile(_path + file, FileMode.Create))
            {
                var serializer = new XmlSerializer(item.GetType());
                serializer.Serialize(stream, item);
            }
        }

        public T Get<T>(String key)
        {
            var file = CleanFilePath(key);
            try
            {
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (store.FileExists(_path + file))
                    {
                        using (var stream = store.OpenFile(_path + file, FileMode.Open))
                        {
                            var serializer = new XmlSerializer(typeof(T));
                            return (T)serializer.Deserialize(stream);
                        }
                    }
                    else
                    {
                        return default(T);
                    }
                }
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        public void Clear()
        {
            foreach (var file in IsolatedStorageFile.GetUserStoreForApplication().GetFileNames(_path + "*"))
            {
                IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(_path + file);
            }
            IsolatedStorageFile.GetUserStoreForApplication().DeleteDirectory(_path.TrimEnd('/'));
        }

        private String CleanFilePath(String key)
        {
            return key.Replace('/', '_').Replace('?', '_').Replace('=', '_').Replace('&', '_');
        }
    }
}
