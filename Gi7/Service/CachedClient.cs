﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Gi7.Utils;
using RestSharp;
using System.Net;
using Gi7.Service.Request.Base;

namespace Gi7.Service
{
    /// <summary>
    /// Specialization of the RestClient that uses the cache provider to answer more rapidly to requests
    /// </summary>
    public class CachedClient : RestClient
    {
        public CacheProvider CacheProvider { get; private set; }

        public event EventHandler ConnectionError;
        public event EventHandler Unauthorized;

        public CachedClient(String baseUri, String username, String password)
            : base (baseUri)
        {
            CacheProvider = new CacheProvider(username);

            if (!String.IsNullOrWhiteSpace(password))
                Authenticator = new HttpBasicAuthenticator(username, password);
        }

        public List<T> GetList<T>(string uri, Action<List<T>> callback = null, bool useCache = true)
            where T : new()
        {
            return Get<List<T>>(uri, callback, useCache);
        }

        public T Get<T>(string uri, Action<T> callback = null, bool useCache = true)
            where T : new()
        {
            GlobalLoading.Instance.IsLoading = true;
            T result;

            if (useCache)
            {
                var cache = CacheProvider.Get<T>(uri);
                result = cache != null ? cache : new T();
            }
            else
            {
                result = new T();
            }

            var restRequest = new RestRequest(uri);
            ExecuteAsync<T>(restRequest, r =>
            {
                if (callback != null)
                    callback(r.Data);
                CacheProvider.Save(uri, r.Data);
                GlobalLoading.Instance.IsLoading = false;
            });

            return result;
        }

        public void ClearCache()
        {
            CacheProvider.Clear();
        }

        public override void ExecuteAsync<T>(RestRequest request, Action<RestResponse<T>> callback)
        {
            base.ExecuteAsync<T>(request, r =>
            {
                if (r.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (Unauthorized != null)
                    {
                        Unauthorized(this, new EventArgs());
                    }
                    GlobalLoading.Instance.IsLoading = false;
                }
                else if (r.ResponseStatus == ResponseStatus.Error)
                {
                    if (ConnectionError != null)
                    {
                        ConnectionError(this, new EventArgs());
                    }
                    GlobalLoading.Instance.IsLoading = false;
                }
                else
                    callback(r);
            });
        }
    }
}
